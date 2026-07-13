using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Quests;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Exceptions;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Application.Scheduling;

/// <summary>
/// Executa as tarefas agendadas vencidas. É chamado periodicamente pelo
/// BackgroundService da API (e, no futuro, por um endpoint acordado por cron externo,
/// já que o plano F1 do App Service não tem Always On).
/// </summary>
public class ScheduledTaskExecutor
{
    private readonly IAppDbContext _db;
    private readonly IWolvesvilleClient _api;
    private readonly IApiKeyProtector _protector;
    private readonly ILogger<ScheduledTaskExecutor> _logger;

    public ScheduledTaskExecutor(
        IAppDbContext db,
        IWolvesvilleClient api,
        IApiKeyProtector protector,
        ILogger<ScheduledTaskExecutor> logger)
    {
        _db = db;
        _api = api;
        _protector = protector;
        _logger = logger;
    }

    /// <summary>Executa todas as tarefas habilitadas com NextRunAtUtc vencido. Retorna quantas rodaram.</summary>
    public async Task<int> ExecuteDueTasksAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dueTasks = await _db.ScheduledTasks
            .Include(t => t.ClanRegistration)
            .Where(t => t.Enabled && t.NextRunAtUtc != null && t.NextRunAtUtc <= now)
            .OrderBy(t => t.NextRunAtUtc)
            .ToListAsync(ct);

        foreach (var task in dueTasks)
        {
            ct.ThrowIfCancellationRequested();

            TaskExecutionOutcome outcome;
            string message;
            try
            {
                var apiKey = _protector.Unprotect(task.ClanRegistration.ProtectedApiKey, task.ClanRegistrationId);
                (outcome, message) = await ExecuteAsync(task, apiKey, ct);
            }
            catch (Exception ex) when (ex is WolvesvilleApiException or HttpRequestException or ApiKeyUnprotectException)
            {
                outcome = TaskExecutionOutcome.Failed;
                message = ex.Message;
            }

            task.LastRunAtUtc = DateTime.UtcNow;
            // Próxima ocorrência calculada a partir de agora — se o app ficou fora do ar e
            // acumulou várias ocorrências perdidas, executa uma única vez e segue o calendário.
            task.NextRunAtUtc = CronScheduleCalculator.GetNextOccurrenceUtc(
                task.CronExpression, task.TimeZoneId, DateTime.UtcNow);

            _db.TaskExecutionLogs.Add(new TaskExecutionLog
            {
                ScheduledTaskId = task.Id,
                RanAtUtc = task.LastRunAtUtc.Value,
                Outcome = outcome,
                Message = Truncate(message, 1000),
            });

            _logger.LogInformation(
                "Tarefa agendada #{TaskId} ({Type}) do clã {Clan}: {Outcome} — {Message}",
                task.Id, task.Type, task.ClanRegistration.ClanName, outcome, message);
        }

        if (dueTasks.Count > 0)
            await _db.SaveChangesAsync(ct);

        await SnapshotMemberXpAsync(ct);

        return dueTasks.Count;
    }

    /// <summary>
    /// Tira uma foto diária do XP de cada membro (base dos relatórios semanal/mensal).
    /// Mora aqui porque este método é o único ponto executado com regularidade garantida
    /// (BackgroundService + cron externo).
    /// </summary>
    private async Task SnapshotMemberXpAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-20);
        var clans = await _db.ClanRegistrations.ToListAsync(ct);

        foreach (var reg in clans)
        {
            ct.ThrowIfCancellationRequested();

            var hasRecent = await _db.MemberXpSnapshots
                .AnyAsync(s => s.ClanRegistrationId == reg.Id && s.TakenAtUtc >= cutoff, ct);
            if (hasRecent) continue;

            try
            {
                var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
                var members = await _api.GetMembersAsync(apiKey, reg.ClanId, ct);
                var now = DateTime.UtcNow;
                foreach (var m in members)
                {
                    _db.MemberXpSnapshots.Add(new MemberXpSnapshot
                    {
                        ClanRegistrationId = reg.Id,
                        PlayerId = m.PlayerId,
                        Username = m.Username,
                        Xp = m.Xp,
                        TakenAtUtc = now,
                    });
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is WolvesvilleApiException or HttpRequestException or ApiKeyUnprotectException)
            {
                _logger.LogWarning("Snapshot de XP do clã {Clan} falhou: {Message}", reg.ClanName, ex.Message);
            }
        }
    }

    private async Task<(TaskExecutionOutcome, string)> ExecuteAsync(
        ScheduledTask task, string apiKey, CancellationToken ct)
    {
        var clanId = task.ClanRegistration.ClanId;
        return task.Type switch
        {
            ScheduledTaskType.ClaimMostVotedQuest => await ClaimMostVotedQuestAsync(task, apiKey, clanId, ct),
            ScheduledTaskType.ClaimSpecificQuest => await ClaimSpecificQuestAsync(task, apiKey, clanId, ct),
            ScheduledTaskType.SkipQuestWaitingTime => await SkipWaitingTimeAsync(apiKey, clanId, ct),
            ScheduledTaskType.ClaimQuestExtraTime => await ClaimExtraTimeAsync(apiKey, clanId, ct),
            _ => (TaskExecutionOutcome.Failed, $"Tipo de tarefa desconhecido: {task.Type}."),
        };
    }

    private async Task<(TaskExecutionOutcome, string)> ClaimMostVotedQuestAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        var active = await _api.GetActiveQuestAsync(apiKey, clanId, ct);
        if (active is not null)
            return (TaskExecutionOutcome.Skipped, "Já existe uma missão ativa — nada a iniciar.");

        var available = await _api.GetAvailableQuestsAsync(apiKey, clanId, ct);
        if (available.Count == 0)
            return (TaskExecutionOutcome.Skipped, "Não há missões disponíveis no momento.");

        Dictionary<string, int> votes;
        try
        {
            votes = QuestVoteCounter.CountVotes(await _api.GetQuestVotesAsync(apiKey, clanId, ct));
        }
        catch (WolvesvilleApiException ex)
        {
            return (TaskExecutionOutcome.Failed, $"Falha ao consultar os votos: {ex.Message}");
        }

        // Empate: vence a que aparece primeiro na lista de disponíveis (ordem da API).
        var winner = available
            .Select(q => (Quest: q, Votes: votes.GetValueOrDefault(q.Id)))
            .OrderByDescending(x => x.Votes)
            .First();

        if (winner.Votes < task.MinVotes)
            return (TaskExecutionOutcome.Skipped,
                $"Votos insuficientes: a mais votada (\"{winner.Quest.DisplayName}\") tem {winner.Votes} " +
                $"voto(s), mínimo configurado é {task.MinVotes}.");

        await _api.ClaimQuestAsync(apiKey, clanId, winner.Quest.Id, ct);
        var currency = winner.Quest.PurchasableWithGems ? "gemas" : "ouro";
        return (TaskExecutionOutcome.Success,
            $"Missão \"{winner.Quest.DisplayName}\" iniciada com {winner.Votes} voto(s) (paga com {currency}).");
    }

    private async Task<(TaskExecutionOutcome, string)> ClaimSpecificQuestAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(task.TargetQuestId) && string.IsNullOrWhiteSpace(task.TargetQuestName))
            return (TaskExecutionOutcome.Failed, "Nenhuma missão específica foi configurada nesta automação.");

        var active = await _api.GetActiveQuestAsync(apiKey, clanId, ct);
        if (active is not null)
            return (TaskExecutionOutcome.Skipped, "Já existe uma missão ativa — nada a iniciar.");

        var available = await _api.GetAvailableQuestsAsync(apiKey, clanId, ct);
        if (available.Count == 0)
            return (TaskExecutionOutcome.Skipped, "Não há missões disponíveis no momento.");

        // Casa por Id (preferível) e, como as ofertas rotacionam e os ids podem variar,
        // cai para o nome legível guardado na automação.
        var target =
            available.FirstOrDefault(q => !string.IsNullOrEmpty(task.TargetQuestId) && q.Id == task.TargetQuestId)
            ?? available.FirstOrDefault(q =>
                !string.IsNullOrWhiteSpace(task.TargetQuestName) &&
                string.Equals(q.DisplayName, task.TargetQuestName, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            var label = string.IsNullOrWhiteSpace(task.TargetQuestName) ? "escolhida" : $"\"{task.TargetQuestName}\"";
            return (TaskExecutionOutcome.Skipped,
                $"A missão {label} não está entre as disponíveis neste horário — as ofertas rotacionam.");
        }

        await _api.ClaimQuestAsync(apiKey, clanId, target.Id, ct);
        var currency = target.PurchasableWithGems ? "gemas" : "ouro";
        return (TaskExecutionOutcome.Success,
            $"Missão \"{target.DisplayName}\" iniciada (paga com {currency}).");
    }

    private async Task<(TaskExecutionOutcome, string)> SkipWaitingTimeAsync(
        string apiKey, string clanId, CancellationToken ct)
    {
        var active = await _api.GetActiveQuestAsync(apiKey, clanId, ct);
        if (active is null)
            return (TaskExecutionOutcome.Skipped, "Não há missão ativa — nada a pular.");
        if (!active.CanSkipWaitingTime)
            return (TaskExecutionOutcome.Skipped,
                "A missão ainda está acumulando XP para o objetivo — não há tempo de espera a pular.");

        await _api.SkipQuestWaitingTimeAsync(apiKey, clanId, ct);
        return (TaskExecutionOutcome.Success,
            $"Tempo de espera da missão \"{active.Quest.DisplayName}\" pulado (ouro debitado).");
    }

    private async Task<(TaskExecutionOutcome, string)> ClaimExtraTimeAsync(
        string apiKey, string clanId, CancellationToken ct)
    {
        var active = await _api.GetActiveQuestAsync(apiKey, clanId, ct);
        if (active is null)
            return (TaskExecutionOutcome.Skipped, "Não há missão ativa — nada a resgatar.");
        if (active.ClaimedTime)
            return (TaskExecutionOutcome.Skipped, "O tempo extra desta missão já foi resgatado.");
        if (active.IsBeforeTierStart)
            return (TaskExecutionOutcome.Skipped, "A missão ainda está na fase de espera.");

        await _api.ClaimQuestExtraTimeAsync(apiKey, clanId, ct);
        return (TaskExecutionOutcome.Success,
            $"Tempo extra resgatado para a missão \"{active.Quest.DisplayName}\" (ouro debitado).");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

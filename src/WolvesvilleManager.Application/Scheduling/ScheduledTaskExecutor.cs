using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Polls;
using WolvesvilleManager.Application.Quests;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Exceptions;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Scheduling;

/// <summary>
/// Executa as tarefas agendadas vencidas. É chamado periodicamente pelo
/// BackgroundService da API (e, no futuro, por um endpoint acordado por cron externo,
/// já que o plano F1 do App Service não tem Always On).
/// </summary>
public class ScheduledTaskExecutor
{
    /// <summary>Quantas retentativas automáticas de 30 em 30 min são feitas no mesmo dia quando o XP do tier ainda não bateu.</summary>
    private const int MaxAutoRetries = 4;

    private static readonly TimeSpan AutoRetryInterval = TimeSpan.FromMinutes(30);

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
            DateTime? nextRunOverrideUtc = null;
            try
            {
                var apiKey = _protector.Unprotect(task.ClanRegistration.ProtectedApiKey, task.ClanRegistrationId);
                (outcome, message, nextRunOverrideUtc) = await ExecuteAsync(task, apiKey, ct);
            }
            catch (Exception ex) when (ex is WolvesvilleApiException or HttpRequestException or ApiKeyUnprotectException)
            {
                outcome = TaskExecutionOutcome.Failed;
                message = ex.Message;
            }

            task.LastRunAtUtc = DateTime.UtcNow;
            // Próxima ocorrência calculada a partir de agora — se o app ficou fora do ar e
            // acumulou várias ocorrências perdidas, executa uma única vez e segue o calendário.
            // Uma retentativa automática (ex.: "pular espera" com XP ainda não batido) pode pedir
            // um horário mais cedo que a próxima ocorrência normal do cron — nesse caso, usa o
            // horário pedido em vez de pular direto para a próxima ocorrência.
            task.NextRunAtUtc = nextRunOverrideUtc
                ?? CronScheduleCalculator.GetNextOccurrenceUtc(task.CronExpression, task.TimeZoneId, DateTime.UtcNow);

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

    private async Task<(TaskExecutionOutcome Outcome, string Message, DateTime? NextRunOverrideUtc)> ExecuteAsync(
        ScheduledTask task, string apiKey, CancellationToken ct)
    {
        var clanId = task.ClanRegistration.ClanId;
        return task.Type switch
        {
            ScheduledTaskType.ClaimMostVotedQuest => NoOverride(await ClaimMostVotedQuestAsync(task, apiKey, clanId, ct)),
            ScheduledTaskType.ClaimMostVotedFormQuest => NoOverride(await ClaimMostVotedFormQuestAsync(task, apiKey, clanId, ct)),
            ScheduledTaskType.ClaimSpecificQuest => NoOverride(await ClaimSpecificQuestAsync(task, apiKey, clanId, ct)),
            ScheduledTaskType.SkipQuestWaitingTime => await SkipWaitingTimeAsync(task, apiKey, clanId, ct),
            ScheduledTaskType.ClaimQuestExtraTime => NoOverride(await ClaimExtraTimeAsync(apiKey, clanId, ct)),
            _ => (TaskExecutionOutcome.Failed, $"Tipo de tarefa desconhecido: {task.Type}.", null),
        };
    }

    private static (TaskExecutionOutcome, string, DateTime?) NoOverride((TaskExecutionOutcome Outcome, string Message) r) =>
        (r.Outcome, r.Message, null);

    private async Task<(TaskExecutionOutcome, string)> ClaimMostVotedQuestAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        var active = await GetActiveQuestSafeAsync(apiKey, clanId, ct);
        if (active is not null)
            return (TaskExecutionOutcome.Skipped, "Já existe uma missão ativa — nada a iniciar.");

        var available = await GetAvailableQuestsSafeAsync(apiKey, clanId, ct);
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

    /// <summary>
    /// Igual à mais votada, mas a urna é o formulário público (votos no nosso banco, um por
    /// nick) em vez dos votos de dentro do jogo. "Embaralhar missões" concorre como mais uma
    /// candidata na mesma urna — se vencer, embaralha em vez de reivindicar.
    ///
    /// Se o clã tem janelas de votação configuradas (<see cref="PollWindow"/>), apura só o
    /// ÚLTIMO CICLO já concluído (votos com <see cref="QuestPollVote.UpdatedAtUtc"/> dentro do
    /// intervalo daquela janela) — não a urna inteira, que pode já ter votos do ciclo seguinte
    /// se a votação reabriu antes desta automação rodar. Sem janelas configuradas (modo manual
    /// por prazo fixo), mantém o comportamento antigo: apura e zera a urna inteira.
    /// </summary>
    private async Task<(TaskExecutionOutcome, string)> ClaimMostVotedFormQuestAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        var active = await GetActiveQuestSafeAsync(apiKey, clanId, ct);
        if (active is not null)
            return (TaskExecutionOutcome.Skipped, "Já existe uma missão ativa — nada a iniciar.");

        var reg = task.ClanRegistration;
        var windows = await _db.PollWindows
            .Where(w => w.ClanRegistrationId == task.ClanRegistrationId)
            .ToListAsync(ct);

        DateTime? cycleStartUtc = null, cycleEndUtc = null;
        if (windows.Count > 0)
        {
            var lastCycle = PollWindowCalculator.GetLastCompletedWindowUtc(
                windows, reg.PollWindowsTimeZoneId ?? "America/Sao_Paulo", DateTime.UtcNow);
            if (lastCycle is null)
                return (TaskExecutionOutcome.Skipped, "Nenhum ciclo de votação foi concluído ainda.");
            if (reg.PollLastClaimedWindowEndUtc is { } lastClaimed && lastCycle.Value.EndUtc <= lastClaimed)
                return (TaskExecutionOutcome.Skipped, "Este ciclo já foi apurado — aguardando o próximo.");
            (cycleStartUtc, cycleEndUtc) = lastCycle.Value;
        }

        // Não corta na cara quando não há missão disponível: se a urna elegeu "embaralhar",
        // é exatamente essa situação (ou o gosto do clã) que ele resolve.
        var available = await GetAvailableQuestsSafeAsync(apiKey, clanId, ct);

        var voteQuery = _db.QuestPollVotes.Where(v => v.ClanRegistrationId == task.ClanRegistrationId);
        if (cycleStartUtc is not null)
            voteQuery = voteQuery.Where(v => v.UpdatedAtUtc >= cycleStartUtc && v.UpdatedAtUtc < cycleEndUtc);

        var votes = await voteQuery
            .GroupBy(v => v.QuestId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        // Candidatas: cada missão disponível + "embaralhar". Empate entre missões: vence a
        // que aparece primeiro na lista de disponíveis; "embaralhar" só desempata por último
        // (item.Quest is null ordena depois via ThenBy) — prefere iniciar a jogar fora.
        var winner = available
            .Select(q => (Quest: (ClanQuest?)q, Votes: votes.GetValueOrDefault(q.Id)))
            .Append((Quest: (ClanQuest?)null, Votes: votes.GetValueOrDefault(QuestPollVote.ShuffleOptionId)))
            .OrderByDescending(x => x.Votes)
            .ThenBy(x => x.Quest is null)
            .First();

        if (winner.Votes < task.MinVotes)
        {
            var context = available.Count == 0
                ? "não há missões disponíveis no momento; a candidata com mais votos no formulário"
                : "no formulário, a candidata com mais votos";
            return (TaskExecutionOutcome.Skipped,
                $"Votos insuficientes: {context} tem {winner.Votes} voto(s), mínimo configurado é {task.MinVotes}.");
        }

        // Grava o resultado antes de zerar — senão a decisão desta rodada se perderia junto com os votos.
        _db.QuestPollResults.Add(new QuestPollResult
        {
            ClanRegistrationId = task.ClanRegistrationId,
            QuestName = winner.Quest?.DisplayName ?? "Embaralhar missões",
            Votes = winner.Votes,
            WasShuffle = winner.Quest is null,
        });
        await _db.SaveChangesAsync(ct);

        if (cycleStartUtc is not null)
        {
            // Só zera os votos DESTE ciclo — votos já lançados no ciclo seguinte (se a janela já
            // reabriu antes desta automação rodar) continuam na urna para a próxima apuração.
            await _db.QuestPollVotes
                .Where(v => v.ClanRegistrationId == task.ClanRegistrationId
                            && v.UpdatedAtUtc >= cycleStartUtc && v.UpdatedAtUtc < cycleEndUtc)
                .ExecuteDeleteAsync(ct);
            reg.PollLastClaimedWindowEndUtc = cycleEndUtc;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // Modo manual (sem janelas configuradas): comportamento antigo — zera a urna inteira.
            await _db.QuestPollVotes
                .Where(v => v.ClanRegistrationId == task.ClanRegistrationId)
                .ExecuteDeleteAsync(ct);
        }

        if (winner.Quest is null)
        {
            await _api.ShuffleQuestsAsync(apiKey, clanId, ct);
            return (TaskExecutionOutcome.Success,
                $"Formulário votou por embaralhar as missões ({winner.Votes} voto(s)); ouro debitado.");
        }

        await _api.ClaimQuestAsync(apiKey, clanId, winner.Quest.Id, ct);
        var currency = winner.Quest.PurchasableWithGems ? "gemas" : "ouro";
        return (TaskExecutionOutcome.Success,
            $"Missão \"{winner.Quest.DisplayName}\" iniciada com {winner.Votes} voto(s) do formulário (paga com {currency}).");
    }

    private async Task<(TaskExecutionOutcome, string)> ClaimSpecificQuestAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(task.TargetQuestId) &&
            string.IsNullOrWhiteSpace(task.TargetQuestName) &&
            string.IsNullOrWhiteSpace(task.TargetQuestPromoImageUrl))
            return (TaskExecutionOutcome.Failed, "Nenhuma missão específica foi configurada nesta automação.");

        var active = await GetActiveQuestSafeAsync(apiKey, clanId, ct);
        if (active is not null)
            return (TaskExecutionOutcome.Skipped, "Já existe uma missão ativa — nada a iniciar.");

        var available = await GetAvailableQuestsSafeAsync(apiKey, clanId, ct);
        if (available.Count == 0)
            return (TaskExecutionOutcome.Skipped, "Não há missões disponíveis no momento.");

        // As ofertas rotacionam: o Id e o nome do arquivo mudam entre cadastro e execução.
        // Casa primeiro pelo Id (quando ainda válido), depois pela identidade estável da imagem
        // promocional (normalizada) e, por último, pelo nome legível — nessa ordem de confiança.
        var targetKey = QuestMatchKey.Normalize(task.TargetQuestPromoImageUrl);
        var target =
            available.FirstOrDefault(q => !string.IsNullOrEmpty(task.TargetQuestId) && q.Id == task.TargetQuestId)
            ?? available.FirstOrDefault(q => targetKey != null && QuestMatchKey.Normalize(q.PromoImageUrl) == targetKey)
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

    private async Task<(TaskExecutionOutcome, string, DateTime?)> SkipWaitingTimeAsync(
        ScheduledTask task, string apiKey, string clanId, CancellationToken ct)
    {
        // No máximo UM pulo por dia (no fuso da tarefa) — sem isso, uma retentativa automática
        // depois do pulo já ter dado certo arriscaria pular a espera do tier SEGUINTE, que o
        // plano manda deixar abrir sozinho.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(task.TimeZoneId);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date, tz);
        var skippedToday = await _db.TaskExecutionLogs.AnyAsync(
            l => l.ScheduledTaskId == task.Id
                 && l.Outcome == TaskExecutionOutcome.Success
                 && l.RanAtUtc >= dayStartUtc, ct);
        if (skippedToday)
            return (TaskExecutionOutcome.Skipped,
                "A espera de hoje já foi pulada por esta automação — as próximas ocorrências do dia são só retentativa.", null);

        var active = await GetActiveQuestSafeAsync(apiKey, clanId, ct);
        if (active is null)
            return (TaskExecutionOutcome.Skipped, "Não há missão ativa — nada a pular.", null);
        if (!active.CanSkipWaitingTime)
        {
            // Conta as retentativas de HOJE (+1 desta, que ainda não foi gravada) para decidir se
            // ainda vale reagendar em 30 min ou se já é hora de desistir até a próxima ocorrência
            // normal do cron (ex.: a mesma automação, semana que vem).
            var retriesSoFar = await _db.TaskExecutionLogs.CountAsync(
                l => l.ScheduledTaskId == task.Id
                     && l.Outcome == TaskExecutionOutcome.WaitingForXp
                     && l.RanAtUtc >= dayStartUtc, ct) + 1;

            if (retriesSoFar <= MaxAutoRetries)
            {
                var retryAtUtc = DateTime.UtcNow.Add(AutoRetryInterval);
                var retryAtLocal = TimeZoneInfo.ConvertTimeFromUtc(retryAtUtc, tz);
                return (TaskExecutionOutcome.WaitingForXp,
                    $"O XP do tier ainda não bateu o objetivo — nada a pular agora. Tenta de novo automaticamente " +
                    $"às {retryAtLocal:HH:mm} (retentativa {retriesSoFar}/{MaxAutoRetries}).",
                    retryAtUtc);
            }

            return (TaskExecutionOutcome.WaitingForXp,
                $"O XP do tier ainda não bateu o objetivo — nada a pular agora. As {MaxAutoRetries} retentativas " +
                "automáticas de hoje já se esgotaram; a próxima tentativa é no horário normal desta automação.", null);
        }

        await _api.SkipQuestWaitingTimeAsync(apiKey, clanId, ct);
        return (TaskExecutionOutcome.Success,
            $"Tempo de espera da missão \"{active.Quest.DisplayName}\" pulado (ouro debitado).", null);
    }

    private async Task<(TaskExecutionOutcome, string)> ClaimExtraTimeAsync(
        string apiKey, string clanId, CancellationToken ct)
    {
        var active = await GetActiveQuestSafeAsync(apiKey, clanId, ct);
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

    // A API do Wolvesville às vezes responde 404 (em vez de 204/lista vazia) quando o clã não
    // tem missão ativa ou nenhuma disponível — mesmo tratamento já aplicado em QuestService,
    // replicado aqui para o executor não marcar a execução como falha nesse cenário normal.
    private async Task<ActiveQuest?> GetActiveQuestSafeAsync(string apiKey, string clanId, CancellationToken ct)
    {
        try
        {
            return await _api.GetActiveQuestAsync(apiKey, clanId, ct);
        }
        catch (WolvesvilleApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<List<ClanQuest>> GetAvailableQuestsSafeAsync(string apiKey, string clanId, CancellationToken ct)
    {
        try
        {
            return await _api.GetAvailableQuestsAsync(apiKey, clanId, ct);
        }
        catch (WolvesvilleApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new List<ClanQuest>();
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

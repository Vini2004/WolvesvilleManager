using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Entities;

namespace WolvesvilleManager.Application.Scheduling;

/// <summary>CRUD das tarefas agendadas de um clã.</summary>
public class ScheduledTaskService
{
    private readonly IAppDbContext _db;
    private readonly ICronTriggerGateway _cron;
    private readonly ILogger<ScheduledTaskService> _logger;

    public ScheduledTaskService(IAppDbContext db, ICronTriggerGateway cron, ILogger<ScheduledTaskService> logger)
    {
        _db = db;
        _cron = cron;
        _logger = logger;
    }

    public async Task<List<ScheduledTaskDto>> ListAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var clanExists = await _db.ClanRegistrations.AnyAsync(c => c.Id == clanRegistrationId, ct);
        if (!clanExists)
            throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        return await _db.ScheduledTasks
            .AsNoTracking()
            .Where(t => t.ClanRegistrationId == clanRegistrationId)
            .OrderBy(t => t.Id)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<ScheduledTaskDto> CreateAsync(
        int clanRegistrationId, CreateScheduledTaskRequest request, CancellationToken ct = default)
    {
        var clanExists = await _db.ClanRegistrations.AnyAsync(c => c.Id == clanRegistrationId, ct);
        if (!clanExists)
            throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        Validate(request);

        var task = new ScheduledTask
        {
            ClanRegistrationId = clanRegistrationId,
            Type = request.Type,
            CronExpression = request.CronExpression.Trim(),
            TimeZoneId = request.TimeZoneId.Trim(),
            MinVotes = request.MinVotes,
            TargetQuestId = request.TargetQuestId?.Trim(),
            TargetQuestName = request.TargetQuestName?.Trim(),
            TargetQuestPromoImageUrl = request.TargetQuestPromoImageUrl?.Trim(),
            Enabled = request.Enabled,
            AutoRetryOnXpNotReached = request.AutoRetryOnXpNotReached,
            AutoRetryMaxAttempts = request.AutoRetryMaxAttempts,
        };
        task.NextRunAtUtc = task.Enabled
            ? CronScheduleCalculator.GetNextOccurrenceUtc(task.CronExpression, task.TimeZoneId, DateTime.UtcNow)
            : null;

        _db.ScheduledTasks.Add(task);
        await _db.SaveChangesAsync(ct); // precisa do Id gerado para nomear o gatilho externo
        if (await SyncTriggerAsync(task, ct))
            await _db.SaveChangesAsync(ct);
        return ToDto(task);
    }

    public async Task<ScheduledTaskDto> UpdateAsync(
        int taskId, CreateScheduledTaskRequest request, CancellationToken ct = default)
    {
        var task = await _db.ScheduledTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException($"Tarefa agendada #{taskId} não encontrada.");

        Validate(request);

        task.Type = request.Type;
        task.CronExpression = request.CronExpression.Trim();
        task.TimeZoneId = request.TimeZoneId.Trim();
        task.MinVotes = request.MinVotes;
        task.TargetQuestId = request.TargetQuestId?.Trim();
        task.TargetQuestName = request.TargetQuestName?.Trim();
        task.TargetQuestPromoImageUrl = request.TargetQuestPromoImageUrl?.Trim();
        task.Enabled = request.Enabled;
        task.AutoRetryOnXpNotReached = request.AutoRetryOnXpNotReached;
        task.AutoRetryMaxAttempts = request.AutoRetryMaxAttempts;
        task.NextRunAtUtc = task.Enabled
            ? CronScheduleCalculator.GetNextOccurrenceUtc(task.CronExpression, task.TimeZoneId, DateTime.UtcNow)
            : null;

        await SyncTriggerAsync(task, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(task);
    }

    public async Task DeleteAsync(int taskId, CancellationToken ct = default)
    {
        var task = await _db.ScheduledTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException($"Tarefa agendada #{taskId} não encontrada.");

        var triggers = new CronTriggerIds(task.ExternalRunJobId, task.ExternalWarmupJobId);
        _db.ScheduledTasks.Remove(task);
        await _db.SaveChangesAsync(ct);

        if (_cron.Enabled)
        {
            try { await _cron.DeleteAsync(triggers, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao remover gatilho externo da tarefa #{TaskId}.", taskId); }
        }
    }

    /// <summary>
    /// Sincroniza os gatilhos externos (execução + pré-aquecimento) da tarefa. Nunca quebra o CRUD:
    /// se o cron-job.org estiver fora, loga e segue — a tarefa fica salva e pode ser re-sincronizada
    /// numa próxima edição. Devolve true se algum id mudou (para persistir).
    /// </summary>
    private async Task<bool> SyncTriggerAsync(ScheduledTask task, CancellationToken ct)
    {
        if (!_cron.Enabled) return false;
        try
        {
            var ids = await _cron.SyncAsync(
                new CronTriggerIds(task.ExternalRunJobId, task.ExternalWarmupJobId),
                new ScheduledTaskTrigger(
                    task.Id, task.Type, task.CronExpression, task.TimeZoneId, task.Enabled,
                    task.AutoRetryOnXpNotReached, task.AutoRetryMaxAttempts),
                ct);
            task.ExternalRunJobId = ids.RunJobId;
            task.ExternalWarmupJobId = ids.WarmupJobId;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível sincronizar o gatilho externo da tarefa #{TaskId}.", task.Id);
            return false;
        }
    }

    public async Task<List<TaskExecutionLogDto>> GetLogsAsync(int taskId, int take = 50, CancellationToken ct = default)
    {
        var taskExists = await _db.ScheduledTasks.AnyAsync(t => t.Id == taskId, ct);
        if (!taskExists)
            throw new NotFoundException($"Tarefa agendada #{taskId} não encontrada.");

        return await _db.TaskExecutionLogs
            .AsNoTracking()
            .Where(l => l.ScheduledTaskId == taskId)
            .OrderByDescending(l => l.RanAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .Select(l => new TaskExecutionLogDto(l.Id, l.RanAtUtc, l.Outcome, l.Message))
            .ToListAsync(ct);
    }

    private static void Validate(CreateScheduledTaskRequest request)
    {
        if (!Enum.IsDefined(request.Type))
            throw new BusinessRuleException("Tipo de tarefa inválido.");
        if (string.IsNullOrWhiteSpace(request.CronExpression) ||
            !CronScheduleCalculator.IsValidCron(request.CronExpression.Trim()))
            throw new BusinessRuleException(
                "Expressão cron inválida. Use o formato de 5 campos, ex.: \"0 20 * * FRI\" (toda sexta às 20h).");
        if (string.IsNullOrWhiteSpace(request.TimeZoneId) ||
            !CronScheduleCalculator.IsValidTimeZone(request.TimeZoneId.Trim()))
            throw new BusinessRuleException(
                "Fuso horário inválido. Use um ID IANA, ex.: \"America/Sao_Paulo\".");
        if (request.MinVotes < 0)
            throw new BusinessRuleException("MinVotes não pode ser negativo.");
        if (request.AutoRetryMaxAttempts is < 1 or > ScheduledTaskExecutor.MaxAutoRetryAttemptsLimit)
            throw new BusinessRuleException(
                $"O número de retentativas automáticas deve ser entre 1 e {ScheduledTaskExecutor.MaxAutoRetryAttemptsLimit}.");
        if (request.Type == ScheduledTaskType.ClaimSpecificQuest &&
            string.IsNullOrWhiteSpace(request.TargetQuestId) &&
            string.IsNullOrWhiteSpace(request.TargetQuestName) &&
            string.IsNullOrWhiteSpace(request.TargetQuestPromoImageUrl))
            throw new BusinessRuleException("Escolha a missão específica que a automação deve iniciar.");
    }

    private static ScheduledTaskDto ToDto(ScheduledTask t) => new(
        t.Id, t.ClanRegistrationId, t.Type, t.CronExpression, t.TimeZoneId,
        t.Enabled, t.MinVotes, t.TargetQuestId, t.TargetQuestName, t.TargetQuestPromoImageUrl,
        t.AutoRetryOnXpNotReached, t.AutoRetryMaxAttempts, t.NextRunAtUtc, t.LastRunAtUtc, t.CreatedAtUtc);
}

public record CreateScheduledTaskRequest(
    ScheduledTaskType Type,
    string CronExpression,
    string TimeZoneId = "America/Sao_Paulo",
    int MinVotes = 1,
    string? TargetQuestId = null,
    string? TargetQuestName = null,
    string? TargetQuestPromoImageUrl = null,
    bool Enabled = true,
    bool AutoRetryOnXpNotReached = true,
    int AutoRetryMaxAttempts = 10);

public record ScheduledTaskDto(
    int Id,
    int ClanRegistrationId,
    ScheduledTaskType Type,
    string CronExpression,
    string TimeZoneId,
    bool Enabled,
    int MinVotes,
    string? TargetQuestId,
    string? TargetQuestName,
    string? TargetQuestPromoImageUrl,
    bool AutoRetryOnXpNotReached,
    int AutoRetryMaxAttempts,
    DateTime? NextRunAtUtc,
    DateTime? LastRunAtUtc,
    DateTime CreatedAtUtc);

public record TaskExecutionLogDto(long Id, DateTime RanAtUtc, TaskExecutionOutcome Outcome, string? Message);

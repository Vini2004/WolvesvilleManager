using WolvesvilleManager.Domain.Entities;

namespace WolvesvilleManager.Application.Scheduling;

/// <summary>
/// Ids dos gatilhos externos de uma tarefa: o de execução (dispara no horário) e o de
/// pré-aquecimento (dispara ~5 min antes, para o app/banco já estarem quentes na hora).
/// </summary>
public readonly record struct CronTriggerIds(int? RunJobId, int? WarmupJobId);

/// <summary>Dados de uma tarefa necessários para montar os gatilhos externos.</summary>
public sealed record ScheduledTaskTrigger(
    int TaskId, ScheduledTaskType Type, string CronExpression, string TimeZoneId, bool Enabled);

/// <summary>
/// Porta para um agendador externo (cron-job.org) que "acorda" o app/banco no horário de cada
/// tarefa. Numa hospedagem sem Always On e com banco serverless que auto-pausa, isso substitui um
/// keep-alive frequente — que manteria o banco ativo 24/7 e queimaria a cota mensal do plano free.
/// Quando <see cref="Enabled"/> é false (integração não configurada), tudo vira no-op.
/// </summary>
public interface ICronTriggerGateway
{
    bool Enabled { get; }

    /// <summary>Cria/atualiza os gatilhos da tarefa e devolve os ids para persistir.</summary>
    Task<CronTriggerIds> SyncAsync(CronTriggerIds existing, ScheduledTaskTrigger trigger, CancellationToken ct = default);

    /// <summary>Remove os gatilhos da tarefa (execução e pré-aquecimento).</summary>
    Task DeleteAsync(CronTriggerIds existing, CancellationToken ct = default);
}

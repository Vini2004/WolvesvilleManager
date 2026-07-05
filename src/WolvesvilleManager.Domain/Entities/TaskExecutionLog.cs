using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>Resultado de uma execução (ou tentativa) de tarefa agendada, para auditoria.</summary>
public class TaskExecutionLog
{
    public long Id { get; set; }

    public int ScheduledTaskId { get; set; }
    public ScheduledTask ScheduledTask { get; set; } = null!;

    public DateTime RanAtUtc { get; set; } = DateTime.UtcNow;

    public TaskExecutionOutcome Outcome { get; set; }

    /// <summary>Detalhe legível: missão iniciada, motivo do skip, mensagem de erro da API...</summary>
    [MaxLength(1000)]
    public string? Message { get; set; }
}

public enum TaskExecutionOutcome
{
    /// <summary>A ação foi executada na API do Wolvesville.</summary>
    Success = 1,

    /// <summary>Condições não atendidas (ex.: sem missão ativa, votos insuficientes) — nada foi gasto.</summary>
    Skipped = 2,

    /// <summary>A API retornou erro ou houve falha de comunicação.</summary>
    Failed = 3,
}

using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Tarefa agendada de um clã (ex.: "toda sexta às 20h, iniciar a missão mais votada").
/// O horário é definido por expressão cron interpretada no fuso <see cref="TimeZoneId"/>.
/// </summary>
public class ScheduledTask
{
    public int Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    public ScheduledTaskType Type { get; set; }

    /// <summary>Expressão cron de 5 campos (min hora dia mês dia-da-semana), ex.: "0 20 * * FRI".</summary>
    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>Fuso em que a expressão cron é interpretada (IANA ou Windows).</summary>
    [Required]
    [MaxLength(64)]
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mínimo de votos para a missão vencedora em <see cref="ScheduledTaskType.ClaimMostVotedQuest"/>;
    /// abaixo disso a execução é pulada (evita iniciar missão sem quórum do clã).
    /// </summary>
    public int MinVotes { get; set; } = 1;

    /// <summary>Próxima execução, pré-calculada em UTC (indexada para o poll do agendador).</summary>
    public DateTime? NextRunAtUtc { get; set; }

    public DateTime? LastRunAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TaskExecutionLog> ExecutionLogs { get; set; } = new();
}

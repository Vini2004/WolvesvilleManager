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

    /// <summary>
    /// Id da missão a iniciar em <see cref="ScheduledTaskType.ClaimSpecificQuest"/>.
    /// Como as ofertas rotacionam, a execução casa por Id ou pelo nome guardado.
    /// </summary>
    [MaxLength(60)]
    public string? TargetQuestId { get; set; }

    /// <summary>Nome legível da missão fixada (para exibição, logs e fallback de correspondência).</summary>
    [MaxLength(120)]
    public string? TargetQuestName { get; set; }

    /// <summary>
    /// URL da imagem promocional da missão fixada. É a identidade mais estável de uma missão
    /// (o Id da oferta e o nome do arquivo rotacionam), então a execução casa por ela
    /// normalizada antes de cair para Id/nome.
    /// </summary>
    [MaxLength(500)]
    public string? TargetQuestPromoImageUrl { get; set; }

    /// <summary>Próxima execução, pré-calculada em UTC (indexada para o poll do agendador).</summary>
    public DateTime? NextRunAtUtc { get; set; }

    public DateTime? LastRunAtUtc { get; set; }

    /// <summary>Id do gatilho de execução no cron-job.org (dispara no horário da tarefa); null se a integração está desligada.</summary>
    public int? ExternalRunJobId { get; set; }

    /// <summary>Id do gatilho de pré-aquecimento (dispara ~5 min antes, acorda app+banco); null quando não aplicável.</summary>
    public int? ExternalWarmupJobId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TaskExecutionLog> ExecutionLogs { get; set; } = new();
}

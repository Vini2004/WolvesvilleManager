using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Um clã registrado na aplicação: guarda o ID do clã no Wolvesville
/// e a chave de API (bot) autorizada para gerenciá-lo, criptografada em repouso.
/// </summary>
public class ClanRegistration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string ClanId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ClanName { get; set; } = string.Empty;

    [MaxLength(16)]
    public string? ClanTag { get; set; }

    /// <summary>Chave de API criptografada via ASP.NET Data Protection — nunca em texto puro.</summary>
    [Required]
    public string ProtectedApiKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token aleatório do formulário público de votação (/votar/{token}).
    /// Nulo enquanto o admin nunca abriu a aba Votação. É a única credencial da
    /// página pública, por isso longo e imprevisível.
    /// </summary>
    [MaxLength(64)]
    public string? PollToken { get; set; }

    /// <summary>
    /// Prazo manual da votação: depois disso o formulário público para de aceitar votos novos
    /// (mas continua visível). Só vale enquanto <see cref="PollWindows"/> estiver vazia — assim
    /// que o admin configura ao menos uma janela, o estado aberto/fechado passa a ser calculado
    /// a partir delas, e este campo é ignorado.
    /// </summary>
    public DateTime? PollExpiresAtUtc { get; set; }

    /// <summary>
    /// Janelas semanais recorrentes em que a votação fica aberta (ex.: domingo 23h–segunda 11h
    /// e quarta 20h–quinta 11h). Quando não vazia, substitui o <see cref="PollExpiresAtUtc"/>
    /// manual — o admin pode configurar quantas janelas quiser.
    /// </summary>
    public List<PollWindow> PollWindows { get; set; } = new();

    /// <summary>Fuso horário (IANA) em que <see cref="PollWindows"/> é interpretada.</summary>
    [MaxLength(64)]
    public string? PollWindowsTimeZoneId { get; set; }

    /// <summary>
    /// Missões que o admin ocultou do formulário público (por chave estável). Uma missão listada
    /// aqui não aparece na página pública nem aceita votos — ver <see cref="PollHiddenQuest"/>.
    /// </summary>
    public List<PollHiddenQuest> PollHiddenQuests { get; set; } = new();

    /// <summary>
    /// Fim (UTC) do último ciclo de <see cref="PollWindows"/> já apurado por uma automação
    /// "mais votada do formulário" — evita que a mesma rodada seja aplicada duas vezes caso a
    /// automação rode mais de uma vez antes do próximo ciclo terminar.
    /// </summary>
    public DateTime? PollLastClaimedWindowEndUtc { get; set; }

    public List<ScheduledTask> ScheduledTasks { get; set; } = new();

    /// <summary>
    /// Liga a mensagem automática de boas-vindas no chat quando um membro novo entra no clã
    /// (detectado pelo log de auditoria — ver <c>ScheduledTaskExecutor.MemberJoinedLogActions</c>).
    /// Desligado por padrão — é um comportamento novo e cada clã decide se quer.
    /// </summary>
    public bool WelcomeMessageEnabled { get; set; }

    /// <summary>
    /// Template da mensagem de boas-vindas; o texto "{mention}" é trocado por "@" + o nick de
    /// quem entrou antes de mandar no chat. Nulo = usa o texto padrão do serviço de boas-vindas.
    /// </summary>
    [MaxLength(500)]
    public string? WelcomeMessageTemplate { get; set; }

    /// <summary>
    /// Data (UTC) da entrada de log de "virou membro" mais recente já recebida como boas-vindas —
    /// evita repetir a mensagem e evita boas-vindas retroativas para quem já estava no clã quando
    /// a feature é ligada pela primeira vez.
    /// </summary>
    public DateTime? LastWelcomedJoinAtUtc { get; set; }

    /// <summary>
    /// Primeiro horário do dia (no fuso das boas-vindas) em que as boas-vindas represadas são
    /// liberadas. Quem entrou depois de um horário configurado só é saudado no PRÓXIMO horário
    /// configurado. Nulo (e <see cref="WelcomeSendTime2"/> nulo) = sem represamento: sauda assim
    /// que o app acordar depois da entrada.
    /// </summary>
    public TimeSpan? WelcomeSendTime1 { get; set; }

    /// <summary>Segundo horário de liberação das boas-vindas (ver <see cref="WelcomeSendTime1"/>).</summary>
    public TimeSpan? WelcomeSendTime2 { get; set; }

    /// <summary>
    /// Id do job no cron-job.org que acorda o app nos horários de boas-vindas configurados
    /// (<see cref="WelcomeSendTime1"/>/<see cref="WelcomeSendTime2"/>) batendo em
    /// <c>/api/scheduler/run</c>; nulo quando não há horários ou a integração está desligada.
    /// </summary>
    public int? WelcomePingJobId { get; set; }

    /// <summary>
    /// Quando (UTC) a checagem de boas-vindas rodou pela última vez — automática ou manual.
    /// É o jeito de saber se o app está mesmo sendo acordado: se este horário fica velho depois
    /// de um horário de envio configurado, o gatilho externo não está disparando.
    /// </summary>
    public DateTime? LastWelcomeCheckAtUtc { get; set; }

    /// <summary>Resumo legível do resultado da última checagem (ex.: "3 entrada(s), 0 enviada(s), 3 aguardando").</summary>
    [MaxLength(300)]
    public string? LastWelcomeCheckResult { get; set; }
}

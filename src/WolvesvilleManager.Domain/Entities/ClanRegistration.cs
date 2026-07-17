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
    /// Fim (UTC) do último ciclo de <see cref="PollWindows"/> já apurado por uma automação
    /// "mais votada do formulário" — evita que a mesma rodada seja aplicada duas vezes caso a
    /// automação rode mais de uma vez antes do próximo ciclo terminar.
    /// </summary>
    public DateTime? PollLastClaimedWindowEndUtc { get; set; }

    public List<ScheduledTask> ScheduledTasks { get; set; } = new();

    /// <summary>
    /// Liga a mensagem automática de boas-vindas no chat quando um membro novo entra no clã
    /// (detectado pelo log de auditoria, ação "JOIN"/"ACCEPT_INVITE"). Desligado por padrão —
    /// é um comportamento novo e cada clã decide se quer.
    /// </summary>
    public bool WelcomeMessageEnabled { get; set; }

    /// <summary>
    /// Template da mensagem de boas-vindas; o texto "{mention}" é trocado por "@" + o nick de
    /// quem entrou antes de mandar no chat. Nulo = usa o texto padrão do serviço de boas-vindas.
    /// </summary>
    [MaxLength(500)]
    public string? WelcomeMessageTemplate { get; set; }

    /// <summary>
    /// Data (UTC) da entrada de log "JOIN"/"ACCEPT_INVITE" mais recente já recebida como
    /// boas-vindas — evita repetir a mensagem e evita boas-vindas retroativas para quem já
    /// estava no clã quando a feature é ligada pela primeira vez.
    /// </summary>
    public DateTime? LastWelcomedJoinAtUtc { get; set; }
}

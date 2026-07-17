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
    /// Prazo da votação: depois disso o formulário público para de aceitar votos
    /// novos (mas continua visível). Sempre definido junto com <see cref="PollToken"/> —
    /// a votação nunca fica aberta indefinidamente.
    /// </summary>
    public DateTime? PollExpiresAtUtc { get; set; }

    public List<ScheduledTask> ScheduledTasks { get; set; } = new();
}

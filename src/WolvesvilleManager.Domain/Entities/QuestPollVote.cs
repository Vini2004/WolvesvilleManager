using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Voto do formulário público de missões. Cada navegador gera um VoterId anônimo
/// (guardado no localStorage) e pode ter no máximo um voto por clã — votar de novo
/// troca a missão escolhida. Não identifica ninguém: é urna de clã, não eleição.
/// </summary>
public class QuestPollVote
{
    public long Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    /// <summary>Id da missão (da API do Wolvesville) escolhida.</summary>
    [Required]
    [MaxLength(64)]
    public string QuestId { get; set; } = string.Empty;

    /// <summary>Identificador anônimo do navegador que votou.</summary>
    [Required]
    [MaxLength(64)]
    public string VoterId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

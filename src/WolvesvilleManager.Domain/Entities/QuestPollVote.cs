using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Voto do formulário público de missões. Identificado pelo nick digitado por quem vota
/// (não por navegador — trocar de navegador ou aba anônima não abre um voto extra, já que
/// a comparação é por nick, não por dispositivo). Votar de novo com o mesmo nick troca a
/// missão escolhida. Não é uma eleição de verdade: não confere se o nick existe no clã.
/// </summary>
public class QuestPollVote
{
    /// <summary>
    /// QuestId reservado para o voto "embaralhar": não é uma missão de verdade, é uma
    /// cédula a mais na mesma urna. Se vencer a apuração, a automação embaralha em vez
    /// de reivindicar uma missão. Nunca colide com um Id real (que vem da API do jogo).
    /// </summary>
    public const string ShuffleOptionId = "__shuffle__";

    public long Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    /// <summary>Id da missão escolhida (da API do Wolvesville), ou <see cref="ShuffleOptionId"/>.</summary>
    [Required]
    [MaxLength(64)]
    public string QuestId { get; set; } = string.Empty;

    /// <summary>Nick digitado por quem votou (como digitado — a comparação de duplicidade ignora maiúsculas).</summary>
    [Required]
    [MaxLength(32)]
    public string Nickname { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Instante do voto atual (criação ou última troca de missão). Usado para determinar a qual
    /// ciclo de <see cref="PollWindow"/> este voto pertence quando a automação apura só o último
    /// ciclo concluído, em vez da urna inteira.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

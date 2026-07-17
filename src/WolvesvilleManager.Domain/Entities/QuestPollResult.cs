namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Registro histórico de uma apuração da votação pública: o que venceu e com quantos votos,
/// gravado no instante em que a automação decide (antes de zerar a urna para a rodada
/// seguinte). Sem isto, o resultado de cada rodada se perderia ao limpar os votos.
/// </summary>
public class QuestPollResult
{
    public long Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    /// <summary>Nome da missão vencedora, ou "Embaralhar missões" quando <see cref="WasShuffle"/>.</summary>
    public string QuestName { get; set; } = string.Empty;

    public int Votes { get; set; }

    /// <summary>true quando a vencedora foi "embaralhar" em vez de uma missão de verdade.</summary>
    public bool WasShuffle { get; set; }

    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}

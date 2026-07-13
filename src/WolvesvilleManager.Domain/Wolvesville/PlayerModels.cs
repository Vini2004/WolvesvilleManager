namespace WolvesvilleManager.Domain.Wolvesville;

/// <summary>
/// Perfil público de um jogador (GET /players/{id}, /players/search).
/// Campos anuláveis: o formato exato varia conforme as configurações de privacidade do jogador.
/// </summary>
public class PlayerProfile
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? PersonalMessage { get; set; }

    /// <summary>Anulável: a API omite/manda null quando o nível está oculto pela privacidade.</summary>
    public int? Level { get; set; }

    public string? Status { get; set; }
    public string? ClanId { get; set; }
    public string? LastOnline { get; set; }
    public string? CreationTime { get; set; }

    // Nomes exatos da API: "receivedRosesCount" / "sentRosesCount".
    public int? ReceivedRosesCount { get; set; }
    public int? SentRosesCount { get; set; }

    public string? ProfileIconColor { get; set; }
    public GameStats? GameStats { get; set; }
}

/// <summary>Estatísticas de partidas do jogador (dentro do perfil).</summary>
public class GameStats
{
    public int? TotalWinCount { get; set; }
    public int? TotalLoseCount { get; set; }
    public int? TotalTieCount { get; set; }
    public int? TotalPlayTimeInMinutes { get; set; }
    public int? ExitGameAfterDeathCount { get; set; }
    public int? ExitGameBySuicideCount { get; set; }
}

/// <summary>Rotação de papéis ativa (GET /roleRotations) — cru para inspeção, o formato varia por modo.</summary>
public class RoleRotationEntry
{
    public string? GameMode { get; set; }

    /// <summary>Rotações do modo, cru: [{ roleRotation: { id, roles: [[{ probability, role }]] }, probability }].</summary>
    public System.Text.Json.JsonElement? RoleRotations { get; set; }
}

/// <summary>Temporada do battle pass (GET /battlePass/season).</summary>
public class BattlePassSeason
{
    public int? Number { get; set; }
    public string? StartTime { get; set; }
    public int? DurationInDays { get; set; }
    public int? XpPerReward { get; set; }
}

/// <summary>Oferta ativa da loja (GET /shop/activeOffers). Formato não confirmado — campos opcionais.</summary>
public class ShopOffer
{
    public string? Type { get; set; }
    public int? CostInGems { get; set; }
    public string? PromoImageUrl { get; set; }
    public string? ExpireDate { get; set; }
}

/// <summary>Resposta de GET /announcements: { announcements: [...] }.</summary>
public class GameAnnouncementsResponse
{
    public List<GameAnnouncement> Announcements { get; set; } = new();
}

/// <summary>Anúncio oficial do jogo (vem do Discord da Wolvesville).</summary>
public class GameAnnouncement
{
    public string? Content { get; set; }
    public string? Timestamp { get; set; }
    public GameAnnouncementAuthor? Author { get; set; }
    public List<GameAnnouncementAttachment>? Attachments { get; set; }
}

public class GameAnnouncementAuthor
{
    public string? Username { get; set; }
}

public class GameAnnouncementAttachment
{
    public string? Filename { get; set; }
    public string? Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

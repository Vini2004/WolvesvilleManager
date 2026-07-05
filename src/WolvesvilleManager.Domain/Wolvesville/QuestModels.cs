using System.Text.Json.Serialization;

namespace WolvesvilleManager.Domain.Wolvesville;

/// <summary>Definição de uma missão de clã (usada nas listas de disponíveis, ativa e histórico).</summary>
public class ClanQuest
{
    public string Id { get; set; } = string.Empty;
    public string? PromoImageUrl { get; set; }

    /// <summary>true = missão comprada com gemas; false = com ouro.</summary>
    public bool PurchasableWithGems { get; set; }

    public List<QuestReward> Rewards { get; set; } = new();

    /// <summary>Nome legível derivado do arquivo da imagem promocional (a API não envia nome).</summary>
    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(PromoImageUrl)) return "Missão";
            var file = PromoImageUrl.Split('/').Last();
            var dot = file.LastIndexOf('.');
            if (dot > 0) file = file[..dot];
            return file.Replace('-', ' ').Replace('_', ' ');
        }
    }
}

public class QuestReward
{
    /// <summary>GOLD, GEMS, AVATAR_ITEM, LOOT_BOX, CLAN_ICON, CLAN_XP...</summary>
    public string? Type { get; set; }
    public int Amount { get; set; }
    public string? AvatarItemId { get; set; }
    public string? LootBoxId { get; set; }
    public string? DisplayType { get; set; }
}

/// <summary>Missão ativa do clã (GET /clans/{id}/quests/active — 204 quando não há).</summary>
public class ActiveQuest
{
    public ClanQuest Quest { get; set; } = new();

    /// <summary>Tier atual, começando em 0 na API.</summary>
    public int Tier { get; set; }

    /// <summary>XP acumulado no tier atual.</summary>
    public long Xp { get; set; }

    public string? TierStartTime { get; set; }
    public string? TierEndTime { get; set; }
    public bool TierFinished { get; set; }

    /// <summary>XP necessário para completar cada tier/recompensa.</summary>
    public long XpPerReward { get; set; }

    /// <summary>true quando o tempo extra já foi resgatado.</summary>
    public bool ClaimedTime { get; set; }

    public List<QuestParticipant> Participants { get; set; } = new();

    /// <summary>
    /// true enquanto a missão está na fase de espera (ainda não começou a contar XP) —
    /// é nessa janela que "pular tempo de espera" faz sentido.
    /// </summary>
    [JsonIgnore]
    public bool IsInWaitingPhase =>
        DateTimeOffset.TryParse(TierStartTime, out var start) && start > DateTimeOffset.UtcNow;
}

public class QuestParticipant
{
    public string? PlayerId { get; set; }
    public string? Username { get; set; }
    public long Xp { get; set; }
}

/// <summary>Missão concluída no passado (GET /clans/{id}/quests/history).</summary>
public class QuestHistoryEntry
{
    public ClanQuest Quest { get; set; } = new();
    public List<QuestParticipant> Participants { get; set; } = new();
    public long Xp { get; set; }

    /// <summary>Quantidade de tiers alcançados.</summary>
    public int Tier { get; set; }

    public string? TierEndTime { get; set; }
}

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

    /// <summary>
    /// XP acumulado desde o INÍCIO DA MISSÃO (soma de todos os tiers já concluídos + o progresso
    /// do tier atual) — não reseta a cada tier. Para o progresso só do tier atual, use <see cref="TierXp"/>.
    /// </summary>
    public long Xp { get; set; }

    public string? TierStartTime { get; set; }
    public string? TierEndTime { get; set; }
    public bool TierFinished { get; set; }

    /// <summary>XP necessário para completar cada tier/recompensa (mesmo valor em todos os tiers da missão).</summary>
    public long XpPerReward { get; set; }

    /// <summary>
    /// XP acumulado só no tier ATUAL, descontando os tiers já concluídos (<see cref="Tier"/> ×
    /// <see cref="XpPerReward"/>). A API só devolve o total corrido desde o tier 0 em <see cref="Xp"/>;
    /// sem esse desconto, a partir do 2º tier o progresso e o "objetivo concluído" aparecem errados
    /// (ex.: 15801/6750 vira 100% quando o tier em si só tem 2301/6750 de verdade).
    /// </summary>
    public long TierXp => XpPerReward > 0 ? Math.Max(0, Xp - (long)Tier * XpPerReward) : Xp;

    /// <summary>true quando o tempo extra já foi resgatado.</summary>
    public bool ClaimedTime { get; set; }

    public List<QuestParticipant> Participants { get; set; } = new();

    /// <summary>
    /// true enquanto o tier atual ainda nem começou a contar XP (janela de espera antes do início).
    /// </summary>
    [JsonIgnore]
    public bool IsBeforeTierStart =>
        DateTimeOffset.TryParse(TierStartTime, out var start) && start > DateTimeOffset.UtcNow;

    /// <summary>
    /// true quando existe um tempo de espera que o líder/co-líder pode pular gastando ouro do clã.
    /// Isso acontece quando o objetivo de XP do tier já foi atingido (<see cref="TierFinished"/> ou
    /// <see cref="TierXp"/> ≥ <see cref="XpPerReward"/>) e agora só falta o cronômetro (TierEndTime)
    /// zerar para liberar o próximo tier — ou quando o tier ainda nem começou (<see cref="IsBeforeTierStart"/>).
    /// Enquanto o clã ainda está acumulando XP rumo ao objetivo NÃO há espera a pular.
    /// </summary>
    [JsonIgnore]
    public bool CanSkipWaitingTime =>
        TierFinished || (XpPerReward > 0 && TierXp >= XpPerReward) || IsBeforeTierStart;
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

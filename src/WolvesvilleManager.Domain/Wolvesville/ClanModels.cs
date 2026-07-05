using System.Text.Json.Serialization;

namespace WolvesvilleManager.Domain.Wolvesville;

/// <summary>
/// Informações de um clã (GET /clans/{id}/info, /clans/search, /clans/authorized).
/// Os campos <see cref="Gold"/> e <see cref="Gems"/> só vêm preenchidos quando a
/// chave de API usada é autorizada como "clan bot" do clã.
/// </summary>
public class ClanInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public string? CreationTime { get; set; }
    public long Xp { get; set; }
    public string? Language { get; set; }
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? JoinType { get; set; }
    public int MemberCount { get; set; }
    public string? LeaderId { get; set; }
    public int MinLevel { get; set; }
    public int QuestHistoryCount { get; set; }
    public long? Gold { get; set; }
    public long? Gems { get; set; }

    /// <summary>true quando a chave usada é clan bot deste clã (a API só envia saldo nesse caso).</summary>
    [JsonIgnore]
    public bool IsAuthorized => Gold.HasValue || Gems.HasValue;
}

/// <summary>Membro do clã (GET /clans/{id}/members[/detailed]).</summary>
public class ClanMember
{
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? PlayerStatus { get; set; }

    /// <summary>XP contribuído ao clã.</summary>
    public long Xp { get; set; }

    public string? Status { get; set; }
    public bool IsCoLeader { get; set; }
    public string? LastOnline { get; set; }
    public string? CreationTime { get; set; }

    /// <summary>Só presente na listagem detalhada (requer clan bot).</summary>
    public bool? ParticipateInClanQuests { get; set; }
}

/// <summary>Entrada do livro-razão do clã (GET /clans/{id}/ledger).</summary>
public class LedgerEntry
{
    public string? Id { get; set; }
    public string? PlayerId { get; set; }
    public string? PlayerUsername { get; set; }
    public long? Gold { get; set; }
    public long? Gems { get; set; }

    /// <summary>Ação: DONATE, CLAIM_QUEST, SHUFFLE_QUESTS etc.</summary>
    public string? Type { get; set; }

    public string? CreationTime { get; set; }
    public string? ClanQuestId { get; set; }
}

/// <summary>Registro de auditoria do clã (GET /clans/{id}/logs).</summary>
public class ClanLogEntry
{
    public string? Action { get; set; }
    public string? PlayerId { get; set; }
    public string? PlayerUsername { get; set; }
    public string? TargetPlayerId { get; set; }
    public string? TargetPlayerUsername { get; set; }
    public string? CreationTime { get; set; }
    public string? PlayerBotId { get; set; }
    public string? PlayerBotOwnerUsername { get; set; }
    public string? Comment { get; set; }
}

/// <summary>Jogador bloqueado (GET /clans/{id}/blocklist).</summary>
public class BlocklistEntry
{
    public string? PlayerId { get; set; }
    public string? PlayerUsername { get; set; }
    public string? CreationTime { get; set; }
}

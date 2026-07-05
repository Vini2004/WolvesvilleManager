using System.Text.Json;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Domain.Interfaces;

/// <summary>
/// Contrato de acesso à API pública do Wolvesville. A regra de negócio depende só
/// deste contrato; a implementação HTTP real fica na Infrastructure.
/// </summary>
public interface IWolvesvilleClient
{
    // ---------- Clãs ----------
    Task<List<ClanInfo>> GetAuthorizedClansAsync(string apiKey, CancellationToken ct = default);
    Task<List<ClanInfo>> SearchClansAsync(string apiKey, string name, CancellationToken ct = default);
    Task<ClanInfo> GetClanInfoAsync(string apiKey, string clanId, CancellationToken ct = default);

    // ---------- Membros ----------
    Task<List<ClanMember>> GetMembersAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task<List<ClanMember>> GetMembersDetailedAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task SetMemberQuestParticipationAsync(string apiKey, string clanId, string playerId, bool participate, CancellationToken ct = default);
    Task SetAllMembersQuestParticipationAsync(string apiKey, string clanId, bool participate, CancellationToken ct = default);
    Task KickMemberAsync(string apiKey, string clanId, string playerId, string? reason, CancellationToken ct = default);
    Task BlockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default);
    Task UnblockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default);
    Task<List<BlocklistEntry>> GetBlocklistAsync(string apiKey, string clanId, CancellationToken ct = default);

    // ---------- Missões ----------
    /// <summary>Missão ativa; null quando não há missão em andamento (HTTP 204).</summary>
    Task<ActiveQuest?> GetActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task<List<ClanQuest>> GetAvailableQuestsAsync(string apiKey, string clanId, CancellationToken ct = default);

    /// <summary>Votos dos membros nas missões disponíveis. Formato não documentado — JSON cru.</summary>
    Task<JsonElement> GetQuestVotesAsync(string apiKey, string clanId, CancellationToken ct = default);

    Task<List<QuestHistoryEntry>> GetQuestHistoryAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task<List<ClanQuest>> GetAllQuestsAsync(string apiKey, CancellationToken ct = default);

    /// <summary>Inicia (compra) uma missão disponível. Gasta ouro ou gemas do clã!</summary>
    Task ClaimQuestAsync(string apiKey, string clanId, string questId, CancellationToken ct = default);

    /// <summary>Embaralha as missões disponíveis. Gasta ouro do clã!</summary>
    Task ShuffleQuestsAsync(string apiKey, string clanId, CancellationToken ct = default);

    /// <summary>Pula o tempo de espera da missão ativa. Gasta gemas do clã!</summary>
    Task SkipQuestWaitingTimeAsync(string apiKey, string clanId, CancellationToken ct = default);

    /// <summary>Resgata tempo extra para a missão ativa. Gasta ouro do clã!</summary>
    Task ClaimQuestExtraTimeAsync(string apiKey, string clanId, CancellationToken ct = default);

    Task CancelActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default);

    // ---------- Chat e anúncios ----------
    Task<List<ChatMessage>> GetChatAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task SendChatMessageAsync(string apiKey, string clanId, string message, CancellationToken ct = default);
    Task<List<ClanAnnouncement>> GetAnnouncementsAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task SendAnnouncementAsync(string apiKey, string clanId, string message, CancellationToken ct = default);

    // ---------- Ledger e logs ----------
    Task<List<LedgerEntry>> GetLedgerAsync(string apiKey, string clanId, CancellationToken ct = default);
    Task<List<ClanLogEntry>> GetLogsAsync(string apiKey, string clanId, CancellationToken ct = default);
}

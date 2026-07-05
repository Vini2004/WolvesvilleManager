using System.Text.Json;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Tests.Fakes;

/// <summary>
/// Implementação de teste do IWolvesvilleClient: estado configurável e
/// registro das ações executadas (missão reivindicada, skip, etc.).
/// </summary>
public class FakeWolvesvilleClient : IWolvesvilleClient
{
    public ActiveQuest? ActiveQuest { get; set; }
    public List<ClanQuest> AvailableQuests { get; set; } = new();
    public JsonElement Votes { get; set; } = JsonDocument.Parse("{}").RootElement;

    public string? ClaimedQuestId { get; private set; }
    public bool SkippedWaitingTime { get; private set; }
    public bool ClaimedExtraTime { get; private set; }

    public Task<ActiveQuest?> GetActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(ActiveQuest);

    public Task<List<ClanQuest>> GetAvailableQuestsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(AvailableQuests);

    public Task<JsonElement> GetQuestVotesAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(Votes);

    public Task ClaimQuestAsync(string apiKey, string clanId, string questId, CancellationToken ct = default)
    {
        ClaimedQuestId = questId;
        return Task.CompletedTask;
    }

    public Task SkipQuestWaitingTimeAsync(string apiKey, string clanId, CancellationToken ct = default)
    {
        SkippedWaitingTime = true;
        return Task.CompletedTask;
    }

    public Task ClaimQuestExtraTimeAsync(string apiKey, string clanId, CancellationToken ct = default)
    {
        ClaimedExtraTime = true;
        return Task.CompletedTask;
    }

    // --- Não usados pelos testes do agendador ---

    public Task<List<ClanInfo>> GetAuthorizedClansAsync(string apiKey, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanInfo>());

    public Task<List<ClanInfo>> SearchClansAsync(string apiKey, string name, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanInfo>());

    public Task<ClanInfo> GetClanInfoAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new ClanInfo());

    public Task<List<ClanMember>> GetMembersAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanMember>());

    public Task<List<ClanMember>> GetMembersDetailedAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanMember>());

    public Task SetMemberQuestParticipationAsync(string apiKey, string clanId, string playerId, bool participate, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SetAllMembersQuestParticipationAsync(string apiKey, string clanId, bool participate, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task KickMemberAsync(string apiKey, string clanId, string playerId, string? reason, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task BlockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task UnblockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<List<BlocklistEntry>> GetBlocklistAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<BlocklistEntry>());

    public Task<List<QuestHistoryEntry>> GetQuestHistoryAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<QuestHistoryEntry>());

    public Task<List<ClanQuest>> GetAllQuestsAsync(string apiKey, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanQuest>());

    public Task ShuffleQuestsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task CancelActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<List<ChatMessage>> GetChatAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<ChatMessage>());

    public Task SendChatMessageAsync(string apiKey, string clanId, string message, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<List<ClanAnnouncement>> GetAnnouncementsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanAnnouncement>());

    public Task SendAnnouncementAsync(string apiKey, string clanId, string message, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<List<LedgerEntry>> GetLedgerAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<LedgerEntry>());

    public Task<List<ClanLogEntry>> GetLogsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        Task.FromResult(new List<ClanLogEntry>());
}

/// <summary>Protetor de chaves "identidade" para testes.</summary>
public class FakeApiKeyProtector : IApiKeyProtector
{
    public string Protect(string apiKey) => apiKey;
    public string Unprotect(string protectedApiKey, int clanRegistrationId) => protectedApiKey;
}

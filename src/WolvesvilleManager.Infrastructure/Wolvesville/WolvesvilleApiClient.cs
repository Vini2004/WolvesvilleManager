using System.Net;
using System.Text;
using System.Text.Json;
using WolvesvilleManager.Domain.Exceptions;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Infrastructure.Wolvesville;

/// <summary>
/// Cliente HTTP da API pública do Wolvesville (https://api.wolvesville.com).
/// Autenticação via header "Authorization: Bot {apiKey}". Endpoints de gerenciamento
/// exigem que o bot esteja configurado como "clan bot" nas configurações do clã.
/// </summary>
public class WolvesvilleApiClient : IWolvesvilleClient
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        // A API às vezes devolve número onde esperamos texto (status, cores…); tolera em vez de quebrar.
        options.Converters.Add(new TolerantStringConverter());
        return options;
    }

    private readonly HttpClient _http;

    public WolvesvilleApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Clãs ----------

    public Task<List<ClanInfo>> GetAuthorizedClansAsync(string apiKey, CancellationToken ct = default) =>
        GetAsync<List<ClanInfo>>(apiKey, "/clans/authorized", ct);

    public Task<List<ClanInfo>> SearchClansAsync(string apiKey, string name, CancellationToken ct = default) =>
        GetAsync<List<ClanInfo>>(apiKey, $"/clans/search?name={Uri.EscapeDataString(name)}", ct);

    public Task<ClanInfo> GetClanInfoAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<ClanInfo>(apiKey, $"/clans/{clanId}/info", ct);

    // ---------- Membros ----------

    public Task<List<ClanMember>> GetMembersAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ClanMember>>(apiKey, $"/clans/{clanId}/members", ct);

    public Task<List<ClanMember>> GetMembersDetailedAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ClanMember>>(apiKey, $"/clans/{clanId}/members/detailed", ct);

    public Task SetMemberQuestParticipationAsync(string apiKey, string clanId, string playerId, bool participate, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Put, $"/clans/{clanId}/members/{playerId}/participateInQuests",
            new { participateInQuests = participate }, ct);

    public Task SetAllMembersQuestParticipationAsync(string apiKey, string clanId, bool participate, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Put, $"/clans/{clanId}/members/all/participateInQuests",
            new { participateInQuests = participate }, ct);

    public Task KickMemberAsync(string apiKey, string clanId, string playerId, string? reason, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/members/{playerId}/kick",
            string.IsNullOrWhiteSpace(reason) ? new { } : new { reason }, ct);

    public Task BlockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/members/{playerId}/block", null, ct);

    public Task UnblockMemberAsync(string apiKey, string clanId, string playerId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/members/{playerId}/unblock", null, ct);

    public Task<List<BlocklistEntry>> GetBlocklistAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<BlocklistEntry>>(apiKey, $"/clans/{clanId}/blocklist", ct);

    // ---------- Missões ----------

    public async Task<ActiveQuest?> GetActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(apiKey, HttpMethod.Get, $"/clans/{clanId}/quests/active", null, ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        return await ReadAsync<ActiveQuest>(response, ct);
    }

    public Task<List<ClanQuest>> GetAvailableQuestsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ClanQuest>>(apiKey, $"/clans/{clanId}/quests/available", ct);

    public Task<JsonElement> GetQuestVotesAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<JsonElement>(apiKey, $"/clans/{clanId}/quests/votes", ct);

    public Task<List<QuestHistoryEntry>> GetQuestHistoryAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<QuestHistoryEntry>>(apiKey, $"/clans/{clanId}/quests/history", ct);

    public Task<List<ClanQuest>> GetAllQuestsAsync(string apiKey, CancellationToken ct = default) =>
        GetAsync<List<ClanQuest>>(apiKey, "/clans/quests/all", ct);

    public Task ClaimQuestAsync(string apiKey, string clanId, string questId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/quests/claim", new { questId }, ct);

    public Task ShuffleQuestsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/quests/available/shuffle", null, ct);

    public Task SkipQuestWaitingTimeAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/quests/active/skipWaitingTime", null, ct);

    public Task ClaimQuestExtraTimeAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/quests/active/claimTime", null, ct);

    public Task CancelActiveQuestAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/quests/active/cancel", null, ct);

    // ---------- Chat e anúncios ----------

    public Task<List<ChatMessage>> GetChatAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ChatMessage>>(apiKey, $"/clans/{clanId}/chat", ct);

    public Task SendChatMessageAsync(string apiKey, string clanId, string message, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/chat", new { message }, ct);

    public Task<List<ClanAnnouncement>> GetAnnouncementsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ClanAnnouncement>>(apiKey, $"/clans/{clanId}/announcements", ct);

    // Body usa "message" (não "content") — confirmado na doc oficial, pág. 27.
    public Task SendAnnouncementAsync(string apiKey, string clanId, string message, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, $"/clans/{clanId}/announcements", new { message }, ct);

    // ---------- Ledger e logs ----------

    public Task<List<LedgerEntry>> GetLedgerAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<LedgerEntry>>(apiKey, $"/clans/{clanId}/ledger", ct);

    public Task<List<ClanLogEntry>> GetLogsAsync(string apiKey, string clanId, CancellationToken ct = default) =>
        GetAsync<List<ClanLogEntry>>(apiKey, $"/clans/{clanId}/logs", ct);

    public Task SetMemberFlairAsync(string apiKey, string clanId, string playerId, string flair, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Put, $"/clans/{clanId}/members/{playerId}/flair", new { flair }, ct);

    public async Task<PlayerProfile?> SearchPlayerAsync(string apiKey, string username, CancellationToken ct = default)
    {
        try
        {
            return await GetAsync<PlayerProfile>(
                apiKey, $"/players/search?username={Uri.EscapeDataString(username)}", ct);
        }
        catch (WolvesvilleApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<PlayerProfile> GetPlayerAsync(string apiKey, string playerId, CancellationToken ct = default) =>
        GetAsync<PlayerProfile>(apiKey, $"/players/{playerId}", ct);

    public Task RedeemApiHatAsync(string apiKey, CancellationToken ct = default) =>
        SendAsync(apiKey, HttpMethod.Post, "/items/redeemApiHat", null, ct);

    public Task<List<RoleRotationEntry>> GetRoleRotationsAsync(string apiKey, CancellationToken ct = default) =>
        GetAsync<List<RoleRotationEntry>>(apiKey, "/roleRotations", ct);

    public Task<BattlePassSeason> GetBattlePassSeasonAsync(string apiKey, CancellationToken ct = default) =>
        GetAsync<BattlePassSeason>(apiKey, "/battlePass/season", ct);

    public Task<List<ShopOffer>> GetShopOffersAsync(string apiKey, CancellationToken ct = default) =>
        GetAsync<List<ShopOffer>>(apiKey, "/shop/activeOffers", ct);

    public async Task<List<GameAnnouncement>> GetGameAnnouncementsAsync(string apiKey, CancellationToken ct = default)
    {
        var response = await GetAsync<GameAnnouncementsResponse>(apiKey, "/announcements", ct);
        return response.Announcements;
    }

    // ---------- Infraestrutura ----------

    private async Task<T> GetAsync<T>(string apiKey, string path, CancellationToken ct)
    {
        using var response = await SendRawAsync(apiKey, HttpMethod.Get, path, null, ct);
        return await ReadAsync<T>(response, ct);
    }

    private async Task SendAsync(string apiKey, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(apiKey, method, path, body, ct);
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        string apiKey, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bot {apiKey}");

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync(ct);
        string message = content;
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var msg))
            {
                message = msg.ToString();
            }
        }
        catch (JsonException) { /* corpo não é JSON — usa o texto cru */ }

        var statusCode = response.StatusCode;
        response.Dispose();
        throw new WolvesvilleApiException(statusCode, message);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
        return result ?? throw new WolvesvilleApiException(response.StatusCode, "Resposta vazia da API.");
    }
}

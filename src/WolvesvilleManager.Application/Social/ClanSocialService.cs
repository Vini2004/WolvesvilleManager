using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Social;

/// <summary>Anúncios e chat do clã.</summary>
public class ClanSocialService
{
    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;

    public ClanSocialService(ClanKeyResolver resolver, IWolvesvilleClient api)
    {
        _resolver = resolver;
        _api = api;
    }

    public async Task<List<ClanAnnouncement>> GetAnnouncementsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetAnnouncementsAsync(apiKey, reg.ClanId, ct);
    }

    public async Task PostAnnouncementAsync(int clanRegistrationId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new BusinessRuleException("O anúncio não pode ser vazio.");
        if (message.Length > 500)
            throw new BusinessRuleException("O anúncio pode ter no máximo 500 caracteres.");

        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SendAnnouncementAsync(apiKey, reg.ClanId, message.Trim(), ct);
    }

    public async Task<List<ChatMessage>> GetChatAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetChatAsync(apiKey, reg.ClanId, ct);
    }

    public async Task SendChatMessageAsync(int clanRegistrationId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new BusinessRuleException("A mensagem não pode ser vazia.");
        if (message.Length > 500)
            throw new BusinessRuleException("A mensagem pode ter no máximo 500 caracteres.");

        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SendChatMessageAsync(apiKey, reg.ClanId, message.Trim(), ct);
    }

    public async Task<List<LedgerEntry>> GetLedgerAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetLedgerAsync(apiKey, reg.ClanId, ct);
    }

    public async Task<List<ClanLogEntry>> GetLogsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetLogsAsync(apiKey, reg.ClanId, ct);
    }
}

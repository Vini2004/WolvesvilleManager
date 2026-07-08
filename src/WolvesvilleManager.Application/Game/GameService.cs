using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Game;

/// <summary>
/// Conteúdo global do jogo (rotações, battle pass, loja, anúncios oficiais), busca de
/// jogadores e o resgate do chapéu da API. Usa a chave do clã registrado só para autenticar.
/// </summary>
public class GameService
{
    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;

    public GameService(ClanKeyResolver resolver, IWolvesvilleClient api)
    {
        _resolver = resolver;
        _api = api;
    }

    public async Task RedeemApiHatAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.RedeemApiHatAsync(apiKey, ct);
    }

    public async Task<PlayerProfile?> SearchPlayerAsync(int clanRegistrationId, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            throw new BusinessRuleException("Informe pelo menos 3 caracteres do nome do jogador.");

        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.SearchPlayerAsync(apiKey, username.Trim(), ct);
    }

    public async Task<PlayerProfile> GetPlayerAsync(int clanRegistrationId, string playerId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetPlayerAsync(apiKey, playerId, ct);
    }

    public async Task<List<RoleRotationEntry>> GetRoleRotationsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetRoleRotationsAsync(apiKey, ct);
    }

    public async Task<BattlePassSeason> GetBattlePassSeasonAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetBattlePassSeasonAsync(apiKey, ct);
    }

    public async Task<List<ShopOffer>> GetShopOffersAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetShopOffersAsync(apiKey, ct);
    }

    public async Task<List<GameAnnouncement>> GetGameAnnouncementsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetGameAnnouncementsAsync(apiKey, ct);
    }
}

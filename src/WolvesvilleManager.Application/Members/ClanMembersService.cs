using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Members;

/// <summary>Casos de uso de membros de um clã registrado.</summary>
public class ClanMembersService
{
    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;

    public ClanMembersService(ClanKeyResolver resolver, IWolvesvilleClient api)
    {
        _resolver = resolver;
        _api = api;
    }

    /// <summary>Listagem detalhada (inclui participação em missões — a chave registrada é clan bot).</summary>
    public async Task<List<ClanMember>> ListAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetMembersDetailedAsync(apiKey, reg.ClanId, ct);
    }

    public async Task SetQuestParticipationAsync(
        int clanRegistrationId, string playerId, bool participate, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SetMemberQuestParticipationAsync(apiKey, reg.ClanId, playerId, participate, ct);
    }

    public async Task SetAllQuestParticipationAsync(
        int clanRegistrationId, bool participate, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SetAllMembersQuestParticipationAsync(apiKey, reg.ClanId, participate, ct);
    }

    public async Task KickAsync(int clanRegistrationId, string playerId, string? reason, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.KickMemberAsync(apiKey, reg.ClanId, playerId, reason, ct);
    }

    public async Task BlockAsync(int clanRegistrationId, string playerId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.BlockMemberAsync(apiKey, reg.ClanId, playerId, ct);
    }

    public async Task UnblockAsync(int clanRegistrationId, string playerId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.UnblockMemberAsync(apiKey, reg.ClanId, playerId, ct);
    }

    public async Task<List<BlocklistEntry>> GetBlocklistAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetBlocklistAsync(apiKey, reg.ClanId, ct);
    }
}

using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Members;

/// <summary>Ganho de XP de um membro no período (baseline = snapshot mais antigo da janela).</summary>
public record XpReportEntry(string PlayerId, string Username, long CurrentXp, long? BaselineXp, long? GainedXp);

/// <summary>SinceUtc = data do snapshot usado como base; null quando ainda não há histórico.</summary>
public record XpReport(DateTime? SinceUtc, List<XpReportEntry> Entries);

/// <summary>Casos de uso de membros de um clã registrado.</summary>
public class ClanMembersService
{
    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;
    private readonly IAppDbContext _db;

    public ClanMembersService(ClanKeyResolver resolver, IWolvesvilleClient api, IAppDbContext db)
    {
        _resolver = resolver;
        _api = api;
        _db = db;
    }

    /// <summary>
    /// Listagem detalhada (inclui participação em missões — a chave registrada é clan bot),
    /// ordenada: líder, co-líderes e depois os demais por XP contribuído.
    /// </summary>
    public async Task<List<ClanMember>> ListAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);

        // Membros e info do clã em paralelo — a listagem espera o mais lento dos dois, não a soma.
        var membersTask = _api.GetMembersDetailedAsync(apiKey, reg.ClanId, ct);
        var infoTask = _api.GetClanInfoAsync(apiKey, reg.ClanId, ct);
        await Task.WhenAll(membersTask, infoTask);
        var members = membersTask.Result;
        var info = infoTask.Result;

        foreach (var m in members)
            m.IsLeader = m.PlayerId == info.LeaderId;

        return members
            .OrderByDescending(m => m.IsLeader)
            .ThenByDescending(m => m.IsCoLeader)
            .ThenByDescending(m => m.Xp)
            .ToList();
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

    public async Task SetFlairAsync(int clanRegistrationId, string playerId, string flair, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(flair))
            throw new BusinessRuleException("Informe o flair.");
        if (flair.Length > 50)
            throw new BusinessRuleException("O flair pode ter no máximo 50 caracteres.");

        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SetMemberFlairAsync(apiKey, reg.ClanId, playerId, flair.Trim(), ct);
    }

    public async Task<List<BlocklistEntry>> GetBlocklistAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetBlocklistAsync(apiKey, reg.ClanId, ct);
    }

    /// <summary>
    /// Ganho de XP por membro nos últimos <paramref name="days"/> dias, comparando o XP atual
    /// com o snapshot diário mais antigo dentro da janela. Membros sem snapshot (entraram há
    /// pouco ou histórico ainda curto) vêm com baseline nulo.
    /// </summary>
    public async Task<XpReport> GetXpReportAsync(int clanRegistrationId, int days, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        var members = await _api.GetMembersAsync(apiKey, reg.ClanId, ct);

        var windowStart = DateTime.UtcNow.AddDays(-days);
        var snapshots = await _db.MemberXpSnapshots
            .Where(s => s.ClanRegistrationId == clanRegistrationId && s.TakenAtUtc >= windowStart)
            .ToListAsync(ct);

        // Baseline de cada membro: o snapshot mais antigo dentro da janela.
        var baselines = snapshots
            .GroupBy(s => s.PlayerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.TakenAtUtc).First());

        var entries = members
            .Select(m =>
            {
                var baseline = baselines.GetValueOrDefault(m.PlayerId);
                return new XpReportEntry(
                    m.PlayerId, m.Username, m.Xp,
                    baseline?.Xp,
                    baseline is null ? null : m.Xp - baseline.Xp);
            })
            .OrderByDescending(e => e.GainedXp ?? -1)
            .ToList();

        var since = baselines.Count > 0 ? baselines.Values.Min(s => s.TakenAtUtc) : (DateTime?)null;
        return new XpReport(since, entries);
    }
}

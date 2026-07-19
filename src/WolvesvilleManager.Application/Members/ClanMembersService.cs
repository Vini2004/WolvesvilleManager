using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Members;

/// <summary>Ganho de XP de um membro no período (baseline = snapshot mais antigo da janela).</summary>
public record XpReportEntry(string PlayerId, string Username, long CurrentXp, long? BaselineXp, long? GainedXp);

/// <summary>
/// SinceUtc/UntilUtc = datas dos snapshots realmente usados como início/fim (podem não bater
/// exatamente com as datas escolhidas, já que o snapshot é diário); nulos quando ainda não há
/// histórico suficiente na janela.
/// </summary>
public record XpReport(DateTime? SinceUtc, DateTime? UntilUtc, List<XpReportEntry> Entries);

/// <summary>Casos de uso de membros de um clã registrado.</summary>
public class ClanMembersService
{
    /// <summary>Maior intervalo entre data inicial e final aceito no relatório de XP.</summary>
    public const int MaxXpReportRangeDays = 31;

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
    /// Ganho de XP por membro entre duas datas escolhidas pelo admin. O início usa o snapshot
    /// diário mais antigo a partir de <paramref name="startUtc"/>; o fim usa o XP atual (ao
    /// vivo) quando <paramref name="endUtc"/> cai em hoje ou depois, ou o snapshot mais recente
    /// até lá quando é uma data passada. Membros sem snapshot no início da janela (entraram há
    /// pouco ou histórico ainda curto) vêm com baseline nulo.
    /// </summary>
    public async Task<XpReport> GetXpReportAsync(
        int clanRegistrationId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        if (startUtc > endUtc)
            throw new BusinessRuleException("A data inicial não pode ser depois da data final.");
        if ((endUtc - startUtc).TotalDays > MaxXpReportRangeDays)
            throw new BusinessRuleException($"O período entre as datas não pode passar de {MaxXpReportRangeDays} dias.");

        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        var members = await _api.GetMembersAsync(apiKey, reg.ClanId, ct);

        var now = DateTime.UtcNow;
        var effectiveEnd = endUtc > now ? now : endUtc;
        var useLiveXpForEnd = endUtc.Date >= now.Date;

        var snapshots = await _db.MemberXpSnapshots
            .Where(s => s.ClanRegistrationId == clanRegistrationId && s.TakenAtUtc >= startUtc && s.TakenAtUtc <= effectiveEnd)
            .ToListAsync(ct);

        // Por membro: lista ordenada por data — o primeiro é o início da janela, o último é o fim.
        var byPlayer = snapshots
            .GroupBy(s => s.PlayerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.TakenAtUtc).ToList());

        var entries = members
            .Select(m =>
            {
                var list = byPlayer.GetValueOrDefault(m.PlayerId);
                long? startXp = list is { Count: > 0 } ? list[0].Xp : null;
                long? endXp = useLiveXpForEnd ? m.Xp : (list is { Count: > 0 } ? list[^1].Xp : null);
                return new XpReportEntry(
                    m.PlayerId, m.Username, m.Xp, startXp,
                    startXp is null || endXp is null ? null : endXp - startXp);
            })
            .OrderByDescending(e => e.GainedXp ?? long.MinValue)
            .ToList();

        DateTime? sinceUtc = byPlayer.Count > 0 ? byPlayer.Values.Min(l => l[0].TakenAtUtc) : null;
        DateTime? untilUtc = useLiveXpForEnd
            ? now
            : (byPlayer.Count > 0 ? byPlayer.Values.Max(l => l[^1].TakenAtUtc) : null);

        return new XpReport(sinceUtc, untilUtc, entries);
    }
}

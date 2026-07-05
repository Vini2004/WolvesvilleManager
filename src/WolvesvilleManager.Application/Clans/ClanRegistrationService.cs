using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Clans;

/// <summary>Registro e manutenção dos clãs gerenciados pela aplicação.</summary>
public class ClanRegistrationService
{
    private readonly IAppDbContext _db;
    private readonly IWolvesvilleClient _api;
    private readonly IApiKeyProtector _protector;

    public ClanRegistrationService(IAppDbContext db, IWolvesvilleClient api, IApiKeyProtector protector)
    {
        _db = db;
        _api = api;
        _protector = protector;
    }

    /// <summary>Clãs autorizados para uma chave — usado no fluxo de registro para o usuário escolher.</summary>
    public Task<List<ClanInfo>> GetAuthorizedClansAsync(string apiKey, CancellationToken ct = default) =>
        _api.GetAuthorizedClansAsync(apiKey, ct);

    /// <summary>
    /// Registra (ou atualiza) um clã. A autorização é revalidada no servidor:
    /// só salva se a chave realmente for clan bot do clã informado — um POST forjado
    /// com combinação chave/clã inválida é rejeitado.
    /// </summary>
    public async Task<RegisteredClanDto> RegisterAsync(string apiKey, string clanId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BusinessRuleException("Informe a chave de API do bot.");
        if (string.IsNullOrWhiteSpace(clanId))
            throw new BusinessRuleException("Informe o ID do clã.");

        var authorized = await _api.GetAuthorizedClansAsync(apiKey, ct);
        var clan = authorized.FirstOrDefault(c => c.Id == clanId)
            ?? throw new BusinessRuleException(
                "Esta chave de API não está autorizada como clan bot do clã informado.");

        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.ClanId == clanId, ct);
        if (reg is null)
        {
            reg = new ClanRegistration { ClanId = clanId };
            _db.ClanRegistrations.Add(reg);
        }

        reg.ClanName = clan.Name;
        reg.ClanTag = clan.Tag;
        reg.ProtectedApiKey = _protector.Protect(apiKey);

        await _db.SaveChangesAsync(ct);
        return ToDto(reg);
    }

    public async Task<List<RegisteredClanDto>> ListAsync(CancellationToken ct = default) =>
        await _db.ClanRegistrations
            .AsNoTracking()
            .OrderBy(c => c.ClanName)
            .Select(c => new RegisteredClanDto(c.Id, c.ClanId, c.ClanName, c.ClanTag, c.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"Clã registrado #{id} não encontrado.");

        _db.ClanRegistrations.Remove(reg);
        await _db.SaveChangesAsync(ct);
    }

    private static RegisteredClanDto ToDto(ClanRegistration c) =>
        new(c.Id, c.ClanId, c.ClanName, c.ClanTag, c.CreatedAtUtc);
}

/// <summary>Clã registrado, sem expor a chave (nem criptografada).</summary>
public record RegisteredClanDto(int Id, string ClanId, string ClanName, string? ClanTag, DateTime CreatedAtUtc);

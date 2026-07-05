using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Application.Common;

/// <summary>
/// Carrega um clã registrado e descriptografa sua chave de API — passo comum a
/// todos os casos de uso que falam com a API do Wolvesville.
/// </summary>
public class ClanKeyResolver
{
    private readonly IAppDbContext _db;
    private readonly IApiKeyProtector _protector;

    public ClanKeyResolver(IAppDbContext db, IApiKeyProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    public async Task<(ClanRegistration Registration, string ApiKey)> ResolveAsync(
        int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        return (reg, _protector.Unprotect(reg.ProtectedApiKey, reg.Id));
    }
}

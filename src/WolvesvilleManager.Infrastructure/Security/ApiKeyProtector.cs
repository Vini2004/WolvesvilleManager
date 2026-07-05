using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using WolvesvilleManager.Domain.Exceptions;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Infrastructure.Security;

/// <summary>
/// Criptografa chaves de API com ASP.NET Data Protection. O keyring precisa estar
/// persistido (configurado no Program.cs) para sobreviver a redeploys.
/// </summary>
public class ApiKeyProtector : IApiKeyProtector
{
    private const string Purpose = "WolvesvilleManager.ApiKeys";

    private readonly IDataProtector _protector;

    public ApiKeyProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string apiKey) => _protector.Protect(apiKey);

    public string Unprotect(string protectedApiKey, int clanRegistrationId)
    {
        try
        {
            return _protector.Unprotect(protectedApiKey);
        }
        catch (CryptographicException ex)
        {
            throw new ApiKeyUnprotectException(clanRegistrationId, ex);
        }
    }
}

namespace WolvesvilleManager.Domain.Interfaces;

/// <summary>Criptografa/descriptografa chaves de API para armazenamento em repouso.</summary>
public interface IApiKeyProtector
{
    string Protect(string apiKey);

    /// <summary>
    /// Descriptografa a chave salva. Lança
    /// <see cref="Exceptions.ApiKeyUnprotectException"/> quando o payload não pode mais
    /// ser lido (chaves de proteção mudaram) — o chamador deve pedir re-registro do clã.
    /// </summary>
    string Unprotect(string protectedApiKey, int clanRegistrationId);
}

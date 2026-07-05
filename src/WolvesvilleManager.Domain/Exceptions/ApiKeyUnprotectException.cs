namespace WolvesvilleManager.Domain.Exceptions;

/// <summary>
/// A chave de API salva no banco não pôde ser descriptografada (chaves de Data Protection
/// mudaram, ex.: troca de máquina sem persistir o keyring). O clã precisa ser re-registrado.
/// </summary>
public class ApiKeyUnprotectException : Exception
{
    public int ClanRegistrationId { get; }

    public ApiKeyUnprotectException(int clanRegistrationId, Exception inner)
        : base($"Não foi possível descriptografar a chave de API do clã registrado #{clanRegistrationId}. " +
               "Registre o clã novamente.", inner)
    {
        ClanRegistrationId = clanRegistrationId;
    }
}

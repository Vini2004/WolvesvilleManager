using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Social;

/// <summary>Configuração da mensagem automática de boas-vindas (aba Chat).</summary>
public record WelcomeSettingsDto(bool Enabled, string Template);

/// <summary>Anúncios e chat do clã.</summary>
public class ClanSocialService
{
    /// <summary>
    /// Texto padrão da mensagem de boas-vindas quando o clã nunca personalizou o template.
    /// "{mention}" é trocado por "@" + o nick de quem entrou.
    /// </summary>
    public const string DefaultWelcomeMessageTemplate =
        "{mention} Seja Bem-Vindo!! 🥳\n" +
        "Para dar continuidade a sua entrada no clã, doe a taxa de entrada (250 moedas) e entre " +
        "no nosso grupo do whatsapp no fixado para dar continuidade na sua entrada.";

    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;
    private readonly IAppDbContext _db;

    public ClanSocialService(ClanKeyResolver resolver, IWolvesvilleClient api, IAppDbContext db)
    {
        _resolver = resolver;
        _api = api;
        _db = db;
    }

    /// <summary>Aba Chat: configuração atual da mensagem de boas-vindas (liga/desliga + texto).</summary>
    public async Task<WelcomeSettingsDto> GetWelcomeSettingsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");
        return new WelcomeSettingsDto(reg.WelcomeMessageEnabled, reg.WelcomeMessageTemplate ?? DefaultWelcomeMessageTemplate);
    }

    /// <summary>Aba Chat: liga/desliga e/ou personaliza o texto da mensagem de boas-vindas.</summary>
    public async Task SetWelcomeSettingsAsync(
        int clanRegistrationId, bool enabled, string template, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new BusinessRuleException("A mensagem de boas-vindas não pode ser vazia.");
        if (template.Length > 500)
            throw new BusinessRuleException("A mensagem de boas-vindas pode ter no máximo 500 caracteres.");
        if (!template.Contains("{mention}"))
            throw new BusinessRuleException(
                "A mensagem precisa conter \"{mention}\" no lugar em que a pessoa deve ser marcada.");

        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        reg.WelcomeMessageEnabled = enabled;
        reg.WelcomeMessageTemplate = template.Trim();
        await _db.SaveChangesAsync(ct);
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

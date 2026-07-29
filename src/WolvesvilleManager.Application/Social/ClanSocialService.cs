using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Social;

/// <summary>
/// Configuração da mensagem automática de boas-vindas (aba Chat). <paramref name="SendTime1"/> e
/// <paramref name="SendTime2"/> ("HH:mm" ou nulo) são os horários em que as boas-vindas represadas
/// são liberadas; ambos nulos = sauda assim que o app acordar depois da entrada.
/// </summary>
public record WelcomeSettingsDto(bool Enabled, string Template, string? SendTime1, string? SendTime2);

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

    /// <summary>Fuso em que os horários de liberação das boas-vindas são interpretados.</summary>
    public const string WelcomeTimeZoneId = "America/Sao_Paulo";

    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;
    private readonly IAppDbContext _db;
    private readonly ICronTriggerGateway _cron;
    private readonly ILogger<ClanSocialService> _logger;

    public ClanSocialService(
        ClanKeyResolver resolver, IWolvesvilleClient api, IAppDbContext db,
        ICronTriggerGateway cron, ILogger<ClanSocialService> logger)
    {
        _resolver = resolver;
        _api = api;
        _db = db;
        _cron = cron;
        _logger = logger;
    }

    /// <summary>Aba Chat: configuração atual da mensagem de boas-vindas (liga/desliga + texto + horários).</summary>
    public async Task<WelcomeSettingsDto> GetWelcomeSettingsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");
        return new WelcomeSettingsDto(
            reg.WelcomeMessageEnabled,
            reg.WelcomeMessageTemplate ?? DefaultWelcomeMessageTemplate,
            FormatTime(reg.WelcomeSendTime1),
            FormatTime(reg.WelcomeSendTime2));
    }

    /// <summary>
    /// Aba Chat: liga/desliga, personaliza o texto e define os horários de liberação das boas-vindas.
    /// Ao salvar horários, (re)cria o ping externo no cron-job.org que acorda o app nesses momentos.
    /// </summary>
    public async Task SetWelcomeSettingsAsync(
        int clanRegistrationId, bool enabled, string template, string? sendTime1, string? sendTime2,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new BusinessRuleException("A mensagem de boas-vindas não pode ser vazia.");
        if (template.Length > 500)
            throw new BusinessRuleException("A mensagem de boas-vindas pode ter no máximo 500 caracteres.");
        if (!template.Contains("{mention}"))
            throw new BusinessRuleException(
                "A mensagem precisa conter \"{mention}\" no lugar em que a pessoa deve ser marcada.");

        var time1 = ParseTimeOrNull(sendTime1);
        var time2 = ParseTimeOrNull(sendTime2);

        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        reg.WelcomeMessageEnabled = enabled;
        reg.WelcomeMessageTemplate = template.Trim();
        reg.WelcomeSendTime1 = time1;
        reg.WelcomeSendTime2 = time2;
        await _db.SaveChangesAsync(ct);

        await SyncWelcomePingAsync(reg, ct);
    }

    /// <summary>
    /// (Re)cria/atualiza/apaga o ping externo das boas-vindas conforme os horários e o liga/desliga
    /// da tarefa. Nunca quebra o salvamento: se o cron-job.org falhar, loga e segue.
    /// </summary>
    private async Task SyncWelcomePingAsync(ClanRegistration reg, CancellationToken ct)
    {
        if (!_cron.Enabled) return;
        var times = new List<TimeSpan>();
        if (reg.WelcomeSendTime1 is { } t1) times.Add(t1);
        if (reg.WelcomeSendTime2 is { } t2) times.Add(t2);
        try
        {
            var jobId = await _cron.SyncWelcomePingAsync(
                new WelcomePingTrigger(reg.Id, reg.WelcomePingJobId, times, WelcomeTimeZoneId, reg.WelcomeMessageEnabled),
                ct);
            if (jobId != reg.WelcomePingJobId)
            {
                reg.WelcomePingJobId = jobId;
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível sincronizar o ping de boas-vindas do clã #{Id}.", reg.Id);
        }
    }

    private static string? FormatTime(TimeSpan? t) => t?.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static TimeSpan? ParseTimeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (TimeSpan.TryParse(value.Trim(), CultureInfo.InvariantCulture, out var ts)
            && ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1))
            return new TimeSpan(ts.Hours, ts.Minutes, 0); // zera segundos — só interessa HH:mm
        throw new BusinessRuleException($"Horário inválido: \"{value}\". Use HH:mm, ex.: 09:00.");
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

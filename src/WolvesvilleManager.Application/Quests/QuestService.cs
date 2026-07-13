using System.Net;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Exceptions;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Application.Quests;

/// <summary>Casos de uso de missões de um clã registrado.</summary>
public class QuestService
{
    private readonly ClanKeyResolver _resolver;
    private readonly IWolvesvilleClient _api;

    public QuestService(ClanKeyResolver resolver, IWolvesvilleClient api)
    {
        _resolver = resolver;
        _api = api;
    }

    /// <summary>Visão geral: missão ativa, disponíveis (com votos) e saldo do clã.</summary>
    public async Task<QuestsOverviewDto> GetOverviewAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);

        // A API do Wolvesville às vezes responde 404 (em vez de 204/lista vazia) quando o
        // clã não tem missão ativa ou nenhuma disponível — tratamos como "nada no momento".
        ActiveQuest? active = null;
        try
        {
            active = await _api.GetActiveQuestAsync(apiKey, reg.ClanId, ct);
        }
        catch (WolvesvilleApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

        var available = new List<ClanQuest>();
        try
        {
            available = await _api.GetAvailableQuestsAsync(apiKey, reg.ClanId, ct);
        }
        catch (WolvesvilleApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

        var votes = new Dictionary<string, int>();
        try
        {
            votes = QuestVoteCounter.CountVotes(await _api.GetQuestVotesAsync(apiKey, reg.ClanId, ct));
        }
        catch (WolvesvilleApiException) { /* endpoint não documentado — votos são opcionais na tela */ }

        long? gold = null, gems = null;
        try
        {
            var info = await _api.GetClanInfoAsync(apiKey, reg.ClanId, ct);
            gold = info.Gold;
            gems = info.Gems;
        }
        catch (WolvesvilleApiException) { /* saldo é opcional na tela */ }

        var availableWithVotes = available
            .Select(q => new AvailableQuestDto(q, votes.GetValueOrDefault(q.Id)))
            .ToList();

        return new QuestsOverviewDto(active, availableWithVotes, gold, gems);
    }

    public async Task<List<QuestHistoryEntry>> GetHistoryAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        return await _api.GetQuestHistoryAsync(apiKey, reg.ClanId, ct);
    }

    /// <summary>Catálogo completo de missões (para escolher uma missão fixa numa automação).</summary>
    public async Task<List<ClanQuest>> GetAllQuestsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (_, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        var all = await _api.GetAllQuestsAsync(apiKey, ct);
        return all
            .OrderBy(q => q.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Inicia (compra) uma missão. Gasta ouro/gemas do clã!</summary>
    public async Task ClaimAsync(int clanRegistrationId, string questId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(questId))
            throw new BusinessRuleException("Informe a missão a iniciar.");

        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.ClaimQuestAsync(apiKey, reg.ClanId, questId, ct);
    }

    /// <summary>Embaralha as missões disponíveis. Gasta ouro do clã!</summary>
    public async Task ShuffleAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.ShuffleQuestsAsync(apiKey, reg.ClanId, ct);
    }

    /// <summary>Pula o tempo de espera da missão ativa. Gasta gemas do clã!</summary>
    public async Task SkipWaitingTimeAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.SkipQuestWaitingTimeAsync(apiKey, reg.ClanId, ct);
    }

    /// <summary>Resgata tempo extra da missão ativa. Gasta ouro do clã!</summary>
    public async Task ClaimExtraTimeAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.ClaimQuestExtraTimeAsync(apiKey, reg.ClanId, ct);
    }

    public async Task CancelAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var (reg, apiKey) = await _resolver.ResolveAsync(clanRegistrationId, ct);
        await _api.CancelActiveQuestAsync(apiKey, reg.ClanId, ct);
    }
}

public record AvailableQuestDto(ClanQuest Quest, int Votes);

public record QuestsOverviewDto(
    ActiveQuest? Active,
    List<AvailableQuestDto> Available,
    long? Gold,
    long? Gems);

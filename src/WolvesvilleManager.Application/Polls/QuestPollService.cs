using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Application.Polls;

/// <summary>Missão candidata no formulário, com a contagem atual de votos.</summary>
public record PollQuestDto(string QuestId, string Name, string? ImageUrl, bool Gems, int Votes);

/// <summary>O que a página pública vê: nome do clã, candidatas e o voto deste navegador.</summary>
public record PollDto(string ClanName, string? ClanTag, List<PollQuestDto> Quests, string? VotedQuestId);

/// <summary>O que a aba admin vê: o link e a apuração.</summary>
public record PollAdminDto(string Token, List<PollQuestDto> Quests, int TotalVotes);

/// <summary>
/// Formulário público de votação de missões. O token do link é a única credencial da
/// página pública; o VoterId (gerado pelo navegador) limita a um voto por navegador.
/// </summary>
public class QuestPollService
{
    private readonly IAppDbContext _db;
    private readonly IWolvesvilleClient _api;
    private readonly IApiKeyProtector _protector;

    public QuestPollService(IAppDbContext db, IWolvesvilleClient api, IApiKeyProtector protector)
    {
        _db = db;
        _api = api;
        _protector = protector;
    }

    /// <summary>Aba admin: garante que o clã tem um token (gera no primeiro acesso) e apura os votos.</summary>
    public async Task<PollAdminDto> GetAdminAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        if (string.IsNullOrEmpty(reg.PollToken))
        {
            reg.PollToken = RandomNumberGenerator.GetHexString(32, lowercase: true);
            await _db.SaveChangesAsync(ct);
        }

        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
        var quests = await BuildQuestsAsync(reg.Id, apiKey, reg.ClanId, ct);
        var total = await _db.QuestPollVotes.CountAsync(v => v.ClanRegistrationId == reg.Id, ct);
        return new PollAdminDto(reg.PollToken, quests, total);
    }

    /// <summary>Aba admin: zera a urna do clã.</summary>
    public async Task ResetAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        await _db.QuestPollVotes
            .Where(v => v.ClanRegistrationId == clanRegistrationId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>Página pública: candidatas + o voto já registrado por este navegador.</summary>
    public async Task<PollDto> GetPublicAsync(string token, string? voterId, CancellationToken ct = default)
    {
        var reg = await ResolveByTokenAsync(token, ct);
        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
        var quests = await BuildQuestsAsync(reg.Id, apiKey, reg.ClanId, ct);

        string? voted = null;
        if (!string.IsNullOrEmpty(voterId))
            voted = await _db.QuestPollVotes
                .Where(v => v.ClanRegistrationId == reg.Id && v.VoterId == voterId)
                .Select(v => v.QuestId)
                .FirstOrDefaultAsync(ct);

        return new PollDto(reg.ClanName, reg.ClanTag, quests, voted);
    }

    /// <summary>Página pública: registra (ou troca) o voto deste navegador.</summary>
    public async Task VoteAsync(string token, string questId, string voterId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(voterId) || voterId.Length > 64)
            throw new BusinessRuleException("Identificador de votante inválido.");
        if (string.IsNullOrWhiteSpace(questId))
            throw new BusinessRuleException("Escolha uma missão para votar.");

        var reg = await ResolveByTokenAsync(token, ct);

        // Só aceita voto em missão que está de fato disponível agora (a lista rotaciona).
        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
        var available = await _api.GetAvailableQuestsAsync(apiKey, reg.ClanId, ct);
        if (!available.Any(q => q.Id == questId))
            throw new BusinessRuleException("Essa missão não está mais disponível — recarregue a página.");

        var vote = await _db.QuestPollVotes
            .FirstOrDefaultAsync(v => v.ClanRegistrationId == reg.Id && v.VoterId == voterId, ct);
        if (vote is null)
            _db.QuestPollVotes.Add(new QuestPollVote { ClanRegistrationId = reg.Id, QuestId = questId, VoterId = voterId });
        else
            vote.QuestId = questId;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Página pública: embaralha as missões disponíveis (gasta ouro do clã, igual à aba admin)
    /// e zera a urna — os votos antigos eram para as missões que acabaram de sair de cartaz.
    /// </summary>
    public async Task<PollDto> ShuffleAsync(string token, CancellationToken ct = default)
    {
        var reg = await ResolveByTokenAsync(token, ct);
        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);

        await _api.ShuffleQuestsAsync(apiKey, reg.ClanId, ct);
        await _db.QuestPollVotes
            .Where(v => v.ClanRegistrationId == reg.Id)
            .ExecuteDeleteAsync(ct);

        var quests = await BuildQuestsAsync(reg.Id, apiKey, reg.ClanId, ct);
        return new PollDto(reg.ClanName, reg.ClanTag, quests, null);
    }

    private async Task<ClanRegistration> ResolveByTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64)
            throw new NotFoundException("Votação não encontrada.");
        return await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.PollToken == token, ct)
            ?? throw new NotFoundException("Votação não encontrada.");
    }

    private async Task<List<PollQuestDto>> BuildQuestsAsync(int clanRegistrationId, string apiKey, string clanId, CancellationToken ct)
    {
        var available = await _api.GetAvailableQuestsAsync(apiKey, clanId, ct);
        var counts = await _db.QuestPollVotes
            .Where(v => v.ClanRegistrationId == clanRegistrationId)
            .GroupBy(v => v.QuestId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        // Votos em missões que saíram de cartaz simplesmente não aparecem (nem contam na apuração).
        return available
            .Select(q => new PollQuestDto(q.Id, q.DisplayName, q.PromoImageUrl, q.PurchasableWithGems, counts.GetValueOrDefault(q.Id)))
            .ToList();
    }
}

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Application.Polls;

/// <summary>Missão candidata no formulário, com a contagem atual de votos.</summary>
public record PollQuestDto(string QuestId, string Name, string? ImageUrl, bool Gems, int Votes);

/// <summary>O que a página pública vê: nome do clã, candidatas, o voto deste nick e o prazo.</summary>
public record PollDto(
    string ClanName, string? ClanTag, List<PollQuestDto> Quests, string? VotedQuestId,
    DateTime? ExpiresAtUtc, bool IsClosed);

/// <summary>Uma rodada de votação já decidida (a automação já claimou ou embaralhou).</summary>
public record PollHistoryEntryDto(string QuestName, int Votes, bool WasShuffle, DateTime DecidedAtUtc);

/// <summary>O que a aba admin vê: o link, a apuração, o prazo e o histórico de rodadas anteriores.</summary>
public record PollAdminDto(
    string Token, List<PollQuestDto> Quests, int TotalVotes, DateTime? ExpiresAtUtc, bool IsClosed,
    List<PollHistoryEntryDto> History, string? CloseCronExpression, string? CloseTimeZoneId);

/// <summary>Durações de prazo que a aba admin oferece — nunca "para sempre".</summary>
public enum PollDuration { SixHours, TwelveHours, OneDay, ThreeDays, SevenDays }

/// <summary>
/// Formulário público de votação de missões. O token do link é a única credencial da
/// página pública; o nick digitado limita a um voto por nick (case-insensitive) — não
/// por navegador, então trocar de navegador ou usar aba anônima não abre voto extra.
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

    /// <summary>Aba admin: garante que o clã tem um token (gera no primeiro acesso, com prazo padrão de 7 dias) e apura os votos.</summary>
    public async Task<PollAdminDto> GetAdminAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        if (string.IsNullOrEmpty(reg.PollToken))
        {
            reg.PollToken = RandomNumberGenerator.GetHexString(32, lowercase: true);
            reg.PollExpiresAtUtc = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync(ct);
        }

        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
        var quests = await BuildQuestsAsync(reg.Id, apiKey, reg.ClanId, ct);
        var total = await _db.QuestPollVotes.CountAsync(v => v.ClanRegistrationId == reg.Id, ct);
        var history = await _db.QuestPollResults
            .Where(r => r.ClanRegistrationId == reg.Id)
            .OrderByDescending(r => r.DecidedAtUtc)
            .Take(20)
            .Select(r => new PollHistoryEntryDto(r.QuestName, r.Votes, r.WasShuffle, r.DecidedAtUtc))
            .ToListAsync(ct);
        return new PollAdminDto(
            reg.PollToken, quests, total, reg.PollExpiresAtUtc, IsClosed(reg), history,
            reg.PollCloseCronExpression, reg.PollCloseTimeZoneId);
    }

    /// <summary>Aba admin: zera a urna do clã.</summary>
    public async Task ResetAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        await _db.QuestPollVotes
            .Where(v => v.ClanRegistrationId == clanRegistrationId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Aba admin: define/estende o prazo a partir de agora (prazo manual, não se repete).
    /// Desliga um prazo recorrente configurado antes, já que é uma escolha explícita do admin.
    /// A votação nunca fica aberta para sempre — não existe opção de prazo indefinido.
    /// </summary>
    public async Task<DateTime> SetExpirationAsync(int clanRegistrationId, PollDuration duration, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        var hours = duration switch
        {
            PollDuration.SixHours => 6,
            PollDuration.TwelveHours => 12,
            PollDuration.OneDay => 24,
            PollDuration.ThreeDays => 72,
            PollDuration.SevenDays => 168,
            _ => throw new BusinessRuleException("Duração inválida."),
        };
        reg.PollExpiresAtUtc = DateTime.UtcNow.AddHours(hours);
        reg.PollCloseCronExpression = null;
        reg.PollCloseTimeZoneId = null;
        await _db.SaveChangesAsync(ct);
        return reg.PollExpiresAtUtc.Value;
    }

    /// <summary>
    /// Aba admin: prazo que se repete sozinho (ex.: toda segunda e quinta às 11h). Cada vez que
    /// a automação "mais votada do formulário" decide a rodada, o prazo pula para a próxima
    /// ocorrência automaticamente — não precisa de um segundo gatilho externo para "fechar",
    /// só o cálculo da próxima ocorrência do cron a cada rodada decidida.
    /// </summary>
    public async Task<DateTime> SetRecurringCloseAsync(
        int clanRegistrationId, string cronExpression, string timeZoneId, CancellationToken ct = default)
    {
        if (!CronScheduleCalculator.IsValidCron(cronExpression))
            throw new BusinessRuleException("Horário inválido.");
        if (!CronScheduleCalculator.IsValidTimeZone(timeZoneId))
            throw new BusinessRuleException("Fuso horário inválido.");

        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        var next = CronScheduleCalculator.GetNextOccurrenceUtc(cronExpression, timeZoneId, DateTime.UtcNow)
            ?? throw new BusinessRuleException("Não foi possível calcular a próxima ocorrência desse horário.");

        reg.PollCloseCronExpression = cronExpression;
        reg.PollCloseTimeZoneId = timeZoneId;
        reg.PollExpiresAtUtc = next;
        await _db.SaveChangesAsync(ct);
        return next;
    }

    /// <summary>Aba admin: volta o prazo recorrente para manual (mantém o prazo atual até ser trocado).</summary>
    public async Task ClearRecurringCloseAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");
        reg.PollCloseCronExpression = null;
        reg.PollCloseTimeZoneId = null;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Página pública: candidatas + o voto já registrado por esse nick (se informado).</summary>
    public async Task<PollDto> GetPublicAsync(string token, string? nickname, CancellationToken ct = default)
    {
        var reg = await ResolveByTokenAsync(token, ct);
        var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
        var quests = await BuildQuestsAsync(reg.Id, apiKey, reg.ClanId, ct);

        string? voted = null;
        var normalized = NormalizeNickname(nickname);
        if (normalized is not null)
            voted = await _db.QuestPollVotes
                .Where(v => v.ClanRegistrationId == reg.Id && v.Nickname.ToLower() == normalized)
                .Select(v => v.QuestId)
                .FirstOrDefaultAsync(ct);

        return new PollDto(reg.ClanName, reg.ClanTag, quests, voted, reg.PollExpiresAtUtc, IsClosed(reg));
    }

    /// <summary>Página pública: registra (ou troca) o voto desse nick.</summary>
    public async Task VoteAsync(string token, string questId, string nickname, CancellationToken ct = default)
    {
        var normalized = NormalizeNickname(nickname)
            ?? throw new BusinessRuleException("Digite seu nick do Wolvesville para votar.");
        if (nickname.Trim().Length > 32)
            throw new BusinessRuleException("Nick muito longo (máximo 32 caracteres).");
        if (string.IsNullOrWhiteSpace(questId))
            throw new BusinessRuleException("Escolha uma missão para votar.");

        var reg = await ResolveByTokenAsync(token, ct);
        if (IsClosed(reg))
            throw new BusinessRuleException("A votação encerrou. Peça para o administrador do clã abrir um novo prazo.");

        // "Embaralhar" é sempre uma opção válida; missão de verdade precisa estar disponível agora
        // (a lista rotaciona).
        if (questId != QuestPollVote.ShuffleOptionId)
        {
            var apiKey = _protector.Unprotect(reg.ProtectedApiKey, reg.Id);
            var available = await _api.GetAvailableQuestsAsync(apiKey, reg.ClanId, ct);
            if (!available.Any(q => q.Id == questId))
                throw new BusinessRuleException("Essa missão não está mais disponível — recarregue a página.");
        }

        // Comparação case-insensitive: "Fulano" e "fulano" são o mesmo voto.
        var vote = await _db.QuestPollVotes
            .FirstOrDefaultAsync(v => v.ClanRegistrationId == reg.Id && v.Nickname.ToLower() == normalized, ct);
        if (vote is null)
            _db.QuestPollVotes.Add(new QuestPollVote { ClanRegistrationId = reg.Id, QuestId = questId, Nickname = nickname.Trim() });
        else
            vote.QuestId = questId;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Nulo se vazio/só espaços; senão o nick aparado e em minúsculas, para comparação.</summary>
    private static string? NormalizeNickname(string? nickname)
    {
        var trimmed = nickname?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLower();
    }

    private static bool IsClosed(ClanRegistration reg) =>
        reg.PollExpiresAtUtc is { } expires && DateTime.UtcNow >= expires;

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
        var quests = available
            .Select(q => new PollQuestDto(q.Id, q.DisplayName, q.PromoImageUrl, q.PurchasableWithGems, counts.GetValueOrDefault(q.Id)))
            .ToList();

        // "Embaralhar" é mais uma cédula na mesma urna — se vencer, a automação embaralha
        // em vez de reivindicar uma missão (ver ScheduledTaskExecutor.ClaimMostVotedFormQuestAsync).
        quests.Add(new PollQuestDto(
            QuestPollVote.ShuffleOptionId, "Embaralhar missões", null, false,
            counts.GetValueOrDefault(QuestPollVote.ShuffleOptionId)));

        return quests;
    }
}

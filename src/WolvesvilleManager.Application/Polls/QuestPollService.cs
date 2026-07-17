using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Interfaces;

namespace WolvesvilleManager.Application.Polls;

/// <summary>Missão candidata no formulário, com a contagem atual de votos.</summary>
public record PollQuestDto(string QuestId, string Name, string? ImageUrl, bool Gems, int Votes);

/// <summary>O que a página pública vê: nome do clã, candidatas, o voto deste nick e a próxima transição.</summary>
public record PollDto(
    string ClanName, string? ClanTag, List<PollQuestDto> Quests, string? VotedQuestId,
    DateTime? NextBoundaryUtc, bool IsClosed);

/// <summary>Uma rodada de votação já decidida (a automação já claimou ou embaralhou).</summary>
public record PollHistoryEntryDto(string QuestName, int Votes, bool WasShuffle, DateTime DecidedAtUtc);

/// <summary>Um voto individual da urna atual — só a aba admin vê quem votou em quê.</summary>
public record PollVoterDto(string Nickname, string QuestId, string QuestName, bool WasShuffle);

/// <summary>
/// Uma janela semanal recorrente de votação, para exibição/edição na aba admin. Dias no mesmo
/// código de 3 letras usado no resto do app (SUN, MON, TUE, WED, THU, FRI, SAT); horários em "HH:mm".
/// </summary>
public record PollWindowDto(int Id, string StartDay, string StartTime, string EndDay, string EndTime);

/// <summary>Entrada para configurar uma janela (sem Id — a lista inteira é substituída a cada salvamento).</summary>
public record PollWindowInput(string StartDay, string StartTime, string EndDay, string EndTime);

/// <summary>
/// O que a aba admin vê: o link, a apuração, os votantes, a próxima transição, as janelas
/// configuradas (se houver) e o histórico.
/// </summary>
public record PollAdminDto(
    string Token, List<PollQuestDto> Quests, int TotalVotes, DateTime? NextBoundaryUtc, bool IsClosed,
    List<PollHistoryEntryDto> History, List<PollWindowDto> Windows, string? WindowsTimeZoneId,
    List<PollVoterDto> Voters);

/// <summary>Durações de prazo que a aba admin oferece — nunca "para sempre".</summary>
public enum PollDuration { SixHours, TwelveHours, OneDay, ThreeDays, SevenDays }

/// <summary>
/// Formulário público de votação de missões. O token do link é a única credencial da
/// página pública; o nick digitado limita a um voto por nick (case-insensitive) — não
/// por navegador, então trocar de navegador ou usar aba anônima não abre voto extra.
/// </summary>
public class QuestPollService
{
    private const string DefaultTimeZone = "America/Sao_Paulo";

    private static readonly Dictionary<string, DayOfWeek> DayCodeToDayOfWeek = new()
    {
        ["SUN"] = DayOfWeek.Sunday,
        ["MON"] = DayOfWeek.Monday,
        ["TUE"] = DayOfWeek.Tuesday,
        ["WED"] = DayOfWeek.Wednesday,
        ["THU"] = DayOfWeek.Thursday,
        ["FRI"] = DayOfWeek.Friday,
        ["SAT"] = DayOfWeek.Saturday,
    };

    private static readonly Dictionary<DayOfWeek, string> DayOfWeekToCode =
        DayCodeToDayOfWeek.ToDictionary(kv => kv.Value, kv => kv.Key);

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
        var questNames = quests.ToDictionary(q => q.QuestId, q => q.Name);

        var currentVotes = await _db.QuestPollVotes
            .Where(v => v.ClanRegistrationId == reg.Id)
            .OrderBy(v => v.Nickname)
            .Select(v => new { v.Nickname, v.QuestId })
            .ToListAsync(ct);
        // Nome resolvido pela lista de disponíveis agora; se a missão votada saiu de cartaz
        // nesse intervalo, mostra o id cru em vez de quebrar (raro, mas não deve derrubar a tela).
        var voters = currentVotes
            .Select(v => new PollVoterDto(
                v.Nickname,
                v.QuestId,
                questNames.GetValueOrDefault(v.QuestId, v.QuestId),
                v.QuestId == QuestPollVote.ShuffleOptionId))
            .ToList();

        var history = await _db.QuestPollResults
            .Where(r => r.ClanRegistrationId == reg.Id)
            .OrderByDescending(r => r.DecidedAtUtc)
            .Take(20)
            .Select(r => new PollHistoryEntryDto(r.QuestName, r.Votes, r.WasShuffle, r.DecidedAtUtc))
            .ToListAsync(ct);

        var windows = await LoadWindowsAsync(reg.Id, ct);
        var tz = reg.PollWindowsTimeZoneId ?? DefaultTimeZone;
        var isClosed = !IsOpenNow(reg, windows, tz);
        var nextBoundary = windows.Count > 0
            ? PollWindowCalculator.GetNextBoundaryUtc(windows, tz, DateTime.UtcNow)
            : reg.PollExpiresAtUtc;

        return new PollAdminDto(
            reg.PollToken, quests, currentVotes.Count, nextBoundary, isClosed, history,
            windows.Select(ToDto).ToList(), reg.PollWindowsTimeZoneId, voters);
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
    /// Desliga janelas configuradas antes, já que é uma escolha explícita do admin de usar o
    /// modo manual. A votação nunca fica aberta para sempre — não existe opção de prazo indefinido.
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
        await _db.PollWindows.Where(w => w.ClanRegistrationId == clanRegistrationId).ExecuteDeleteAsync(ct);
        reg.PollWindowsTimeZoneId = null;
        await _db.SaveChangesAsync(ct);
        return reg.PollExpiresAtUtc.Value;
    }

    /// <summary>
    /// Aba admin: substitui a lista inteira de janelas semanais recorrentes (ex.: "domingo 23:00
    /// até segunda 11:00" + "quarta 20:00 até quinta 11:00") — quantas o admin quiser. Quando
    /// não vazia, o prazo manual (<see cref="PollExpiresAtUtc"/>) deixa de valer: o estado
    /// aberto/fechado passa a ser calculado a partir das janelas.
    /// </summary>
    public async Task<List<PollWindowDto>> SetWindowsAsync(
        int clanRegistrationId, List<PollWindowInput> windows, string timeZoneId, CancellationToken ct = default)
    {
        if (windows.Count == 0)
            throw new BusinessRuleException("Defina pelo menos um ciclo de votação.");
        if (!CronScheduleCalculator.IsValidTimeZone(timeZoneId))
            throw new BusinessRuleException("Fuso horário inválido.");

        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");

        var parsed = windows.Select(w =>
        {
            if (!DayCodeToDayOfWeek.TryGetValue(w.StartDay, out var startDay) ||
                !DayCodeToDayOfWeek.TryGetValue(w.EndDay, out var endDay))
                throw new BusinessRuleException("Dia da semana inválido.");
            if (!TimeSpan.TryParse(w.StartTime, out var startTime) || !TimeSpan.TryParse(w.EndTime, out var endTime))
                throw new BusinessRuleException("Horário inválido.");
            if (startDay == endDay && startTime == endTime)
                throw new BusinessRuleException("O início e o fim de um ciclo não podem ser iguais.");

            return new PollWindow
            {
                ClanRegistrationId = clanRegistrationId,
                StartDayOfWeek = startDay,
                StartTime = startTime,
                EndDayOfWeek = endDay,
                EndTime = endTime,
            };
        }).ToList();

        await _db.PollWindows.Where(w => w.ClanRegistrationId == clanRegistrationId).ExecuteDeleteAsync(ct);
        reg.PollWindowsTimeZoneId = timeZoneId;
        _db.PollWindows.AddRange(parsed);
        await _db.SaveChangesAsync(ct);

        return (await LoadWindowsAsync(clanRegistrationId, ct)).Select(ToDto).ToList();
    }

    /// <summary>Aba admin: remove todas as janelas configuradas, voltando ao prazo manual.</summary>
    public async Task ClearWindowsAsync(int clanRegistrationId, CancellationToken ct = default)
    {
        var reg = await _db.ClanRegistrations.FirstOrDefaultAsync(c => c.Id == clanRegistrationId, ct)
            ?? throw new NotFoundException($"Clã registrado #{clanRegistrationId} não encontrado.");
        await _db.PollWindows.Where(w => w.ClanRegistrationId == clanRegistrationId).ExecuteDeleteAsync(ct);
        reg.PollWindowsTimeZoneId = null;
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

        var windows = await LoadWindowsAsync(reg.Id, ct);
        var tz = reg.PollWindowsTimeZoneId ?? DefaultTimeZone;
        var isClosed = !IsOpenNow(reg, windows, tz);
        var nextBoundary = windows.Count > 0
            ? PollWindowCalculator.GetNextBoundaryUtc(windows, tz, DateTime.UtcNow)
            : reg.PollExpiresAtUtc;

        return new PollDto(reg.ClanName, reg.ClanTag, quests, voted, nextBoundary, isClosed);
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
        var windows = await LoadWindowsAsync(reg.Id, ct);
        if (!IsOpenNow(reg, windows, reg.PollWindowsTimeZoneId ?? DefaultTimeZone))
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
        var now = DateTime.UtcNow;
        if (vote is null)
            _db.QuestPollVotes.Add(new QuestPollVote
            {
                ClanRegistrationId = reg.Id, QuestId = questId, Nickname = nickname.Trim(),
                CreatedAtUtc = now, UpdatedAtUtc = now,
            });
        else
        {
            vote.QuestId = questId;
            vote.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Nulo se vazio/só espaços; senão o nick aparado e em minúsculas, para comparação.</summary>
    private static string? NormalizeNickname(string? nickname)
    {
        var trimmed = nickname?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLower();
    }

    private static bool IsOpenNow(ClanRegistration reg, List<PollWindow> windows, string timeZoneId) =>
        windows.Count > 0
            ? PollWindowCalculator.IsOpen(windows, timeZoneId, DateTime.UtcNow)
            : reg.PollExpiresAtUtc is { } expires && DateTime.UtcNow < expires;

    private async Task<List<PollWindow>> LoadWindowsAsync(int clanRegistrationId, CancellationToken ct) =>
        await _db.PollWindows
            .Where(w => w.ClanRegistrationId == clanRegistrationId)
            .OrderBy(w => w.StartDayOfWeek).ThenBy(w => w.StartTime)
            .ToListAsync(ct);

    private static PollWindowDto ToDto(PollWindow w) => new(
        w.Id, DayOfWeekToCode[w.StartDayOfWeek], w.StartTime.ToString(@"hh\:mm"),
        DayOfWeekToCode[w.EndDayOfWeek], w.EndTime.ToString(@"hh\:mm"));

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

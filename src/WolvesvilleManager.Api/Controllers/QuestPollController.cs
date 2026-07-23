using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Polls;

namespace WolvesvilleManager.Api.Controllers;

public record VoteRequest(string QuestId, string Nickname);
public record SetExpirationRequest(PollDuration Duration);
public record PollWindowRequest(string StartDay, string StartTime, string EndDay, string EndTime);
public record SetPollWindowsRequest(List<PollWindowRequest> Windows, string TimeZoneId);
public record SetQuestVisibilityRequest(string QuestId, bool Hidden);

/// <summary>
/// Formulário público de votação de missões. As rotas /api/poll/* são as ÚNICAS
/// sem X-Api-Key (liberadas no ApiKeyAuthMiddleware): o token do link é a credencial.
/// As rotas /api/clans/{id}/poll são da aba admin e seguem protegidas como o resto.
/// </summary>
[ApiController]
public class QuestPollController : ControllerBase
{
    private readonly QuestPollService _service;

    public QuestPollController(QuestPollService service) => _service = service;

    /// <summary>Aba admin: link do formulário + apuração atual (gera o token no 1º acesso).</summary>
    [HttpGet("api/clans/{id:int}/poll")]
    public async Task<PollAdminDto> GetAdmin(int id, CancellationToken ct) =>
        await _service.GetAdminAsync(id, ct);

    /// <summary>Aba admin: zera a urna.</summary>
    [HttpPost("api/clans/{id:int}/poll/reset")]
    public async Task<IActionResult> Reset(int id, CancellationToken ct)
    {
        await _service.ResetAsync(id, ct);
        return NoContent();
    }

    /// <summary>Aba admin: define/estende o prazo da votação a partir de agora (manual, não se repete).</summary>
    [HttpPost("api/clans/{id:int}/poll/expiration")]
    public async Task<object> SetExpiration(int id, [FromBody] SetExpirationRequest request, CancellationToken ct)
    {
        var expiresAtUtc = await _service.SetExpirationAsync(id, request.Duration, ct);
        return new { expiresAtUtc };
    }

    /// <summary>Aba admin: substitui as janelas semanais recorrentes de votação (quantas o admin quiser).</summary>
    [HttpPost("api/clans/{id:int}/poll/windows")]
    public async Task<List<PollWindowDto>> SetWindows(int id, [FromBody] SetPollWindowsRequest request, CancellationToken ct) =>
        await _service.SetWindowsAsync(
            id,
            request.Windows.Select(w => new PollWindowInput(w.StartDay, w.StartTime, w.EndDay, w.EndTime)).ToList(),
            request.TimeZoneId, ct);

    /// <summary>Aba admin: remove as janelas configuradas, voltando ao prazo manual.</summary>
    [HttpDelete("api/clans/{id:int}/poll/windows")]
    public async Task<IActionResult> ClearWindows(int id, CancellationToken ct)
    {
        await _service.ClearWindowsAsync(id, ct);
        return NoContent();
    }

    /// <summary>Aba admin: liga/desliga a visibilidade de uma missão no formulário público.</summary>
    [HttpPut("api/clans/{id:int}/poll/quests/visibility")]
    public async Task<IActionResult> SetQuestVisibility(
        int id, [FromBody] SetQuestVisibilityRequest request, CancellationToken ct)
    {
        await _service.SetQuestHiddenAsync(id, request.QuestId, request.Hidden, ct);
        return NoContent();
    }

    /// <summary>Página pública: candidatas + voto atual desse nick (via ?nickname=).</summary>
    [HttpGet("api/poll/{token}")]
    public async Task<PollDto> GetPublic(string token, [FromQuery] string? nickname, CancellationToken ct) =>
        await _service.GetPublicAsync(token, nickname, ct);

    /// <summary>
    /// Página pública: registra ou troca o voto desse nick. "Embaralhar" também é
    /// votado aqui (QuestId = <see cref="Domain.Entities.QuestPollVote.ShuffleOptionId"/>) —
    /// não é uma ação imediata, é mais uma cédula na urna.
    /// </summary>
    [HttpPost("api/poll/{token}/vote")]
    public async Task<IActionResult> Vote(string token, [FromBody] VoteRequest request, CancellationToken ct)
    {
        await _service.VoteAsync(token, request.QuestId, request.Nickname, ct);
        return NoContent();
    }
}

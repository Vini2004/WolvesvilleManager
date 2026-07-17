using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Polls;

namespace WolvesvilleManager.Api.Controllers;

public record VoteRequest(string QuestId, string VoterId);

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

    /// <summary>Página pública: candidatas + voto atual deste navegador (via ?voterId=).</summary>
    [HttpGet("api/poll/{token}")]
    public async Task<PollDto> GetPublic(string token, [FromQuery] string? voterId, CancellationToken ct) =>
        await _service.GetPublicAsync(token, voterId, ct);

    /// <summary>Página pública: registra ou troca o voto deste navegador.</summary>
    [HttpPost("api/poll/{token}/vote")]
    public async Task<IActionResult> Vote(string token, [FromBody] VoteRequest request, CancellationToken ct)
    {
        await _service.VoteAsync(token, request.QuestId, request.VoterId, ct);
        return NoContent();
    }

    /// <summary>Página pública: embaralha as missões disponíveis (gasta ouro do clã) e zera a urna.</summary>
    [HttpPost("api/poll/{token}/shuffle")]
    public async Task<PollDto> Shuffle(string token, CancellationToken ct) =>
        await _service.ShuffleAsync(token, ct);
}

using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Social;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

/// <summary>Chat, livro-razão e logs de auditoria do clã.</summary>
[ApiController]
[Route("api/clans/{id:int}")]
public class SocialController : ControllerBase
{
    private readonly ClanSocialService _service;

    public SocialController(ClanSocialService service)
    {
        _service = service;
    }

    [HttpGet("chat")]
    public Task<List<ChatMessage>> Chat(int id, CancellationToken ct) =>
        _service.GetChatAsync(id, ct);

    [HttpPost("chat")]
    public async Task<IActionResult> SendChat(int id, [FromBody] AnnouncementRequest request, CancellationToken ct)
    {
        await _service.SendChatMessageAsync(id, request.Message, ct);
        return NoContent();
    }

    [HttpGet("ledger")]
    public Task<List<LedgerEntry>> Ledger(int id, CancellationToken ct) =>
        _service.GetLedgerAsync(id, ct);

    [HttpGet("logs")]
    public Task<List<ClanLogEntry>> Logs(int id, CancellationToken ct) =>
        _service.GetLogsAsync(id, ct);
}

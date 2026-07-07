using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Social;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

[ApiController]
[Route("api/clans/{id:int}/announcements")]
public class AnnouncementsController : ControllerBase
{
    private readonly ClanSocialService _service;

    public AnnouncementsController(ClanSocialService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<ClanAnnouncement>> List(int id, CancellationToken ct) =>
        _service.GetAnnouncementsAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Post(int id, [FromBody] AnnouncementRequest request, CancellationToken ct)
    {
        await _service.PostAnnouncementAsync(id, request.Message, ct);
        return NoContent();
    }
}

public record AnnouncementRequest(string Message);

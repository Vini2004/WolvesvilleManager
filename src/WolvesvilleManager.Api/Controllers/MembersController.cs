using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Members;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

[ApiController]
[Route("api/clans/{id:int}/members")]
public class MembersController : ControllerBase
{
    private readonly ClanMembersService _service;

    public MembersController(ClanMembersService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<ClanMember>> List(int id, CancellationToken ct) =>
        _service.ListAsync(id, ct);

    [HttpGet("xp-report")]
    public Task<XpReport> XpReport(int id, [FromQuery] int days = 7, CancellationToken ct = default) =>
        _service.GetXpReportAsync(id, Math.Clamp(days, 1, 90), ct);

    [HttpGet("blocklist")]
    public Task<List<BlocklistEntry>> Blocklist(int id, CancellationToken ct) =>
        _service.GetBlocklistAsync(id, ct);

    [HttpPut("{playerId}/participation")]
    public async Task<IActionResult> SetParticipation(
        int id, string playerId, [FromBody] ParticipationRequest request, CancellationToken ct)
    {
        await _service.SetQuestParticipationAsync(id, playerId, request.Participate, ct);
        return NoContent();
    }

    [HttpPut("participation")]
    public async Task<IActionResult> SetAllParticipation(
        int id, [FromBody] ParticipationRequest request, CancellationToken ct)
    {
        await _service.SetAllQuestParticipationAsync(id, request.Participate, ct);
        return NoContent();
    }

    [HttpPut("{playerId}/flair")]
    public async Task<IActionResult> SetFlair(
        int id, string playerId, [FromBody] FlairRequest request, CancellationToken ct)
    {
        await _service.SetFlairAsync(id, playerId, request.Flair, ct);
        return NoContent();
    }

    [HttpPost("{playerId}/kick")]
    public async Task<IActionResult> Kick(
        int id, string playerId, [FromBody] KickRequest? request, CancellationToken ct)
    {
        await _service.KickAsync(id, playerId, request?.Reason, ct);
        return NoContent();
    }

    [HttpPost("{playerId}/block")]
    public async Task<IActionResult> Block(int id, string playerId, CancellationToken ct)
    {
        await _service.BlockAsync(id, playerId, ct);
        return NoContent();
    }

    [HttpPost("{playerId}/unblock")]
    public async Task<IActionResult> Unblock(int id, string playerId, CancellationToken ct)
    {
        await _service.UnblockAsync(id, playerId, ct);
        return NoContent();
    }
}

public record ParticipationRequest(bool Participate);
public record FlairRequest(string Flair);
public record KickRequest(string? Reason);

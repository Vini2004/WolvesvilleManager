using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Game;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

/// <summary>Conteúdo global do jogo, busca de jogadores e resgate do chapéu da API.</summary>
[ApiController]
[Route("api/clans/{id:int}/game")]
public class GameController : ControllerBase
{
    private readonly GameService _service;

    public GameController(GameService service)
    {
        _service = service;
    }

    [HttpPost("redeem-api-hat")]
    public async Task<IActionResult> RedeemApiHat(int id, CancellationToken ct)
    {
        await _service.RedeemApiHatAsync(id, ct);
        return NoContent();
    }

    [HttpGet("players/search")]
    public async Task<ActionResult<PlayerProfile>> SearchPlayer(int id, [FromQuery] string username, CancellationToken ct)
    {
        var player = await _service.SearchPlayerAsync(id, username, ct);
        return player is null ? NotFound() : player;
    }

    [HttpGet("players/{playerId}")]
    public Task<PlayerProfile> GetPlayer(int id, string playerId, CancellationToken ct) =>
        _service.GetPlayerAsync(id, playerId, ct);

    [HttpGet("role-rotations")]
    public Task<List<RoleRotationEntry>> RoleRotations(int id, CancellationToken ct) =>
        _service.GetRoleRotationsAsync(id, ct);

    [HttpGet("battle-pass")]
    public Task<BattlePassSeason> BattlePass(int id, CancellationToken ct) =>
        _service.GetBattlePassSeasonAsync(id, ct);

    [HttpGet("shop-offers")]
    public Task<List<ShopOffer>> ShopOffers(int id, CancellationToken ct) =>
        _service.GetShopOffersAsync(id, ct);

    [HttpGet("announcements")]
    public Task<List<GameAnnouncement>> Announcements(int id, CancellationToken ct) =>
        _service.GetGameAnnouncementsAsync(id, ct);
}

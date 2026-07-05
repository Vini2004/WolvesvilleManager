using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Quests;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

[ApiController]
[Route("api/clans/{id:int}/quests")]
public class QuestsController : ControllerBase
{
    private readonly QuestService _service;

    public QuestsController(QuestService service)
    {
        _service = service;
    }

    /// <summary>Visão geral: missão ativa, disponíveis com votos e saldo do clã.</summary>
    [HttpGet]
    public Task<QuestsOverviewDto> Overview(int id, CancellationToken ct) =>
        _service.GetOverviewAsync(id, ct);

    [HttpGet("history")]
    public Task<List<QuestHistoryEntry>> History(int id, CancellationToken ct) =>
        _service.GetHistoryAsync(id, ct);

    /// <summary>Inicia (compra) uma missão. Gasta ouro/gemas do clã!</summary>
    [HttpPost("claim")]
    public async Task<IActionResult> Claim(int id, [FromBody] ClaimQuestRequest request, CancellationToken ct)
    {
        await _service.ClaimAsync(id, request.QuestId, ct);
        return NoContent();
    }

    /// <summary>Embaralha as missões disponíveis. Gasta ouro do clã!</summary>
    [HttpPost("shuffle")]
    public async Task<IActionResult> Shuffle(int id, CancellationToken ct)
    {
        await _service.ShuffleAsync(id, ct);
        return NoContent();
    }

    /// <summary>Pula o tempo de espera da missão ativa. Gasta gemas do clã!</summary>
    [HttpPost("skip-waiting-time")]
    public async Task<IActionResult> SkipWaitingTime(int id, CancellationToken ct)
    {
        await _service.SkipWaitingTimeAsync(id, ct);
        return NoContent();
    }

    /// <summary>Resgata tempo extra da missão ativa. Gasta ouro do clã!</summary>
    [HttpPost("claim-extra-time")]
    public async Task<IActionResult> ClaimExtraTime(int id, CancellationToken ct)
    {
        await _service.ClaimExtraTimeAsync(id, ct);
        return NoContent();
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}

public record ClaimQuestRequest(string QuestId);

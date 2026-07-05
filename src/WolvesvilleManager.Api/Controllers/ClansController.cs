using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Clans;

namespace WolvesvilleManager.Api.Controllers;

[ApiController]
[Route("api/clans")]
public class ClansController : ControllerBase
{
    private readonly ClanRegistrationService _service;

    public ClansController(ClanRegistrationService service)
    {
        _service = service;
    }

    /// <summary>Lista os clãs registrados na aplicação.</summary>
    [HttpGet]
    public Task<List<RegisteredClanDto>> List(CancellationToken ct) =>
        _service.ListAsync(ct);

    /// <summary>
    /// Passo 1 do registro: dado uma chave de API, lista os clãs para os quais
    /// ela é autorizada como clan bot (a chave não é salva neste passo).
    /// </summary>
    [HttpPost("authorized")]
    public Task<List<Domain.Wolvesville.ClanInfo>> GetAuthorizedClans(
        [FromBody] AuthorizedClansRequest request, CancellationToken ct) =>
        _service.GetAuthorizedClansAsync(request.ApiKey, ct);

    /// <summary>
    /// Passo 2 do registro: salva o clã escolhido. A autorização da chave sobre o clã
    /// é revalidada no servidor antes de salvar; a chave é criptografada em repouso.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegisteredClanDto>> Register(
        [FromBody] RegisterClanRequest request, CancellationToken ct)
    {
        var dto = await _service.RegisterAsync(request.ApiKey, request.ClanId, ct);
        return CreatedAtAction(nameof(List), new { }, dto);
    }

    /// <summary>Remove o registro do clã (e suas tarefas agendadas, em cascata).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

public record AuthorizedClansRequest(string ApiKey);
public record RegisterClanRequest(string ApiKey, string ClanId);

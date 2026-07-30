using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Social;
using WolvesvilleManager.Domain.Wolvesville;

namespace WolvesvilleManager.Api.Controllers;

public record SetWelcomeSettingsRequest(bool Enabled, string Template, string? SendTime1, string? SendTime2);

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

    /// <summary>Configuração da mensagem automática de boas-vindas para membros novos.</summary>
    [HttpGet("welcome")]
    public Task<WelcomeSettingsDto> GetWelcomeSettings(int id, CancellationToken ct) =>
        _service.GetWelcomeSettingsAsync(id, ct);

    [HttpPut("welcome")]
    public async Task<IActionResult> SetWelcomeSettings(int id, [FromBody] SetWelcomeSettingsRequest request, CancellationToken ct)
    {
        await _service.SetWelcomeSettingsAsync(
            id, request.Enabled, request.Template, request.SendTime1, request.SendTime2, ct);
        return NoContent();
    }

    /// <summary>
    /// Roda a checagem de boas-vindas agora e devolve o que aconteceu com cada entrada recente —
    /// serve para testar sem depender do gatilho externo nem entrar/sair do clã no escuro.
    /// </summary>
    [HttpPost("welcome/run")]
    public Task<WelcomeCheckResultDto> RunWelcomeCheck(int id, CancellationToken ct) =>
        _service.RunWelcomeCheckAsync(id, ct);

    [HttpGet("ledger")]
    public Task<List<LedgerEntry>> Ledger(int id, CancellationToken ct) =>
        _service.GetLedgerAsync(id, ct);

    [HttpGet("logs")]
    public Task<List<ClanLogEntry>> Logs(int id, CancellationToken ct) =>
        _service.GetLogsAsync(id, ct);
}

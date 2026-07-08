using Microsoft.AspNetCore.Mvc;
using WolvesvilleManager.Application.Scheduling;

namespace WolvesvilleManager.Api.Controllers;

[ApiController]
public class ScheduledTasksController : ControllerBase
{
    private readonly ScheduledTaskService _service;
    private readonly ScheduledTaskExecutor _executor;

    public ScheduledTasksController(ScheduledTaskService service, ScheduledTaskExecutor executor)
    {
        _service = service;
        _executor = executor;
    }

    /// <summary>Tarefas agendadas de um clã.</summary>
    [HttpGet("api/clans/{id:int}/scheduled-tasks")]
    public Task<List<ScheduledTaskDto>> List(int id, CancellationToken ct) =>
        _service.ListAsync(id, ct);

    /// <summary>
    /// Cria uma tarefa agendada. Ex.: iniciar a missão mais votada toda sexta às 20h:
    /// { "type": "ClaimMostVotedQuest", "cronExpression": "0 20 * * FRI", "minVotes": 5 }.
    /// </summary>
    [HttpPost("api/clans/{id:int}/scheduled-tasks")]
    public async Task<ActionResult<ScheduledTaskDto>> Create(
        int id, [FromBody] CreateScheduledTaskRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(id, request, ct);
        return CreatedAtAction(nameof(List), new { id }, dto);
    }

    [HttpPut("api/scheduled-tasks/{taskId:int}")]
    public Task<ScheduledTaskDto> Update(
        int taskId, [FromBody] CreateScheduledTaskRequest request, CancellationToken ct) =>
        _service.UpdateAsync(taskId, request, ct);

    [HttpDelete("api/scheduled-tasks/{taskId:int}")]
    public async Task<IActionResult> Delete(int taskId, CancellationToken ct)
    {
        await _service.DeleteAsync(taskId, ct);
        return NoContent();
    }

    /// <summary>Histórico de execuções de uma tarefa (mais recentes primeiro).</summary>
    [HttpGet("api/scheduled-tasks/{taskId:int}/logs")]
    public Task<List<TaskExecutionLogDto>> Logs(int taskId, [FromQuery] int take = 50, CancellationToken ct = default) =>
        _service.GetLogsAsync(taskId, take, ct);

    /// <summary>
    /// Gatilho externo do agendador: executa imediatamente as tarefas vencidas.
    /// Pensado para um cron gratuito (ex.: cron-job.org) acordar o app em hospedagem
    /// sem Always On. Protegido pelo X-Api-Key como todo o resto da API.
    /// Aceita GET também para que o gatilho do cron-job.org seja um simples GET (método padrão dele).
    /// </summary>
    [HttpPost("api/scheduler/run")]
    [HttpGet("api/scheduler/run")]
    public async Task<ActionResult<object>> RunNow(CancellationToken ct)
    {
        var executed = await _executor.ExecuteDueTasksAsync(ct);
        return Ok(new { executed });
    }
}

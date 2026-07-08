using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Scheduling;

namespace WolvesvilleManager.Infrastructure.Scheduling;

/// <summary>
/// Cria/atualiza/apaga gatilhos no cron-job.org (API REST, auth via Bearer). Cada tarefa vira
/// dois jobs: execução (no horário) e pré-aquecimento (~5 min antes). Ambos fazem GET no nosso
/// <c>/api/scheduler/run</c> com o header X-Api-Key, para acordar o app/banco só nesses momentos.
/// </summary>
public sealed class CronJobOrgGateway : ICronTriggerGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<CronJobOrgGateway> _logger;
    private readonly string _targetUrl;
    private readonly string _targetApiKey;

    public CronJobOrgGateway(HttpClient http, IConfiguration config, ILogger<CronJobOrgGateway> logger)
    {
        _http = http;
        _logger = logger;
        _targetUrl = config["CronJobOrg:TargetUrl"] ?? "";
        _targetApiKey = config["CronJobOrg:TargetApiKey"] ?? "";
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_targetUrl);

    public async Task<CronTriggerIds> SyncAsync(CronTriggerIds existing, ScheduledTaskTrigger t, CancellationToken ct = default)
    {
        var runId = await UpsertJobAsync(
            existing.RunJobId, $"WVM #{t.TaskId} {t.Type}",
            CronJobOrgTranslator.ToSchedule(t.CronExpression), t.TimeZoneId, t.Enabled, ct);

        int? warmupId = existing.WarmupJobId;
        var warmupCron = t.Enabled ? CronJobOrgTranslator.TryWarmupCron(t.CronExpression) : null;
        if (warmupCron is not null)
        {
            warmupId = await UpsertJobAsync(
                existing.WarmupJobId, $"WVM #{t.TaskId} pré-aquecer",
                CronJobOrgTranslator.ToSchedule(warmupCron), t.TimeZoneId, true, ct);
        }
        else if (existing.WarmupJobId is int wid)
        {
            await DeleteJobAsync(wid, ct);
            warmupId = null;
        }

        return new CronTriggerIds(runId, warmupId);
    }

    public async Task DeleteAsync(CronTriggerIds existing, CancellationToken ct = default)
    {
        if (existing.RunJobId is int r) await DeleteJobAsync(r, ct);
        if (existing.WarmupJobId is int w) await DeleteJobAsync(w, ct);
    }

    private async Task<int?> UpsertJobAsync(
        int? jobId, string title, CronJobOrgSchedule schedule, string timeZone, bool enabled, CancellationToken ct)
    {
        var payload = new
        {
            job = new
            {
                url = _targetUrl,
                enabled,
                title,
                saveResponses = false,
                requestMethod = 0, // GET — /api/scheduler/run aceita GET
                extendedData = new { headers = new Dictionary<string, string> { ["X-Api-Key"] = _targetApiKey } },
                schedule = new
                {
                    timezone = timeZone,
                    expiresAt = 0,
                    minutes = schedule.Minutes,
                    hours = schedule.Hours,
                    mdays = schedule.Mdays,
                    months = schedule.Months,
                    wdays = schedule.Wdays,
                },
            },
        };

        // Job já existe → PATCH; se sumiu (404), recria. Sem id → cria (PUT).
        if (jobId is int id)
        {
            using var patch = await _http.PatchAsJsonAsync($"/jobs/{id}", payload, Json, ct);
            if (patch.StatusCode != HttpStatusCode.NotFound)
            {
                patch.EnsureSuccessStatusCode();
                return id;
            }
            _logger.LogWarning("Job {JobId} não existe mais no cron-job.org — recriando.", id);
        }

        using var put = await _http.PutAsJsonAsync("/jobs", payload, Json, ct);
        put.EnsureSuccessStatusCode();
        var body = await put.Content.ReadFromJsonAsync<CreateJobResponse>(Json, ct);
        return body?.JobId;
    }

    private async Task DeleteJobAsync(int jobId, CancellationToken ct)
    {
        using var res = await _http.DeleteAsync($"/jobs/{jobId}", ct);
        if (res.StatusCode != HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }

    private sealed record CreateJobResponse(int JobId);
}

/// <summary>No-op usado quando a integração com o cron-job.org não está configurada.</summary>
public sealed class NullCronTriggerGateway : ICronTriggerGateway
{
    public bool Enabled => false;
    public Task<CronTriggerIds> SyncAsync(CronTriggerIds existing, ScheduledTaskTrigger trigger, CancellationToken ct = default) =>
        Task.FromResult(default(CronTriggerIds));
    public Task DeleteAsync(CronTriggerIds existing, CancellationToken ct = default) => Task.CompletedTask;
}

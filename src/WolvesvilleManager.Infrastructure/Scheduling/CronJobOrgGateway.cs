using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Entities;

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
        // "Pular tempo de espera" com retentativa automática ligada: o gatilho de execução
        // precisa disparar de novo a cada retentativa (30 em 30 min, algumas vezes) — o
        // cron-job.org não enxerga o reagendamento interno de "próxima execução", só o próprio
        // horário configurado. Sem isso, a retentativa nunca chega a rodar em produção (sem
        // Always On). Cron "avançado" (fora do formato simples) cai pro schedule normal.
        var runSchedule = t.Type == ScheduledTaskType.SkipQuestWaitingTime && t.AutoRetryOnXpNotReached
            ? CronJobOrgTranslator.TryScheduleWithRetries(
                t.CronExpression, (int)ScheduledTaskExecutor.AutoRetryInterval.TotalMinutes, t.AutoRetryMaxAttempts)
              ?? CronJobOrgTranslator.ToSchedule(t.CronExpression)
            : CronJobOrgTranslator.ToSchedule(t.CronExpression);

        var runId = await UpsertJobAsync(
            existing.RunJobId, $"WVM #{t.TaskId} {t.Type}",
            runSchedule, t.TimeZoneId, t.Enabled, ct);

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

    public async Task<int?> SyncWelcomePingAsync(WelcomePingTrigger t, CancellationToken ct = default)
    {
        // Sem horários ou desligado: apaga o ping (se existia) e não deixa nenhum.
        if (!t.Enabled || t.Times.Count == 0)
        {
            if (t.ExistingJobId is int old) await DeleteJobAsync(old, ct);
            return null;
        }

        // Um único job cobre todos os horários: minutos × horas (produto cartesiano). Se os
        // minutos forem iguais (ex.: 09:00 e 19:00), dispara exatamente nesses horários; minutos
        // diferentes geram alguns pings a mais, inofensivos (o executor só solta boas-vindas cujo
        // horário de liberação já passou).
        var minutes = t.Times.Select(x => x.Minutes).Distinct().OrderBy(x => x).ToArray();
        var hours = t.Times.Select(x => x.Hours).Distinct().OrderBy(x => x).ToArray();
        var schedule = new CronJobOrgSchedule(minutes, hours, new[] { -1 }, new[] { -1 }, new[] { -1 });

        return await UpsertJobAsync(
            t.ExistingJobId, $"WVM #{t.ClanRegistrationId} boas-vindas", schedule, t.TimeZoneId, true, ct);
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
            using var patch = await SendWithRetryAsync(HttpMethod.Patch, $"/jobs/{id}", payload, ct);
            if (patch.StatusCode != HttpStatusCode.NotFound)
            {
                patch.EnsureSuccessStatusCode();
                return id;
            }
            _logger.LogWarning("Job {JobId} não existe mais no cron-job.org — recriando.", id);
        }

        using var put = await SendWithRetryAsync(HttpMethod.Put, "/jobs", payload, ct);
        put.EnsureSuccessStatusCode();
        var body = await put.Content.ReadFromJsonAsync<CreateJobResponse>(Json, ct);
        return body?.JobId;
    }

    // A API do cron-job.org limita a taxa (429) — como cada tarefa cria 2 jobs (execução + pré-aquecer)
    // em sequência, o segundo request costuma bater no limite. Repete respeitando o Retry-After
    // (ou backoff curto), poucas vezes, para o par de jobs ser criado por completo.
    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string uri, object payload, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var res = await _http.SendAsync(
                new HttpRequestMessage(method, uri) { Content = JobContent(payload) }, ct);
            if (res.StatusCode != HttpStatusCode.TooManyRequests || attempt >= 4)
                return res;

            var wait = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1.5 * (attempt + 1));
            res.Dispose();
            await Task.Delay(wait, ct);
        }
    }

    // A API do cron-job.org devolve 400 se o Content-Type vier com "; charset=utf-8"
    // (o que PutAsJsonAsync/PatchAsJsonAsync adicionam por padrão). Mandamos "application/json" puro.
    private static JsonContent JobContent(object payload)
    {
        var content = JsonContent.Create(payload, options: Json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
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
    public Task<int?> SyncWelcomePingAsync(WelcomePingTrigger trigger, CancellationToken ct = default) =>
        Task.FromResult<int?>(null);
}

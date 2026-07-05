using WolvesvilleManager.Application.Scheduling;

namespace WolvesvilleManager.Api.Background;

/// <summary>
/// Loop do agendador: acorda periodicamente e executa as tarefas vencidas.
/// Em hospedagem sem Always On (App Service F1) este loop só roda enquanto o app
/// está de pé — o endpoint POST /api/scheduler/run existe como gatilho externo
/// (cron gratuito tipo cron-job.org) para cobrir esse cenário.
/// </summary>
public class ScheduledTaskRunnerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledTaskRunnerService> _logger;
    private readonly TimeSpan _interval;

    public ScheduledTaskRunnerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ScheduledTaskRunnerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(
            Math.Max(10, configuration.GetValue("Scheduler:PollIntervalSeconds", 30)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agendador iniciado (intervalo de {Interval}s).", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval);
        while (await WaitNextTickAsync(timer, stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<ScheduledTaskExecutor>();
                var ran = await executor.ExecuteDueTasksAsync(stoppingToken);
                if (ran > 0)
                    _logger.LogInformation("Agendador executou {Count} tarefa(s).", ran);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Nunca derruba o loop — a próxima iteração tenta de novo.
                _logger.LogError(ex, "Erro no ciclo do agendador.");
            }
        }
    }

    private static async Task<bool> WaitNextTickAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

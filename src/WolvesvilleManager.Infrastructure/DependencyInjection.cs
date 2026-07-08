using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Infrastructure.Persistence;
using WolvesvilleManager.Infrastructure.Scheduling;
using WolvesvilleManager.Infrastructure.Security;
using WolvesvilleManager.Infrastructure.Wolvesville;

namespace WolvesvilleManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EnableRetryOnFailure: o Azure SQL free é serverless e auto-pausa; a primeira conexão
        // depois da pausa precisa "acordar" o banco (~30-60s) e falha com erros transitórios.
        // A estratégia de retry absorve esse cold-resume em vez de estourar (evita o 500.30 no boot).
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IApiKeyProtector, ApiKeyProtector>();

        services.AddHttpClient<IWolvesvilleClient, WolvesvilleApiClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["Wolvesville:BaseUrl"] ?? "https://api.wolvesville.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Gatilhos externos (cron-job.org) só entram se houver token configurado; senão, no-op.
        var cronApiKey = configuration["CronJobOrg:ApiKey"];
        if (!string.IsNullOrWhiteSpace(cronApiKey))
        {
            services.AddHttpClient<ICronTriggerGateway, CronJobOrgGateway>(client =>
            {
                client.BaseAddress = new Uri("https://api.cron-job.org");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cronApiKey);
                client.Timeout = TimeSpan.FromSeconds(20);
            });
        }
        else
        {
            services.AddSingleton<ICronTriggerGateway, NullCronTriggerGateway>();
        }

        return services;
    }
}

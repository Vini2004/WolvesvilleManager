using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Interfaces;
using WolvesvilleManager.Infrastructure.Persistence;
using WolvesvilleManager.Infrastructure.Security;
using WolvesvilleManager.Infrastructure.Wolvesville;

namespace WolvesvilleManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IApiKeyProtector, ApiKeyProtector>();

        services.AddHttpClient<IWolvesvilleClient, WolvesvilleApiClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["Wolvesville:BaseUrl"] ?? "https://api.wolvesville.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

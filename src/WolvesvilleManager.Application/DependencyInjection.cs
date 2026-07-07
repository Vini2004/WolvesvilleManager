using Microsoft.Extensions.DependencyInjection;
using WolvesvilleManager.Application.Clans;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Members;
using WolvesvilleManager.Application.Quests;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Application.Social;

namespace WolvesvilleManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClanKeyResolver>();
        services.AddScoped<ClanRegistrationService>();
        services.AddScoped<QuestService>();
        services.AddScoped<ClanMembersService>();
        services.AddScoped<ClanSocialService>();
        services.AddScoped<ScheduledTaskService>();
        services.AddScoped<ScheduledTaskExecutor>();
        return services;
    }
}

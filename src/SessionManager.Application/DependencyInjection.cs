using Microsoft.Extensions.DependencyInjection;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Application.Services;

namespace SessionManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAllowedProcessService, AllowedProcessService>();
        services.AddScoped<IServerService, ServerService>();
        return services;
    }
}

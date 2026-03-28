using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SessionManager.Application;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Windows;
using SessionManager.Infrastructure.Data;
using SessionManager.Infrastructure.Options;
using SessionManager.Infrastructure.Repositories;
using SessionManager.Infrastructure.Security;
using SessionManager.Infrastructure.Windows;

namespace SessionManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=sessionmanager.db";
        var connectionString = ResolveSqliteConnectionString(rawConnectionString, contentRootPath);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AdminSeedOptions>(configuration.GetSection("AdminSeed"));
        services.Configure<WindowsSessionOptions>(configuration.GetSection("WindowsSession"));

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddApplication();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IAllowedProcessRepository, AllowedProcessRepository>();
        services.AddScoped<IServerRepository, ServerRepository>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IWindowsCommandExecutor, WindowsCommandExecutor>();
        services.AddScoped<IWindowsSessionGateway, WindowsSessionGateway>();

        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    private static string ResolveSqliteConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            return connectionString;
        }

        if (builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(builder.DataSource)
            || builder.DataSource.StartsWith("|DataDirectory|", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var fullDataSourcePath = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        var directory = Path.GetDirectoryName(fullDataSourcePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = fullDataSourcePath;
        return builder.ToString();
    }
}

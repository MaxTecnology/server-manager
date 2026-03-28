using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Domain.Constants;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Options;

namespace SessionManager.Infrastructure.Data;

public sealed class DatabaseInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOptions<AdminSeedOptions> _adminOptions;
    private readonly IClock _clock;

    public DatabaseInitializer(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<AdminSeedOptions> adminOptions,
        IClock clock)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _adminOptions = adminOptions;
        _clock = clock;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        await SeedRolesAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedDefaultServerAsync(cancellationToken);
        await SeedSettingsAsync(cancellationToken);
        await SeedAllowedProcessesAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedAdminUserAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in RoleNames.All)
        {
            var exists = await _dbContext.Roles.AnyAsync(r => r.Name == roleName, cancellationToken);
            if (!exists)
            {
                _dbContext.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = roleName == RoleNames.Administrator
                        ? "Acesso administrativo completo"
                        : "Operação restrita de sessões",
                    CreatedAtUtc = _clock.UtcNow
                });
            }
        }
    }

    private async Task SeedDefaultServerAsync(CancellationToken cancellationToken)
    {
        var machine = Environment.MachineName;
        var anyServer = await _dbContext.Servers.AnyAsync(cancellationToken);
        if (!anyServer)
        {
            _dbContext.Servers.Add(new Server
            {
                Name = machine,
                Hostname = machine,
                IsDefault = true,
                IsActive = true,
                CreatedAtUtc = _clock.UtcNow
            });
            return;
        }

        var hasDefault = await _dbContext.Servers.AnyAsync(x => x.IsDefault, cancellationToken);
        if (!hasDefault)
        {
            var first = await _dbContext.Servers.OrderBy(x => x.CreatedAtUtc).FirstAsync(cancellationToken);
            first.IsDefault = true;
            first.UpdatedAtUtc = _clock.UtcNow;
        }
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        await EnsureSettingAsync("AutoRefreshSeconds", "30", "Intervalo padrão de atualização automática (segundos)", cancellationToken);
        await EnsureSettingAsync("DefaultServerName", Environment.MachineName, "Servidor padrão para operações", cancellationToken);
    }

    private async Task SeedAllowedProcessesAsync(CancellationToken cancellationToken)
    {
        var defaults = new[] { "excel.exe", "winword.exe", "chrome.exe" };
        foreach (var process in defaults)
        {
            var exists = await _dbContext.AllowedProcesses.AnyAsync(p => p.ProcessName == process, cancellationToken);
            if (!exists)
            {
                _dbContext.AllowedProcesses.Add(new AllowedProcess
                {
                    ProcessName = process,
                    IsActive = true,
                    CreatedBy = "system",
                    CreatedAtUtc = _clock.UtcNow
                });
            }
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var configured = _adminOptions.Value;
        var username = string.IsNullOrWhiteSpace(configured.Username) ? "admin" : configured.Username.Trim();
        var displayName = string.IsNullOrWhiteSpace(configured.DisplayName) ? "Administrador" : configured.DisplayName.Trim();
        var password = string.IsNullOrWhiteSpace(configured.Password) ? "Admin@12345" : configured.Password;

        var existing = await _dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken);
        if (existing)
        {
            return;
        }

        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Administrator, cancellationToken);
        var operatorRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Operator, cancellationToken);

        if (adminRole is null || operatorRole is null)
        {
            return;
        }

        var user = new User
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.HashPassword(password),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        user.UserRoles.Add(new UserRole { User = user, Role = adminRole, UserId = user.Id, RoleId = adminRole.Id });
        user.UserRoles.Add(new UserRole { User = user, Role = operatorRole, UserId = user.Id, RoleId = operatorRole.Id });

        _dbContext.Users.Add(user);
    }

    private async Task EnsureSettingAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Settings.AnyAsync(s => s.Key == key, cancellationToken);
        if (!exists)
        {
            _dbContext.Settings.Add(new Setting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedAtUtc = _clock.UtcNow
            });
        }
    }
}

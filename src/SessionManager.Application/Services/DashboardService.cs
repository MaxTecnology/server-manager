using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Dashboard;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Application.Interfaces.Windows;

namespace SessionManager.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IServerRepository _serverRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IWindowsSessionGateway _windowsSessionGateway;
    private readonly IClock _clock;

    public DashboardService(
        IServerRepository serverRepository,
        IAuditLogRepository auditLogRepository,
        IWindowsSessionGateway windowsSessionGateway,
        IClock clock)
    {
        _serverRepository = serverRepository;
        _auditLogRepository = auditLogRepository;
        _windowsSessionGateway = windowsSessionGateway;
        _clock = clock;
    }

    public async Task<Result<DashboardMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var serverName = (await _serverRepository.GetDefaultAsync(cancellationToken))?.Hostname ?? Environment.MachineName;
        var sessionsResult = await _windowsSessionGateway.ListSessionsAsync(serverName, cancellationToken);
        var sessions = sessionsResult.Value ?? Array.Empty<WindowsSessionRecord>();

        var activeSessions = sessions.Count(s => s.State.Equals("Active", StringComparison.OrdinalIgnoreCase));
        var disconnectedSessions = sessions.Count(s => s.State.Equals("Disc", StringComparison.OrdinalIgnoreCase));

        var today = _clock.Today;
        var actionsToday = await _auditLogRepository.CountForDateAsync(today, onlyErrors: false, cancellationToken);
        var errorsToday = await _auditLogRepository.CountForDateAsync(today, onlyErrors: true, cancellationToken);

        var metrics = new DashboardMetricsDto(activeSessions, disconnectedSessions, actionsToday, errorsToday, _clock.UtcNow);
        return Result<DashboardMetricsDto>.Success(metrics);
    }
}

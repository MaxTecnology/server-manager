using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Dashboard;
using SessionManager.Application.DTOs.Sessions;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IServerRepository _serverRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISessionService _sessionService;
    private readonly IClock _clock;

    public DashboardService(
        IServerRepository serverRepository,
        IAuditLogRepository auditLogRepository,
        ISessionService sessionService,
        IClock clock)
    {
        _serverRepository = serverRepository;
        _auditLogRepository = auditLogRepository;
        _sessionService = sessionService;
        _clock = clock;
    }

    public async Task<Result<DashboardMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<SessionInfoDto>();
        var servers = await _serverRepository.GetAllAsync(cancellationToken);
        foreach (var server in servers.Where(s => s.IsActive))
        {
            var result = await _sessionService.GetSessionsAsync(server.Hostname, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                sessions.AddRange(result.Value);
            }
        }

        var activeSessions = sessions.Count(s => s.State.Equals("Active", StringComparison.OrdinalIgnoreCase));
        var disconnectedSessions = sessions.Count(s => s.State.Equals("Disc", StringComparison.OrdinalIgnoreCase));

        var today = _clock.Today;
        var actionsToday = await _auditLogRepository.CountForDateAsync(today, onlyErrors: false, cancellationToken);
        var errorsToday = await _auditLogRepository.CountForDateAsync(today, onlyErrors: true, cancellationToken);

        var metrics = new DashboardMetricsDto(activeSessions, disconnectedSessions, actionsToday, errorsToday, _clock.UtcNow);
        return Result<DashboardMetricsDto>.Success(metrics);
    }
}

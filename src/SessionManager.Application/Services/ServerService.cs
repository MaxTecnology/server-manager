using SessionManager.Application.DTOs.Servers;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class ServerService : IServerService
{
    private static readonly TimeSpan AgentOnlineThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SnapshotFreshnessThreshold = TimeSpan.FromMinutes(5);

    private readonly IServerRepository _serverRepository;
    private readonly IClock _clock;

    public ServerService(IServerRepository serverRepository, IClock clock)
    {
        _serverRepository = serverRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ServerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var servers = await _serverRepository.GetAllAsync(cancellationToken);
        var now = _clock.UtcNow;

        return servers
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Select(s => new ServerDto(
                s.Id,
                s.Name,
                s.Hostname,
                s.IsDefault,
                s.IsActive,
                s.AgentId,
                s.AgentVersion,
                s.AgentLastHeartbeatUtc,
                s.AgentSessionSnapshotUtc,
                s.AgentLastHeartbeatUtc.HasValue && (now - s.AgentLastHeartbeatUtc.Value) <= AgentOnlineThreshold,
                s.AgentSessionSnapshotUtc.HasValue && (now - s.AgentSessionSnapshotUtc.Value) <= SnapshotFreshnessThreshold))
            .ToArray();
    }
}

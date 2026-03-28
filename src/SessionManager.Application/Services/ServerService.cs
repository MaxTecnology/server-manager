using SessionManager.Application.DTOs.Servers;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class ServerService : IServerService
{
    private readonly IServerRepository _serverRepository;

    public ServerService(IServerRepository serverRepository)
    {
        _serverRepository = serverRepository;
    }

    public async Task<IReadOnlyList<ServerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var servers = await _serverRepository.GetAllAsync(cancellationToken);
        return servers
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Select(s => new ServerDto(s.Id, s.Name, s.Hostname, s.IsDefault, s.IsActive))
            .ToArray();
    }
}

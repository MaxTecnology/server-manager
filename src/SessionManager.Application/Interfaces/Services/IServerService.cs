using SessionManager.Application.DTOs.Servers;

namespace SessionManager.Application.Interfaces.Services;

public interface IServerService
{
    Task<IReadOnlyList<ServerDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface IServerRepository
{
    Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Server?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Server?> GetDefaultAsync(CancellationToken cancellationToken = default);
    void Add(Server server);
}

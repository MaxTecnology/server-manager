using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}

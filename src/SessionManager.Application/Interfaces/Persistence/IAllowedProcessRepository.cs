using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface IAllowedProcessRepository
{
    Task<IReadOnlyList<AllowedProcess>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllowedProcess>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<AllowedProcess?> GetByNameAsync(string processName, CancellationToken cancellationToken = default);
    Task<AllowedProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(AllowedProcess process);
}

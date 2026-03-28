using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface ISettingRepository
{
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    void Add(Setting setting);
}

using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class SettingRepository : ISettingRepository
{
    private readonly AppDbContext _dbContext;

    public SettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Settings.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Settings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public void Add(Setting setting)
    {
        _dbContext.Settings.Add(setting);
    }
}

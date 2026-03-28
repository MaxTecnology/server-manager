using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class ServerRepository : IServerRepository
{
    private readonly AppDbContext _dbContext;

    public ServerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Servers.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Server?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Servers.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<Server?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Servers.FirstOrDefaultAsync(x => x.IsDefault && x.IsActive, cancellationToken);
    }

    public void Add(Server server)
    {
        _dbContext.Servers.Add(server);
    }
}

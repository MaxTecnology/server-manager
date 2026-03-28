using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Roles.AddAsync(role, cancellationToken);
    }
}

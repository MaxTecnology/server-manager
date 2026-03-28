using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class AllowedProcessRepository : IAllowedProcessRepository
{
    private readonly AppDbContext _dbContext;

    public AllowedProcessRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AllowedProcess>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AllowedProcesses.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AllowedProcess>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AllowedProcesses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<AllowedProcess?> GetByNameAsync(string processName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AllowedProcesses
            .FirstOrDefaultAsync(x => x.ProcessName == processName, cancellationToken);
    }

    public async Task<AllowedProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AllowedProcesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public void Add(AllowedProcess process)
    {
        _dbContext.AllowedProcesses.Add(process);
    }
}

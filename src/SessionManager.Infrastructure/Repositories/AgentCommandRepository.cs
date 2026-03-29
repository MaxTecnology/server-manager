using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Constants;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class AgentCommandRepository : IAgentCommandRepository
{
    private readonly AppDbContext _dbContext;

    public AgentCommandRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentCommand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentCommands
            .Include(x => x.Server)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AgentCommand?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentCommands
            .AsNoTracking()
            .Include(x => x.Server)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AgentCommand?> GetNextPendingAsync(Guid serverId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentCommands
            .Where(x => x.ServerId == serverId && x.Status == AgentCommandStatuses.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(AgentCommand command)
    {
        _dbContext.AgentCommands.Add(command);
    }
}

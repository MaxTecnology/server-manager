using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface IAgentCommandRepository
{
    Task<AgentCommand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentCommand?> GetNextPendingAsync(Guid serverId, CancellationToken cancellationToken = default);
    void Add(AgentCommand command);
}

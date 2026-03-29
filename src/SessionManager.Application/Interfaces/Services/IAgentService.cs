using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Agent;

namespace SessionManager.Application.Interfaces.Services;

public interface IAgentService
{
    Task<Result<AgentHeartbeatResponseDto>> RegisterHeartbeatAsync(
        AgentHeartbeatRequestDto request,
        string? clientIpAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AgentCommandDto>> EnqueueCommandAsync(
        Guid serverId,
        EnqueueAgentCommandRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);

    Task<Result<AgentCommandDispatchDto?>> GetNextCommandAsync(
        AgentPollRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteCommandAsync(
        Guid commandId,
        AgentCommandResultRequestDto request,
        string? clientIpAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AgentCommandDto>> GetCommandAsync(Guid commandId, CancellationToken cancellationToken = default);
}

using SessionManager.Application.Common;
using SessionManager.Application.DTOs.ActiveDirectory;
using SessionManager.Application.DTOs.Agent;

namespace SessionManager.Application.Interfaces.Services;

public interface IActiveDirectoryService
{
    Task<Result<AgentCommandDto>> CreateUserAsync(
        Guid serverId,
        CreateAdUserRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);

    Task<Result<AgentCommandDto>> ResetPasswordAsync(
        Guid serverId,
        string username,
        ResetAdUserPasswordRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);
}

using SessionManager.Application.Common;
using SessionManager.Application.DTOs.ActiveDirectory;
using SessionManager.Application.DTOs.Agent;

namespace SessionManager.Application.Interfaces.Services;

public interface IActiveDirectoryService
{
    Task<Result<IReadOnlyList<AdUserSearchItemDto>>> SearchUsersAsync(
        Guid serverId,
        SearchAdUsersRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AdOrganizationalUnitDto>>> GetOrganizationalUnitsAsync(
        Guid serverId,
        CancellationToken cancellationToken = default);

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

    Task<Result<AgentCommandDto>> BlockUserAsync(
        Guid serverId,
        string username,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);

    Task<Result<AgentCommandDto>> UnblockUserAsync(
        Guid serverId,
        string username,
        ActionContext actionContext,
        CancellationToken cancellationToken = default);
}

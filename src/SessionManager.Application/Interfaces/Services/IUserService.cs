using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Users;

namespace SessionManager.Application.Interfaces.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> SetStatusAsync(Guid userId, UpdateUserStatusRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> SetPasswordAsync(Guid userId, UpdateUserPasswordRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> SetRolesAsync(Guid userId, UpdateUserRolesRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
}

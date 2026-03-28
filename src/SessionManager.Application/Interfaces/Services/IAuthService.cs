using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Auth;

namespace SessionManager.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

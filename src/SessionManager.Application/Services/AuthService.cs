using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Auth;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponseDto>.Failure("Usuário e senha são obrigatórios.");
        }

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<LoginResponseDto>.Failure("Credenciais inválidas.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<LoginResponseDto>.Failure("Credenciais inválidas.");
        }

        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tokenData = _tokenService.CreateToken(user, roles);
        var payload = new LoginResponseDto(
            tokenData.Token,
            tokenData.ExpiresAtUtc,
            new UserSummaryDto(user.Id, user.Username, user.DisplayName, roles));

        return Result<LoginResponseDto>.Success(payload);
    }
}

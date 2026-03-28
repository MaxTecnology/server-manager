using System.Text.RegularExpressions;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Users;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class UserService : IUserService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9._-]{3,40}$", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllWithRolesAsync(cancellationToken);
        return users
            .OrderBy(u => u.Username)
            .Select(MapUser)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return roles.Select(r => r.Name).OrderBy(r => r).ToArray();
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        if (!UsernameRegex.IsMatch(username))
        {
            return Result<UserDto>.Failure("Username inválido. Use 3-40 caracteres (letras, números, ponto, underline ou hífen).");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Result<UserDto>.Failure("Senha deve ter no mínimo 8 caracteres.");
        }

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            return Result<UserDto>.Failure("Já existe usuário com esse username.");
        }

        var resolvedRoles = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (!resolvedRoles.IsSuccess || resolvedRoles.Value is null)
        {
            return Result<UserDto>.Failure(resolvedRoles.Error ?? "Perfis inválidos.");
        }

        var user = new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? username : request.DisplayName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        foreach (var role in resolvedRoles.Value)
        {
            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = role,
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _userRepository.AddAsync(user, cancellationToken);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "USER_CREATE",
            ServerName = "SECURITY",
            TargetUsername = user.Username,
            Success = true,
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<UserDto>.Success(MapUser(user));
    }

    public async Task<Result> SetStatusAsync(Guid userId, UpdateUserStatusRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = _clock.UtcNow;

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "USER_STATUS_UPDATE",
            ServerName = "SECURITY",
            TargetUsername = user.Username,
            Success = true,
            MetadataJson = $"{{\"isActive\":{request.IsActive.ToString().ToLowerInvariant()}}}",
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetPasswordAsync(Guid userId, UpdateUserPasswordRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("UsuÃ¡rio nÃ£o encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Result.Failure("Senha deve ter no mÃ­nimo 8 caracteres.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        user.UpdatedAtUtc = _clock.UtcNow;

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "USER_PASSWORD_UPDATE",
            ServerName = "SECURITY",
            TargetUsername = user.Username,
            Success = true,
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetRolesAsync(Guid userId, UpdateUserRolesRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        var resolvedRoles = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (!resolvedRoles.IsSuccess || resolvedRoles.Value is null)
        {
            return Result.Failure(resolvedRoles.Error ?? "Perfis inválidos.");
        }

        user.UserRoles.Clear();
        foreach (var role in resolvedRoles.Value)
        {
            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = role,
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        user.UpdatedAtUtc = _clock.UtcNow;

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "USER_ROLES_UPDATE",
            ServerName = "SECURITY",
            TargetUsername = user.Username,
            Success = true,
            MetadataJson = $"{{\"roles\":\"{string.Join(',', resolvedRoles.Value.Select(r => r.Name))}\"}}",
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<Role>>> ResolveRolesAsync(IReadOnlyCollection<string> requestedRoles, CancellationToken cancellationToken)
    {
        if (requestedRoles.Count == 0)
        {
            return Result<IReadOnlyList<Role>>.Failure("Informe ao menos um perfil.");
        }

        var distinctRoles = requestedRoles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctRoles.Any(r => !RoleNames.All.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            return Result<IReadOnlyList<Role>>.Failure("Perfil inválido.");
        }

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var mapped = roles.Where(r => distinctRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

        if (mapped.Length != distinctRoles.Length)
        {
            return Result<IReadOnlyList<Role>>.Failure("Um ou mais perfis não existem no banco.");
        }

        return Result<IReadOnlyList<Role>>.Success(mapped);
    }

    private static UserDto MapUser(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToArray();
        return new UserDto(user.Id, user.Username, user.DisplayName, user.IsActive, roles);
    }
}

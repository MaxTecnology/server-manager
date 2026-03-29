using System.Text.RegularExpressions;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.ActiveDirectory;
using SessionManager.Application.DTOs.Agent;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class ActiveDirectoryService : IActiveDirectoryService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9._-]{3,64}$", RegexOptions.Compiled);

    private readonly IServerRepository _serverRepository;
    private readonly IAgentService _agentService;
    private readonly IAgentCommandProtector _agentCommandProtector;

    public ActiveDirectoryService(
        IServerRepository serverRepository,
        IAgentService agentService,
        IAgentCommandProtector agentCommandProtector)
    {
        _serverRepository = serverRepository;
        _agentService = agentService;
        _agentCommandProtector = agentCommandProtector;
    }

    public async Task<Result<AgentCommandDto>> CreateUserAsync(
        Guid serverId,
        CreateAdUserRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default)
    {
        var serverValidation = await ValidateServerSupportsAdAsync(serverId, cancellationToken);
        if (!serverValidation.IsSuccess)
        {
            return Result<AgentCommandDto>.Failure(serverValidation.Error ?? "Servidor inválido para operação AD.");
        }

        var username = request.Username.Trim();
        if (!UsernameRegex.IsMatch(username))
        {
            return Result<AgentCommandDto>.Failure("Username AD inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Result<AgentCommandDto>.Failure("Senha AD deve ter no mínimo 8 caracteres.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? username
            : request.DisplayName.Trim();

        var plainCommand = BuildCreateUserCommand(
            username,
            displayName,
            request.Password,
            request.UserPrincipalName,
            request.OrganizationalUnitPath,
            request.ChangePasswordAtLogon);

        var protectedCommand = _agentCommandProtector.ProtectCommand(plainCommand);

        return await _agentService.EnqueueCommandAsync(
            serverId,
            new EnqueueAgentCommandRequestDto { CommandText = protectedCommand },
            actionContext,
            cancellationToken);
    }

    public async Task<Result<AgentCommandDto>> ResetPasswordAsync(
        Guid serverId,
        string username,
        ResetAdUserPasswordRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default)
    {
        var serverValidation = await ValidateServerSupportsAdAsync(serverId, cancellationToken);
        if (!serverValidation.IsSuccess)
        {
            return Result<AgentCommandDto>.Failure(serverValidation.Error ?? "Servidor inválido para operação AD.");
        }

        var normalizedUsername = username.Trim();
        if (!UsernameRegex.IsMatch(normalizedUsername))
        {
            return Result<AgentCommandDto>.Failure("Username AD inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Result<AgentCommandDto>.Failure("Senha AD deve ter no mínimo 8 caracteres.");
        }

        var plainCommand = BuildResetPasswordCommand(
            normalizedUsername,
            request.Password,
            request.ChangePasswordAtLogon,
            request.EnableAccount);

        var protectedCommand = _agentCommandProtector.ProtectCommand(plainCommand);

        return await _agentService.EnqueueCommandAsync(
            serverId,
            new EnqueueAgentCommandRequestDto { CommandText = protectedCommand },
            actionContext,
            cancellationToken);
    }

    private static string BuildCreateUserCommand(
        string username,
        string displayName,
        string password,
        string? userPrincipalName,
        string? organizationalUnitPath,
        bool changePasswordAtLogon)
    {
        var usernamePs = Ps(username);
        var displayPs = Ps(displayName);
        var passwordPs = Ps(password);

        var upnPart = string.IsNullOrWhiteSpace(userPrincipalName)
            ? string.Empty
            : $" -UserPrincipalName '{Ps(userPrincipalName.Trim())}'";

        var ouPart = string.IsNullOrWhiteSpace(organizationalUnitPath)
            ? string.Empty
            : $" -Path '{Ps(organizationalUnitPath.Trim())}'";

        var changeAtLogonLiteral = changePasswordAtLogon ? "$true" : "$false";

        return
            $"Import-Module ActiveDirectory;$p=ConvertTo-SecureString '{passwordPs}' -AsPlainText -Force;New-ADUser -Name '{displayPs}' -SamAccountName '{usernamePs}' -DisplayName '{displayPs}' -AccountPassword $p -Enabled $true{upnPart}{ouPart};Set-ADUser -Identity '{usernamePs}' -ChangePasswordAtLogon {changeAtLogonLiteral};Write-Output AD_USER_CREATE_OK";
    }

    private static string BuildResetPasswordCommand(
        string username,
        string password,
        bool changePasswordAtLogon,
        bool enableAccount)
    {
        var usernamePs = Ps(username);
        var passwordPs = Ps(password);
        var changeAtLogonLiteral = changePasswordAtLogon ? "$true" : "$false";

        var enableAccountPart = enableAccount
            ? $";Enable-ADAccount -Identity '{usernamePs}'"
            : string.Empty;

        return
            $"Import-Module ActiveDirectory;$p=ConvertTo-SecureString '{passwordPs}' -AsPlainText -Force;Set-ADAccountPassword -Identity '{usernamePs}' -Reset -NewPassword $p;Set-ADUser -Identity '{usernamePs}' -ChangePasswordAtLogon {changeAtLogonLiteral}{enableAccountPart};Write-Output AD_PASSWORD_RESET_OK";
    }

    private static string Ps(string value)
    {
        return value.Replace("'", "''");
    }

    private async Task<Result> ValidateServerSupportsAdAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var server = await _serverRepository.GetByIdAsync(serverId, cancellationToken);
        if (server is null)
        {
            return Result.Failure("Servidor não encontrado.");
        }

        if (!server.IsActive)
        {
            return Result.Failure("Servidor inativo.");
        }

        if (!server.SupportsAd)
        {
            return Result.Failure("Servidor não possui capacidade Active Directory habilitada.");
        }

        return Result.Success();
    }
}

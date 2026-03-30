using System.Text.Json;
using System.Text.RegularExpressions;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.ActiveDirectory;
using SessionManager.Application.DTOs.Agent;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class ActiveDirectoryService : IActiveDirectoryService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9._-]{3,64}$", RegexOptions.Compiled);
    private static readonly TimeSpan AgentHeartbeatFreshness = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AdOuSnapshotFreshness = TimeSpan.FromMinutes(15);

    private readonly IServerRepository _serverRepository;
    private readonly IAgentService _agentService;
    private readonly IAgentCommandProtector _agentCommandProtector;
    private readonly IClock _clock;

    public ActiveDirectoryService(
        IServerRepository serverRepository,
        IAgentService agentService,
        IAgentCommandProtector agentCommandProtector,
        IClock clock)
    {
        _serverRepository = serverRepository;
        _agentService = agentService;
        _agentCommandProtector = agentCommandProtector;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<AdOrganizationalUnitDto>>> GetOrganizationalUnitsAsync(
        Guid serverId,
        CancellationToken cancellationToken = default)
    {
        var serverResult = await GetValidatedAdServerAsync(serverId, cancellationToken);
        if (!serverResult.IsSuccess || serverResult.Value is null)
        {
            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Failure(
                serverResult.Error ?? "Servidor inválido para operação AD.");
        }

        var server = serverResult.Value;

        if (!HasRecentHeartbeat(server))
        {
            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Failure(
                "Agent sem heartbeat recente para este servidor AD.");
        }

        if (!HasRecentAdOuSnapshot(server))
        {
            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Failure(
                "Snapshot de OUs desatualizado. Verifique o agent AD.");
        }

        return ParseOrganizationalUnits(server.AgentAdOuSnapshotOutput);
    }

    public async Task<Result<AgentCommandDto>> CreateUserAsync(
        Guid serverId,
        CreateAdUserRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default)
    {
        var serverValidation = await GetValidatedAdServerAsync(serverId, cancellationToken);
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
        var serverValidation = await GetValidatedAdServerAsync(serverId, cancellationToken);
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

    private async Task<Result<Server>> GetValidatedAdServerAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var server = await _serverRepository.GetByIdAsync(serverId, cancellationToken);
        if (server is null)
        {
            return Result<Server>.Failure("Servidor não encontrado.");
        }

        if (!server.IsActive)
        {
            return Result<Server>.Failure("Servidor inativo.");
        }

        if (!server.SupportsAd)
        {
            return Result<Server>.Failure("Servidor não possui capacidade Active Directory habilitada.");
        }

        return Result<Server>.Success(server);
    }

    private bool HasRecentHeartbeat(Server server)
    {
        if (server.AgentLastHeartbeatUtc is null)
        {
            return false;
        }

        return _clock.UtcNow - server.AgentLastHeartbeatUtc.Value <= AgentHeartbeatFreshness;
    }

    private bool HasRecentAdOuSnapshot(Server server)
    {
        if (server.AgentAdOuSnapshotUtc is null)
        {
            return false;
        }

        return _clock.UtcNow - server.AgentAdOuSnapshotUtc.Value <= AdOuSnapshotFreshness;
    }

    private static Result<IReadOnlyList<AdOrganizationalUnitDto>> ParseOrganizationalUnits(string? snapshotOutput)
    {
        if (string.IsNullOrWhiteSpace(snapshotOutput))
        {
            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Success(Array.Empty<AdOrganizationalUnitDto>());
        }

        try
        {
            using var document = JsonDocument.Parse(snapshotOutput);
            var units = new List<AdOrganizationalUnitDto>();

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (TryMapOrganizationalUnit(item, out var mapped))
                    {
                        units.Add(mapped);
                    }
                }
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (TryMapOrganizationalUnit(document.RootElement, out var mapped))
                {
                    units.Add(mapped);
                }
            }
            else
            {
                return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Failure(
                    "Formato inválido no snapshot de OUs recebido do agent.");
            }

            if (units.Count == 0)
            {
                return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Success(Array.Empty<AdOrganizationalUnitDto>());
            }

            var distinct = units
                .GroupBy(item => item.DistinguishedName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Success(distinct);
        }
        catch (JsonException)
        {
            return Result<IReadOnlyList<AdOrganizationalUnitDto>>.Failure(
                "Snapshot de OUs inválido. Verifique o agent AD.");
        }
    }

    private static bool TryMapOrganizationalUnit(JsonElement element, out AdOrganizationalUnitDto mapped)
    {
        mapped = default!;

        if (!TryGetPropertyString(element, "DistinguishedName", out var distinguishedName)
            || string.IsNullOrWhiteSpace(distinguishedName))
        {
            return false;
        }

        TryGetPropertyString(element, "CanonicalName", out var canonicalName);
        TryGetPropertyString(element, "Name", out var name);

        var resolvedCanonical = string.IsNullOrWhiteSpace(canonicalName)
            ? distinguishedName
            : canonicalName.Trim();

        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? ResolveLeafNameFromCanonical(resolvedCanonical)
            : name.Trim();

        mapped = new AdOrganizationalUnitDto(
            resolvedName,
            distinguishedName.Trim(),
            resolvedCanonical,
            CalculateDepth(resolvedCanonical));

        return true;
    }

    private static string ResolveLeafNameFromCanonical(string canonicalName)
    {
        var parts = canonicalName
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 0
            ? canonicalName
            : parts[^1];
    }

    private static int CalculateDepth(string canonicalName)
    {
        var parts = canonicalName
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length <= 1)
        {
            return 0;
        }

        return Math.Max(0, parts.Length - 2);
    }

    private static bool TryGetPropertyString(JsonElement element, string propertyName, out string? value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!property.NameEquals(propertyName) && !property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                value = null;
                return true;
            }

            value = property.Value.GetString();
            return true;
        }

        value = null;
        return false;
    }
}

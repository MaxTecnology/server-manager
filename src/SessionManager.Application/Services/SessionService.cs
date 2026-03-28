using System.Text.RegularExpressions;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Sessions;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Application.Interfaces.Windows;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class SessionService : ISessionService
{
    private static readonly Regex ProcessNameRegex = new("^[a-zA-Z0-9._-]{1,64}(\\.exe)?$", RegexOptions.Compiled);

    private readonly IWindowsSessionGateway _windowsSessionGateway;
    private readonly IServerRepository _serverRepository;
    private readonly IAllowedProcessRepository _allowedProcessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SessionService(
        IWindowsSessionGateway windowsSessionGateway,
        IServerRepository serverRepository,
        IAllowedProcessRepository allowedProcessRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _windowsSessionGateway = windowsSessionGateway;
        _serverRepository = serverRepository;
        _allowedProcessRepository = allowedProcessRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<SessionInfoDto>>> GetSessionsAsync(string? serverName, CancellationToken cancellationToken = default)
    {
        var resolvedServer = await ResolveServerNameAsync(serverName, cancellationToken);
        var result = await _windowsSessionGateway.ListSessionsAsync(resolvedServer, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Failure(result.Error ?? "Falha ao obter sessões.");
        }

        var sessions = result.Value
            .Select(s => new SessionInfoDto(
                s.SessionId,
                s.Username,
                s.SessionName,
                s.State,
                s.IdleTime,
                s.LogonTime,
                s.ServerName))
            .OrderBy(s => s.SessionId)
            .ToArray();

        return Result<IReadOnlyList<SessionInfoDto>>.Success(sessions);
    }

    public async Task<Result> DisconnectAsync(SessionActionRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        if (request.SessionId <= 0)
        {
            return Result.Failure("ID de sessão inválido.");
        }

        var serverName = await ResolveServerNameAsync(request.ServerName, cancellationToken);
        var execution = await _windowsSessionGateway.DisconnectAsync(serverName, request.SessionId, cancellationToken);

        await RegisterAuditAsync(
            action: "DISCONNECT",
            actionContext: actionContext,
            serverName: serverName,
            sessionId: request.SessionId,
            targetUsername: request.TargetUsername,
            processName: null,
            success: execution.IsSuccess,
            error: execution.Error,
            cancellationToken);

        return execution.IsSuccess ? Result.Success() : Result.Failure(execution.Error ?? "Falha ao desconectar sessão.");
    }

    public async Task<Result> LogoffAsync(SessionActionRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        if (request.SessionId <= 0)
        {
            return Result.Failure("ID de sessão inválido.");
        }

        var serverName = await ResolveServerNameAsync(request.ServerName, cancellationToken);
        var execution = await _windowsSessionGateway.LogoffAsync(serverName, request.SessionId, cancellationToken);

        await RegisterAuditAsync(
            action: "LOGOFF",
            actionContext: actionContext,
            serverName: serverName,
            sessionId: request.SessionId,
            targetUsername: request.TargetUsername,
            processName: null,
            success: execution.IsSuccess,
            error: execution.Error,
            cancellationToken);

        return execution.IsSuccess ? Result.Success() : Result.Failure(execution.Error ?? "Falha ao fazer logoff.");
    }

    public async Task<Result> KillProcessAsync(KillProcessRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        if (request.SessionId <= 0)
        {
            return Result.Failure("ID de sessão inválido.");
        }

        var normalizedProcess = NormalizeProcessName(request.ProcessName);
        if (normalizedProcess is null)
        {
            return Result.Failure("Nome de processo inválido.");
        }

        var allowed = await _allowedProcessRepository.GetActiveAsync(cancellationToken);
        var isAllowed = allowed.Any(p => p.ProcessName.Equals(normalizedProcess, StringComparison.OrdinalIgnoreCase));
        if (!isAllowed)
        {
            await RegisterAuditAsync(
                action: "TASKKILL_BLOCKED",
                actionContext: actionContext,
                serverName: request.ServerName ?? string.Empty,
                sessionId: request.SessionId,
                targetUsername: request.TargetUsername,
                processName: normalizedProcess,
                success: false,
                error: "Processo não permitido.",
                cancellationToken);

            return Result.Failure("Processo não permitido pela política atual.");
        }

        var serverName = await ResolveServerNameAsync(request.ServerName, cancellationToken);
        var execution = await _windowsSessionGateway.KillProcessAsync(serverName, request.SessionId, normalizedProcess, cancellationToken);

        await RegisterAuditAsync(
            action: "TASKKILL",
            actionContext: actionContext,
            serverName: serverName,
            sessionId: request.SessionId,
            targetUsername: request.TargetUsername,
            processName: normalizedProcess,
            success: execution.IsSuccess,
            error: execution.Error,
            cancellationToken);

        return execution.IsSuccess ? Result.Success() : Result.Failure(execution.Error ?? "Falha ao encerrar processo.");
    }

    private async Task<string> ResolveServerNameAsync(string? requestedServerName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedServerName))
        {
            return requestedServerName.Trim();
        }

        var defaultServer = await _serverRepository.GetDefaultAsync(cancellationToken);
        return defaultServer?.Hostname ?? Environment.MachineName;
    }

    private static string? NormalizeProcessName(string processName)
    {
        var candidate = processName.Trim();
        if (!ProcessNameRegex.IsMatch(candidate))
        {
            return null;
        }

        if (!candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            candidate = $"{candidate}.exe";
        }

        return candidate.ToLowerInvariant();
    }

    private async Task RegisterAuditAsync(
        string action,
        ActionContext actionContext,
        string serverName,
        int? sessionId,
        string? targetUsername,
        string? processName,
        bool success,
        string? error,
        CancellationToken cancellationToken)
    {
        var log = new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = action,
            ServerName = string.IsNullOrWhiteSpace(serverName) ? "N/A" : serverName,
            SessionId = sessionId,
            TargetUsername = targetUsername,
            ProcessName = processName,
            Success = success,
            ErrorMessage = error,
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        };

        await _auditLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

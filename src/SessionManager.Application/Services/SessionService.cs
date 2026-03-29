using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Sessions;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Application.Interfaces.Windows;
using SessionManager.Domain.Constants;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class SessionService : ISessionService
{
    private static readonly Regex ProcessNameRegex = new("^[a-zA-Z0-9._-]{1,64}(\\.exe)?$", RegexOptions.Compiled);
    private static readonly Regex QueryUserPattern = new(
        "^(?<USERNAME>\\S+)?\\s+(?<SESSIONNAME>\\S+)?\\s+(?<ID>\\d+)\\s+(?<STATE>\\S+)\\s+(?<IDLETIME>.+?)\\s+(?<LOGONTIME>\\d{1,2}[/-]\\d{1,2}[/-]\\d{2,4}.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly TimeSpan AgentCommandWaitTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan AgentCommandPollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan AgentHeartbeatFreshness = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AgentSessionSnapshotFreshness = TimeSpan.FromMinutes(5);

    private readonly IWindowsSessionGateway _windowsSessionGateway;
    private readonly IServerRepository _serverRepository;
    private readonly IAgentCommandRepository _agentCommandRepository;
    private readonly IAllowedProcessRepository _allowedProcessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SessionService(
        IWindowsSessionGateway windowsSessionGateway,
        IServerRepository serverRepository,
        IAgentCommandRepository agentCommandRepository,
        IAllowedProcessRepository allowedProcessRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _windowsSessionGateway = windowsSessionGateway;
        _serverRepository = serverRepository;
        _agentCommandRepository = agentCommandRepository;
        _allowedProcessRepository = allowedProcessRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<SessionInfoDto>>> GetSessionsAsync(string? serverName, CancellationToken cancellationToken = default)
    {
        var resolvedServer = await ResolveServerNameAsync(serverName, cancellationToken);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var directResult = await _windowsSessionGateway.ListSessionsAsync(resolvedServer, cancellationToken);
            if (!directResult.IsSuccess || directResult.Value is null)
            {
                return Result<IReadOnlyList<SessionInfoDto>>.Failure(directResult.Error ?? "Falha ao obter sessões.");
            }

            var directSessions = directResult.Value
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

            return Result<IReadOnlyList<SessionInfoDto>>.Success(directSessions);
        }

        var server = await _serverRepository.GetByHostnameAsync(resolvedServer, cancellationToken);
        if (server is null)
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Failure("Servidor não encontrado para consulta de sessões.");
        }

        if (!HasRecentHeartbeat(server))
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Failure("Agent sem heartbeat recente para este servidor.");
        }

        if (!HasRecentSessionSnapshot(server))
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Failure("Snapshot de sessões desatualizado. Verifique o agent.");
        }

        var snapshotOutput = server.AgentSessionSnapshotOutput ?? string.Empty;
        if (string.IsNullOrWhiteSpace(snapshotOutput))
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Success(Array.Empty<SessionInfoDto>());
        }

        var parsed = ParseQueryUser(snapshotOutput, resolvedServer);
        if (parsed.Count > 0)
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Success(MapSessions(parsed));
        }

        if (LooksLikeQueryUserHeader(snapshotOutput) || IsNoSessionsMessage(snapshotOutput, null))
        {
            return Result<IReadOnlyList<SessionInfoDto>>.Success(Array.Empty<SessionInfoDto>());
        }

        return Result<IReadOnlyList<SessionInfoDto>>.Failure("Snapshot de sessões retornado pelo agent não pôde ser interpretado.");
    }

    public async Task<Result> DisconnectAsync(SessionActionRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        if (request.SessionId <= 0)
        {
            return Result.Failure("ID de sessão inválido.");
        }

        var serverName = await ResolveServerNameAsync(request.ServerName, cancellationToken);
        var execution = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await _windowsSessionGateway.DisconnectAsync(serverName, request.SessionId, cancellationToken)
            : await ExecuteAgentActionAsync(serverName, $"rwinsta {request.SessionId}", actionContext.OperatorUsername, cancellationToken);

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
        var execution = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await _windowsSessionGateway.LogoffAsync(serverName, request.SessionId, cancellationToken)
            : await ExecuteAgentActionAsync(serverName, $"logoff {request.SessionId}", actionContext.OperatorUsername, cancellationToken);

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
        var execution = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await _windowsSessionGateway.KillProcessAsync(serverName, request.SessionId, normalizedProcess, cancellationToken)
            : await ExecuteAgentActionAsync(
                serverName,
                $"taskkill /FI \"SESSION eq {request.SessionId}\" /IM {normalizedProcess} /F",
                actionContext.OperatorUsername,
                cancellationToken);

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

    private async Task<Result> ExecuteAgentActionAsync(
        string serverName,
        string commandText,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAgentCommandAsync(serverName, commandText, requestedBy, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Result.Failure(result.Error ?? "Falha ao executar comando via agent.");
        }

        return result.Value.Status == AgentCommandStatuses.Succeeded
            ? Result.Success()
            : Result.Failure(result.Value.ErrorMessage ?? "Comando rejeitado pelo agent.");
    }

    private async Task<Result<AgentCommand>> ExecuteAgentCommandAsync(
        string serverName,
        string commandText,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        var server = await _serverRepository.GetByHostnameAsync(serverName, cancellationToken);
        if (server is null)
        {
            return Result<AgentCommand>.Failure("Servidor não encontrado para operação via agent.");
        }

        if (!server.IsActive)
        {
            return Result<AgentCommand>.Failure("Servidor inativo.");
        }

        if (!HasRecentHeartbeat(server))
        {
            return Result<AgentCommand>.Failure("Agent sem heartbeat recente para este servidor.");
        }

        var command = new AgentCommand
        {
            ServerId = server.Id,
            Server = server,
            RequestedBy = Truncate(requestedBy, 120)!,
            CommandText = commandText,
            Status = AgentCommandStatuses.Pending,
            CreatedAtUtc = _clock.UtcNow
        };

        _agentCommandRepository.Add(command);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var deadline = _clock.UtcNow.Add(AgentCommandWaitTimeout);
        while (_clock.UtcNow < deadline)
        {
            await Task.Delay(AgentCommandPollInterval, cancellationToken);

            var latest = await _agentCommandRepository.GetByIdReadOnlyAsync(command.Id, cancellationToken);
            if (latest is null)
            {
                return Result<AgentCommand>.Failure("Comando não encontrado após enfileirar.");
            }

            if (latest.Status is AgentCommandStatuses.Succeeded or AgentCommandStatuses.Failed)
            {
                return Result<AgentCommand>.Success(latest);
            }
        }

        return Result<AgentCommand>.Failure("Tempo limite aguardando retorno do agent.");
    }

    private bool HasRecentHeartbeat(Server server)
    {
        if (server.AgentLastHeartbeatUtc is null)
        {
            return false;
        }

        return _clock.UtcNow - server.AgentLastHeartbeatUtc.Value <= AgentHeartbeatFreshness;
    }

    private bool HasRecentSessionSnapshot(Server server)
    {
        if (server.AgentSessionSnapshotUtc is null)
        {
            return false;
        }

        return _clock.UtcNow - server.AgentSessionSnapshotUtc.Value <= AgentSessionSnapshotFreshness;
    }

    private static IReadOnlyList<SessionInfoDto> MapSessions(IReadOnlyList<WindowsSessionRecord> sessions)
    {
        return sessions
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
    }

    private static IReadOnlyList<WindowsSessionRecord> ParseQueryUser(string output, string serverName)
    {
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sessions = new List<WindowsSessionRecord>();

        foreach (var line in lines)
        {
            if (line.Contains("USERNAME", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("SESSIONNAME", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var clean = line.TrimStart();
            if (clean.StartsWith('>'))
            {
                clean = clean[1..].TrimStart();
            }

            if (string.IsNullOrWhiteSpace(clean))
            {
                continue;
            }

            var match = QueryUserPattern.Match(clean);
            if (match.Success)
            {
                sessions.Add(new WindowsSessionRecord(
                    SessionId: int.Parse(match.Groups["ID"].Value),
                    Username: match.Groups["USERNAME"].Value.Trim(),
                    SessionName: match.Groups["SESSIONNAME"].Value.Trim(),
                    State: match.Groups["STATE"].Value.Trim(),
                    IdleTime: match.Groups["IDLETIME"].Value.Trim(),
                    LogonTime: match.Groups["LOGONTIME"].Value.Trim(),
                    ServerName: serverName));
                continue;
            }

            var fallback = ParseWithFallback(clean, serverName);
            if (fallback is not null)
            {
                sessions.Add(fallback);
            }
        }

        return sessions.OrderBy(s => s.SessionId).ToArray();
    }

    private static WindowsSessionRecord? ParseWithFallback(string line, string serverName)
    {
        var compact = Regex.Replace(line.Trim(), "\\s+", " ");
        var idMatch = Regex.Match(compact, "\\b(?<id>\\d+)\\b");
        if (!idMatch.Success || !int.TryParse(idMatch.Groups["id"].Value, out var sessionId))
        {
            return null;
        }

        var left = compact[..idMatch.Index].Trim();
        var right = compact[(idMatch.Index + idMatch.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(right))
        {
            return null;
        }

        var rightParts = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (rightParts.Length < 2)
        {
            return null;
        }

        var state = rightParts[0];
        var tail = string.Join(" ", rightParts.Skip(1));

        var timeMatch = Regex.Match(
            tail,
            "(?<idle>.*?)\\s+(?<logon>\\d{1,2}[/-]\\d{1,2}[/-]\\d{2,4}.+)$",
            RegexOptions.IgnoreCase);

        var idle = timeMatch.Success ? timeMatch.Groups["idle"].Value.Trim() : tail;
        var logon = timeMatch.Success ? timeMatch.Groups["logon"].Value.Trim() : string.Empty;

        var leftParts = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var username = leftParts.Length >= 1 ? leftParts[0].Trim() : string.Empty;
        var sessionName = leftParts.Length >= 2 ? string.Join(" ", leftParts.Skip(1)).Trim() : string.Empty;

        return new WindowsSessionRecord(
            SessionId: sessionId,
            Username: username,
            SessionName: sessionName,
            State: state,
            IdleTime: string.IsNullOrWhiteSpace(idle) ? "." : idle,
            LogonTime: logon,
            ServerName: serverName);
    }

    private static bool LooksLikeQueryUserHeader(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        return output.Contains("USERNAME", StringComparison.OrdinalIgnoreCase)
               && output.Contains("SESSIONNAME", StringComparison.OrdinalIgnoreCase)
               && output.Contains("LOGON TIME", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoSessionsMessage(string? stdout, string? stderr)
    {
        var combined = $"{stdout}\n{stderr}";
        if (string.IsNullOrWhiteSpace(combined))
        {
            return false;
        }

        return combined.Contains("No User exists", StringComparison.OrdinalIgnoreCase)
               || combined.Contains("Nenhum usuario existe", StringComparison.OrdinalIgnoreCase)
               || combined.Contains("Nenhum usu", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
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

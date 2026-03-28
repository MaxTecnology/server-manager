using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SessionManager.Application.Common;
using SessionManager.Application.Interfaces.Windows;

namespace SessionManager.Infrastructure.Windows;

public sealed class WindowsSessionGateway : IWindowsSessionGateway
{
    private static readonly Regex QueryUserPattern = new(
        "^(?<USERNAME>\\S+)?\\s+(?<SESSIONNAME>\\S+)?\\s+(?<ID>\\d+)\\s+(?<STATE>Active|Disc|Listen|Idle|Down|Conn)\\s+(?<IDLETIME>.+?)\\s+(?<LOGONTIME>\\d{1,2}/\\d{1,2}/\\d{4}.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IWindowsCommandExecutor _commandExecutor;
    private readonly ILogger<WindowsSessionGateway> _logger;

    public WindowsSessionGateway(IWindowsCommandExecutor commandExecutor, ILogger<WindowsSessionGateway> logger)
    {
        _commandExecutor = commandExecutor;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WindowsSessionRecord>>> ListSessionsAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var command = await ExecuteWithLocalFallbackAsync(
            command: "query",
            serverName: serverName,
            remoteArguments: new[] { "user", $"/server:{serverName}" },
            localArguments: new[] { "user" },
            cancellationToken: cancellationToken);

        if (command.TimedOut)
        {
            return Result<IReadOnlyList<WindowsSessionRecord>>.Failure("Tempo limite excedido ao consultar sessoes.");
        }

        var sessions = ParseQueryUser(command.StandardOutput, serverName);

        // In some Windows builds `query user` may return non-zero even with valid tabular output.
        // If we parsed sessions (or clearly detected "no sessions"), treat as success.
        if (command.ExitCode != 0)
        {
            if (sessions.Count > 0)
            {
                _logger.LogWarning(
                    "query user retornou ExitCode {ExitCode} no servidor {Server}, mas houve parse de {Count} sessao(oes).",
                    command.ExitCode,
                    serverName,
                    sessions.Count);
                return Result<IReadOnlyList<WindowsSessionRecord>>.Success(sessions);
            }

            if (LooksLikeQueryUserHeader(command.StandardOutput) || IsNoSessionsMessage(command.StandardOutput, command.StandardError))
            {
                _logger.LogInformation(
                    "query user retornou ExitCode {ExitCode} no servidor {Server} sem sessoes ativas detectadas.",
                    command.ExitCode,
                    serverName);
                return Result<IReadOnlyList<WindowsSessionRecord>>.Success(Array.Empty<WindowsSessionRecord>());
            }
        }

        if (command.ExitCode != 0)
        {
            var detailedError = ExtractError(command, "Nao foi possivel consultar sessoes no servidor informado.");
            _logger.LogWarning("query user falhou no servidor {Server}. Erro: {Error}", serverName, detailedError);
            return Result<IReadOnlyList<WindowsSessionRecord>>.Failure(detailedError);
        }

        return Result<IReadOnlyList<WindowsSessionRecord>>.Success(sessions);
    }

    public async Task<Result> DisconnectAsync(string serverName, int sessionId, CancellationToken cancellationToken = default)
    {
        var command = await ExecuteWithLocalFallbackAsync(
            command: "rwinsta",
            serverName: serverName,
            remoteArguments: new[] { sessionId.ToString(), $"/server:{serverName}" },
            localArguments: new[] { sessionId.ToString() },
            cancellationToken: cancellationToken);

        return ToResult(command, "Falha ao desconectar sessao.");
    }

    public async Task<Result> LogoffAsync(string serverName, int sessionId, CancellationToken cancellationToken = default)
    {
        var command = await ExecuteWithLocalFallbackAsync(
            command: "logoff",
            serverName: serverName,
            remoteArguments: new[] { sessionId.ToString(), $"/server:{serverName}" },
            localArguments: new[] { sessionId.ToString() },
            cancellationToken: cancellationToken);

        return ToResult(command, "Falha ao fazer logoff.");
    }

    public async Task<Result> KillProcessAsync(string serverName, int sessionId, string processName, CancellationToken cancellationToken = default)
    {
        var command = await ExecuteWithLocalFallbackAsync(
            command: "taskkill",
            serverName: serverName,
            remoteArguments: new[] { "/server", serverName, "/FI", $"SESSION eq {sessionId}", "/IM", processName, "/F" },
            localArguments: new[] { "/FI", $"SESSION eq {sessionId}", "/IM", processName, "/F" },
            cancellationToken: cancellationToken);

        return ToResult(command, "Falha ao encerrar processo.");
    }

    private Result ToResult(CommandExecutionResult command, string defaultErrorMessage)
    {
        if (command.TimedOut)
        {
            return Result.Failure("Tempo limite excedido na operacao.");
        }

        if (command.ExitCode == 0)
        {
            return Result.Success();
        }

        return Result.Failure(ExtractError(command, defaultErrorMessage));
    }

    private async Task<CommandExecutionResult> ExecuteWithLocalFallbackAsync(
        string command,
        string serverName,
        IReadOnlyCollection<string> remoteArguments,
        IReadOnlyCollection<string> localArguments,
        CancellationToken cancellationToken)
    {
        var firstAttempt = await _commandExecutor.ExecuteAsync(command, remoteArguments, cancellationToken);
        if (firstAttempt.ExitCode == 0 || firstAttempt.TimedOut || !IsLocalServer(serverName))
        {
            return firstAttempt;
        }

        _logger.LogInformation("Tentando fallback local para comando {Command}.", command);
        return await _commandExecutor.ExecuteAsync(command, localArguments, cancellationToken);
    }

    private static bool IsLocalServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return true;
        }

        var normalized = serverName.Trim();
        if (normalized == "." || normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var withoutDomain = normalized.Split('.', 2)[0];
        return withoutDomain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
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
        var parts = Regex.Split(line, "\\s{2,}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (parts.Length < 5)
        {
            return null;
        }

        var idIndex = Array.FindIndex(parts, p => int.TryParse(p, out _));
        if (idIndex < 0 || idIndex + 2 >= parts.Length)
        {
            return null;
        }

        if (!int.TryParse(parts[idIndex], out var sessionId))
        {
            return null;
        }

        var username = idIndex >= 1 ? parts[0].Trim() : string.Empty;
        var sessionName = idIndex >= 2 ? parts[1].Trim() : string.Empty;
        var state = parts[idIndex + 1].Trim();
        var idle = parts[idIndex + 2].Trim();
        var logon = idIndex + 3 < parts.Length ? string.Join(" ", parts[(idIndex + 3)..]).Trim() : string.Empty;

        return new WindowsSessionRecord(sessionId, username, sessionName, state, idle, logon, serverName);
    }

    private static string ExtractError(CommandExecutionResult command, string defaultErrorMessage)
    {
        var message = command.StandardError;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = command.StandardOutput;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = defaultErrorMessage;
        }

        return SanitizeError(message);
    }

    private static string SanitizeError(string error)
    {
        if (error.Length <= 300)
        {
            return error;
        }

        return error[..300];
    }

    private static bool LooksLikeQueryUserHeader(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        return output.Contains("USERNAME", StringComparison.OrdinalIgnoreCase)
            && output.Contains("SESSIONNAME", StringComparison.OrdinalIgnoreCase)
            && output.Contains("LOGON TIME", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoSessionsMessage(string stdout, string stderr)
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
}

using SessionManager.Application.Common;

namespace SessionManager.Application.Interfaces.Windows;

public sealed record WindowsSessionRecord(
    int SessionId,
    string Username,
    string SessionName,
    string State,
    string IdleTime,
    string LogonTime,
    string ServerName);

public interface IWindowsSessionGateway
{
    Task<Result<IReadOnlyList<WindowsSessionRecord>>> ListSessionsAsync(string serverName, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAsync(string serverName, int sessionId, CancellationToken cancellationToken = default);
    Task<Result> LogoffAsync(string serverName, int sessionId, CancellationToken cancellationToken = default);
    Task<Result> KillProcessAsync(string serverName, int sessionId, string processName, CancellationToken cancellationToken = default);
}

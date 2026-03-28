namespace SessionManager.Infrastructure.Windows;

public interface IWindowsCommandExecutor
{
    Task<CommandExecutionResult> ExecuteAsync(
        string command,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken = default);
}

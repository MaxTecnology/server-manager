namespace SessionManager.Infrastructure.Windows;

public sealed record CommandExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

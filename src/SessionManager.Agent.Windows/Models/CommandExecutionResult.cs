namespace SessionManager.Agent.Windows.Models;

public sealed record CommandExecutionResult(
    bool Success,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);

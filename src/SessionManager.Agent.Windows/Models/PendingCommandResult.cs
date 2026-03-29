namespace SessionManager.Agent.Windows.Models;

public sealed class PendingCommandResult
{
    public Guid CommandId { get; init; }
    public bool Success { get; init; }
    public string? ResultOutput { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}

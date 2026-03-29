namespace SessionManager.Agent.Windows.Models;

public sealed class AgentSessionSnapshotRequestDto
{
    public string? ServerName { get; init; }
    public string? Hostname { get; init; }
    public string? AgentId { get; init; }
    public string? AgentVersion { get; init; }
    public bool SupportsRds { get; init; }
    public bool SupportsAd { get; init; }
    public string? SessionsOutput { get; init; }
    public DateTime? CapturedAtUtc { get; init; }
}

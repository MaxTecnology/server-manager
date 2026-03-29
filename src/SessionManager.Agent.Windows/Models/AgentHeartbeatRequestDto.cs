namespace SessionManager.Agent.Windows.Models;

public sealed class AgentHeartbeatRequestDto
{
    public string? ServerName { get; init; }
    public string? Hostname { get; init; }
    public string? AgentId { get; init; }
    public string? AgentVersion { get; init; }
    public bool SupportsRds { get; init; }
    public bool SupportsAd { get; init; }
}

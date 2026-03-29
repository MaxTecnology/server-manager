namespace SessionManager.Agent.Windows.Models;

public sealed class AgentHeartbeatResponseDto
{
    public Guid ServerId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string Hostname { get; init; } = string.Empty;
    public DateTime ReceivedAtUtc { get; init; }
}

namespace SessionManager.Agent.Windows.Models;

public sealed class AgentPollRequestDto
{
    public string? Hostname { get; init; }
    public string? AgentId { get; init; }
}

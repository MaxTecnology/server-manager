namespace SessionManager.Application.DTOs.Agent;

public sealed class AgentPollRequestDto
{
    public string? Hostname { get; init; }
    public string? AgentId { get; init; }
}

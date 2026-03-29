namespace SessionManager.Application.DTOs.Agent;

public sealed class AgentCommandResultRequestDto
{
    public bool Success { get; init; }
    public string? ResultOutput { get; init; }
    public string? ErrorMessage { get; init; }
}

namespace SessionManager.Agent.Windows.Models;

public sealed class AgentCommandResultRequestDto
{
    public bool Success { get; init; }
    public string? ResultOutput { get; init; }
    public string? ErrorMessage { get; init; }
}

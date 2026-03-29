namespace SessionManager.Agent.Windows.Models;

public sealed class AgentCommandDispatchDto
{
    public Guid CommandId { get; init; }
    public string CommandText { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; }
}

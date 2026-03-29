namespace SessionManager.Application.DTOs.Agent;

public sealed class EnqueueAgentCommandRequestDto
{
    public string CommandText { get; init; } = string.Empty;
}

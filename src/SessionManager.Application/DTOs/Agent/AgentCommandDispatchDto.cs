namespace SessionManager.Application.DTOs.Agent;

public sealed record AgentCommandDispatchDto(
    Guid CommandId,
    string CommandText,
    DateTime RequestedAtUtc);

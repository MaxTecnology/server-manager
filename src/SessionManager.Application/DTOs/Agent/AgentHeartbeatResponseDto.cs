namespace SessionManager.Application.DTOs.Agent;

public sealed record AgentHeartbeatResponseDto(
    Guid ServerId,
    string ServerName,
    string Hostname,
    DateTime ReceivedAtUtc);

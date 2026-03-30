namespace SessionManager.Application.DTOs.Agent;

public sealed record AgentAdOuSnapshotResponseDto(
    Guid ServerId,
    string ServerName,
    string Hostname,
    DateTime ReceivedAtUtc,
    DateTime CapturedAtUtc);

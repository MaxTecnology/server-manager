namespace SessionManager.Agent.Windows.Models;

public sealed record AgentAdOuSnapshotResponseDto(
    Guid ServerId,
    string ServerName,
    string Hostname,
    DateTime ReceivedAtUtc,
    DateTime CapturedAtUtc);

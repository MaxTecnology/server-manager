namespace SessionManager.Agent.Windows.Models;

public sealed record AgentSessionSnapshotResponseDto(
    Guid ServerId,
    string ServerName,
    string Hostname,
    DateTime ReceivedAtUtc,
    DateTime CapturedAtUtc);

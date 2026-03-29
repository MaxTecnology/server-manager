namespace SessionManager.Application.DTOs.Servers;

public sealed record ServerDto(
    Guid Id,
    string Name,
    string Hostname,
    bool IsDefault,
    bool IsActive,
    bool SupportsRds,
    bool SupportsAd,
    string? AgentId,
    string? AgentVersion,
    DateTime? AgentLastHeartbeatUtc,
    DateTime? AgentSessionSnapshotUtc,
    bool IsAgentOnline,
    bool HasRecentSnapshot);

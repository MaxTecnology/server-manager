using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class Server : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SupportsRds { get; set; } = true;
    public bool SupportsAd { get; set; }
    public string? AgentId { get; set; }
    public string? AgentVersion { get; set; }
    public DateTime? AgentLastHeartbeatUtc { get; set; }
    public string? AgentLastIpAddress { get; set; }
    public string? AgentSessionSnapshotOutput { get; set; }
    public DateTime? AgentSessionSnapshotUtc { get; set; }
}

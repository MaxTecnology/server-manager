namespace SessionManager.Application.DTOs.Agent;

public sealed class AgentAdOuSnapshotRequestDto
{
    public string? ServerName { get; init; }
    public string? Hostname { get; init; }
    public string? AgentId { get; init; }
    public string? AgentVersion { get; init; }
    public bool? SupportsRds { get; init; }
    public bool? SupportsAd { get; init; }
    public string? OrganizationalUnitsOutput { get; init; }
    public DateTime? CapturedAtUtc { get; init; }
}

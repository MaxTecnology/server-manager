using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string OperatorUsername { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public int? SessionId { get; set; }
    public string? TargetUsername { get; set; }
    public string? ProcessName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? MetadataJson { get; set; }
}

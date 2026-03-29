using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class AgentCommand : BaseEntity
{
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public string RequestedBy { get; set; } = string.Empty;
    public string CommandText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedAgentId { get; set; }
    public DateTime? PickedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultOutput { get; set; }
    public string? ErrorMessage { get; set; }
}

using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class AllowedProcess : BaseEntity
{
    public string ProcessName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
}

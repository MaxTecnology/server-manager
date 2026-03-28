using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class Server : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

using SessionManager.Domain.Common;

namespace SessionManager.Domain.Entities;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

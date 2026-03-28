namespace SessionManager.Application.DTOs.Users;

public sealed class UpdateUserRolesRequestDto
{
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}

namespace SessionManager.Application.DTOs.Users;

public sealed class CreateUserRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}

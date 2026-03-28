namespace SessionManager.Application.DTOs.Users;

public sealed class UpdateUserPasswordRequestDto
{
    public string Password { get; set; } = string.Empty;
}

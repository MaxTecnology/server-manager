namespace SessionManager.Application.DTOs.ActiveDirectory;

public sealed class ResetAdUserPasswordRequestDto
{
    public string Password { get; set; } = string.Empty;
    public bool ChangePasswordAtLogon { get; set; } = true;
    public bool EnableAccount { get; set; } = true;
}

namespace SessionManager.Application.DTOs.ActiveDirectory;

public sealed class CreateAdUserRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? UserPrincipalName { get; set; }
    public string? OrganizationalUnitPath { get; set; }
    public bool ChangePasswordAtLogon { get; set; } = true;
}

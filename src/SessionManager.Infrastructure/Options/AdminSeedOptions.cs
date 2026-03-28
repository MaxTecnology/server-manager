namespace SessionManager.Infrastructure.Options;

public sealed class AdminSeedOptions
{
    public string Username { get; set; } = "admin";
    public string DisplayName { get; set; } = "Administrador";
    public string Password { get; set; } = "Admin@12345";
}

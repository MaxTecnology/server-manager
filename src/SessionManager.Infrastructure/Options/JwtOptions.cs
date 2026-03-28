namespace SessionManager.Infrastructure.Options;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "SessionManager";
    public string Audience { get; set; } = "SessionManager.Internal";
    public string SigningKey { get; set; } = "CHANGE_THIS_KEY_WITH_32_PLUS_CHARACTERS";
    public int ExpirationMinutes { get; set; } = 480;
}

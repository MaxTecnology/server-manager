using System.Security.Cryptography;
using SessionManager.Application.Interfaces.Security;

namespace SessionManager.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        var segments = hash.Split('.');
        if (segments.Length != 4 || segments[0] != "v1")
        {
            return false;
        }

        if (!int.TryParse(segments[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(segments[2]);
        var expectedHash = Convert.FromBase64String(segments[3]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

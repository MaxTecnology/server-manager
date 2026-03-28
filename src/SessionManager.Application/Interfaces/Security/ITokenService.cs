using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Security;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, IReadOnlyCollection<string> roles);
}

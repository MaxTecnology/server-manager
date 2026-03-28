using SessionManager.Application.Interfaces.Security;

namespace SessionManager.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

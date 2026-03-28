namespace SessionManager.Application.Interfaces.Security;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

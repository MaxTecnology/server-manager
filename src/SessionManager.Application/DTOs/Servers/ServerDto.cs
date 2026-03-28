namespace SessionManager.Application.DTOs.Servers;

public sealed record ServerDto(
    Guid Id,
    string Name,
    string Hostname,
    bool IsDefault,
    bool IsActive);

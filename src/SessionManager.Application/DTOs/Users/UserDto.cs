namespace SessionManager.Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

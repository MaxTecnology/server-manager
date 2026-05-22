namespace SessionManager.Application.DTOs.ActiveDirectory;

public sealed record AdUserSearchItemDto(
    string Username,
    string DisplayName,
    bool Enabled,
    bool LockedOut);

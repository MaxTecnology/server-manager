namespace SessionManager.Application.DTOs.Auth;

public sealed record UserSummaryDto(
    Guid Id,
    string Username,
    string DisplayName,
    IReadOnlyCollection<string> Roles);

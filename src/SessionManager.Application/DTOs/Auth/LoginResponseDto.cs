namespace SessionManager.Application.DTOs.Auth;

public sealed record LoginResponseDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserSummaryDto User);

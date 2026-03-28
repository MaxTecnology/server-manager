namespace SessionManager.Application.DTOs.Audit;

public sealed record AuditLogDto(
    Guid Id,
    DateTime TimestampUtc,
    string OperatorUsername,
    string Action,
    string ServerName,
    int? SessionId,
    string? TargetUsername,
    string? ProcessName,
    bool Success,
    string? ErrorMessage,
    string? ClientIpAddress);

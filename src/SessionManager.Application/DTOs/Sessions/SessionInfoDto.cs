namespace SessionManager.Application.DTOs.Sessions;

public sealed record SessionInfoDto(
    int SessionId,
    string Username,
    string SessionName,
    string State,
    string IdleTime,
    string LogonTime,
    string ServerName);

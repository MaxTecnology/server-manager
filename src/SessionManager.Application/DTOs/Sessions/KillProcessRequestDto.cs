namespace SessionManager.Application.DTOs.Sessions;

public sealed class KillProcessRequestDto : SessionActionRequestDto
{
    public string ProcessName { get; set; } = string.Empty;
}

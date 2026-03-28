namespace SessionManager.Application.DTOs.Sessions;

public class SessionActionRequestDto
{
    public int SessionId { get; set; }
    public string? ServerName { get; set; }
    public string? TargetUsername { get; set; }
}

namespace SessionManager.Application.DTOs.AllowedProcesses;

public sealed record AllowedProcessDto(
    Guid Id,
    string ProcessName,
    bool IsActive,
    string CreatedBy);

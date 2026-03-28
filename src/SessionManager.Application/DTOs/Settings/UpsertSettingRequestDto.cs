namespace SessionManager.Application.DTOs.Settings;

public sealed class UpsertSettingRequestDto
{
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

namespace SessionManager.Application.DTOs.ActiveDirectory;

public sealed class SearchAdUsersRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int? Limit { get; set; }
}

namespace SessionManager.Application.Common;

public sealed class AuditLogFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Action { get; set; }
    public bool? Success { get; set; }
}

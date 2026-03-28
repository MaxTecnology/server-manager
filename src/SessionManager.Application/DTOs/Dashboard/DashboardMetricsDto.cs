namespace SessionManager.Application.DTOs.Dashboard;

public sealed record DashboardMetricsDto(
    int ActiveSessions,
    int DisconnectedSessions,
    int ActionsToday,
    int ErrorsToday,
    DateTime GeneratedAtUtc);

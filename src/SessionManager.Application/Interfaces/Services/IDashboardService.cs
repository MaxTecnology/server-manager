using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Dashboard;

namespace SessionManager.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<Result<DashboardMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.WebApi.Controllers;

[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetMetricsAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao obter métricas." });
        }

        return Ok(result.Value);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SessionManager.WebApi.Controllers;

[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            timestampUtc = DateTime.UtcNow
        });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Operator}")]
[Route("api/servers")]
public sealed class ServersController : ApiControllerBase
{
    private readonly IServerService _serverService;

    public ServersController(IServerService serverService)
    {
        _serverService = serverService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var servers = await _serverService.GetAllAsync(cancellationToken);
        return Ok(servers);
    }
}

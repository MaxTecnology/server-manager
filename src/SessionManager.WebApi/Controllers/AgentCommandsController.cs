using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.Agent;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
[Route("api/agent-commands")]
public sealed class AgentCommandsController : ApiControllerBase
{
    private readonly IAgentService _agentService;

    public AgentCommandsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("servers/{serverId:guid}/commands")]
    public async Task<IActionResult> Enqueue(Guid serverId, [FromBody] EnqueueAgentCommandRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _agentService.EnqueueCommandAsync(serverId, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao enfileirar comando." });
        }

        return Ok(result.Value);
    }

    [HttpGet("{commandId:guid}")]
    public async Task<IActionResult> GetById(Guid commandId, CancellationToken cancellationToken)
    {
        var result = await _agentService.GetCommandAsync(commandId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound(new { message = result.Error ?? "Comando não encontrado." });
        }

        return Ok(result.Value);
    }
}

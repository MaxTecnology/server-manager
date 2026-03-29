using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SessionManager.Application.DTOs.Agent;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Infrastructure.Options;

namespace SessionManager.WebApi.Controllers;

[AllowAnonymous]
[Route("api/agent")]
public sealed class AgentController : ApiControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IOptions<AgentOptions> _agentOptions;

    public AgentController(IAgentService agentService, IOptions<AgentOptions> agentOptions)
    {
        _agentService = agentService;
        _agentOptions = agentOptions;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeatRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAgentAuthorized())
        {
            return Unauthorized(new { message = "Agent não autorizado." });
        }

        var result = await _agentService.RegisterHeartbeatAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao registrar heartbeat." });
        }

        return Ok(result.Value);
    }

    [HttpPost("session-snapshot")]
    public async Task<IActionResult> SessionSnapshot([FromBody] AgentSessionSnapshotRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAgentAuthorized())
        {
            return Unauthorized(new { message = "Agent não autorizado." });
        }

        var result = await _agentService.RegisterSessionSnapshotAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao registrar snapshot de sessões." });
        }

        return Ok(result.Value);
    }

    [HttpPost("next-command")]
    public async Task<IActionResult> NextCommand([FromBody] AgentPollRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAgentAuthorized())
        {
            return Unauthorized(new { message = "Agent não autorizado." });
        }

        var result = await _agentService.GetNextCommandAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao obter comando." });
        }

        if (result.Value is null)
        {
            return NoContent();
        }

        return Ok(result.Value);
    }

    [HttpPost("commands/{commandId:guid}/result")]
    public async Task<IActionResult> CompleteCommand(Guid commandId, [FromBody] AgentCommandResultRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAgentAuthorized())
        {
            return Unauthorized(new { message = "Agent não autorizado." });
        }

        var result = await _agentService.CompleteCommandAsync(
            commandId,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao registrar resultado do comando." });
        }

        return Ok(new { message = "Resultado recebido." });
    }

    private bool IsAgentAuthorized()
    {
        var configuredKey = _agentOptions.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Agent-Key", out var headerValue))
        {
            return false;
        }

        var informedKey = headerValue.ToString().Trim();
        if (string.IsNullOrWhiteSpace(informedKey))
        {
            return false;
        }

        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var informedBytes = Encoding.UTF8.GetBytes(informedKey);
        return configuredBytes.Length == informedBytes.Length
               && CryptographicOperations.FixedTimeEquals(configuredBytes, informedBytes);
    }
}

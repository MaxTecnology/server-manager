using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.Sessions;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Operator}")]
[Route("api/sessions")]
public sealed class SessionsController : ApiControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] string? serverName, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSessionsAsync(serverName, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao consultar sessões." });
        }

        return Ok(result.Value);
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect([FromBody] SessionActionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.DisconnectAsync(request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao desconectar sessão." });
        }

        return Ok(new { message = "Sessão desconectada com sucesso." });
    }

    [Authorize(Roles = RoleNames.Administrator)]
    [HttpPost("logoff")]
    public async Task<IActionResult> Logoff([FromBody] SessionActionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.LogoffAsync(request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao fazer logoff." });
        }

        return Ok(new { message = "Logoff realizado com sucesso." });
    }

    [HttpPost("kill-process")]
    public async Task<IActionResult> KillProcess([FromBody] KillProcessRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.KillProcessAsync(request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao encerrar processo." });
        }

        return Ok(new { message = "Processo encerrado com sucesso." });
    }
}

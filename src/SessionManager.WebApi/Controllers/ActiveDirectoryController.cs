using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.ActiveDirectory;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
[Route("api/ad")]
public sealed class ActiveDirectoryController : ApiControllerBase
{
    private readonly IActiveDirectoryService _activeDirectoryService;

    public ActiveDirectoryController(IActiveDirectoryService activeDirectoryService)
    {
        _activeDirectoryService = activeDirectoryService;
    }

    [HttpGet("servers/{serverId:guid}/organizational-units")]
    public async Task<IActionResult> GetOrganizationalUnits(Guid serverId, CancellationToken cancellationToken)
    {
        var result = await _activeDirectoryService.GetOrganizationalUnitsAsync(serverId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao listar OUs do AD." });
        }

        return Ok(result.Value);
    }

    [HttpPost("servers/{serverId:guid}/users")]
    public async Task<IActionResult> CreateUser(
        Guid serverId,
        [FromBody] CreateAdUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _activeDirectoryService.CreateUserAsync(
            serverId,
            request,
            BuildActionContext(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao enfileirar criação de usuário no AD." });
        }

        return Ok(result.Value);
    }

    [HttpPost("servers/{serverId:guid}/users/{username}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid serverId,
        string username,
        [FromBody] ResetAdUserPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _activeDirectoryService.ResetPasswordAsync(
            serverId,
            username,
            request,
            BuildActionContext(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao enfileirar reset de senha no AD." });
        }

        return Ok(result.Value);
    }
}

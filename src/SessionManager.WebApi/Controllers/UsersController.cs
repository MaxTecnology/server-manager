using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.Users;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
[Route("api/users")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _userService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao criar usuário." });
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateUserStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.SetStatusAsync(id, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao atualizar status." });
        }

        return Ok(new { message = "Status atualizado com sucesso." });
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] UpdateUserPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.SetPasswordAsync(id, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao atualizar senha." });
        }

        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    [HttpPatch("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, [FromBody] UpdateUserRolesRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.SetRolesAsync(id, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao atualizar perfis." });
        }

        return Ok(new { message = "Perfis atualizados com sucesso." });
    }
}

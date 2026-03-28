using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.Auth;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.WebApi.Controllers;

[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(new { message = result.Error ?? "Falha de autenticação." });
        }

        return Ok(result.Value);
    }
}

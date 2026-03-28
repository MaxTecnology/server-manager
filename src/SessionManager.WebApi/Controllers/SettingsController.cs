using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.Settings;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
[Route("api/settings")]
public sealed class SettingsController : ApiControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAllAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(string key, [FromBody] UpsertSettingRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _settingsService.UpsertAsync(key, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao salvar configuração." });
        }

        return Ok(new { message = "Configuração salva com sucesso." });
    }
}

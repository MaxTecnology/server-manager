using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.DTOs.AllowedProcesses;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
[Route("api/allowed-processes")]
public sealed class AllowedProcessesController : ApiControllerBase
{
    private readonly IAllowedProcessService _allowedProcessService;

    public AllowedProcessesController(IAllowedProcessService allowedProcessService)
    {
        _allowedProcessService = allowedProcessService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _allowedProcessService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAllowedProcessRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _allowedProcessService.CreateAsync(request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao cadastrar processo." });
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetAllowedProcessStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _allowedProcessService.SetStatusAsync(id, request, BuildActionContext(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao atualizar processo." });
        }

        return Ok(new { message = "Processo atualizado com sucesso." });
    }
}

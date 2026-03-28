using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Audit;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;

namespace SessionManager.WebApi.Controllers;

[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Operator}")]
[Route("api/audit")]
public sealed class AuditController : ApiControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? action = null,
        [FromQuery] bool? success = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Action = action,
            Success = success
        };

        var result = await _auditService.SearchAsync(filter, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Error ?? "Falha ao consultar auditoria." });
        }

        if (User.IsInRole(RoleNames.Administrator))
        {
            return Ok(result.Value);
        }

        var masked = result.Value with
        {
            Items = result.Value.Items
                .Select(x => new AuditLogDto(
                    x.Id,
                    x.TimestampUtc,
                    x.OperatorUsername,
                    x.Action,
                    x.ServerName,
                    x.SessionId,
                    x.TargetUsername,
                    x.ProcessName,
                    x.Success,
                    null,
                    x.ClientIpAddress))
                .ToArray()
        };

        return Ok(masked);
    }
}

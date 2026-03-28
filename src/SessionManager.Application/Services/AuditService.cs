using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Audit;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Services;

namespace SessionManager.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Page = filter.Page <= 0 ? 1 : filter.Page;
        filter.PageSize = filter.PageSize is < 1 or > 200 ? 20 : filter.PageSize;

        var data = await _auditLogRepository.SearchAsync(filter, cancellationToken);
        var mapped = data.Items
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AuditLogDto(
                a.Id,
                a.CreatedAtUtc,
                a.OperatorUsername,
                a.Action,
                a.ServerName,
                a.SessionId,
                a.TargetUsername,
                a.ProcessName,
                a.Success,
                a.ErrorMessage,
                a.ClientIpAddress))
            .ToArray();

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto>(mapped, data.Page, data.PageSize, data.TotalCount));
    }
}

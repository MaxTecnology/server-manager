using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Audit;

namespace SessionManager.Application.Interfaces.Services;

public interface IAuditService
{
    Task<Result<PagedResult<AuditLogDto>>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

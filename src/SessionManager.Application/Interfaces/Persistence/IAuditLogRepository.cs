using SessionManager.Application.Common;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Interfaces.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
    Task<int> CountForDateAsync(DateOnly date, bool onlyErrors, CancellationToken cancellationToken = default);
}

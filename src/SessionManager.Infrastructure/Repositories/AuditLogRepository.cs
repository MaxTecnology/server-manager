using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Common;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;
using SessionManager.Infrastructure.Data;

namespace SessionManager.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<PagedResult<AuditLog>> SearchAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.Success.HasValue)
        {
            query = query.Where(a => a.Success == filter.Success.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(a =>
                a.OperatorUsername.Contains(term) ||
                (a.TargetUsername != null && a.TargetUsername.Contains(term)) ||
                (a.ProcessName != null && a.ProcessName.Contains(term)) ||
                a.ServerName.Contains(term) ||
                a.Action.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var skip = (filter.Page - 1) * filter.PageSize;
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<int> CountForDateAsync(DateOnly date, bool onlyErrors, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var query = _dbContext.AuditLogs.Where(a => a.CreatedAtUtc >= start && a.CreatedAtUtc < end);
        if (onlyErrors)
        {
            query = query.Where(a => !a.Success);
        }

        return await query.CountAsync(cancellationToken);
    }
}

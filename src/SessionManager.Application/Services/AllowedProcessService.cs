using System.Text.RegularExpressions;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.AllowedProcesses;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class AllowedProcessService : IAllowedProcessService
{
    private static readonly Regex ProcessNameRegex = new("^[a-zA-Z0-9._-]{1,64}(\\.exe)?$", RegexOptions.Compiled);

    private readonly IAllowedProcessRepository _allowedProcessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AllowedProcessService(
        IAllowedProcessRepository allowedProcessRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _allowedProcessRepository = allowedProcessRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AllowedProcessDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _allowedProcessRepository.GetAllAsync(cancellationToken);
        return items
            .OrderBy(p => p.ProcessName)
            .Select(p => new AllowedProcessDto(p.Id, p.ProcessName, p.IsActive, p.CreatedBy))
            .ToArray();
    }

    public async Task<Result<AllowedProcessDto>> CreateAsync(CreateAllowedProcessRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeProcessName(request.ProcessName);
        if (normalized is null)
        {
            return Result<AllowedProcessDto>.Failure("Nome de processo inválido.");
        }

        var existing = await _allowedProcessRepository.GetByNameAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            return Result<AllowedProcessDto>.Failure("Processo já cadastrado.");
        }

        var entity = new AllowedProcess
        {
            ProcessName = normalized,
            IsActive = true,
            CreatedBy = actionContext.OperatorUsername,
            CreatedAtUtc = _clock.UtcNow
        };

        _allowedProcessRepository.Add(entity);

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "ALLOWED_PROCESS_CREATE",
            ServerName = "CONFIG",
            ProcessName = normalized,
            Success = true,
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AllowedProcessDto>.Success(new AllowedProcessDto(entity.Id, entity.ProcessName, entity.IsActive, entity.CreatedBy));
    }

    public async Task<Result> SetStatusAsync(Guid id, SetAllowedProcessStatusRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        var process = await _allowedProcessRepository.GetByIdAsync(id, cancellationToken);
        if (process is null)
        {
            return Result.Failure("Processo não encontrado.");
        }

        process.IsActive = request.IsActive;
        process.UpdatedAtUtc = _clock.UtcNow;

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "ALLOWED_PROCESS_STATUS",
            ServerName = "CONFIG",
            ProcessName = process.ProcessName,
            Success = true,
            MetadataJson = $"{{\"isActive\":{request.IsActive.ToString().ToLowerInvariant()}}}",
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? NormalizeProcessName(string processName)
    {
        var value = processName.Trim();
        if (!ProcessNameRegex.IsMatch(value))
        {
            return null;
        }

        if (!value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            value = $"{value}.exe";
        }

        return value.ToLowerInvariant();
    }
}

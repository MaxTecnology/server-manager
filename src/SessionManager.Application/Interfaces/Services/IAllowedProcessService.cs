using SessionManager.Application.Common;
using SessionManager.Application.DTOs.AllowedProcesses;

namespace SessionManager.Application.Interfaces.Services;

public interface IAllowedProcessService
{
    Task<IReadOnlyList<AllowedProcessDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AllowedProcessDto>> CreateAsync(CreateAllowedProcessRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> SetStatusAsync(Guid id, SetAllowedProcessStatusRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
}

using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Sessions;

namespace SessionManager.Application.Interfaces.Services;

public interface ISessionService
{
    Task<Result<IReadOnlyList<SessionInfoDto>>> GetSessionsAsync(string? serverName, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAsync(SessionActionRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> LogoffAsync(SessionActionRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
    Task<Result> KillProcessAsync(KillProcessRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
}

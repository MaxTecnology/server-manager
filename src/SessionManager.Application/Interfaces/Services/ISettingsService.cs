using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Settings;

namespace SessionManager.Application.Interfaces.Services;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result> UpsertAsync(string key, UpsertSettingRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default);
}

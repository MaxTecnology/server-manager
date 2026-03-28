using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Settings;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingRepository _settingRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SettingsService(
        ISettingRepository settingRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _settingRepository = settingRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingRepository.GetAllAsync(cancellationToken);
        return settings
            .OrderBy(s => s.Key)
            .Select(s => new SettingDto(s.Key, s.Value, s.Description))
            .ToArray();
    }

    public async Task<Result> UpsertAsync(string key, UpsertSettingRequestDto request, ActionContext actionContext, CancellationToken cancellationToken = default)
    {
        key = key.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure("Chave da configuração é obrigatória.");
        }

        var setting = await _settingRepository.GetByKeyAsync(key, cancellationToken);
        if (setting is null)
        {
            setting = new Setting
            {
                Key = key,
                Value = request.Value.Trim(),
                Description = request.Description.Trim(),
                CreatedAtUtc = _clock.UtcNow
            };
            _settingRepository.Add(setting);
        }
        else
        {
            setting.Value = request.Value.Trim();
            setting.Description = request.Description.Trim();
            setting.UpdatedAtUtc = _clock.UtcNow;
        }

        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "SETTING_UPSERT",
            ServerName = "CONFIG",
            Success = true,
            MetadataJson = $"{{\"key\":\"{setting.Key}\",\"value\":\"{setting.Value}\"}}",
            ClientIpAddress = actionContext.ClientIpAddress,
            CreatedAtUtc = _clock.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

using System.Text.Json;
using SessionManager.Application.Common;
using SessionManager.Application.DTOs.Agent;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Application.Interfaces.Services;
using SessionManager.Domain.Constants;
using SessionManager.Domain.Entities;

namespace SessionManager.Application.Services;

public sealed class AgentService : IAgentService
{
    private readonly IServerRepository _serverRepository;
    private readonly IAgentCommandRepository _agentCommandRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AgentService(
        IServerRepository serverRepository,
        IAgentCommandRepository agentCommandRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _serverRepository = serverRepository;
        _agentCommandRepository = agentCommandRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AgentHeartbeatResponseDto>> RegisterHeartbeatAsync(
        AgentHeartbeatRequestDto request,
        string? clientIpAddress,
        CancellationToken cancellationToken = default)
    {
        var normalizedHostname = NormalizeValue(request.Hostname ?? request.ServerName);
        if (normalizedHostname is null)
        {
            return Result<AgentHeartbeatResponseDto>.Failure("Hostname do servidor é obrigatório.");
        }

        var serverName = NormalizeValue(request.ServerName) ?? normalizedHostname;
        var now = _clock.UtcNow;
        var server = await ResolveOrCreateServerAsync(normalizedHostname, serverName, now, cancellationToken);

        server.AgentId = Truncate(NormalizeValue(request.AgentId), 120);
        server.AgentVersion = Truncate(NormalizeValue(request.AgentVersion), 40);
        server.AgentLastHeartbeatUtc = now;
        server.AgentLastIpAddress = Truncate(clientIpAddress, 80);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgentHeartbeatResponseDto>.Success(new AgentHeartbeatResponseDto(
            server.Id,
            server.Name,
            server.Hostname,
            now));
    }

    public async Task<Result<AgentSessionSnapshotResponseDto>> RegisterSessionSnapshotAsync(
        AgentSessionSnapshotRequestDto request,
        string? clientIpAddress,
        CancellationToken cancellationToken = default)
    {
        var normalizedHostname = NormalizeValue(request.Hostname ?? request.ServerName);
        if (normalizedHostname is null)
        {
            return Result<AgentSessionSnapshotResponseDto>.Failure("Hostname do servidor é obrigatório.");
        }

        var serverName = NormalizeValue(request.ServerName) ?? normalizedHostname;
        var now = _clock.UtcNow;
        var capturedAtUtc = NormalizeCapturedAt(request.CapturedAtUtc, now);

        var server = await ResolveOrCreateServerAsync(normalizedHostname, serverName, now, cancellationToken);
        server.AgentId = Truncate(NormalizeValue(request.AgentId), 120);
        server.AgentVersion = Truncate(NormalizeValue(request.AgentVersion), 40);
        server.AgentLastHeartbeatUtc = now;
        server.AgentLastIpAddress = Truncate(clientIpAddress, 80);
        server.AgentSessionSnapshotOutput = Truncate(NormalizeValue(request.SessionsOutput), 20000);
        server.AgentSessionSnapshotUtc = capturedAtUtc;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgentSessionSnapshotResponseDto>.Success(new AgentSessionSnapshotResponseDto(
            server.Id,
            server.Name,
            server.Hostname,
            now,
            capturedAtUtc));
    }

    public async Task<Result<AgentCommandDto>> EnqueueCommandAsync(
        Guid serverId,
        EnqueueAgentCommandRequestDto request,
        ActionContext actionContext,
        CancellationToken cancellationToken = default)
    {
        var commandText = NormalizeValue(request.CommandText);
        if (commandText is null)
        {
            return Result<AgentCommandDto>.Failure("Comando é obrigatório.");
        }

        if (commandText.Length > 400)
        {
            return Result<AgentCommandDto>.Failure("Comando excede limite de 400 caracteres.");
        }

        var server = await _serverRepository.GetByIdAsync(serverId, cancellationToken);
        if (server is null)
        {
            return Result<AgentCommandDto>.Failure("Servidor não encontrado.");
        }

        if (!server.IsActive)
        {
            return Result<AgentCommandDto>.Failure("Servidor inativo.");
        }

        var now = _clock.UtcNow;
        var command = new AgentCommand
        {
            ServerId = server.Id,
            Server = server,
            RequestedBy = Truncate(actionContext.OperatorUsername, 120)!,
            CommandText = commandText,
            Status = AgentCommandStatuses.Pending,
            CreatedAtUtc = now
        };

        _agentCommandRepository.Add(command);
        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = actionContext.OperatorUsername,
            Action = "AGENT_COMMAND_ENQUEUE",
            ServerName = server.Hostname,
            Success = true,
            ClientIpAddress = actionContext.ClientIpAddress,
            MetadataJson = Truncate(JsonSerializer.Serialize(new
            {
                commandId = command.Id,
                command = command.CommandText
            }), 2000),
            CreatedAtUtc = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AgentCommandDto>.Success(MapCommand(command));
    }

    public async Task<Result<AgentCommandDispatchDto?>> GetNextCommandAsync(
        AgentPollRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedHostname = NormalizeValue(request.Hostname);
        if (normalizedHostname is null)
        {
            return Result<AgentCommandDispatchDto?>.Failure("Hostname do servidor é obrigatório.");
        }

        var server = await _serverRepository.GetByHostnameAsync(normalizedHostname, cancellationToken);
        if (server is null)
        {
            return Result<AgentCommandDispatchDto?>.Failure("Servidor não registrado.");
        }

        var now = _clock.UtcNow;
        server.AgentLastHeartbeatUtc = now;

        var agentId = NormalizeValue(request.AgentId);
        if (agentId is not null)
        {
            server.AgentId = Truncate(agentId, 120);
        }

        var command = await _agentCommandRepository.GetNextPendingAsync(server.Id, cancellationToken);
        if (command is null)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<AgentCommandDispatchDto?>.Success(null);
        }

        command.Status = AgentCommandStatuses.Running;
        command.AssignedAgentId = Truncate(agentId ?? server.AgentId, 120);
        command.PickedAtUtc = now;
        command.UpdatedAtUtc = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AgentCommandDispatchDto?>.Success(new AgentCommandDispatchDto(
            command.Id,
            command.CommandText,
            command.CreatedAtUtc));
    }

    public async Task<Result> CompleteCommandAsync(
        Guid commandId,
        AgentCommandResultRequestDto request,
        string? clientIpAddress,
        CancellationToken cancellationToken = default)
    {
        var command = await _agentCommandRepository.GetByIdAsync(commandId, cancellationToken);
        if (command is null)
        {
            return Result.Failure("Comando não encontrado.");
        }

        if (command.Status == AgentCommandStatuses.Succeeded || command.Status == AgentCommandStatuses.Failed)
        {
            return Result.Failure("Comando já finalizado.");
        }

        var now = _clock.UtcNow;
        if (command.PickedAtUtc is null)
        {
            command.PickedAtUtc = now;
        }
        else if (now < command.PickedAtUtc.Value)
        {
            now = command.PickedAtUtc.Value;
        }

        command.CompletedAtUtc = now;
        command.Status = request.Success ? AgentCommandStatuses.Succeeded : AgentCommandStatuses.Failed;
        command.ResultOutput = Truncate(NormalizeValue(request.ResultOutput), 4000);
        command.ErrorMessage = request.Success
            ? null
            : Truncate(NormalizeValue(request.ErrorMessage) ?? "Erro informado pelo agent.", 2000);
        command.UpdatedAtUtc = now;

        var operatorName = $"agent:{command.Server.Hostname}";
        await _auditLogRepository.AddAsync(new AuditLog
        {
            OperatorUsername = Truncate(operatorName, 80)!,
            Action = "AGENT_COMMAND_RESULT",
            ServerName = command.Server.Hostname,
            Success = request.Success,
            ErrorMessage = command.ErrorMessage,
            ClientIpAddress = Truncate(clientIpAddress, 80),
            MetadataJson = Truncate(JsonSerializer.Serialize(new
            {
                commandId = command.Id,
                status = command.Status,
                agentId = command.AssignedAgentId
            }), 2000),
            CreatedAtUtc = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<AgentCommandDto>> GetCommandAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        var command = await _agentCommandRepository.GetByIdAsync(commandId, cancellationToken);
        if (command is null)
        {
            return Result<AgentCommandDto>.Failure("Comando não encontrado.");
        }

        return Result<AgentCommandDto>.Success(MapCommand(command));
    }

    private static AgentCommandDto MapCommand(AgentCommand command)
    {
        return new AgentCommandDto(
            command.Id,
            command.ServerId,
            command.Server.Name,
            command.Server.Hostname,
            command.RequestedBy,
            command.CommandText,
            command.Status,
            command.CreatedAtUtc,
            command.PickedAtUtc,
            command.CompletedAtUtc,
            command.AssignedAgentId,
            command.ResultOutput,
            command.ErrorMessage);
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<Server> ResolveOrCreateServerAsync(
        string normalizedHostname,
        string serverName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var server = await _serverRepository.GetByHostnameAsync(normalizedHostname, cancellationToken);
        if (server is null)
        {
            server = new Server
            {
                Name = Truncate(serverName, 80)!,
                Hostname = Truncate(normalizedHostname, 120)!,
                IsActive = true,
                IsDefault = false,
                CreatedAtUtc = now
            };
            _serverRepository.Add(server);
            return server;
        }

        server.Name = Truncate(serverName, 80)!;
        server.Hostname = Truncate(normalizedHostname, 120)!;
        server.IsActive = true;
        server.UpdatedAtUtc = now;
        return server;
    }

    private static DateTime NormalizeCapturedAt(DateTime? capturedAtUtc, DateTime now)
    {
        if (!capturedAtUtc.HasValue)
        {
            return now;
        }

        var normalized = DateTime.SpecifyKind(capturedAtUtc.Value, DateTimeKind.Utc);
        return normalized > now ? now : normalized;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}

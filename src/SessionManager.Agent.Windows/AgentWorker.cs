using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionManager.Agent.Windows.Models;
using SessionManager.Agent.Windows.Options;
using SessionManager.Agent.Windows.Services;

namespace SessionManager.Agent.Windows;

public sealed class AgentWorker : BackgroundService
{
    private const string AdOuSnapshotCommand =
        "Import-Module ActiveDirectory;$ous=Get-ADOrganizationalUnit -Filter * -Properties CanonicalName | Select-Object Name,DistinguishedName,CanonicalName | Sort-Object CanonicalName;ConvertTo-Json -InputObject @($ous) -Compress";

    private readonly IOptions<AgentOptions> _options;
    private readonly AgentApiClient _apiClient;
    private readonly CommandExecutionService _commandExecutionService;
    private readonly SecureCommandCodec _secureCommandCodec;
    private readonly PendingCommandResultStore _pendingStore;
    private readonly ILogger<AgentWorker> _logger;

    public AgentWorker(
        IOptions<AgentOptions> options,
        AgentApiClient apiClient,
        CommandExecutionService commandExecutionService,
        SecureCommandCodec secureCommandCodec,
        PendingCommandResultStore pendingStore,
        ILogger<AgentWorker> logger)
    {
        _options = options;
        _apiClient = apiClient;
        _commandExecutionService = commandExecutionService;
        _secureCommandCodec = secureCommandCodec;
        _pendingStore = pendingStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        var identity = BuildIdentity(options);

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));
        var heartbeatInterval = TimeSpan.FromSeconds(Math.Max(5, options.HeartbeatIntervalSeconds));
        var adOuSnapshotInterval = TimeSpan.FromSeconds(Math.Max(30, options.AdOuSnapshotIntervalSeconds));
        var nextHeartbeatAt = DateTimeOffset.MinValue;
        var nextAdOuSnapshotAt = DateTimeOffset.MinValue;

        _logger.LogInformation(
            "Agent iniciado. AgentId={AgentId} Hostname={Hostname} Server={ServerName} SupportsRds={SupportsRds} SupportsAd={SupportsAd}",
            identity.AgentId,
            identity.Hostname,
            identity.ServerName,
            identity.SupportsRds,
            identity.SupportsAd);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushPendingResultsAsync(stoppingToken);

                if (DateTimeOffset.UtcNow >= nextHeartbeatAt)
                {
                    await SendHeartbeatAsync(identity, stoppingToken);
                    if (identity.SupportsRds)
                    {
                        await SendSessionSnapshotAsync(identity, options, stoppingToken);
                    }

                    nextHeartbeatAt = DateTimeOffset.UtcNow.Add(heartbeatInterval);
                }

                if (identity.SupportsAd && DateTimeOffset.UtcNow >= nextAdOuSnapshotAt)
                {
                    await SendAdOuSnapshotAsync(identity, options, stoppingToken);
                    nextAdOuSnapshotAt = DateTimeOffset.UtcNow.Add(adOuSnapshotInterval);
                }

                await PollAndExecuteAsync(identity, options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada no loop principal do agent.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Agent finalizado.");
    }

    private async Task SendHeartbeatAsync(AgentIdentity identity, CancellationToken cancellationToken)
    {
        var heartbeat = await _apiClient.SendHeartbeatAsync(new AgentHeartbeatRequestDto
        {
            ServerName = identity.ServerName,
            Hostname = identity.Hostname,
            AgentId = identity.AgentId,
            AgentVersion = identity.AgentVersion,
            SupportsRds = identity.SupportsRds,
            SupportsAd = identity.SupportsAd
        }, cancellationToken);

        if (heartbeat.Success)
        {
            _logger.LogDebug("Heartbeat enviado com sucesso.");
            return;
        }

        if (heartbeat.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Heartbeat rejeitado (401). Verifique Agent:ApiKey.");
            return;
        }

        _logger.LogWarning("Falha ao enviar heartbeat: {Error}", heartbeat.Error);
    }

    private async Task SendSessionSnapshotAsync(AgentIdentity identity, AgentOptions options, CancellationToken cancellationToken)
    {
        var execution = await _commandExecutionService.ExecuteAsync("query user", options.CommandTimeoutSeconds, cancellationToken);
        var output = execution.StandardOutput;

        if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(execution.StandardError))
        {
            output = execution.StandardError;
        }

        var snapshot = await _apiClient.SendSessionSnapshotAsync(new AgentSessionSnapshotRequestDto
        {
            ServerName = identity.ServerName,
            Hostname = identity.Hostname,
            AgentId = identity.AgentId,
            AgentVersion = identity.AgentVersion,
            SupportsRds = identity.SupportsRds,
            SupportsAd = identity.SupportsAd,
            SessionsOutput = Truncate(output, options.MaxResultOutputLength),
            CapturedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        if (snapshot.Success)
        {
            _logger.LogDebug("Snapshot de sessões enviado com sucesso.");
            return;
        }

        if (snapshot.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Snapshot de sessões rejeitado (401). Verifique Agent:ApiKey.");
            return;
        }

        _logger.LogWarning("Falha ao enviar snapshot de sessões: {Error}", snapshot.Error);
    }

    private async Task SendAdOuSnapshotAsync(AgentIdentity identity, AgentOptions options, CancellationToken cancellationToken)
    {
        var execution = await _commandExecutionService.ExecuteAsync(
            AdOuSnapshotCommand,
            options.CommandTimeoutSeconds,
            cancellationToken);

        var output = execution.StandardOutput;
        if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(execution.StandardError))
        {
            output = execution.StandardError;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            _logger.LogWarning("Snapshot de OUs AD vazio para o servidor {ServerName}.", identity.ServerName);
            return;
        }

        if (output.Length > options.MaxAdOuSnapshotOutputLength)
        {
            _logger.LogWarning(
                "Snapshot de OUs AD excedeu o limite configurado ({Length}/{MaxLength}) e nao foi enviado.",
                output.Length,
                options.MaxAdOuSnapshotOutputLength);
            return;
        }

        var snapshot = await _apiClient.SendAdOuSnapshotAsync(new AgentAdOuSnapshotRequestDto
        {
            ServerName = identity.ServerName,
            Hostname = identity.Hostname,
            AgentId = identity.AgentId,
            AgentVersion = identity.AgentVersion,
            SupportsRds = identity.SupportsRds,
            SupportsAd = identity.SupportsAd,
            OrganizationalUnitsOutput = output,
            CapturedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        if (snapshot.Success)
        {
            _logger.LogDebug("Snapshot de OUs AD enviado com sucesso.");
            return;
        }

        if (snapshot.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Snapshot de OUs AD rejeitado (401). Verifique Agent:ApiKey.");
            return;
        }

        _logger.LogWarning("Falha ao enviar snapshot de OUs AD: {Error}", snapshot.Error);
    }

    private async Task PollAndExecuteAsync(AgentIdentity identity, AgentOptions options, CancellationToken cancellationToken)
    {
        var poll = await _apiClient.PollNextCommandAsync(new AgentPollRequestDto
        {
            Hostname = identity.Hostname,
            AgentId = identity.AgentId
        }, cancellationToken);

        if (!poll.Success)
        {
            if (poll.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Poll rejeitado (401). Verifique Agent:ApiKey.");
                return;
            }

            _logger.LogDebug("Poll sem sucesso: {Error}", poll.Error);
            return;
        }

        var command = poll.Value;
        if (command is null)
        {
            return;
        }

        _logger.LogInformation("Comando recebido. CommandId={CommandId}", command.CommandId);

        if (!_secureCommandCodec.TryDecodeCommand(command.CommandText, out var commandText, out var decodeError))
        {
            var decodePayload = new AgentCommandResultRequestDto
            {
                Success = false,
                ResultOutput = null,
                ErrorMessage = decodeError ?? "Falha ao decodificar comando."
            };

            var decodeSend = await _apiClient.SendCommandResultAsync(command.CommandId, decodePayload, cancellationToken);
            if (!decodeSend.Success)
            {
                await _pendingStore.EnqueueAsync(new PendingCommandResult
                {
                    CommandId = command.CommandId,
                    Success = false,
                    ResultOutput = null,
                    ErrorMessage = decodePayload.ErrorMessage,
                    CapturedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            _logger.LogWarning("Comando {CommandId} rejeitado por falha de decodificacao.", command.CommandId);
            return;
        }

        var execution = await _commandExecutionService.ExecuteAsync(
            commandText,
            options.CommandTimeoutSeconds,
            cancellationToken);

        var resultPayload = BuildResultPayload(execution, options);

        var sendResult = await _apiClient.SendCommandResultAsync(command.CommandId, resultPayload, cancellationToken);
        if (sendResult.Success)
        {
            _logger.LogInformation("Resultado enviado. CommandId={CommandId} Success={Success}", command.CommandId, resultPayload.Success);
            return;
        }

        await _pendingStore.EnqueueAsync(new PendingCommandResult
        {
            CommandId = command.CommandId,
            Success = resultPayload.Success,
            ResultOutput = resultPayload.ResultOutput,
            ErrorMessage = resultPayload.ErrorMessage,
            CapturedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogWarning(
            "Falha ao enviar resultado do comando {CommandId}. Resultado entrou na fila local. Erro: {Error}",
            command.CommandId,
            sendResult.Error);
    }

    private async Task FlushPendingResultsAsync(CancellationToken cancellationToken)
    {
        var pending = await _pendingStore.GetAllAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var item in pending.OrderBy(x => x.CapturedAtUtc).Take(20))
        {
            var payload = new AgentCommandResultRequestDto
            {
                Success = item.Success,
                ResultOutput = item.ResultOutput,
                ErrorMessage = item.ErrorMessage
            };

            var sent = await _apiClient.SendCommandResultAsync(item.CommandId, payload, cancellationToken);
            if (sent.Success)
            {
                await _pendingStore.RemoveAsync(item.CommandId, cancellationToken);
                _logger.LogInformation("Resultado pendente reenviado com sucesso. CommandId={CommandId}", item.CommandId);
                continue;
            }

            await _pendingStore.IncrementRetryAsync(item.CommandId, cancellationToken);
            _logger.LogWarning("Falha ao reenviar resultado pendente {CommandId}: {Error}", item.CommandId, sent.Error);

            if (sent.StatusCode == HttpStatusCode.Unauthorized)
            {
                break;
            }
        }
    }

    private static AgentCommandResultRequestDto BuildResultPayload(CommandExecutionResult execution, AgentOptions options)
    {
        var resultOutput = Truncate(execution.StandardOutput, options.MaxResultOutputLength);

        if (!execution.Success && string.IsNullOrWhiteSpace(resultOutput) && !string.IsNullOrWhiteSpace(execution.StandardError))
        {
            resultOutput = Truncate(execution.StandardError, options.MaxResultOutputLength);
        }

        var errorMessage = execution.Success
            ? null
            : BuildErrorMessage(execution, options.CommandTimeoutSeconds);

        return new AgentCommandResultRequestDto
        {
            Success = execution.Success,
            ResultOutput = string.IsNullOrWhiteSpace(resultOutput) ? null : resultOutput,
            ErrorMessage = errorMessage
        };
    }

    private static string BuildErrorMessage(CommandExecutionResult execution, int commandTimeoutSeconds)
    {
        if (execution.TimedOut)
        {
            return $"Tempo limite excedido ({commandTimeoutSeconds}s).";
        }

        if (!string.IsNullOrWhiteSpace(execution.StandardError))
        {
            return Truncate(execution.StandardError, 2000) ?? "Erro na execucao do comando.";
        }

        if (!string.IsNullOrWhiteSpace(execution.StandardOutput))
        {
            return Truncate(execution.StandardOutput, 2000) ?? "Erro na execucao do comando.";
        }

        return $"Comando retornou codigo {execution.ExitCode}.";
    }

    private static AgentIdentity BuildIdentity(AgentOptions options)
    {
        var machine = Environment.MachineName;
        var serverName = Normalize(options.ServerName) ?? machine;
        var hostname = Normalize(options.Hostname) ?? serverName;
        var agentId = Normalize(options.AgentId) ?? $"{machine}-agent";
        var supportsRds = options.SupportsRds;
        var supportsAd = options.SupportsAd;

        var version = typeof(AgentWorker).Assembly.GetName().Version?.ToString() ?? "0.1.0";

        return new AgentIdentity(agentId, serverName, hostname, version, supportsRds, supportsAd);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

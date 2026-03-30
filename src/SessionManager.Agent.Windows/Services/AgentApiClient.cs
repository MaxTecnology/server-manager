using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SessionManager.Agent.Windows.Models;

namespace SessionManager.Agent.Windows.Services;

public sealed class AgentApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentApiClient> _logger;

    public AgentApiClient(HttpClient httpClient, ILogger<AgentApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<AgentApiCallResult<AgentHeartbeatResponseDto>> SendHeartbeatAsync(
        AgentHeartbeatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<AgentHeartbeatRequestDto, AgentHeartbeatResponseDto>(
            "api/agent/heartbeat",
            request,
            cancellationToken);
    }

    public Task<AgentApiCallResult<AgentSessionSnapshotResponseDto>> SendSessionSnapshotAsync(
        AgentSessionSnapshotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<AgentSessionSnapshotRequestDto, AgentSessionSnapshotResponseDto>(
            "api/agent/session-snapshot",
            request,
            cancellationToken);
    }

    public Task<AgentApiCallResult<AgentAdOuSnapshotResponseDto>> SendAdOuSnapshotAsync(
        AgentAdOuSnapshotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<AgentAdOuSnapshotRequestDto, AgentAdOuSnapshotResponseDto>(
            "api/agent/ad-ou-snapshot",
            request,
            cancellationToken);
    }

    public async Task<AgentApiCallResult<AgentCommandDispatchDto?>> PollNextCommandAsync(
        AgentPollRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/agent/next-command",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new AgentApiCallResult<AgentCommandDispatchDto?>(true, null, null, response.StatusCode);
            }

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<AgentCommandDispatchDto>(JsonOptions, cancellationToken);
                return new AgentApiCallResult<AgentCommandDispatchDto?>(true, payload, null, response.StatusCode);
            }

            var error = await BuildErrorMessageAsync(response, cancellationToken);
            return new AgentApiCallResult<AgentCommandDispatchDto?>(false, null, error, response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AgentApiCallResult<AgentCommandDispatchDto?>(false, null, "Timeout ao consultar proximo comando.", HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha de comunicacao ao consultar proximo comando.");
            return new AgentApiCallResult<AgentCommandDispatchDto?>(false, null, ex.Message, HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<AgentApiCallResult<object?>> SendCommandResultAsync(
        Guid commandId,
        AgentCommandResultRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/agent/commands/{commandId}/result",
                request,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new AgentApiCallResult<object?>(true, null, null, response.StatusCode);
            }

            var error = await BuildErrorMessageAsync(response, cancellationToken);
            return new AgentApiCallResult<object?>(false, null, error, response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AgentApiCallResult<object?>(false, null, "Timeout ao enviar resultado do comando.", HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha de comunicacao ao enviar resultado do comando {CommandId}.", commandId);
            return new AgentApiCallResult<object?>(false, null, ex.Message, HttpStatusCode.ServiceUnavailable);
        }
    }

    private async Task<AgentApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, payload, JsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
                return new AgentApiCallResult<TResponse>(true, data, null, response.StatusCode);
            }

            var error = await BuildErrorMessageAsync(response, cancellationToken);
            return new AgentApiCallResult<TResponse>(false, default, error, response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AgentApiCallResult<TResponse>(false, default, "Timeout em chamada da API.", HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha de comunicacao em chamada para {Path}.", path);
            return new AgentApiCallResult<TResponse>(false, default, ex.Message, HttpStatusCode.ServiceUnavailable);
        }
    }

    private static async Task<string> BuildErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(parsed?.Message))
            {
                return parsed.Message;
            }
        }
        catch
        {
            // Keep raw body when payload is not JSON.
        }

        return body.Length <= 500 ? body : body[..500];
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

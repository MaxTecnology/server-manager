using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionManager.Agent.Windows.Models;
using SessionManager.Agent.Windows.Options;

namespace SessionManager.Agent.Windows.Services;

public sealed class PendingCommandResultStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IOptions<AgentOptions> _options;
    private readonly ILogger<PendingCommandResultStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PendingCommandResultStore(IOptions<AgentOptions> options, ILogger<PendingCommandResultStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PendingCommandResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetStorageFilePath();
            var items = await ReadUnsafeAsync(filePath, cancellationToken);
            return items.OrderBy(x => x.CapturedAtUtc).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnqueueAsync(PendingCommandResult pendingResult, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetStorageFilePath();
            var items = await ReadUnsafeAsync(filePath, cancellationToken);

            if (items.Any(x => x.CommandId == pendingResult.CommandId))
            {
                return;
            }

            items.Add(pendingResult);
            await WriteUnsafeAsync(filePath, items, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetStorageFilePath();
            var items = await ReadUnsafeAsync(filePath, cancellationToken);
            items.RemoveAll(x => x.CommandId == commandId);
            await WriteUnsafeAsync(filePath, items, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task IncrementRetryAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetStorageFilePath();
            var items = await ReadUnsafeAsync(filePath, cancellationToken);
            var target = items.FirstOrDefault(x => x.CommandId == commandId);
            if (target is null)
            {
                return;
            }

            target.RetryCount += 1;
            await WriteUnsafeAsync(filePath, items, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetStorageFilePath()
    {
        var dataDirectory = _options.Value.DataDirectory;
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            dataDirectory = @"C:\ProgramData\SessionManagerAgent\data";
        }

        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, "pending-results.json");
    }

    private async Task<List<PendingCommandResult>> ReadUnsafeAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return new List<PendingCommandResult>();
        }

        try
        {
            await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var parsed = await JsonSerializer.DeserializeAsync<List<PendingCommandResult>>(stream, JsonOptions, cancellationToken);
            return parsed ?? new List<PendingCommandResult>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler arquivo de resultados pendentes. Um novo arquivo sera criado.");
            return new List<PendingCommandResult>();
        }
    }

    private static async Task WriteUnsafeAsync(
        string filePath,
        List<PendingCommandResult> items,
        CancellationToken cancellationToken)
    {
        var tempFilePath = $"{filePath}.tmp";

        await using (var stream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions, cancellationToken);
        }

        File.Move(tempFilePath, filePath, overwrite: true);
    }
}

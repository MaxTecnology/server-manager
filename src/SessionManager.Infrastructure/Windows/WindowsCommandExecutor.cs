using System.Diagnostics;
using Microsoft.Extensions.Options;
using SessionManager.Infrastructure.Options;

namespace SessionManager.Infrastructure.Windows;

public sealed class WindowsCommandExecutor : IWindowsCommandExecutor
{
    private readonly WindowsSessionOptions _options;

    public WindowsCommandExecutor(IOptions<WindowsSessionOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CommandExecutionResult> ExecuteAsync(
        string command,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.CommandTimeoutSeconds)));

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore best-effort kill failures.
            }
        }

        var output = await outputTask;
        var error = await errorTask;
        return new CommandExecutionResult(timedOut ? -1 : process.ExitCode, output.Trim(), error.Trim(), timedOut);
    }
}

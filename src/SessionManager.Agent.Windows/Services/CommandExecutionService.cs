using System.ComponentModel;
using System.Diagnostics;
using SessionManager.Agent.Windows.Models;

namespace SessionManager.Agent.Windows.Services;

public sealed class CommandExecutionService
{
    public async Task<CommandExecutionResult> ExecuteAsync(
        string commandText,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return new CommandExecutionResult(
                Success: false,
                ExitCode: -1,
                StandardOutput: string.Empty,
                StandardError: "Comando vazio.",
                TimedOut: false);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(commandText);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return new CommandExecutionResult(
                Success: false,
                ExitCode: -1,
                StandardOutput: string.Empty,
                StandardError: ex.Message.Trim(),
                TimedOut: false);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));

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

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        if (timedOut)
        {
            return new CommandExecutionResult(
                Success: false,
                ExitCode: -1,
                StandardOutput: output,
                StandardError: error,
                TimedOut: true);
        }

        return new CommandExecutionResult(
            Success: process.ExitCode == 0,
            ExitCode: process.ExitCode,
            StandardOutput: output,
            StandardError: error,
            TimedOut: false);
    }
}

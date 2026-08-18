using System.Diagnostics;

namespace Smile.Compiler;

internal enum ProcessExecutionStatus
{
    Completed,
    TimedOut,
    Cancelled,
    StartFailed
}

internal sealed class ProcessExecutionResult
{
    public ProcessExecutionResult(ProcessExecutionStatus status, int? exitCode, string standardOutput,
        string standardError, string? startError = null)
    {
        Status = status;
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        StartError = startError;
    }

    public ProcessExecutionStatus Status { get; }
    public int? ExitCode { get; }
    public string StandardOutput { get; }
    public string StandardError { get; }
    public string? StartError { get; }
    public string CombinedOutput => StandardOutput + StandardError;
}

internal static class BoundedProcessRunner
{
    public static ProcessExecutionResult Run(ProcessStartInfo startInfo, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Process? process = null;
        try
        {
            process = new Process { StartInfo = startInfo };
            if (!process.Start())
                return new ProcessExecutionResult(ProcessExecutionStatus.StartFailed, null, string.Empty,
                    string.Empty, $"Could not start {startInfo.FileName}.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            process?.Dispose();
            return new ProcessExecutionResult(ProcessExecutionStatus.StartFailed, null, string.Empty,
                string.Empty, exception.Message);
        }

        using (process)
        using (var timeoutSource = new CancellationTokenSource(timeout))
        using (var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(
                   cancellationToken, timeoutSource.Token))
        {
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            var status = ProcessExecutionStatus.Completed;
            try
            {
                process.WaitForExitAsync(combinedSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                status = cancellationToken.IsCancellationRequested
                    ? ProcessExecutionStatus.Cancelled
                    : ProcessExecutionStatus.TimedOut;
                TryKillTree(process);
                process.WaitForExit();
            }

            Task.WhenAll(standardOutput, standardError).GetAwaiter().GetResult();
            return new ProcessExecutionResult(status, process.HasExited ? process.ExitCode : null,
                standardOutput.Result, standardError.Result);
        }
    }

    private static void TryKillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        catch (NotSupportedException)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }
    }
}

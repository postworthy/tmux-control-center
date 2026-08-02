using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Executable);
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in request.Arguments) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        logger.LogDebug("Starting operation {Operation}", request.Operation);
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Process did not start.");
            using var timeout = new CancellationTokenSource(request.Timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            var stdoutTask = ReadBoundedAsync(process.StandardOutput, request.MaxOutputBytes);
            var stderrTask = ReadBoundedAsync(process.StandardError, request.MaxOutputBytes);
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                TryKill(process);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            stopwatch.Stop();
            var exitCode = timedOut ? -1 : process.ExitCode;
            logger.Log(exitCode == 0 ? LogLevel.Debug : LogLevel.Warning,
                "Operation {Operation} finished with exit code {ExitCode} in {DurationMs} ms; timed out: {TimedOut}",
                request.Operation, exitCode, stopwatch.ElapsedMilliseconds, timedOut);
            return new ProcessResult(exitCode, stdout.Text, stderr.Text, stopwatch.Elapsed,
                stdout.Truncated || stderr.Truncated, timedOut);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Operation {Operation} failed after {DurationMs} ms",
                request.Operation, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<(string Text, bool Truncated)> ReadBoundedAsync(StreamReader reader, int maxBytes)
    {
        var buffer = new char[4096];
        var builder = new StringBuilder(Math.Min(maxBytes, 16_384));
        var retainedBytes = 0;
        var truncated = false;
        while (true)
        {
            var read = await reader.ReadAsync(buffer).ConfigureAwait(false);
            if (read == 0) break;
            if (truncated) continue;
            var chunk = new string(buffer, 0, read);
            var chunkBytes = Encoding.UTF8.GetByteCount(chunk);
            if (retainedBytes + chunkBytes <= maxBytes)
            {
                builder.Append(chunk);
                retainedBytes += chunkBytes;
            }
            else
            {
                var remaining = maxBytes - retainedBytes;
                foreach (var rune in chunk.EnumerateRunes())
                {
                    var bytes = rune.Utf8SequenceLength;
                    if (bytes > remaining) break;
                    builder.Append(rune.ToString());
                    remaining -= bytes;
                }
                truncated = true;
            }
        }
        return (builder.ToString(), truncated);
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }
}

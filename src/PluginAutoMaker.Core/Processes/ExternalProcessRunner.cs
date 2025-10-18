using System.Diagnostics;
using System.Text;
using PluginAutoMaker.Core.Logging;

namespace PluginAutoMaker.Core.Processes;

public sealed class ExternalProcessRunner : IExternalProcessRunner
{
    private readonly ILogSink _log;

    public ExternalProcessRunner(ILogSink log)
    {
        _log = log;
    }

    public async Task<ProcessExecutionResult> ExecuteAsync(ProcessStartOptions options, Action<string>? outputHandler = null, Action<string>? errorHandler = null, CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            Arguments = options.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? Directory.GetCurrentDirectory() : options.WorkingDirectory,
            RedirectStandardOutput = options.RedirectOutput,
            RedirectStandardError = options.RedirectOutput,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var pair in options.EnvironmentVariables)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        void HandleOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            stdout.AppendLine(e.Data);
            outputHandler?.Invoke(e.Data);
            _log.Publish(LogLevel.Information, e.Data);
        }

        void HandleError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            stderr.AppendLine(e.Data);
            errorHandler?.Invoke(e.Data);
            _log.Publish(LogLevel.Warning, e.Data);
        }

        _log.Publish(LogLevel.Debug, $"外部プロセス起動: {options.FileName} {options.Arguments}");
        process.Start();

        if (options.RedirectOutput)
        {
            process.OutputDataReceived += HandleOutput;
            process.ErrorDataReceived += HandleError;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        var timeout = options.Timeout ?? Timeout.InfiniteTimeSpan;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            linkedCts.CancelAfter(timeout);
        }

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                _log.Publish(LogLevel.Warning, "プロセスがタイムアウトしたため終了要求を送信します。");
                TryTerminateProcess(process);
                return new ProcessExecutionResult
                {
                    TimedOut = true,
                    ExitCode = -1,
                    StandardOutput = stdout.ToString(),
                    StandardError = stderr.ToString()
                };
            }
        }

        return new ProcessExecutionResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString(),
            TimedOut = false
        };
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.WriteLine("stop");
                process.WaitForExit(3000);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch
        {
            // ignore
        }
    }
}

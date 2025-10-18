
using System.Diagnostics;
using System.IO;
using System.Text;
using PluginAutoMaker.Core.Diagnostics;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;

namespace PluginAutoMaker.Core.Test;

public sealed class PaperTestRunner : ITestRunner
{
    private readonly PaperServerManager _paperManager;
    private readonly JavaEnvironmentValidator _javaValidator;
    private readonly ILogSink _log;

    public PaperTestRunner(PaperServerManager paperManager, JavaEnvironmentValidator javaValidator, ILogSink log)
    {
        _paperManager = paperManager;
        _javaValidator = javaValidator;
        _log = log;
    }

    public async Task<TestResult> RunTestsAsync(AutomationContext context, CancellationToken cancellationToken = default)
    {
        if (context.BuiltJarPath is null || !File.Exists(context.BuiltJarPath))
        {
            return new TestResult { Succeeded = false, FailureReason = "ビルド済みJarが見つかりません。" };
        }

        await _javaValidator.EnsureJava17Async(cancellationToken);
        var paperJar = await _paperManager.EnsurePaperJarAsync(context.PaperDirectory, cancellationToken, context.PreferredPaperJarPath);

        var serverDir = Path.Combine(context.WorkingDirectory, "server");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(Path.Combine(serverDir, "plugins"));
        await File.WriteAllTextAsync(Path.Combine(serverDir, "eula.txt"), "eula=true", cancellationToken);
        var pluginDestination = Path.Combine(serverDir, "plugins", Path.GetFileName(context.BuiltJarPath));
        File.Copy(context.BuiltJarPath, pluginDestination, overwrite: true);

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        var javaExecutable = string.IsNullOrWhiteSpace(javaHome)
            ? "java"
            : Path.Combine(javaHome, OperatingSystem.IsWindows() ? "bin\java.exe" : "bin/java");

        var startInfo = new ProcessStartInfo
        {
            FileName = javaExecutable,
            Arguments = $"-jar "{paperJar}" nogui",
            WorkingDirectory = serverDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var logBuilder = new StringBuilder();
        var doneDetected = false;
        var enabledDetected = false;
        var timeout = TimeSpan.FromSeconds(context.TestTimeoutSeconds);
        var startTime = DateTimeOffset.Now;

        _log.Publish(LogLevel.Information, "Paper サーバーを起動してスモークテストを実行します。");
        process.Start();

        async Task ReadStreamAsync(StreamReader reader)
        {
            while (!reader.EndOfStream && !process.HasExited)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                logBuilder.AppendLine(line);
                _log.Publish(LogLevel.Information, line);

                if (!doneDetected && line.Contains("Done ("))
                {
                    doneDetected = true;
                }

                if (!enabledDetected && line.Contains($"Enabled {context.Specification.PluginName}"))
                {
                    enabledDetected = true;
                }
            }
        }

        var outputTask = ReadStreamAsync(process.StandardOutput);
        var errorTask = ReadStreamAsync(process.StandardError);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (doneDetected && enabledDetected)
            {
                TrySendStop(process);
                break;
            }

            if (DateTimeOffset.Now - startTime > timeout)
            {
                TrySendStop(process);
                await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(2000, cancellationToken));
                if (!process.HasExited)
                {
                    process.Kill(true);
                }

                await Task.WhenAll(outputTask, errorTask);
                return new TestResult
                {
                    Succeeded = false,
                    Log = logBuilder.ToString(),
                    FailureReason = "Paperサーバーの起動がタイムアウトしました。"
                };
            }

            if (process.HasExited)
            {
                break;
            }

            await Task.Delay(500, cancellationToken);
        }

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync(cancellationToken);

        var success = doneDetected && enabledDetected;
        if (success)
        {
            _log.Publish(LogLevel.Information, "Paper サーバーでのスモークテストが成功しました。");
        }
        else
        {
            _log.Publish(LogLevel.Error, "Paper サーバーでのスモークテストが失敗しました。");
        }

        return new TestResult
        {
            Succeeded = success,
            Log = logBuilder.ToString(),
            FailureReason = success ? null : "必要なログパターンを検知できませんでした。"
        };
    }

    private void TrySendStop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.WriteLine("stop");
            }
        }
        catch (Exception ex)
        {
            _log.Publish(LogLevel.Warning, $"Paper サーバー停止コマンド送信に失敗しました: {ex.Message}");
        }
    }
}

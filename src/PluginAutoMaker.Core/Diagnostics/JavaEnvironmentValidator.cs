using System.ComponentModel;
using PluginAutoMaker.Core.Logging;

namespace PluginAutoMaker.Core.Diagnostics;

public sealed class JavaEnvironmentValidator
{
    private readonly ILogSink _log;

    public JavaEnvironmentValidator(ILogSink log)
    {
        _log = log;
    }

    public async Task EnsureJava17Async(CancellationToken cancellationToken = default)
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        var javaExecutable = string.IsNullOrWhiteSpace(javaHome)
            ? "java"
            : Path.Combine(javaHome, OperatingSystem.IsWindows() ? "bin\\java.exe" : "bin/java");

        var processStart = new System.Diagnostics.ProcessStartInfo
        {
            FileName = javaExecutable,
            Arguments = "-version",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = System.Diagnostics.Process.Start(processStart);
            if (process is null)
            {
                throw new InvalidOperationException("javaコマンドの起動に失敗しました。");
            }

            await process.WaitForExitAsync(cancellationToken);
            var output = await process.StandardError.ReadToEndAsync(cancellationToken);
            var versionLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (versionLine is null || !versionLine.Contains("17"))
            {
                throw new InvalidOperationException("Java 17 が検出できません。JAVA_HOME を JDK17 に設定してください。");
            }

            _log.Publish(LogLevel.Information, $"Java環境: {versionLine.Trim()}");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException("javaコマンドが見つかりません。JDK17 をインストールし、JAVA_HOME を設定してください。", ex);
        }
    }
}

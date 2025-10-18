
using System.Text;
using PluginAutoMaker.Core.Diagnostics;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Core.Processes;

namespace PluginAutoMaker.Core.Build;

public sealed class GradleBuildRunner : IBuildRunner
{
    private readonly IExternalProcessRunner _processRunner;
    private readonly IGradleWrapperProvider _wrapperProvider;
    private readonly JavaEnvironmentValidator _javaValidator;
    private readonly ILogSink _log;

    public GradleBuildRunner(IExternalProcessRunner processRunner, IGradleWrapperProvider wrapperProvider, JavaEnvironmentValidator javaValidator, ILogSink log)
    {
        _processRunner = processRunner;
        _wrapperProvider = wrapperProvider;
        _javaValidator = javaValidator;
        _log = log;
    }

    public async Task<BuildResult> BuildAsync(AutomationContext context, CancellationToken cancellationToken = default)
    {
        await _javaValidator.EnsureJava17Async(cancellationToken);
        var wrapperJar = await _wrapperProvider.EnsureWrapperAsync(context.ProjectDirectory, context.GradleVersion, cancellationToken);
        _log.Publish(LogLevel.Debug, $"Gradle Wrapper 確認済み: {wrapperJar}");

        var wrapperScript = OperatingSystem.IsWindows()
            ? Path.Combine(context.ProjectDirectory, "gradlew.bat")
            : Path.Combine(context.ProjectDirectory, "gradlew");

        var command = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
        var arguments = OperatingSystem.IsWindows()
            ? $"/c \"{wrapperScript}\" clean build copyPluginJar"
            : $"\"{wrapperScript}\" clean build copyPluginJar";

        var logBuilder = new StringBuilder();
        var result = await _processRunner.ExecuteAsync(new ProcessStartOptions
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = context.ProjectDirectory,
            Timeout = TimeSpan.FromMinutes(5)
        },
        outputHandler: line =>
        {
            logBuilder.AppendLine(line);
        },
        errorHandler: line =>
        {
            logBuilder.AppendLine(line);
        }, cancellationToken: cancellationToken);

        var jarPath = Path.Combine(context.ProjectDirectory, "build", "libs", context.Specification.OutputJarFileName);
        var success = result.ExitCode == 0 && File.Exists(jarPath);

        if (!success)
        {
            _log.Publish(LogLevel.Error, "Gradleビルドが失敗しました。");
        }
        else
        {
            _log.Publish(LogLevel.Information, "Gradleビルドが完了しました。");
        }

        if (success)
        {
            context.BuiltJarPath = jarPath;
        }

        return new BuildResult
        {
            Succeeded = success,
            Log = logBuilder.ToString(),
            OutputJarPath = success ? jarPath : null
        };
    }
}

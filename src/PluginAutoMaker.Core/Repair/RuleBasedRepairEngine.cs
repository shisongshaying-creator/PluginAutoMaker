using System.Text;
using PluginAutoMaker.Core.Build;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Core.Test;

namespace PluginAutoMaker.Core.Repair;

public sealed class RuleBasedRepairEngine : IRepairEngine
{
    private readonly ILogSink _log;

    public RuleBasedRepairEngine(ILogSink log)
    {
        _log = log;
    }

    public Task<RepairResult> TryRepairAsync(AutomationContext context, BuildResult? buildResult, TestResult? testResult, CancellationToken cancellationToken = default)
    {
        var log = buildResult?.Log ?? testResult?.Log ?? string.Empty;
        if (string.IsNullOrWhiteSpace(log))
        {
            return Task.FromResult(new RepairResult { Applied = false, Description = "修復対象のログがありません。" });
        }

        if (buildResult != null)
        {
            if (log.Contains("Unsupported class file major version") || log.Contains("class file major version"))
            {
                EnsureJavaToolchain(context);
                return Task.FromResult(new RepairResult
                {
                    Applied = true,
                    Description = "Gradleビルド設定に Java 17 toolchain を強制指定しました。"
                });
            }

            if (log.Contains("Could not find or load main class"))
            {
                EnsurePluginMainClass(context);
                return Task.FromResult(new RepairResult
                {
                    Applied = true,
                    Description = "plugin.yml の main エントリを修復しました。"
                });
            }

            if (log.Contains("Could not resolve") && log.Contains("io.papermc.paper"))
            {
                EnsureRepositories(context);
                return Task.FromResult(new RepairResult
                {
                    Applied = true,
                    Description = "Paper API リポジトリ設定を再適用しました。"
                });
            }
        }

        if (testResult != null)
        {
            if (log.Contains("Could not load plugin") || log.Contains("InvalidPluginException"))
            {
                EnsurePluginMainClass(context);
                return Task.FromResult(new RepairResult
                {
                    Applied = true,
                    Description = "plugin.yml の main エントリとjar配置を確認しました。"
                });
            }
        }

        return Task.FromResult(new RepairResult { Applied = false, Description = "適用可能な修復ルールが見つかりませんでした。" });
    }

    private void EnsureJavaToolchain(AutomationContext context)
    {
        var buildGradle = Path.Combine(context.ProjectDirectory, "build.gradle");
        if (!File.Exists(buildGradle))
        {
            return;
        }

        var content = File.ReadAllText(buildGradle, Encoding.UTF8);
        if (!content.Contains("JavaLanguageVersion.of(17)"))
        {
            content = content.Replace(
                "java {",
                "java {\n    toolchain {\n        languageVersion = JavaLanguageVersion.of(17)\n    }",
                StringComparison.Ordinal);
        }

        if (!content.Contains("tasks.withType(JavaCompile)"))
        {
            content += "\n\ntasks.withType(JavaCompile).configureEach {\n    options.encoding = 'UTF-8'\n}\n";
        }

        File.WriteAllText(buildGradle, content, Encoding.UTF8);
        _log.Publish(LogLevel.Information, "build.gradle を更新し Java 17 を強制しました。");
    }

    private void EnsureRepositories(AutomationContext context)
    {
        var buildGradle = Path.Combine(context.ProjectDirectory, "build.gradle");
        if (!File.Exists(buildGradle))
        {
            return;
        }

        var content = File.ReadAllText(buildGradle, Encoding.UTF8);
        if (!content.Contains("repo.papermc.io"))
        {
            content = content.Replace(
                "repositories {\n    mavenCentral()\n}",
                "repositories {\n    mavenCentral()\n    maven { url = uri('https://repo.papermc.io/repository/maven-public/') }\n}\n",
                StringComparison.Ordinal);
        }

        File.WriteAllText(buildGradle, content, Encoding.UTF8);
        _log.Publish(LogLevel.Information, "repositories セクションを修復しました。");
    }

    private void EnsurePluginMainClass(AutomationContext context)
    {
        var pluginYaml = Path.Combine(context.ProjectDirectory, "src", "main", "resources", "plugin.yml");
        if (!File.Exists(pluginYaml))
        {
            return;
        }

        var lines = File.ReadAllLines(pluginYaml, Encoding.UTF8).ToList();
        var mainLineIndex = lines.FindIndex(line => line.StartsWith("main:"));
        if (mainLineIndex >= 0)
        {
            lines[mainLineIndex] = $"main: {context.Specification.MainClass}";
        }
        else
        {
            lines.Insert(1, $"main: {context.Specification.MainClass}");
        }

        if (!lines.Any(line => line.StartsWith("api-version:")))
        {
            lines.Insert(2, $"api-version: {context.Specification.ApiVersion}");
        }

        File.WriteAllLines(pluginYaml, lines, Encoding.UTF8);
        _log.Publish(LogLevel.Information, "plugin.yml を修復しました。");
    }
}

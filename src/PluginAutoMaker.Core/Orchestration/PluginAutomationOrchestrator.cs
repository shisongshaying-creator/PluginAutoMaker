
using PluginAutoMaker.Core.Build;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Packaging;
using PluginAutoMaker.Core.Repair;
using PluginAutoMaker.Core.Scaffolding;
using PluginAutoMaker.Core.Test;
using PluginAutoMaker.Spec.Generation;
using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Core.Orchestration;

public sealed class PluginAutomationOrchestrator
{
    private readonly ISpecGenerator _specGenerator;
    private readonly IScaffolder _scaffolder;
    private readonly IBuildRunner _buildRunner;
    private readonly ITestRunner _testRunner;
    private readonly IRepairEngine _repairEngine;
    private readonly IArtifactPackager _packager;
    private readonly ILogSink _log;

    public PluginAutomationOrchestrator(
        ISpecGenerator specGenerator,
        IScaffolder scaffolder,
        IBuildRunner buildRunner,
        ITestRunner testRunner,
        IRepairEngine repairEngine,
        IArtifactPackager packager,
        ILogSink log)
    {
        _specGenerator = specGenerator;
        _scaffolder = scaffolder;
        _buildRunner = buildRunner;
        _testRunner = testRunner;
        _repairEngine = repairEngine;
        _packager = packager;
        _log = log;
    }

    public event EventHandler<OrchestratorProgress>? ProgressChanged;

    public async Task<AutomationResult> RunAsync(AutomationOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.RequirementText))
        {
            throw new ArgumentException("要件文が空です。", nameof(options));
        }

        try
        {
            Report(OrchestrationPhase.GeneratingSpecification, 0.05, "仕様を生成しています...");
            var specification = await _specGenerator.GenerateAsync(options.RequirementText, cancellationToken);
            specification.ZipSources = options.ZipSources;

            var workingDir = Path.Combine(Path.GetTempPath(), "PluginAutoMaker", DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff"));
            var projectDir = Path.Combine(workingDir, "project");
            var outputDir = string.IsNullOrWhiteSpace(options.OutputDirectory)
                ? Path.Combine(AppContext.BaseDirectory, "output", specification.PluginId + "-" + DateTimeOffset.Now.ToString("yyyyMMddHHmmss"))
                : Path.Combine(options.OutputDirectory, specification.PluginId + "-" + DateTimeOffset.Now.ToString("yyyyMMddHHmmss"));

            Directory.CreateDirectory(workingDir);
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(outputDir);

            var context = new AutomationContext
            {
                Specification = specification,
                WorkingDirectory = workingDir,
                ProjectDirectory = projectDir,
                OutputDirectory = outputDir,
                PaperDirectory = string.IsNullOrWhiteSpace(options.PaperDirectory) ? Path.Combine(AppContext.BaseDirectory, "paper") : options.PaperDirectory,
                TestTimeoutSeconds = options.TestTimeoutSeconds,
                GradleVersion = options.GradleVersion,
                PreferredPaperJarPath = options.PreferredPaperJarPath
            };

            Report(OrchestrationPhase.Scaffolding, 0.15, "プロジェクトをスキャフォールドしています...");
            await _scaffolder.ScaffoldAsync(context, cancellationToken);

            var appliedRepairs = new List<string>();
            BuildResult? buildResult = null;
            TestResult? testResult = null;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                context.Attempt = attempt;
                Report(OrchestrationPhase.Building, 0.3 + 0.2 * (attempt - 1), $"Gradleビルドを実行中 (試行 {attempt})...");
                buildResult = await _buildRunner.BuildAsync(context, cancellationToken);
                context.BuildLog = buildResult.Log;

                if (!buildResult.Succeeded)
                {
                    var repair = await _repairEngine.TryRepairAsync(context, buildResult, null, cancellationToken);
                    if (!repair.Applied)
                    {
                        return Failure(specification, outputDir, appliedRepairs, repair.Description);
                    }

                    appliedRepairs.Add(repair.Description);
                    _log.Publish(LogLevel.Information, $"自動修復を適用しました: {repair.Description}");
                    continue;
                }

                Report(OrchestrationPhase.Testing, 0.5 + 0.1 * (attempt - 1), $"Paperサーバーテストを実行中 (試行 {attempt})...");
                testResult = await _testRunner.RunTestsAsync(context, cancellationToken);
                context.TestLog = testResult.Log;

                if (testResult.Succeeded)
                {
                    Report(OrchestrationPhase.Packaging, 0.8, "成果物をパッケージングしています...");
                    var package = await _packager.PackageAsync(context, cancellationToken);
                    if (!package.Succeeded)
                    {
                        return Failure(specification, outputDir, appliedRepairs, "成果物のコピーに失敗しました。");
                    }

                    Report(OrchestrationPhase.Completed, 1.0, "すべての工程が完了しました。");
                    return new AutomationResult
                    {
                        Succeeded = true,
                        Specification = specification,
                        OutputPath = package.OutputDirectory,
                        AppliedRepairs = appliedRepairs
                    };
                }

                var repairResult = await _repairEngine.TryRepairAsync(context, buildResult, testResult, cancellationToken);
                if (!repairResult.Applied)
                {
                    return Failure(specification, outputDir, appliedRepairs, repairResult.Description);
                }

                appliedRepairs.Add(repairResult.Description);
                _log.Publish(LogLevel.Information, $"自動修復を適用しました: {repairResult.Description}");
            }

            return Failure(specification, outputDir, appliedRepairs, "最大試行回数に達しました。");
        }
        catch (Exception ex)
        {
            _log.Publish(LogLevel.Critical, $"予期しないエラー: {ex.Message}");
            return new AutomationResult
            {
                Succeeded = false,
                FailureReason = ex.Message
            };
        }
    }

    private AutomationResult Failure(PluginSpecification spec, string outputDir, List<string> repairs, string reason)
    {
        Report(OrchestrationPhase.Failed, 1.0, reason);
        return new AutomationResult
        {
            Succeeded = false,
            Specification = spec,
            OutputPath = outputDir,
            FailureReason = string.IsNullOrWhiteSpace(reason) ? "不明なエラーが発生しました。" : reason,
            AppliedRepairs = repairs
        };
    }

    private void Report(OrchestrationPhase phase, double progress, string message)
    {
        ProgressChanged?.Invoke(this, new OrchestratorProgress
        {
            Phase = phase,
            Percentage = progress,
            Message = message
        });
    }
}

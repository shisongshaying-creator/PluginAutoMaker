using PluginAutoMaker.Core.Build;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Core.Packaging;
using PluginAutoMaker.Core.Processes;
using PluginAutoMaker.Core.Repair;
using PluginAutoMaker.Core.Scaffolding;
using PluginAutoMaker.Core.Settings;
using PluginAutoMaker.Core.Test;
using PluginAutoMaker.Spec.Generation;

var logBuffer = new InMemoryLogBuffer();
logBuffer.EntryAdded += (_, entry) => Console.WriteLine(entry.ToString());

var requirement = args.Length > 0
    ? string.Join(' ', args)
    : "プラグイン名: HelloGUI\n/hello コマンドで 'Hello' を返す";

var settingsService = new SettingsService();
var settings = await settingsService.LoadAsync();

var specGenerator = new RuleBasedSpecGenerator();
var scaffolder = new TemplateScaffolder(logBuffer);
var processRunner = new ExternalProcessRunner(logBuffer);
var javaValidator = new PluginAutoMaker.Core.Diagnostics.JavaEnvironmentValidator(logBuffer);
await javaValidator.EnsureJava17Async();
using var gradleWrapperProvider = new GradleWrapperProvider(logBuffer);
using var paperManager = new PaperServerManager(logBuffer);
var buildRunner = new GradleBuildRunner(processRunner, gradleWrapperProvider, javaValidator, logBuffer);
var testRunner = new PaperTestRunner(paperManager, javaValidator, logBuffer);
var repairEngine = new RuleBasedRepairEngine(logBuffer);
var packager = new ArtifactPackager(logBuffer);

var orchestrator = new PluginAutomationOrchestrator(specGenerator, scaffolder, buildRunner, testRunner, repairEngine, packager, logBuffer);
orchestrator.ProgressChanged += (_, progress) =>
{
    Console.WriteLine($"[PROGRESS] {progress.Percentage:P0} {progress.Message}");
};

var options = new AutomationOptions
{
    RequirementText = requirement,
    OutputDirectory = settings.OutputDirectory,
    PaperDirectory = settings.PaperDirectory,
    ZipSources = false,
    GradleVersion = settings.GradleVersion,
    PreferredPaperJarPath = settings.PreferredPaperJarPath,
    TestTimeoutSeconds = settings.TestTimeoutSeconds
};

var result = await orchestrator.RunAsync(options);
Console.WriteLine(result.Succeeded
    ? $"完了: {result.OutputPath}"
    : $"失敗: {result.FailureReason}");

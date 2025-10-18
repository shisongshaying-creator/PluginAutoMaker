namespace PluginAutoMaker.Core.Orchestration;

public sealed class AutomationOptions
{
    public string RequirementText { get; init; } = string.Empty;

    public string OutputDirectory { get; init; } = string.Empty;

    public bool ZipSources { get; init; }

    public int TestTimeoutSeconds { get; init; } = 90;

    public string PaperDirectory { get; init; } = string.Empty;

    public string GradleVersion { get; init; } = "8.7";

    public string? PreferredPaperJarPath { get; init; }
}

using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Core.Orchestration;

public sealed class AutomationContext
{
    public required PluginSpecification Specification { get; init; }

    public required string WorkingDirectory { get; init; }

    public required string ProjectDirectory { get; init; }

    public required string OutputDirectory { get; init; }

    public required string PaperDirectory { get; init; }

    public string? BuildLog { get; set; }

    public string? TestLog { get; set; }

    public string? BuiltJarPath { get; set; }

    public int TestTimeoutSeconds { get; set; } = 90;

    public string GradleVersion { get; set; } = "8.7";

    public string? PreferredPaperJarPath { get; set; }

    public int Attempt { get; set; }
}

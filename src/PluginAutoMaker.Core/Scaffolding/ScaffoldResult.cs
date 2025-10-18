namespace PluginAutoMaker.Core.Scaffolding;

public sealed class ScaffoldResult
{
    public required string ProjectDirectory { get; init; }

    public required string GradleWrapperPath { get; init; }

    public required string BuildGradlePath { get; init; }

    public required string PluginYamlPath { get; init; }

    public required string SourceMainClassPath { get; init; }
}

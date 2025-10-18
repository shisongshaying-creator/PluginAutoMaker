namespace PluginAutoMaker.Core.Build;

public sealed class BuildResult
{
    public bool Succeeded { get; init; }

    public string Log { get; init; } = string.Empty;

    public string? OutputJarPath { get; init; }
}

namespace PluginAutoMaker.Core.Packaging;

public sealed class PackagingResult
{
    public bool Succeeded { get; init; }

    public string OutputDirectory { get; init; } = string.Empty;
}

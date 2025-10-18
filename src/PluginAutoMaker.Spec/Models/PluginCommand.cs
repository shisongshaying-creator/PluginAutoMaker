namespace PluginAutoMaker.Spec.Models;

public sealed class PluginCommand
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Usage { get; set; } = string.Empty;

    public string Permission { get; set; } = string.Empty;

    public override string ToString() => $"/{Name}: {Description}";
}

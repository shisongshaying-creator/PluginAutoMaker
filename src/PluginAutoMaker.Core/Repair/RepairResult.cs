namespace PluginAutoMaker.Core.Repair;

public sealed class RepairResult
{
    public bool Applied { get; init; }

    public string Description { get; init; } = string.Empty;
}

namespace PluginAutoMaker.Core.Test;

public sealed class TestResult
{
    public bool Succeeded { get; init; }

    public string Log { get; init; } = string.Empty;

    public string? FailureReason { get; init; }
}

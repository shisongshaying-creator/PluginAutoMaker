namespace PluginAutoMaker.Core.Orchestration;

public sealed class OrchestratorProgress
{
    public OrchestrationPhase Phase { get; init; }

    public double Percentage { get; init; }

    public string Message { get; init; } = string.Empty;

    public int Attempt { get; init; }
}

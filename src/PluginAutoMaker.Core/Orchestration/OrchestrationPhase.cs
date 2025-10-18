namespace PluginAutoMaker.Core.Orchestration;

public enum OrchestrationPhase
{
    Idle,
    GeneratingSpecification,
    Scaffolding,
    Building,
    Testing,
    Repairing,
    Packaging,
    Completed,
    Failed
}

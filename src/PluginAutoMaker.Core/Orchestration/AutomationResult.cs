using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Core.Orchestration;

public sealed class AutomationResult
{
    public bool Succeeded { get; init; }

    public PluginSpecification? Specification { get; init; }

    public string? OutputPath { get; init; }

    public string? FailureReason { get; init; }

    public IReadOnlyList<string> AppliedRepairs { get; init; } = Array.Empty<string>();
}

using PluginAutoMaker.Core.Build;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Core.Test;

namespace PluginAutoMaker.Core.Repair;

public interface IRepairEngine
{
    Task<RepairResult> TryRepairAsync(AutomationContext context, BuildResult? buildResult, TestResult? testResult, CancellationToken cancellationToken = default);
}

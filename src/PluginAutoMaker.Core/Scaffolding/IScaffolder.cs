using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Core.Scaffolding;

public interface IScaffolder
{
    Task<ScaffoldResult> ScaffoldAsync(AutomationContext context, CancellationToken cancellationToken = default);
}

using PluginAutoMaker.Core.Orchestration;

namespace PluginAutoMaker.Core.Build;

public interface IBuildRunner
{
    Task<BuildResult> BuildAsync(AutomationContext context, CancellationToken cancellationToken = default);
}

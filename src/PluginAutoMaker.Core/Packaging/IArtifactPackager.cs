using PluginAutoMaker.Core.Orchestration;

namespace PluginAutoMaker.Core.Packaging;

public interface IArtifactPackager
{
    Task<PackagingResult> PackageAsync(AutomationContext context, CancellationToken cancellationToken = default);
}

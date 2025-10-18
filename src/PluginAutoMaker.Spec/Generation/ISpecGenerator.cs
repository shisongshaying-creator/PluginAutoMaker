using PluginAutoMaker.Spec.Models;

namespace PluginAutoMaker.Spec.Generation;

public interface ISpecGenerator
{
    Task<PluginSpecification> GenerateAsync(string requirementText, CancellationToken cancellationToken = default);
}

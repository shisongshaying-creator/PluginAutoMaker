namespace PluginAutoMaker.Core.Build;

public interface IGradleWrapperProvider
{
    Task<string> EnsureWrapperAsync(string projectDirectory, string gradleVersion, CancellationToken cancellationToken = default);
}

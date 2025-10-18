using PluginAutoMaker.Core.Orchestration;

namespace PluginAutoMaker.Core.Test;

public interface ITestRunner
{
    Task<TestResult> RunTestsAsync(AutomationContext context, CancellationToken cancellationToken = default);
}

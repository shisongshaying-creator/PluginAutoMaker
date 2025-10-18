namespace PluginAutoMaker.Core.Processes;

public interface IExternalProcessRunner
{
    Task<ProcessExecutionResult> ExecuteAsync(ProcessStartOptions options, Action<string>? outputHandler = null, Action<string>? errorHandler = null, CancellationToken cancellationToken = default);
}

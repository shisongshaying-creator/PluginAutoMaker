namespace PluginAutoMaker.Core.Processes;

public sealed class ProcessStartOptions
{
    public required string FileName { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public IDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();

    public TimeSpan? Timeout { get; init; }

    public bool RedirectOutput { get; init; } = true;
}

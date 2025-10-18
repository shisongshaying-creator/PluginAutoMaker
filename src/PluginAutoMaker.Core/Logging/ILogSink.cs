namespace PluginAutoMaker.Core.Logging;

public interface ILogSink
{
    void Publish(LogLevel level, string message);

    void Publish(LogEntry entry);
}

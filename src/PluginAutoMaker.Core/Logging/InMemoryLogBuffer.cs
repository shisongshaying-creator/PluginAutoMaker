using System.Collections.Concurrent;

namespace PluginAutoMaker.Core.Logging;

public sealed class InMemoryLogBuffer : ILogSink
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public event EventHandler<LogEntry>? EntryAdded;

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public void Publish(LogLevel level, string message)
    {
        Publish(new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Message = message
        });
    }

    public void Publish(LogEntry entry)
    {
        _entries.Enqueue(entry);
        EntryAdded?.Invoke(this, entry);
    }
}

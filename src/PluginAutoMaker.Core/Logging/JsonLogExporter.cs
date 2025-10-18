using System.Text.Json;

namespace PluginAutoMaker.Core.Logging;

public static class JsonLogExporter
{
    public static async Task ExportAsync(IEnumerable<LogEntry> entries, string filePath, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, entries, options, cancellationToken);
    }
}

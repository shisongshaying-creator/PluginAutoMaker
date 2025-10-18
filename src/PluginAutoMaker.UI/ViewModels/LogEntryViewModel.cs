using System.Windows.Media;
using PluginAutoMaker.Core.Logging;

namespace PluginAutoMaker.UI.ViewModels;

public sealed class LogEntryViewModel
{
    public required string Message { get; init; }

    public required Brush Brush { get; init; }

    public static LogEntryViewModel FromLog(LogEntry entry)
    {
        return new LogEntryViewModel
        {
            Message = entry.ToString(),
            Brush = GetBrush(entry.Level)
        };
    }

    private static Brush GetBrush(LogLevel level)
    {
        return level switch
        {
            LogLevel.Warning => Brushes.Goldenrod,
            LogLevel.Error => Brushes.OrangeRed,
            LogLevel.Critical => Brushes.Red,
            LogLevel.Debug => Brushes.LightBlue,
            LogLevel.Trace => Brushes.Gray,
            _ => Brushes.White
        };
    }
}

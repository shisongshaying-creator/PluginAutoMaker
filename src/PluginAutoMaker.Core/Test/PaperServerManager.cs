
using PluginAutoMaker.Core.Logging;

namespace PluginAutoMaker.Core.Test;

public sealed class PaperServerManager : IDisposable
{
    private readonly ILogSink _log;
    private readonly HttpClient _httpClient = new();

    public PaperServerManager(ILogSink log)
    {
        _log = log;
    }

    public async Task<string> EnsurePaperJarAsync(string paperDirectory, CancellationToken cancellationToken = default, string? preferredJarPath = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredJarPath) && File.Exists(preferredJarPath))
        {
            return preferredJarPath;
        }

        Directory.CreateDirectory(paperDirectory);
        var jarPath = Path.Combine(paperDirectory, "paper-1.20.1.jar");
        if (File.Exists(jarPath))
        {
            return jarPath;
        }

        var url = new Uri("https://papermc.io/api/v2/projects/paper/versions/1.20.1/builds/400/downloads/paper-1.20.1-400.jar");
        _log.Publish(LogLevel.Information, "Paper 1.20.1 をダウンロードしています...");
        await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
        await using var file = File.Create(jarPath);
        await stream.CopyToAsync(file, cancellationToken);
        return jarPath;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

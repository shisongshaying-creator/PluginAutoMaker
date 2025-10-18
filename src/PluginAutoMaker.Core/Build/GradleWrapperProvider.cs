using System.IO.Compression;
using System.Linq;
using PluginAutoMaker.Core.Logging;

namespace PluginAutoMaker.Core.Build;

public sealed class GradleWrapperProvider : IGradleWrapperProvider, IDisposable
{
    private readonly ILogSink _log;
    private readonly HttpClient _httpClient = new();

    public GradleWrapperProvider(ILogSink log)
    {
        _log = log;
    }

    public async Task<string> EnsureWrapperAsync(string projectDirectory, string gradleVersion, CancellationToken cancellationToken = default)
    {
        var wrapperJar = Path.Combine(projectDirectory, "gradle", "wrapper", "gradle-wrapper.jar");
        if (File.Exists(wrapperJar))
        {
            return wrapperJar;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(wrapperJar)!);
        var cacheDir = Path.Combine(AppContext.BaseDirectory, "tools", "gradle", gradleVersion);
        Directory.CreateDirectory(cacheDir);
        var distributionFile = Path.Combine(cacheDir, $"gradle-{gradleVersion}-bin.zip");
        var distributionUrl = new Uri($"https://services.gradle.org/distributions/gradle-{gradleVersion}-bin.zip");

        if (!File.Exists(distributionFile))
        {
            _log.Publish(LogLevel.Information, $"Gradle {gradleVersion} のディストリビューションをダウンロードしています...");
            await using var downloadStream = await _httpClient.GetStreamAsync(distributionUrl, cancellationToken);
            await using var fileStream = File.Create(distributionFile);
            await downloadStream.CopyToAsync(fileStream, cancellationToken);
        }
        else
        {
            _log.Publish(LogLevel.Debug, "Gradleディストリビューションはキャッシュを利用します。");
        }

        using var archive = ZipFile.OpenRead(distributionFile);
        var wrapperEntry = archive.Entries
            .Select(entry => (Entry: entry, FileName: Path.GetFileName(entry.FullName)))
            .FirstOrDefault(tuple => string.Equals(tuple.FileName, "gradle-wrapper.jar", StringComparison.OrdinalIgnoreCase))
            .Entry;

        if (wrapperEntry is null)
        {
            wrapperEntry = archive.Entries
                .Select(entry => (Entry: entry, FileName: Path.GetFileName(entry.FullName)))
                .FirstOrDefault(tuple => tuple.FileName.StartsWith("gradle-wrapper", StringComparison.OrdinalIgnoreCase) &&
                                         tuple.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                .Entry;
        }

        if (wrapperEntry is null)
        {
            throw new InvalidOperationException("Gradleディストリビューションからwrapper.jarを特定できませんでした。");
        }

        await using var entryStream = wrapperEntry.Open();
        await using var target = File.Create(wrapperJar);
        await entryStream.CopyToAsync(target, cancellationToken);

        _log.Publish(LogLevel.Information, "Gradle Wrapper を初期化しました。");
        return wrapperJar;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

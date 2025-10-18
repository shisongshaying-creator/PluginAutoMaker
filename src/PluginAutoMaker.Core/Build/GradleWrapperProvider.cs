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
        var version = string.IsNullOrWhiteSpace(gradleVersion) ? "8.7" : gradleVersion.Trim();
        var wrapperJar = Path.Combine(projectDirectory, "gradle", "wrapper", "gradle-wrapper.jar");
        if (File.Exists(wrapperJar))
        {
            return wrapperJar;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(wrapperJar)!);
        var cacheDir = Path.Combine(AppContext.BaseDirectory, "tools", "gradle", version);
        Directory.CreateDirectory(cacheDir);
        var distributionFile = Path.Combine(cacheDir, $"gradle-{version}-bin.zip");
        var distributionUrl = new Uri($"https://services.gradle.org/distributions/gradle-{version}-bin.zip");

        if (!File.Exists(distributionFile))
        {
            _log.Publish(LogLevel.Information, $"Gradle {version} のディストリビューションをダウンロードしています...");
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
            .FirstOrDefault(entry => entry.FullName.EndsWith("gradle-wrapper.jar", StringComparison.OrdinalIgnoreCase));

        if (wrapperEntry is null)
        {
            wrapperEntry = archive.Entries
                .FirstOrDefault(entry => entry.FullName.Contains("gradle-wrapper", StringComparison.OrdinalIgnoreCase) &&
                                          entry.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase));
        }

        if (wrapperEntry is not null)
        {
            await using var entryStream = wrapperEntry.Open();
            await using var target = File.Create(wrapperJar);
            await entryStream.CopyToAsync(target, cancellationToken);
            _log.Publish(LogLevel.Information, "Gradle Wrapper を初期化しました。");
            return wrapperJar;
        }

        _log.Publish(LogLevel.Debug, "gradle-wrapper.jar がアーカイブ内で見つからなかったため、ディストリビューションを展開して検索します。");

        var extractionRoot = Path.Combine(cacheDir, "extracted");
        Directory.CreateDirectory(extractionRoot);
        if (!Directory.EnumerateFileSystemEntries(extractionRoot).Any())
        {
            ZipFile.ExtractToDirectory(distributionFile, extractionRoot, overwriteFiles: true);
        }

        var extractedWrapper = Directory.EnumerateFiles(extractionRoot, "gradle-wrapper*.jar", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (extractedWrapper is null)
        {
            throw new InvalidOperationException("Gradleディストリビューションからwrapper.jarを特定できませんでした。");
        }

        await using (var source = File.OpenRead(extractedWrapper))
        await using (var target = File.Create(wrapperJar))
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        _log.Publish(LogLevel.Information, "Gradle Wrapper を初期化しました。");
        return wrapperJar;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}


using System.IO.Compression;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;

namespace PluginAutoMaker.Core.Packaging;

public sealed class ArtifactPackager : IArtifactPackager
{
    private readonly ILogSink _log;

    public ArtifactPackager(ILogSink log)
    {
        _log = log;
    }

    public Task<PackagingResult> PackageAsync(AutomationContext context, CancellationToken cancellationToken = default)
    {
        if (context.BuiltJarPath is null || !File.Exists(context.BuiltJarPath))
        {
            return Task.FromResult(new PackagingResult { Succeeded = false, OutputDirectory = context.OutputDirectory });
        }

        Directory.CreateDirectory(context.OutputDirectory);
        var destinationJar = Path.Combine(context.OutputDirectory, Path.GetFileName(context.BuiltJarPath));
        File.Copy(context.BuiltJarPath, destinationJar, overwrite: true);
        _log.Publish(LogLevel.Information, $"Jar を出力先へコピーしました: {destinationJar}");

        var sourceCopyDir = Path.Combine(context.OutputDirectory, "source");
        if (Directory.Exists(sourceCopyDir))
        {
            Directory.Delete(sourceCopyDir, recursive: true);
        }
        CopyDirectory(context.ProjectDirectory, sourceCopyDir);

        if (context.Specification.ZipSources)
        {
            var zipPath = Path.Combine(context.OutputDirectory, context.Specification.SourceArchiveName);
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(context.ProjectDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            _log.Publish(LogLevel.Information, $"ソースコードをZip化しました: {zipPath}");
        }

        return Task.FromResult(new PackagingResult
        {
            Succeeded = true,
            OutputDirectory = context.OutputDirectory
        });
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
        }
    }
}

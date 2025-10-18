
using System.Text.Json;
using System.IO;

namespace PluginAutoMaker.Core.Settings;

public sealed class AppSettings
{
    public string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PluginAutoMaker", "output");

    public string PaperDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PluginAutoMaker", "paper");

    public string GradleVersion { get; set; } = "8.7";

    public int TestTimeoutSeconds { get; set; } = 90;

    public string? PreferredPaperJarPath { get; set; }
}

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PluginAutoMaker", "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new AppSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken);
        return settings ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }
}

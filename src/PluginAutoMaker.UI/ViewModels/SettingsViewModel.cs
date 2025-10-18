using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PluginAutoMaker.Core.Settings;

namespace PluginAutoMaker.UI.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _outputDirectory;
    private string _paperDirectory;
    private string _preferredPaperJarPath;
    private string _gradleVersion;
    private int _testTimeoutSeconds;

    public SettingsViewModel(AppSettings settings)
    {
        _outputDirectory = settings.OutputDirectory;
        _paperDirectory = settings.PaperDirectory;
        _preferredPaperJarPath = settings.PreferredPaperJarPath ?? string.Empty;
        _gradleVersion = settings.GradleVersion;
        _testTimeoutSeconds = settings.TestTimeoutSeconds;
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public string PaperDirectory
    {
        get => _paperDirectory;
        set => SetProperty(ref _paperDirectory, value);
    }

    public string PreferredPaperJarPath
    {
        get => _preferredPaperJarPath;
        set => SetProperty(ref _preferredPaperJarPath, value);
    }

    public string GradleVersion
    {
        get => _gradleVersion;
        set => SetProperty(ref _gradleVersion, value);
    }

    public int TestTimeoutSeconds
    {
        get => _testTimeoutSeconds;
        set => SetProperty(ref _testTimeoutSeconds, value);
    }

    public AppSettings BuildSettings()
    {
        return new AppSettings
        {
            OutputDirectory = OutputDirectory,
            PaperDirectory = PaperDirectory,
            PreferredPaperJarPath = string.IsNullOrWhiteSpace(PreferredPaperJarPath) ? null : PreferredPaperJarPath,
            GradleVersion = GradleVersion,
            TestTimeoutSeconds = TestTimeoutSeconds
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

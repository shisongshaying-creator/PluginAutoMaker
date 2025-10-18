
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PluginAutoMaker.Core.Build;
using PluginAutoMaker.Core.Logging;
using PluginAutoMaker.Core.Orchestration;
using PluginAutoMaker.Core.Packaging;
using PluginAutoMaker.Core.Processes;
using PluginAutoMaker.Core.Repair;
using PluginAutoMaker.Core.Scaffolding;
using PluginAutoMaker.Core.Settings;
using PluginAutoMaker.Core.Test;
using PluginAutoMaker.Spec.Generation;
using PluginAutoMaker.UI.Commands;
using PluginAutoMaker.UI.ViewModels;

namespace PluginAutoMaker.UI;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly InMemoryLogBuffer _logBuffer = new();
    private readonly SettingsService _settingsService = new();
    private readonly GradleWrapperProvider _gradleWrapperProvider;
    private readonly PaperServerManager _paperServerManager;
    private readonly PluginAutomationOrchestrator _orchestrator;
    private AppSettings _settings = new();
    private bool _isRunning;
    private string _statusMessage = "待機中";
    private double _progressValue;
    private string _outputPath = string.Empty;
    private string _requirementText = "プラグイン名: AutoPlugin\n/hello コマンドで挨拶を返す";
    private bool _zipSources;

    public MainViewModel()
    {
        _logBuffer.EntryAdded += (_, entry) => Application.Current.Dispatcher.Invoke(() => Logs.Add(LogEntryViewModel.FromLog(entry)));

        var specGenerator = new RuleBasedSpecGenerator();
        var scaffolder = new TemplateScaffolder(_logBuffer);
        var processRunner = new ExternalProcessRunner(_logBuffer);
        var javaValidator = new PluginAutoMaker.Core.Diagnostics.JavaEnvironmentValidator(_logBuffer);
        _gradleWrapperProvider = new GradleWrapperProvider(_logBuffer);
        var buildRunner = new GradleBuildRunner(processRunner, _gradleWrapperProvider, javaValidator, _logBuffer);
        _paperServerManager = new PaperServerManager(_logBuffer);
        var testRunner = new PaperTestRunner(_paperServerManager, javaValidator, _logBuffer);
        var repairEngine = new RuleBasedRepairEngine(_logBuffer);
        var packager = new ArtifactPackager(_logBuffer);

        _orchestrator = new PluginAutomationOrchestrator(specGenerator, scaffolder, buildRunner, testRunner, repairEngine, packager, _logBuffer);
        _orchestrator.ProgressChanged += (_, progress) => Application.Current.Dispatcher.Invoke(() =>
        {
            ProgressValue = progress.Percentage;
            StatusMessage = progress.Message;
        });

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning);
        OpenOutputCommand = new RelayCommand(OpenOutput, () => CanOpenOutput);
        ExportLogCommand = new AsyncRelayCommand(ExportLogAsync);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
    }

    public ObservableCollection<LogEntryViewModel> Logs { get; } = new();

    public AsyncRelayCommand StartCommand { get; }

    public RelayCommand OpenOutputCommand { get; }

    public AsyncRelayCommand ExportLogCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RequirementText
    {
        get => _requirementText;
        set => SetProperty(ref _requirementText, value);
    }

    public bool ZipSources
    {
        get => _zipSources;
        set => SetProperty(ref _zipSources, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                OpenOutputCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        private set
        {
            if (SetProperty(ref _outputPath, value))
            {
                OnPropertyChanged(nameof(CanOpenOutput));
                OpenOutputCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanOpenOutput => !string.IsNullOrWhiteSpace(OutputPath) && Directory.Exists(OutputPath);

    public async Task InitializeAsync()
    {
        _settings = await _settingsService.LoadAsync();
        ZipSources = false;
        StatusMessage = "準備完了";
    }

    private async Task StartAsync()
    {
        IsRunning = true;
        Logs.Clear();
        OutputPath = string.Empty;
        ProgressValue = 0;
        StatusMessage = "処理を開始します...";

        try
        {
            var options = new AutomationOptions
            {
                RequirementText = RequirementText,
                OutputDirectory = _settings.OutputDirectory,
                ZipSources = ZipSources,
                TestTimeoutSeconds = _settings.TestTimeoutSeconds,
                PaperDirectory = _settings.PaperDirectory,
                GradleVersion = _settings.GradleVersion,
                PreferredPaperJarPath = _settings.PreferredPaperJarPath
            };

            var result = await _orchestrator.RunAsync(options);
            if (result.Succeeded)
            {
                StatusMessage = "成功しました。出力先を確認してください。";
                if (!string.IsNullOrWhiteSpace(result.OutputPath))
                {
                    OutputPath = result.OutputPath;
                }
                else
                {
                    OutputPath = string.Empty;
                }
            }
            else
            {
                StatusMessage = $"失敗: {result.FailureReason}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
            _logBuffer.Publish(LogLevel.Error, ex.ToString());
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void OpenOutput()
    {
        if (!CanOpenOutput)
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = OutputPath,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $""{OutputPath}"",
                    UseShellExecute = false
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $""{OutputPath}"",
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"フォルダーを開けませんでした: {ex.Message}";
        }
    }

    private async Task ExportLogAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = $"PluginAutoMaker-log-{DateTime.Now:yyyyMMddHHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            await JsonLogExporter.ExportAsync(_logBuffer.Entries, dialog.FileName);
            StatusMessage = "ログを保存しました。";
        }
    }

    private async Task OpenSettingsAsync()
    {
        var window = new Views.SettingsWindow();
        var vm = new ViewModels.SettingsViewModel(_settings);
        window.DataContext = vm;
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _settings = vm.BuildSettings();
            await _settingsService.SaveAsync(_settings);
            StatusMessage = "設定を保存しました。";
        }
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _gradleWrapperProvider.Dispose();
        _paperServerManager.Dispose();
    }
}

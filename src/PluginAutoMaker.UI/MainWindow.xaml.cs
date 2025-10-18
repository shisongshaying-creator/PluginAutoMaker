
using System.Collections.Specialized;
using System.Windows;

namespace PluginAutoMaker.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
        _viewModel.Logs.CollectionChanged += LogsOnCollectionChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }

    private void LogsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LogScrollViewer?.ScrollToEnd();
    }
}

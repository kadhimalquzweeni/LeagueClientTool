using System.Windows;
using System.Windows.Threading;
using LeagueClientTool.App.ViewModels;

namespace LeagueClientTool.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _connectionTimer;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _connectionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _connectionTimer.Tick += OnConnectionTimerTick;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshConnectionCommand.ExecuteAsync(null);
        _connectionTimer.Start();
    }

    private async void OnConnectionTimerTick(object? sender, EventArgs e)
    {
        await _viewModel.RefreshConnectionCommand.ExecuteAsync(null);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _connectionTimer.Stop();
        _connectionTimer.Tick -= OnConnectionTimerTick;
    }
}

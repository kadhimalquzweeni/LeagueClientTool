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

    private async void OnClearChallengeTokensClick(object sender, RoutedEventArgs e)
    {
        if (Confirm("Clear all equipped challenge tokens from your profile?", "Clear challenge tokens"))
        {
            await _viewModel.ClearChallengeTokensCommand.ExecuteAsync(null);
        }
    }

    private async void OnCopyFirstChallengeTokenClick(object sender, RoutedEventArgs e)
    {
        if (Confirm("Copy the first equipped challenge token into all three slots?", "Copy challenge token"))
        {
            await _viewModel.CopyFirstChallengeTokenCommand.ExecuteAsync(null);
        }
    }

    private async void OnAcceptAllFriendRequestsClick(object sender, RoutedEventArgs e)
    {
        if (Confirm("Accept every incoming friend request?", "Accept friend requests"))
        {
            await _viewModel.AcceptAllFriendRequestsCommand.ExecuteAsync(null);
        }
    }

    private async void OnDeleteAllFriendRequestsClick(object sender, RoutedEventArgs e)
    {
        if (Confirm("Delete all pending friend requests? This cannot be undone.", "Delete friend requests"))
        {
            await _viewModel.DeleteAllFriendRequestsCommand.ExecuteAsync(null);
        }
    }

    private async void OnDeleteFriendsFromSelectedGroupClick(object sender, RoutedEventArgs e)
    {
        string groupName = _viewModel.SelectedFriendGroup?.Name ?? "the selected folder";
        if (Confirm($"Permanently remove every friend in '{groupName}' from your friends list?", "Delete friends"))
        {
            await _viewModel.DeleteFriendsFromSelectedGroupCommand.ExecuteAsync(null);
        }
    }

    private void OnRestartLeagueUxClick(object sender, RoutedEventArgs e)
    {
        if (Confirm("Restart the League client interface? The client UI will close and reopen.", "Restart League UX"))
        {
            _viewModel.RestartLeagueUxCommand.Execute(null);
        }
    }

    private bool Confirm(string message, string title) =>
        MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private void OnClosed(object? sender, EventArgs e)
    {
        _connectionTimer.Stop();
        _connectionTimer.Tick -= OnConnectionTimerTick;
    }
}

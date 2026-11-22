using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeagueClientTool.Core.Interfaces;

namespace LeagueClientTool.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILeagueClientDetector _clientDetector;

    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("home", "Overview", "⌂", "Your League Client Tool dashboard."),
        new("profile", "Profile", "◉", "Manage your League profile and visible details."),
        new("customisation", "Customisation", "✧", "Customise profile icons, backgrounds and other client options."),
        new("friends", "Friends", "♧", "Manage your League friends and social features."),
        new("tools", "Client Tools", "⚙", "Useful utilities for the running League client."),
        new("settings", "Settings", "☷", "Configure League Client Tool.")
    ];

    [ObservableProperty]
    private NavigationItem? selectedNavigation;

    [ObservableProperty]
    private string pageTitle = "Overview";

    [ObservableProperty]
    private string pageDescription = "Your League Client Tool dashboard.";

    [ObservableProperty]
    private bool isDashboardVisible = true;

    [ObservableProperty]
    private bool isPlaceholderVisible;

    [ObservableProperty]
    private string clientStatus = "Not detected";

    [ObservableProperty]
    private string connectionDetails = "Open the League of Legends client, then refresh detection.";

    [ObservableProperty]
    private bool isRefreshing;

    public MainViewModel(ILeagueClientDetector clientDetector)
    {
        _clientDetector = clientDetector;
        SelectedNavigation = NavigationItems[0];
    }

    [RelayCommand]
    private async Task RefreshConnectionAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            var connection = await _clientDetector.DetectAsync();

            if (connection is null)
            {
                ClientStatus = "Not detected";
                ConnectionDetails = "No running League client lockfile was found.";
                return;
            }

            ClientStatus = "Detected";
            ConnectionDetails = $"League Client lockfile found on local port {connection.Port}.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ClientStatus = "Detection error";
            ConnectionDetails = "League Client could not be inspected. Try refreshing again.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    partial void OnSelectedNavigationChanged(NavigationItem? value)
    {
        if (value is null)
        {
            return;
        }

        PageTitle = value.Title;
        PageDescription = value.Description;
        IsDashboardVisible = value.Key == "home";
        IsPlaceholderVisible = !IsDashboardVisible;
    }
}

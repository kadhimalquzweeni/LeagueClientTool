using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LeagueClientTool.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
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

    public MainViewModel()
    {
        SelectedNavigation = NavigationItems[0];
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

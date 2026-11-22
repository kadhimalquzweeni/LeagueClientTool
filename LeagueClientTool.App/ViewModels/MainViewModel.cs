using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILeagueClientDetector _clientDetector;
    private readonly ILeagueClientApiClientFactory _apiClientFactory;
    private readonly ILeagueProfileService _profileService;
    private readonly IDataDragonService _dataDragonService;
    private readonly IChallengeTokenService _challengeTokenService;
    private readonly ILeagueCustomRequestService _customRequestService;
    private readonly IFriendToolsService _friendToolsService;
    private readonly IClientToolsService _clientToolsService;

    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("home", "Overview", "⌂", "Your League Client Tool dashboard."),
        new("profile", "Profile", "◉", "Manage your Riot ID, status, visible rank and profile banner."),
        new("customisation", "Customisation", "✧", "Change your profile icon and champion background."),
        new("friends", "Friends", "♧", "Manage friend requests and friend folders."),
        new("tools", "Client Tools", "⚙", "Send custom LCU requests and restart the League interface."),
        new("settings", "Settings", "☷", "Application information and connection details.")
    ];

    public ObservableCollection<ProfileIconOption> AvailableProfileIcons { get; } =
        new(Enumerable.Range(50, 29).Select(id => new ProfileIconOption(
            id,
            $"https://ddragon.leagueoflegends.com/cdn/15.24.1/img/profileicon/{id}.png")));

    public ObservableCollection<string> QueueOptions { get; } =
    [
        "RANKED_SOLO_5x5", "RANKED_FLEX_SR", "RANKED_TFT",
        "RANKED_TFT_TURBO", "RANKED_TFT_DOUBLE_UP", "CHERRY"
    ];

    public ObservableCollection<string> TierOptions { get; } =
    [
        "IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM",
        "EMERALD", "DIAMOND", "MASTER", "GRANDMASTER", "CHALLENGER"
    ];

    public ObservableCollection<string> DivisionOptions { get; } = ["I", "II", "III", "IV", ""];

    public ObservableCollection<string> RequestMethods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public ObservableCollection<ChampionBackground> ChampionBackgrounds { get; } = [];
    public ObservableCollection<ChampionSkin> ChampionSkins { get; } = [];
    public ObservableCollection<FriendGroup> FriendGroups { get; } = [];

    [ObservableProperty] private NavigationItem? selectedNavigation;
    [ObservableProperty] private string pageTitle = "Overview";
    [ObservableProperty] private string pageDescription = "Your League Client Tool dashboard.";
    [ObservableProperty] private bool isDashboardVisible = true;
    [ObservableProperty] private bool isProfileVisible;
    [ObservableProperty] private bool isCustomisationVisible;
    [ObservableProperty] private bool isFriendsVisible;
    [ObservableProperty] private bool isToolsVisible;
    [ObservableProperty] private bool isSettingsVisible;

    [ObservableProperty] private string clientStatus = "Not detected";
    [ObservableProperty] private string connectionDetails = "Open the League of Legends client, then refresh detection.";
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isClientConnected;
    [ObservableProperty] private string playerName = "Not available";
    [ObservableProperty] private string playerLevel = "—";
    [ObservableProperty] private string playerRegion = "—";

    [ObservableProperty] private string operationMessage = "Ready.";
    [ObservableProperty] private string profileIconIdInput = "50";
    [ObservableProperty] private string statusMessageInput = string.Empty;
    [ObservableProperty] private string gameNameInput = string.Empty;
    [ObservableProperty] private string tagLineInput = string.Empty;
    [ObservableProperty] private string visibleRankQueue = "RANKED_SOLO_5x5";
    [ObservableProperty] private string visibleRankTier = "GOLD";
    [ObservableProperty] private string visibleRankDivision = "IV";
    [ObservableProperty] private ChampionBackground? selectedChampion;
    [ObservableProperty] private ChampionSkin? selectedSkin;
    [ObservableProperty] private FriendGroup? selectedFriendGroup;

    [ObservableProperty] private string customRequestMethod = "GET";
    [ObservableProperty] private string customRequestEndpoint = "/lol-summoner/v1/current-summoner";
    [ObservableProperty] private string customRequestBody = string.Empty;
    [ObservableProperty] private string customRequestResponseBody = string.Empty;
    [ObservableProperty] private string customRequestStatus = string.Empty;

    public MainViewModel(
        ILeagueClientDetector clientDetector,
        ILeagueClientApiClientFactory apiClientFactory,
        ILeagueProfileService profileService,
        IDataDragonService dataDragonService,
        IChallengeTokenService challengeTokenService,
        ILeagueCustomRequestService customRequestService,
        IFriendToolsService friendToolsService,
        IClientToolsService clientToolsService)
    {
        _clientDetector = clientDetector;
        _apiClientFactory = apiClientFactory;
        _profileService = profileService;
        _dataDragonService = dataDragonService;
        _challengeTokenService = challengeTokenService;
        _customRequestService = customRequestService;
        _friendToolsService = friendToolsService;
        _clientToolsService = clientToolsService;
        SelectedNavigation = NavigationItems[0];
    }

    [RelayCommand]
    private async Task RefreshConnectionAsync()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        try
        {
            var connection = await _clientDetector.DetectAsync();
            if (connection is null)
            {
                IsClientConnected = false;
                ClientStatus = "Not detected";
                ConnectionDetails = "No running League client lockfile was found.";
                PlayerName = "Not available";
                PlayerLevel = "—";
                PlayerRegion = "—";
                return;
            }

            using ILeagueClientApiClient api = _apiClientFactory.Create(connection);
            string regionJson = await api.GetAsync("/riotclient/region-locale");
            using JsonDocument regionDocument = JsonDocument.Parse(regionJson);
            JsonElement regionRoot = regionDocument.RootElement;
            PlayerRegion = ReadString(regionRoot, "region") ?? ReadString(regionRoot, "locale") ?? "Unknown";

            IsClientConnected = true;
            ClientStatus = "Connected";
            ConnectionDetails = $"Authenticated local API connection on port {connection.Port}.";

            try
            {
                string summonerJson = await api.GetAsync("/lol-summoner/v1/current-summoner");
                using JsonDocument summonerDocument = JsonDocument.Parse(summonerJson);
                JsonElement summoner = summonerDocument.RootElement;
                string? gameName = ReadString(summoner, "gameName");
                string? tagLine = ReadString(summoner, "tagLine");
                PlayerName = !string.IsNullOrWhiteSpace(gameName)
                    ? $"{gameName}#{tagLine}"
                    : ReadString(summoner, "displayName") ?? "Connected player";
                PlayerLevel = ReadString(summoner, "summonerLevel") ?? "—";
                GameNameInput = gameName ?? string.Empty;
                TagLineInput = tagLine ?? string.Empty;
            }
            catch (HttpRequestException)
            {
                PlayerName = "Connected player";
            }
        }
        catch (HttpRequestException)
        {
            IsClientConnected = false;
            ClientStatus = "Lockfile detected";
            ConnectionDetails = "The lockfile was found, but the authenticated League API did not respond.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException or JsonException)
        {
            IsClientConnected = false;
            ClientStatus = "Connection error";
            ConnectionDetails = "League Client could not be inspected. Try refreshing the connection.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SetProfileIconAsync(int? iconId)
    {
        int selectedIconId;
        if (iconId.HasValue)
        {
            selectedIconId = iconId.Value;
            ProfileIconIdInput = selectedIconId.ToString();
        }
        else if (!int.TryParse(ProfileIconIdInput, out selectedIconId))
        {
            OperationMessage = "Enter a valid numeric profile icon ID.";
            return;
        }

        await RunProfileActionAsync(() => _profileService.SetProfileIconAsync(selectedIconId));
    }

    [RelayCommand]
    private async Task SetProfileBackgroundAsync()
    {
        if (SelectedSkin is null)
        {
            OperationMessage = "Choose a champion and skin first.";
            return;
        }

        await RunProfileActionAsync(() => _profileService.SetProfileBackgroundAsync(SelectedSkin.SkinId));
    }

    [RelayCommand]
    private async Task UpdateStatusMessageAsync() =>
        await RunProfileActionAsync(() => _profileService.UpdateStatusMessageAsync(StatusMessageInput));

    [RelayCommand]
    private async Task ChangeRiotIdAsync() =>
        await RunProfileActionAsync(() => _profileService.ChangeRiotIdAsync(GameNameInput, TagLineInput));

    [RelayCommand]
    private async Task SetVisibleRankAsync() =>
        await RunProfileActionAsync(() => _profileService.SetVisibleRankAsync(
            VisibleRankQueue, VisibleRankTier, VisibleRankDivision));

    [RelayCommand]
    private async Task ClearVisibleRankAsync() =>
        await RunProfileActionAsync(() => _profileService.ClearVisibleRankAsync());

    [RelayCommand]
    private async Task SetLastRankBannerAsync() =>
        await RunProfileActionAsync(() => _profileService.SetLastRankBannerAsync());

    [RelayCommand]
    private async Task ClearChallengeTokensAsync() =>
        await RunProfileActionAsync(() => _challengeTokenService.ClearAsync());

    [RelayCommand]
    private async Task CopyFirstChallengeTokenAsync() =>
        await RunProfileActionAsync(() => _challengeTokenService.CopyFirstTokenToAllSlotsAsync());

    [RelayCommand]
    private async Task LoadChampionBackgroundsAsync()
    {
        if (ChampionBackgrounds.Count > 0) return;

        try
        {
            OperationMessage = "Loading champion backgrounds from Data Dragon…";
            var backgrounds = await _dataDragonService.GetChampionBackgroundsAsync();
            ChampionBackgrounds.Clear();
            foreach (var background in backgrounds) ChampionBackgrounds.Add(background);
            OperationMessage = $"Loaded {ChampionBackgrounds.Count} champion backgrounds.";
            SelectedChampion = ChampionBackgrounds.FirstOrDefault();
        }
        catch (Exception exception)
        {
            OperationMessage = $"Could not load champion backgrounds: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadFriendGroupsAsync()
    {
        try
        {
            var groups = await _friendToolsService.GetFriendGroupsAsync();
            FriendGroups.Clear();
            foreach (var group in groups) FriendGroups.Add(group);
            SelectedFriendGroup = FriendGroups.FirstOrDefault();
            OperationMessage = $"Loaded {FriendGroups.Count} friend folders.";
        }
        catch (Exception exception)
        {
            OperationMessage = $"Could not load friend folders: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task AcceptAllFriendRequestsAsync() =>
        await RunProfileActionAsync(() => _friendToolsService.AcceptAllFriendRequestsAsync());

    [RelayCommand]
    private async Task DeleteAllFriendRequestsAsync() =>
        await RunProfileActionAsync(() => _friendToolsService.DeleteAllFriendRequestsAsync());

    [RelayCommand]
    private async Task DeleteFriendsFromSelectedGroupAsync()
    {
        if (SelectedFriendGroup is null)
        {
            OperationMessage = "Select a friend folder first.";
            return;
        }

        await RunProfileActionAsync(() => _friendToolsService.DeleteFriendsFromGroupAsync(SelectedFriendGroup.GroupId));
        await LoadFriendGroupsAsync();
    }

    [RelayCommand]
    private async Task SendCustomRequestAsync()
    {
        try
        {
            var result = await _customRequestService.SendAsync(
                CustomRequestMethod,
                CustomRequestEndpoint,
                CustomRequestBody);
            CustomRequestStatus = $"HTTP {result.StatusCode?.ToString() ?? "—"} · {(result.Success ? "Success" : "Failed")}";
            CustomRequestResponseBody = result.ResponseBody ?? result.Message;
            OperationMessage = result.Message;
        }
        catch (Exception exception)
        {
            CustomRequestStatus = "Request failed";
            CustomRequestResponseBody = exception.Message;
            OperationMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void RestartLeagueUx()
    {
        var result = _clientToolsService.RestartLeagueUx();
        OperationMessage = result.Message;
    }

    private async Task RunProfileActionAsync(Func<Task<LeagueClientOperationResult>> action)
    {
        try
        {
            var result = await action();
            OperationMessage = result.Message;
        }
        catch (Exception exception)
        {
            OperationMessage = $"Operation failed: {exception.Message}";
        }
    }

    partial void OnSelectedNavigationChanged(NavigationItem? value)
    {
        if (value is null) return;

        PageTitle = value.Title;
        PageDescription = value.Description;
        IsDashboardVisible = value.Key == "home";
        IsProfileVisible = value.Key == "profile";
        IsCustomisationVisible = value.Key == "customisation";
        IsFriendsVisible = value.Key == "friends";
        IsToolsVisible = value.Key == "tools";
        IsSettingsVisible = value.Key == "settings";

        if (IsCustomisationVisible) _ = LoadChampionBackgroundsAsync();
        if (IsFriendsVisible) _ = LoadFriendGroupsAsync();
    }

    partial void OnSelectedChampionChanged(ChampionBackground? value)
    {
        _ = LoadChampionSkinsAsync(value);
    }

    private async Task LoadChampionSkinsAsync(ChampionBackground? champion)
    {
        ChampionSkins.Clear();
        SelectedSkin = null;
        if (champion is null) return;

        try
        {
            var skins = await _dataDragonService.GetChampionSkinsAsync(champion.ChampionId);
            foreach (var skin in skins) ChampionSkins.Add(skin);
            SelectedSkin = ChampionSkins.FirstOrDefault();
        }
        catch (Exception exception)
        {
            OperationMessage = $"Could not load {champion.ChampionName} skins: {exception.Message}";
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using LeagueClientTool.App.ViewModels;

namespace LeagueClientTool.App;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int GwlExStyle = -20;
    private const int WsExDlgModalFrame = 0x0001;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

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
        ApplyDarkTitleBar();
        HideWindowCaptionIcon();
        await _viewModel.RefreshConnectionCommand.ExecuteAsync(null);
        _connectionTimer.Start();
    }

    private void HideWindowCaptionIcon()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;

        // Clearing WM_SETICON alone can leave the class icon visible in the caption.
        // WS_EX_DLGMODALFRAME tells Windows not to reserve or draw the caption icon.
        long extendedStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        _ = SetWindowLongPtr(
            handle,
            GwlExStyle,
            new IntPtr(extendedStyle | WsExDlgModalFrame));

        _ = SendMessage(handle, WmSetIcon, new IntPtr(IconSmall), IntPtr.Zero);
        _ = SendMessage(handle, WmSetIcon, new IntPtr(IconBig), IntPtr.Zero);

        _ = SetWindowPos(
            handle,
            IntPtr.Zero,
            0, 0, 0, 0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpFrameChanged);
    }

    private void ApplyDarkTitleBar()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int enabled = 1;

        // Windows 11 uses attribute 20; attribute 19 is supported by earlier builds.
        int result = DwmSetWindowAttribute(
            handle,
            DwmwaUseImmersiveDarkMode,
            ref enabled,
            sizeof(int));

        if (result != 0)
        {
            _ = DwmSetWindowAttribute(
                handle,
                DwmwaUseImmersiveDarkModeLegacy,
                ref enabled,
                sizeof(int));
        }

        // COLORREF values use 0x00BBGGRR ordering.
        SetDwmColor(handle, DwmwaCaptionColor, 0x00271911); // #111927
        SetDwmColor(handle, DwmwaTextColor, 0x00FCFAF8);    // #F8FAFC
        SetDwmColor(handle, DwmwaBorderColor, 0x00483426);  // #263448
    }

    private static void SetDwmColor(IntPtr handle, int attribute, int color)
    {
        _ = DwmSetWindowAttribute(handle, attribute, ref color, sizeof(int));
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
        MessageBox.Show(this, message, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private void OnClosed(object? sender, EventArgs e)
    {
        _connectionTimer.Stop();
        _connectionTimer.Tick -= OnConnectionTimerTick;
    }
}

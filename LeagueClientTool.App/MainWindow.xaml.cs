using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
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
    private const int GclpHIcon = -14;
    private const int GclpHIconSmall = -34;
    private const int WsExDlgModalFrame = 0x0001;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 2;
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

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
    private static extern IntPtr SetClassLongPtr(IntPtr hwnd, int index, IntPtr newValue);

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


    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

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
        SourceInitialized += OnSourceInitialized;
        StateChanged += (_, _) => UpdateMaximizeRestoreButton();
        Closed += OnClosed;
    }


    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowMessageHook);
    }

    private static IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo || lParam == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
            return IntPtr.Zero;

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
            return IntPtr.Zero;

        var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        minMax.MaxPosition = new NativePoint
        {
            X = info.Work.Left - info.Monitor.Left,
            Y = info.Work.Top - info.Monitor.Top
        };
        minMax.MaxSize = new NativePoint
        {
            X = info.Work.Right - info.Work.Left,
            Y = info.Work.Bottom - info.Work.Top
        };

        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateMaximizeRestoreButton();
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

        // Also clear the window-class icons: Windows can fall back to these when
        // the per-window WM_SETICON values are null.
        _ = SetClassLongPtr(handle, GclpHIcon, IntPtr.Zero);
        _ = SetClassLongPtr(handle, GclpHIconSmall, IntPtr.Zero);

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

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            // Restore first so dragging a maximized window behaves naturally.
            Point cursor = e.GetPosition(this);
            double horizontalRatio = ActualWidth > 0 ? cursor.X / ActualWidth : 0.5;
            WindowState = WindowState.Normal;
            Left = Math.Max(0, e.GetPosition(this).X - RestoreBounds.Width * horizontalRatio);
            Top = Math.Max(0, e.GetPosition(this).Y - 16);
        }

        DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        ToggleMaximizeRestore();

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        Close();

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateMaximizeRestoreButton();
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (MaximizeRestoreButton is not null)
            MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
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

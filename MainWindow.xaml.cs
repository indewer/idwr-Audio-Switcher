using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace AudioSwitcherWidget;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly AppSettings _settings;
    private readonly Icon _appIcon;
    private bool _isFullyLoaded;
    private DispatcherTimer? _saveLocationDebounce;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Position window: restore on loaded
        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        // Generate matching custom sound wave icon
        _appIcon = TrayIconHelper.CreateSoundWaveIcon();
        Icon = TrayIconHelper.CreateSoundWaveImageSource(_appIcon);

        // System Tray Icon
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _appIcon,
            Text = "(idwr) Audio Switcher 0.2 b5",
            Visible = true
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Показать виджет", null, (s, e) => ShowAndActivate());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Выход", null, (s, e) => RealExit());
        _notifyIcon.ContextMenuStrip = contextMenu;

        _notifyIcon.DoubleClick += (s, e) => ShowAndActivate();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePosition();
        _isFullyLoaded = true;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            RestorePosition();
        });
    }

    private void RestorePosition()
    {
        try
        {
            var allScreens = Forms.Screen.AllScreens;
            if (allScreens.Length == 0) return;

            var primaryScreen = Forms.Screen.PrimaryScreen ?? allScreens[0];

            double w = ActualWidth > 0 ? ActualWidth : 350;
            double h = ActualHeight > 0 ? ActualHeight : 250;

            if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue)
            {
                double savedLeft = _settings.WindowLeft.Value;
                double savedTop = _settings.WindowTop.Value;

                // 1. First, check if the coordinates lie inside any current screen
                var samplePt = new System.Drawing.Point((int)savedLeft + 20, (int)savedTop + 20);
                Forms.Screen? targetScreen = allScreens.FirstOrDefault(s => s.Bounds.Contains(samplePt));

                // 2. If not found by point, check if we saved a specific monitor (e.g. DISPLAY1 / primary)
                if (targetScreen == null && !string.IsNullOrEmpty(_settings.SavedScreenDeviceName))
                {
                    targetScreen = allScreens.FirstOrDefault(s => s.DeviceName.Equals(_settings.SavedScreenDeviceName, StringComparison.OrdinalIgnoreCase));
                }

                if (targetScreen == null && _settings.IsPrimaryScreen == true)
                {
                    targetScreen = primaryScreen;
                }

                // If we found a valid screen that contains or matches the position
                if (targetScreen != null)
                {
                    var wa = targetScreen.WorkingArea;
                    // Clamp inside the found screen's working area
                    Left = Math.Max(wa.Left, Math.Min(savedLeft, wa.Right - w));
                    Top = Math.Max(wa.Top, Math.Min(savedTop, wa.Bottom - h));
                    return;
                }
            }

            // Fallback: place on Primary monitor top-right or bottom-right
            var pwa = primaryScreen.WorkingArea;
            Left = pwa.Right - w - 20;
            Top = pwa.Top + 40;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RestorePosition error: {ex.Message}");
        }
    }

    private void SaveCurrentPosition()
    {
        if (!_isFullyLoaded) return;
        if (WindowState != WindowState.Normal) return;

        try
        {
            var pt = new System.Drawing.Point((int)Left + 40, (int)Top + 40);
            var screen = Forms.Screen.FromPoint(pt);

            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
            if (screen != null)
            {
                _settings.SavedScreenDeviceName = screen.DeviceName;
                _settings.IsPrimaryScreen = screen.Primary;
            }
            _settings.Save();
        }
        catch { }
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (!_isFullyLoaded) return;
        if (WindowState != WindowState.Normal) return;

        _saveLocationDebounce?.Stop();
        _saveLocationDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveLocationDebounce.Tick += (s, ev) =>
        {
            _saveLocationDebounce?.Stop();
            SaveCurrentPosition();
        };
        _saveLocationDebounce.Start();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
            SaveCurrentPosition();
        }
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
            SaveCurrentPosition();
        }
    }

    private void DeviceCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is DeviceCardViewModel dev)
        {
            _vm.SelectDevice(dev);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsSettingsOpen = !_vm.IsSettingsOpen;
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsFromUI();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.AlwaysOnTop = !_vm.AlwaysOnTop;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsFromUI(closeSettings: false);
        SaveCurrentPosition();
        Hide();
    }

    private async void CheckUpdatesManual_Click(object sender, RoutedEventArgs e)
    {
        await _vm.CheckUpdatesManuallyAsync();
    }

    private void OpenTelegram_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://t.me/indewer") { UseShellExecute = true });
        }
        catch { }
    }

    private void OpenEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("mailto:indewer@ya.ru") { UseShellExecute = true });
        }
        catch { }
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/indewer/idwr-Audio-Switcher") { UseShellExecute = true });
        }
        catch { }
    }

    private async void ApplyUpdate_Click(object sender, RoutedEventArgs e)
    {
        await _vm.ApplyUpdateAsync();
    }

    private void DismissUpdateNotification_Click(object sender, RoutedEventArgs e)
    {
        _vm.DismissUpdateNotification(remindLater: false);
    }

    private void RemindLater_Click(object sender, RoutedEventArgs e)
    {
        _vm.DismissUpdateNotification(remindLater: true);
    }

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        RestorePosition();
        Activate();
    }

    private void RealExit()
    {
        SaveCurrentPosition();
        _vm.SaveSettingsFromUI(closeSettings: false);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SaveCurrentPosition();
        _vm.SaveSettingsFromUI(closeSettings: false);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        base.OnClosed(e);
    }
}

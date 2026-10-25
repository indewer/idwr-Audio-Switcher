using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace AudioSwitcherWidget;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly AppSettings _settings;
    private readonly Icon _appIcon;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Position window: either saved or top-right of screen
        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;

        // Generate matching custom sound wave icon
        _appIcon = TrayIconHelper.CreateSoundWaveIcon();
        Icon = TrayIconHelper.CreateSoundWaveImageSource(_appIcon);

        // System Tray Icon
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _appIcon,
            Text = "idwr Audio Switcher 0.1 b3",
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
        if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue)
        {
            // Verify inside virtual screen bounds
            if (_settings.WindowLeft >= SystemParameters.VirtualScreenLeft &&
                _settings.WindowLeft + ActualWidth <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
                _settings.WindowTop >= SystemParameters.VirtualScreenTop &&
                _settings.WindowTop + ActualHeight <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                Left = _settings.WindowLeft.Value;
                Top = _settings.WindowTop.Value;
                return;
            }
        }

        // Default position: near bottom-right above taskbar
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 40;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal && Left > -1000 && Top > -1000)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
            _settings.Save();
        }
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
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

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsFromUI(closeSettings: false);
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettingsFromUI(closeSettings: false);
        Hide();
        _notifyIcon.ShowBalloonTip(1500, "idwr Audio Switcher", "Виджет свернут в трей. Кликните дважды, чтобы открыть.", Forms.ToolTipIcon.Info);
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

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void RealExit()
    {
        _vm.SaveSettingsFromUI(closeSettings: false);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.SaveSettingsFromUI(closeSettings: false);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        base.OnClosed(e);
    }
}

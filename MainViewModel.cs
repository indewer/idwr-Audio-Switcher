using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AudioSwitcherWidget;

public class DeviceCardViewModel : INotifyPropertyChanged
{
    private bool _isDefault;
    private float _volume;
    private bool _isMuted;
    private string _displayName = string.Empty;

    public string Id { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DeviceKind Kind { get; set; }

    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public bool IsDefault
    {
        get => _isDefault;
        set { _isDefault = value; OnPropertyChanged(); }
    }

    public float Volume
    {
        get => _volume;
        set { _volume = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumeText)); }
    }

    public string VolumeText => $"{(int)Math.Round(Volume)}%";

    public bool IsMuted
    {
        get => _isMuted;
        set { _isMuted = value; OnPropertyChanged(); }
    }

    public string IconGeometry => Kind switch
    {
        DeviceKind.Headphones => "M3 14v5a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2zm13 0v5a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-5a2 2 0 0 0-2-2h-1a2 2 0 0 0-2 2z M4 14V11a8 8 0 0 1 16 0v3",
        DeviceKind.Display => "M2 5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5zm6 16h8m-4-4v4",
        DeviceKind.Digital => "M13 2L3 14h9l-1 8 10-12h-9l1-8z",
        _ => "M11 5L6 9H2v6h4l5 4V5zm4.5 3a5 5 0 0 1 0 8m2.5-11a9 9 0 0 1 0 14" // Speakers
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DeviceSettingItemViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;
    private string _customName = string.Empty;

    public string Id { get; set; } = string.Empty;
    public string HardwareName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string OriginalFullName { get; set; } = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnPropertyChanged(); }
    }

    public string CustomName
    {
        get => _customName;
        set { _customName = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isSettingsOpen;
    private bool _alwaysOnTop;
    private bool _startWithWindows;
    private bool _sortByRecent;
    private bool _autoCheckUpdates;
    private bool _isUpdatingInternally;

    // Update notification state
    private bool _isUpdateAvailable;
    private string _newVersionTag = string.Empty;
    private string _downloadUrl = string.Empty;
    private bool _isUpdating;
    private string _updateButtonText = "Обновить";

    public ObservableCollection<DeviceCardViewModel> VisibleDevices { get; } = new();
    public ObservableCollection<DeviceSettingItemViewModel> AllDevicesSettings { get; } = new();

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen && !value)
            {
                // Auto-save on closing settings
                SaveSettingsFromUI(closeSettings: false);
            }
            _isSettingsOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWidgetViewVisible));

            if (value && !_isUpdateAvailable && _autoCheckUpdates)
            {
                CheckPeriodicUpdatesAsync();
            }
        }
    }

    public bool IsWidgetViewVisible => !IsSettingsOpen;

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set
        {
            _alwaysOnTop = value;
            _settings.AlwaysOnTop = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            _startWithWindows = value;
            _settings.StartWithWindows = value;
            AppSettings.SetStartup(value);
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool SortByRecent
    {
        get => _sortByRecent;
        set
        {
            _sortByRecent = value;
            _settings.SortByRecent = value;
            _settings.Save();
            OnPropertyChanged();
            UpdateVisibleDevices();
        }
    }

    public bool AutoCheckUpdates
    {
        get => _autoCheckUpdates;
        set
        {
            _autoCheckUpdates = value;
            _settings.AutoCheckUpdates = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        set { _isUpdateAvailable = value; OnPropertyChanged(); }
    }

    public string NewVersionTag
    {
        get => _newVersionTag;
        set { _newVersionTag = value; OnPropertyChanged(); }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        set { _isUpdating = value; OnPropertyChanged(); }
    }

    public string UpdateButtonText
    {
        get => _updateButtonText;
        set { _updateButtonText = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        _settings = AppSettings.Load();
        _alwaysOnTop = _settings.AlwaysOnTop;
        _startWithWindows = _settings.StartWithWindows;
        _sortByRecent = _settings.SortByRecent;
        _autoCheckUpdates = _settings.AutoCheckUpdates;

        RefreshDevices(firstRun: true);

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _refreshTimer.Tick += (s, e) => RefreshDevices(firstRun: false);
        _refreshTimer.Start();

        if (_autoCheckUpdates)
        {
            CheckPeriodicUpdatesAsync();
        }
    }

    private async void CheckPeriodicUpdatesAsync()
    {
        try
        {
            bool shouldCheck = !_settings.LastUpdateCheckUtc.HasValue ||
                               (DateTime.UtcNow - _settings.LastUpdateCheckUtc.Value).TotalDays >= 12;

            if (shouldCheck)
            {
                var res = await UpdateHelper.CheckForUpdateAsync();
                _settings.LastUpdateCheckUtc = DateTime.UtcNow;
                _settings.Save();

                if (res.IsUpdateAvailable && !string.IsNullOrEmpty(res.DownloadUrl))
                {
                    _downloadUrl = res.DownloadUrl;
                    NewVersionTag = res.LatestVersionTag;
                    IsUpdateAvailable = true;
                }
            }
        }
        catch { }
    }

    public async Task ApplyUpdateAsync()
    {
        if (string.IsNullOrEmpty(_downloadUrl) || IsUpdating) return;
        IsUpdating = true;
        UpdateButtonText = "Загрузка...";
        await UpdateHelper.DownloadAndInstallUpdateAsync(_downloadUrl, status =>
        {
            UpdateButtonText = status;
        });
    }

    public void RefreshDevices(bool firstRun = false)
    {
        var rawDevices = CoreAudioHelper.GetPlaybackDevices();

        // Check if devices were connected or disconnected
        bool devicesChanged = AllDevicesSettings.Count != rawDevices.Count ||
            !AllDevicesSettings.Select(s => s.Id).ToHashSet().SetEquals(rawDevices.Select(d => d.Id));

        if (devicesChanged || firstRun)
        {
            _isUpdatingInternally = true;
            AllDevicesSettings.Clear();
            foreach (var d in rawDevices)
            {
                bool isEnabled = _settings.IsDeviceEnabled(d.Id, d.Name, d.FullName);
                string customName = _settings.GetCustomName(d.Id, d.Name, d.FullName);

                var item = new DeviceSettingItemViewModel
                {
                    Id = d.Id,
                    HardwareName = d.Name,
                    FullName = d.FullName,
                    OriginalFullName = string.IsNullOrEmpty(d.Subtitle) ? d.Name : $"{d.Name} ({d.Subtitle})",
                    IsEnabled = isEnabled,
                    CustomName = customName
                };

                // Realtime auto-save whenever user toggles checkbox or renames
                item.PropertyChanged += (s, e) =>
                {
                    if (_isUpdatingInternally) return;
                    AutoSaveSettings();
                };

                AllDevicesSettings.Add(item);
            }
            _isUpdatingInternally = false;
        }

        // Initialize ticks for current default if not set
        var defaultDev = rawDevices.FirstOrDefault(d => d.IsDefault);
        if (defaultDev != null && !_settings.DeviceLastUsedTicks.ContainsKey(defaultDev.Id))
        {
            _settings.DeviceLastUsedTicks[defaultDev.Id] = DateTime.UtcNow.Ticks;
            _settings.Save();
        }

        UpdateVisibleDevices(rawDevices);
    }

    private void AutoSaveSettings()
    {
        _settings.IsConfigured = true;

        _settings.DisabledDeviceIds.Clear();
        _settings.DisabledDeviceNames.Clear();

        foreach (var item in AllDevicesSettings)
        {
            if (!item.IsEnabled)
            {
                _settings.DisabledDeviceIds.Add(item.Id);
                if (!string.IsNullOrWhiteSpace(item.HardwareName))
                    _settings.DisabledDeviceNames.Add(item.HardwareName);
                if (!string.IsNullOrWhiteSpace(item.FullName))
                    _settings.DisabledDeviceNames.Add(item.FullName);
                if (!string.IsNullOrWhiteSpace(item.OriginalFullName))
                    _settings.DisabledDeviceNames.Add(item.OriginalFullName);
            }

            if (!string.IsNullOrWhiteSpace(item.CustomName))
            {
                string trimmed = item.CustomName.Trim();
                _settings.CustomDisplayNames[item.Id] = trimmed;
                if (!string.IsNullOrWhiteSpace(item.HardwareName))
                    _settings.CustomDisplayNamesByHardware[item.HardwareName] = trimmed;
                if (!string.IsNullOrWhiteSpace(item.FullName))
                    _settings.CustomDisplayNamesByHardware[item.FullName] = trimmed;
                if (!string.IsNullOrWhiteSpace(item.OriginalFullName))
                    _settings.CustomDisplayNamesByHardware[item.OriginalFullName] = trimmed;
            }
            else
            {
                _settings.CustomDisplayNames.Remove(item.Id);
                if (!string.IsNullOrWhiteSpace(item.HardwareName))
                    _settings.CustomDisplayNamesByHardware.Remove(item.HardwareName);
                if (!string.IsNullOrWhiteSpace(item.FullName))
                    _settings.CustomDisplayNamesByHardware.Remove(item.FullName);
                if (!string.IsNullOrWhiteSpace(item.OriginalFullName))
                    _settings.CustomDisplayNamesByHardware.Remove(item.OriginalFullName);
            }
        }

        _settings.Save();

        var rawDevices = CoreAudioHelper.GetPlaybackDevices();
        UpdateVisibleDevices(rawDevices);
    }

    private void UpdateVisibleDevices(System.Collections.Generic.List<AudioDeviceInfo>? rawDevices = null)
    {
        rawDevices ??= CoreAudioHelper.GetPlaybackDevices();
        var enabledRaw = rawDevices.Where(d => _settings.IsDeviceEnabled(d.Id, d.Name, d.FullName)).ToList();

        // Sort by recent / usage if enabled
        if (_settings.SortByRecent)
        {
            enabledRaw = enabledRaw
                .OrderByDescending(d => d.IsDefault ? 1 : 0)
                .ThenByDescending(d => _settings.DeviceLastUsedTicks.TryGetValue(d.Id, out var ticks) ? ticks : 0)
                .ThenBy(d => d.Name)
                .ToList();
        }

        var existingMap = VisibleDevices.ToDictionary(x => x.Id);
        var currentIds = enabledRaw.Select(x => x.Id).ToHashSet();

        // Remove unselected or disconnected
        for (int i = VisibleDevices.Count - 1; i >= 0; i--)
        {
            if (!currentIds.Contains(VisibleDevices[i].Id))
            {
                VisibleDevices.RemoveAt(i);
            }
        }

        // Add or update
        foreach (var raw in enabledRaw)
        {
            string custom = _settings.GetCustomName(raw.Id, raw.Name, raw.FullName);
            string displayName = !string.IsNullOrWhiteSpace(custom) ? custom : raw.Name;

            if (existingMap.TryGetValue(raw.Id, out var existing))
            {
                existing.DisplayName = displayName;
                existing.Subtitle = raw.Subtitle;
                existing.IsDefault = raw.IsDefault;
                existing.Volume = raw.Volume;
                existing.IsMuted = raw.IsMuted;
            }
            else
            {
                VisibleDevices.Add(new DeviceCardViewModel
                {
                    Id = raw.Id,
                    OriginalName = raw.Name,
                    DisplayName = displayName,
                    Subtitle = raw.Subtitle,
                    IsDefault = raw.IsDefault,
                    Volume = raw.Volume,
                    IsMuted = raw.IsMuted,
                    Kind = raw.Kind
                });
            }
        }

        // Align order
        for (int i = 0; i < enabledRaw.Count; i++)
        {
            var raw = enabledRaw[i];
            var current = VisibleDevices.FirstOrDefault(x => x.Id == raw.Id);
            if (current != null)
            {
                int oldIndex = VisibleDevices.IndexOf(current);
                if (oldIndex != i && oldIndex >= 0 && i < VisibleDevices.Count)
                {
                    VisibleDevices.Move(oldIndex, i);
                }
            }
        }
    }

    public void SelectDevice(DeviceCardViewModel device)
    {
        if (device == null) return;
        bool success = CoreAudioHelper.SetDefaultDevice(device.Id);
        if (success)
        {
            _settings.DeviceLastUsedTicks[device.Id] = DateTime.UtcNow.Ticks;
            _settings.Save();

            foreach (var d in VisibleDevices)
            {
                d.IsDefault = (d.Id == device.Id);
            }

            if (SortByRecent)
            {
                UpdateVisibleDevices();
            }
        }
    }

    public void SaveSettingsFromUI(bool closeSettings = true)
    {
        AutoSaveSettings();
        if (closeSettings)
        {
            IsSettingsOpen = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

namespace AudioSwitcherWidget;

public class AppSettings
{
    public bool IsConfigured { get; set; } = false;

    // Legacy support
    public List<string> EnabledDeviceIds { get; set; } = new();

    // Blacklist: explicitly disabled devices by user
    public HashSet<string> DisabledDeviceIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DisabledDeviceNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Custom display names: by device GUID
    public Dictionary<string, string> CustomDisplayNames { get; set; } = new();

    // Custom display names: by hardware/full name so reconnected USB devices preserve their custom names
    public Dictionary<string, string> CustomDisplayNamesByHardware { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool AlwaysOnTop { get; set; } = true;
    public double? WindowLeft { get; set; } = null;
    public double? WindowTop { get; set; } = null;
    public string? SavedScreenDeviceName { get; set; } = null;
    public bool? IsPrimaryScreen { get; set; } = null;
    public bool StartWithWindows { get; set; } = false;
    public bool SortByRecent { get; set; } = true;
    public bool AutoCheckUpdates { get; set; } = true;
    public DateTime? LastUpdateCheckUtc { get; set; } = null;
    public Dictionary<string, long> DeviceLastUsedTicks { get; set; } = new();

    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioSwitcherWidget",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    settings.DisabledDeviceIds = new HashSet<string>(settings.DisabledDeviceIds ?? new(), StringComparer.OrdinalIgnoreCase);
                    settings.DisabledDeviceNames = new HashSet<string>(settings.DisabledDeviceNames ?? new(), StringComparer.OrdinalIgnoreCase);
                    settings.CustomDisplayNames ??= new();
                    settings.CustomDisplayNamesByHardware = new Dictionary<string, string>(settings.CustomDisplayNamesByHardware ?? new(), StringComparer.OrdinalIgnoreCase);
                    settings.DeviceLastUsedTicks ??= new();

                    return settings;
                }
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsFilePath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }

    /// <summary>
    /// Looks up custom name: first by exact endpoint ID, then by hardware/model name,
    /// then by full name, then by partial match against existing custom names.
    /// </summary>
    public string GetCustomName(string deviceId, string hardwareName, string fullName)
    {
        if (CustomDisplayNames.TryGetValue(deviceId, out var name) && !string.IsNullOrWhiteSpace(name))
            return name;

        if (!string.IsNullOrWhiteSpace(hardwareName) &&
            CustomDisplayNamesByHardware.TryGetValue(hardwareName, out var hwName) && !string.IsNullOrWhiteSpace(hwName))
            return hwName;

        if (!string.IsNullOrWhiteSpace(fullName) &&
            CustomDisplayNamesByHardware.TryGetValue(fullName, out var fnName) && !string.IsNullOrWhiteSpace(fnName))
            return fnName;

        // Smart substring match against existing custom names (e.g. user assigned "1MORE" to 1MORE USB headset)
        foreach (var kvp in CustomDisplayNames)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                if ((!string.IsNullOrWhiteSpace(hardwareName) && hardwareName.Contains(kvp.Value, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(fullName) && fullName.Contains(kvp.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    return kvp.Value;
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Determines whether device should be visible.
    /// Newly connected devices are visible (enabled) by default.
    /// Only devices explicitly unchecked by user in settings are disabled.
    /// </summary>
    public bool IsDeviceEnabled(string deviceId, string hardwareName, string fullName)
    {
        if (DisabledDeviceIds.Contains(deviceId)) return false;
        if (!string.IsNullOrWhiteSpace(hardwareName) && DisabledDeviceNames.Contains(hardwareName)) return false;
        if (!string.IsNullOrWhiteSpace(fullName) && DisabledDeviceNames.Contains(fullName)) return false;

        return true;
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "AudioSwitcherWidget";
            using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
            if (key != null)
            {
                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }
        catch { }
    }
}

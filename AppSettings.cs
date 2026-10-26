using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace AudioSwitcherWidget;

public class AppSettings
{
    public bool IsConfigured { get; set; } = false;
    public List<string> EnabledDeviceIds { get; set; } = new();
    public Dictionary<string, string> CustomDisplayNames { get; set; } = new();
    public bool AlwaysOnTop { get; set; } = true;
    public double? WindowLeft { get; set; } = null;
    public double? WindowTop { get; set; } = null;
    public string? SavedScreenDeviceName { get; set; } = null;
    public bool? IsPrimaryScreen { get; set; } = null;
    public bool StartWithWindows { get; set; } = false;
    public bool SortByRecent { get; set; } = true;
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
                    if (settings.EnabledDeviceIds != null && settings.EnabledDeviceIds.Count > 0)
                    {
                        settings.IsConfigured = true;
                    }
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

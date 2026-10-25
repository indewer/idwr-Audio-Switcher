using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace AudioSwitcherWidget;

public enum DeviceKind
{
    Headphones,
    Speakers,
    Display,
    Digital,
    Generic
}

public class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public float Volume { get; set; }
    public bool IsMuted { get; set; }
    public DeviceKind Kind { get; set; }
}

public static class CoreAudioHelper
{
    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class CPolicyConfigClient { }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bFlow, IntPtr ppFormat);
        [PreserveSig] int ResetDeviceFormat(string pszDeviceName);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr pMixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bCapture, IntPtr pbDefaultPeriod, IntPtr pbMinimumPeriod);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, IntPtr pbPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr pMode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, IntPtr pKey, IntPtr pvValue);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, IntPtr pKey, IntPtr pvValue);
        [PreserveSig] int SetDefaultEndpoint([In, MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId, [In, MarshalAs(UnmanagedType.U4)] ERole eRole);
        [PreserveSig] int SetEndpointVisibility(string pszDeviceName, int bVisible);
    }

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    public static List<AudioDeviceInfo> GetPlaybackDevices()
    {
        var list = new List<AudioDeviceInfo>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            MMDevice? defaultDevice = null;
            try
            {
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch { }

            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var ep in endpoints)
            {
                float vol = 0f;
                bool muted = false;
                try
                {
                    vol = ep.AudioEndpointVolume.MasterVolumeLevelScalar * 100f;
                    muted = ep.AudioEndpointVolume.Mute;
                }
                catch { }

                string fullName = ep.FriendlyName ?? "Audio Device";
                var (title, subtitle, kind) = ParseDeviceNameAndKind(fullName);

                list.Add(new AudioDeviceInfo
                {
                    Id = ep.ID,
                    Name = title,
                    Subtitle = subtitle,
                    IsDefault = defaultDevice != null && string.Equals(ep.ID, defaultDevice.ID, StringComparison.OrdinalIgnoreCase),
                    Volume = vol,
                    IsMuted = muted,
                    Kind = kind
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetPlaybackDevices error: {ex.Message}");
        }

        return list;
    }

    public static bool SetDefaultDevice(string deviceId)
    {
        try
        {
            var policyConfig = (IPolicyConfig)new CPolicyConfigClient();
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetDefaultDevice error: {ex.Message}");
            return false;
        }
    }

    public static void SetVolume(string deviceId, float volumePercent)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDevice(deviceId);
            if (device != null)
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(volumePercent / 100f, 0f, 1f);
            }
        }
        catch { }
    }

    public static void ToggleMute(string deviceId)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDevice(deviceId);
            if (device != null)
            {
                device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
            }
        }
        catch { }
    }

    private static (string title, string subtitle, DeviceKind kind) ParseDeviceNameAndKind(string fullName)
    {
        string lower = fullName.ToLowerInvariant();
        DeviceKind kind = DeviceKind.Generic;

        if (lower.Contains("headset") || lower.Contains("headphone") || lower.Contains("наушник") || lower.Contains("spearhead") || lower.Contains("wh-") || lower.Contains("airpods"))
        {
            kind = DeviceKind.Headphones;
        }
        else if (lower.Contains("tv") || lower.Contains("sscr") || lower.Contains("lu28") || lower.Contains("display") || lower.Contains("hdmi") || lower.Contains("nvidia") || lower.Contains("монитор"))
        {
            kind = DeviceKind.Display;
        }
        else if (lower.Contains("spdif") || lower.Contains("digital") || lower.Contains("цифров") || lower.Contains("optical"))
        {
            kind = DeviceKind.Digital;
        }
        else if (lower.Contains("speaker") || lower.Contains("динамик") || lower.Contains("sound blaster") || lower.Contains("realtek"))
        {
            kind = DeviceKind.Speakers;
        }

        // Clean up string like "Динамики (1MORE Spearhead VR )"
        string title = fullName;
        string subtitle = string.Empty;

        int openParen = fullName.IndexOf('(');
        int closeParen = fullName.LastIndexOf(')');
        if (openParen > 0 && closeParen > openParen)
        {
            string inside = fullName.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            string outside = fullName.Substring(0, openParen).Trim();

            // Typically outside is "Динамики" and inside is the actual hardware name "1MORE Spearhead VR"
            if (!string.IsNullOrEmpty(inside))
            {
                title = inside;
                subtitle = outside;
            }
        }

        return (title, subtitle, kind);
    }
}

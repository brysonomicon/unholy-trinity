using System;
using System.Linq;
using UnityEngine;

public enum VolumeChannel
{
    Master,
    Music,
    SFX,
    Announcer
}

/// Stores player settings in PlayerPrefs.
/// Display settings are applied here; audio code reads volumes itself
/// (GetVolume on startup, OnVolumeChanged for live updates).
public static class GameSettings
{
    #region Audio

    const float DefaultVolume = 1f;

    public static event Action<VolumeChannel, float> OnVolumeChanged;

    public static float GetVolume(VolumeChannel ch) =>
        PlayerPrefs.GetFloat(ch + "Volume", DefaultVolume);

    public static void SetVolume(VolumeChannel ch, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(ch + "Volume", value);
        OnVolumeChanged?.Invoke(ch, value);
    }

    #endregion

    #region Graphics

    public static event Action<bool> OnCRTChanged;

    public static bool CRTEnabled
    {
        get => PlayerPrefs.GetInt("CRTEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("CRTEnabled", value ? 1 : 0);
            OnCRTChanged?.Invoke(value);
        }
    }

    #endregion

    #region Display

    public static FullScreenMode DisplayMode
    {
        get => (FullScreenMode)PlayerPrefs.GetInt("DisplayMode", (int)FullScreenMode.FullScreenWindow);
        set { PlayerPrefs.SetInt("DisplayMode", (int)value); ApplyDisplay(); }
    }

    public static Vector2Int Resolution
    {
        get => new Vector2Int(
            PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width),
            PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height));
        set
        {
            PlayerPrefs.SetInt("ResolutionWidth", value.x);
            PlayerPrefs.SetInt("ResolutionHeight", value.y);
            ApplyDisplay();
        }
    }

    public static Vector2Int[] AvailableResolutions() =>
        Screen.resolutions.Select(r => new Vector2Int(r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.x * r.y)
            .ToArray();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyDisplay()
    {
        var res = Resolution;
        if (!AvailableResolutions().Contains(res))
        {
            res = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        Screen.SetResolution(res.x, res.y, DisplayMode);
    }

    #endregion
}

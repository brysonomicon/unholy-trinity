using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Reusable settings panel. The individual controls (VolumeSlider, ResolutionDropdown,
/// DisplayModeDropdown) handle their own values; this only opens/closes the panel.
/// Hosts (title screen, pause menu) wire onClosed in the Inspector to restore their own buttons.
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] Selectable firstSelected;
    [SerializeField] UnityEvent onClosed;

    public void Open()
    {
        gameObject.SetActive(true);
        if (firstSelected)
        {
            firstSelected.Select();
        }
    }

    public void Close()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
        onClosed.Invoke();
    }
}

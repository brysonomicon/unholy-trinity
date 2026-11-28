using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Reusable settings panel. The individual controls (VolumeSlider, ResolutionDropdown,
/// DisplayModeDropdown) handle their own values; this only opens/closes the panel.
/// Hosts (title screen, pause menu) wire onClosed in the Inspector to restore their own buttons.
public class PanelSelection : MonoBehaviour
{
    [SerializeField] Selectable firstSelected;
    [SerializeField] UnityEvent onClosed;

    GameObject lastSelected;

    public void Open()
    {
        gameObject.SetActive(true);
        lastSelected = null;
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

    // Keep something selected so we can navigate with kb/controller.
    void Update()
    {
        var es = EventSystem.current;
        if (es == null) return;

        var current = es.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy && current.transform.IsChildOf(transform))
        {
            lastSelected = current;
            return;
        }

        var fallback = lastSelected != null && lastSelected.activeInHierarchy
            ? lastSelected
            : firstSelected != null ? firstSelected.gameObject : null;
        if (fallback != null)
        {
            es.SetSelectedGameObject(fallback);
        }
    }
}

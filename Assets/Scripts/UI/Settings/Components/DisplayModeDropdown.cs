using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class DisplayModeDropdown : MonoBehaviour
{
    static readonly (FullScreenMode mode, string label)[] Options =
    {
        (FullScreenMode.FullScreenWindow,    "Borderless"),
        (FullScreenMode.ExclusiveFullScreen, "Fullscreen"),
        (FullScreenMode.Windowed,            "Windowed"),
    };

    void OnEnable()
    {
        var dd = GetComponent<TMP_Dropdown>();
        dd.ClearOptions();
        dd.AddOptions(Options.Select(o => o.label).ToList());
        dd.SetValueWithoutNotify(Mathf.Max(0, System.Array.FindIndex(Options, o => o.mode == GameSettings.DisplayMode)));
        dd.onValueChanged.AddListener(i => GameSettings.DisplayMode = Options[i].mode);
    }

    void OnDisable() => GetComponent<TMP_Dropdown>().onValueChanged.RemoveAllListeners();
}
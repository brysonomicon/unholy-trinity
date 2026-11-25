using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class ResolutionDropdown : MonoBehaviour
{
    Vector2Int[] options;

    void OnEnable()
    {
        var dd = GetComponent<TMP_Dropdown>();
        options = GameSettings.AvailableResolutions();

        dd.ClearOptions();
        dd.AddOptions(options.Select(r => $"{r.x} x {r.y}").ToList());

        int current = System.Array.IndexOf(options, GameSettings.Resolution);
        dd.SetValueWithoutNotify(Mathf.Max(0, current));
        dd.onValueChanged.AddListener(i => GameSettings.Resolution = options[i]);
    }

    void OnDisable() => GetComponent<TMP_Dropdown>().onValueChanged.RemoveAllListeners();
}
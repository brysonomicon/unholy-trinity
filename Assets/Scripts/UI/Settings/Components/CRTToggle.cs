using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CRTToggle : MonoBehaviour
{
    void OnEnable()
    {
        var t = GetComponent<Toggle>();
        t.SetIsOnWithoutNotify(GameSettings.CRTEnabled);
        t.onValueChanged.AddListener(on => GameSettings.CRTEnabled = on);
    }

    void OnDisable() => GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
}
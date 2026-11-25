using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] VolumeChannel channel;

    void OnEnable()
    {
        var slider = GetComponent<Slider>();
        slider.SetValueWithoutNotify(GameSettings.GetVolume(channel));
        slider.onValueChanged.AddListener(v => GameSettings.SetVolume(channel, v));
    }

    void OnDisable() => GetComponent<Slider>().onValueChanged.RemoveAllListeners();
}

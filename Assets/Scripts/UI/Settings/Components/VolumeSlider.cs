using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] VolumeChannel channel;
    [SerializeField] TMP_Text valueLabel;   // optional, shows 0-100

    void OnEnable()
    {
        var slider = GetComponent<Slider>();
        float v = GameSettings.GetVolume(channel);
        slider.SetValueWithoutNotify(v);
        UpdateLabel(v);

        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnDisable() => GetComponent<Slider>().onValueChanged.RemoveListener(OnChanged);

    void OnChanged(float v)
    {
        GameSettings.SetVolume(channel, v);
        UpdateLabel(v);
    }

    void UpdateLabel(float v)
    {
        if (valueLabel) 
        {
            valueLabel.SetText("{0:0}", v * 100);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WeaponButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Color normalColor = Color.clear;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.4f, 0.4f);

    public void Setup(UnityAction onClick, Sprite icon, bool selected)
    {
        button.onClick.AddListener(onClick);
        button.image.color = selected
            ? selectedColor
            : normalColor;
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
    }
}

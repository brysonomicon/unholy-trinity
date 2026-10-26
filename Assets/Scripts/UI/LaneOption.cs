using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LaneOption : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image iconImage;
    [SerializeField][Range(0f, 1f)] private float disabledIconAlpha = 0.35f;

    public void Setup(string label, UnityAction onClick, bool clickable, Sprite icon)
    {
        labelText.text = label;
        button.onClick.AddListener(onClick);
        button.interactable = clickable;

        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.enabled = icon != null;

        iconImage.color = clickable 
            ? Color.white : 
            new Color(1f, 1f, 1f, disabledIconAlpha);

        labelText.verticalAlignment = icon != null
            ? VerticalAlignmentOptions.Bottom
            : VerticalAlignmentOptions.Middle;
    }
}

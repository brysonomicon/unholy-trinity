using System;
using TMPro;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SamGridCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private RectTransform aimLine;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private Color confirmedColor = new Color(0.15f, 0.15f, 0.15f);

    private static readonly float DegreesPerAimStep =
        360f / Enum.GetValues(typeof(AimDirection)).Length;

    public void Setup(UnityAction onClick, bool confirmed, bool current)
    {
        button.onClick.AddListener(onClick);
        button.interactable = confirmed;

        button.image.color = confirmed
            ? confirmedColor
            : current
                ? currentColor
                : emptyColor;
    }

    public void ShowIcon(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.gameObject.SetActive(sprite != null);
    }

    public void ShowAim(AimDirection aim)
    {
        aimLine.localEulerAngles = new Vector3(0f, 0f, (int)aim * DegreesPerAimStep);
        aimLine.parent.gameObject.SetActive(true);
    }

    public void ShowText(string text)
    {
        label.text = text;
        label.gameObject.SetActive(true);
    }
}
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SamGridCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 0.25f);
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private Color confirmedColor = Color.white;

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
}

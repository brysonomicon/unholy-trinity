using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LaneCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform optionRow;
    [SerializeField] private Button optionPrefab;

    public LaneCard Setup(string title)
    {
        titleText.text = title;
        return this;
    }

    public LaneCard AddOption(string label, UnityAction onClick, bool clickable = true)
    {
        Button option = Instantiate(optionPrefab, optionRow);
        option.GetComponentInChildren<TextMeshProUGUI>().text = label;
        option.onClick.AddListener(onClick);
        option.interactable = clickable;
        return this;
    }
}

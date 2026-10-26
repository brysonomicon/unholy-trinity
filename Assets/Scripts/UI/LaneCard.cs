using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LaneCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform optionRows;
    [SerializeField] private float optionSpacing = 10f;
    [SerializeField] private LaneOption optionPrefab;

    private Transform currentRow;

    public LaneCard Setup(string title)
    {
        titleText.text = title;
        return this;
    }

    public LaneCard AddRow()
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(optionRows, false);

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = optionSpacing;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        currentRow = row.transform;
        return this;
    }

    public LaneCard AddOption(
        string label, UnityAction onClick, bool clickable = true, Sprite icon = null, bool selected = false
    ){
        if (currentRow == null)
        {
            AddRow();
        }

        LaneOption option = Instantiate(optionPrefab, currentRow);
        option.Setup(label, onClick, clickable, icon, selected);
        return this;
    }

}

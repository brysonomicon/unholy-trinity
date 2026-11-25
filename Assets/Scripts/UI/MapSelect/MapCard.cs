using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// One tile in the map grid. Focusing it (click or kb/controller navigation) picks it;
/// Submit or double-click confirms it.
public class MapCard : Selectable, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] Image thumbnail;
    [SerializeField] TMP_Text label;
    [Tooltip("Shown while this card is the picked map (stays on when focus moves to Back/Select).")]
    [SerializeField] GameObject pickedIndicator;

    public MapCatalog.Entry Entry { get; private set; }

    Action<MapCard> onPicked;
    Action<MapCard> onConfirmed;

    public void Setup(MapCatalog.Entry entry, Action<MapCard> picked, Action<MapCard> confirmed)
    {
        Entry = entry;
        onPicked = picked;
        onConfirmed = confirmed;

        if (thumbnail)
        {
            thumbnail.sprite = entry.thumbnail;
            thumbnail.enabled = entry.thumbnail != null;
        }
        if (label)
        {
            label.text = entry.displayName;
        }
        SetPicked(false);
    }

    public void SetPicked(bool picked)
    {
        if (pickedIndicator)
        {
            pickedIndicator.SetActive(picked);
        }
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        onPicked?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left) 
        {
            return;
        }
        if (eventData.clickCount >= 2)
        {
            onConfirmed?.Invoke(this);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!IsInteractable()) 
        {
            return;
        }
        onConfirmed?.Invoke(this);
    }
}

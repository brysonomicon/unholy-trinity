using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Makes mouse hover move the keyboard/gamepad selection, so there's only ever
/// one "focused" control
[RequireComponent(typeof(Selectable))]
public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData _)
    {
        var s = GetComponent<Selectable>();
        if (s.IsInteractable()) s.Select();
    }
}

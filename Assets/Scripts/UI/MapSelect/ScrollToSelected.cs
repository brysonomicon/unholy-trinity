using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Scrolls a horizontal ScrollRect so the selected child stays inside the viewport.
/// ScrollRect doesn't do this itself, so kb/controller navigation would otherwise move focus
/// onto cards hidden behind the mask. Only reacts when the selection changes, so it never
/// fights the player dragging the scrollbar or using the mouse wheel.
[RequireComponent(typeof(ScrollRect))]
public class ScrollToSelected : MonoBehaviour
{
    [Tooltip("Gap kept between the selected item and the viewport edge.")]
    [SerializeField] float padding = 16f;
    [Tooltip("Higher = faster. 0 snaps instantly.")]
    [SerializeField] float smoothing = 12f;

    ScrollRect scroll;
    GameObject lastSelected;
    float? target;
    float lastWritten;

    void Awake()
    {
        scroll = GetComponent<ScrollRect>();
    }

    void OnEnable()
    {
        lastSelected = null;
        target = null;
    }

    void LateUpdate()
    {
        var es = EventSystem.current;
        var selected = es ? es.currentSelectedGameObject : null;

        if (selected != lastSelected)
        {
            lastSelected = selected;
            if (selected && selected.transform.IsChildOf(scroll.content))
            {
                target = ComputeTarget((RectTransform)selected.transform);
                if (target.HasValue)
                {
                    scroll.velocity = Vector2.zero;
                    if (smoothing <= 0f)
                    {
                        scroll.horizontalNormalizedPosition = target.Value;
                        target = null;
                        return;
                    }
                    lastWritten = scroll.horizontalNormalizedPosition;
                }
            }
        }

        if (!target.HasValue) 
        {
            return;
        }

        // Something else (drag, wheel, scrollbar) moved the content: let it win.
        float current = scroll.horizontalNormalizedPosition;
        if (!Mathf.Approximately(current, lastWritten))
        {
            target = null;
            return;
        }

        float next = Mathf.Lerp(current, target.Value, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        if (Mathf.Abs(next - target.Value) < 0.001f)
        {
            next = target.Value;
            target = null;
        }
        scroll.horizontalNormalizedPosition = next;
        lastWritten = scroll.horizontalNormalizedPosition;
    }

    /// Normalized position that brings <paramref name="item"/> fully into view, or null if it already is.
    float? ComputeTarget(RectTransform item)
    {
        var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
        Canvas.ForceUpdateCanvases();

        float overflow = scroll.content.rect.width - viewport.rect.width;
        if (overflow <= 0f) 
        {
            return null;
        }

        var corners = new Vector3[4];
        item.GetWorldCorners(corners);
        float left = viewport.InverseTransformPoint(corners[0]).x;
        float right = viewport.InverseTransformPoint(corners[2]).x;

        var view = viewport.rect;
        float shift; // how far the content needs to move right (+) or left (-), in viewport units
        if (left < view.xMin + padding)
        {
            shift = view.xMin + padding - left;
        }
        else if (right > view.xMax - padding)
        {
            shift = view.xMax - padding - right;
        }
        else
        {
            return null;
        }

        // Content moving left by d increases the normalized position by d / overflow.
        float contentScale = scroll.content.lossyScale.x / viewport.lossyScale.x;
        float delta = -shift / (overflow * contentScale);
        return Mathf.Clamp01(scroll.horizontalNormalizedPosition + delta);
    }
}

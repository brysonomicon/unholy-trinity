using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleHUD : MonoBehaviour
{
    public TurnManager turns;

    [Header("Panels")]

    [Header("Center")]
    public TMP_Text banner;
    public Button endTurnButton;

    [Header("Optional swoop toggle")]
    public CameraHandler cameraHandler;
    public Button swoopButton;

    void OnEnable() => turns.PhaseChanged += Refresh;
    void OnDisable() { if (turns != null) turns.PhaseChanged -= Refresh; }

    void Start()
    {
        endTurnButton.onClick.AddListener(turns.EndTurn);

        if (swoopButton)
        {
            bool enabled = cameraHandler && cameraHandler.allowSwoop;
            swoopButton.gameObject.SetActive(enabled);
            if (enabled) swoopButton.onClick.AddListener(cameraHandler.ToggleSwoop);
        }
        Refresh(turns.Phase);
    }

    void Refresh(TurnPhase phase)
    {
        var cur = turns.Current;
        endTurnButton.interactable = cur != null;
        if (swoopButton) swoopButton.interactable = cur != null;

        banner.text = cur != null
            ? $"ROUND {turns.Round}  -  {cur.name.ToUpperInvariant()}'S TURN"
            : "RESOLVING...";
    }
}
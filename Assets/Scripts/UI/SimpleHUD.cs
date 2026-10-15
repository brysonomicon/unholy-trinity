using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleHUD : MonoBehaviour
{
    private TurnManager _turns = TurnManager.Instance;

    [Header("Panels")] [Header("Center")] [SerializeField]
    private TMP_Text roundNumberText;

    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private Button endTurnButton;

    [Header("Optional swoop toggle")] 
    public CameraHandler cameraHandler;
    public Button swoopButton;

    private void OnEnable() => TurnManager.PhaseChangedEvent += Refresh;
    private void OnDisable() => TurnManager.PhaseChangedEvent -= Refresh;

    private void Start()
    {
        TurnManager.PhaseChangedEvent += OnPhaseChanged;
        GameManager.RoundChangedEvent += OnRoundChanged;
        endTurnButton.onClick.AddListener(_turns.EndTurn);

        if (swoopButton)
        {
            bool isEnabled = cameraHandler && cameraHandler.allowSwoop;
            swoopButton.gameObject.SetActive(isEnabled);
            if (isEnabled)
            {
                swoopButton.onClick.AddListener(cameraHandler.ToggleSwoop);
            }
        }

        Refresh(_turns.CurrentPhase);
        OnPhaseChanged(TurnManager.Instance.CurrentPhase);
    }

    private void OnRoundChanged(int roundNumber)
    {
        roundNumberText.text = "ROUND " + roundNumber;
    }

    private void OnPhaseChanged(TurnPhase newPhase)
    {
        phaseText.text =
            newPhase.ToString().PascalToString().ToUpperInvariant();
    }

    private void Refresh(TurnPhase phase)
    {
        bool canInteract = phase != TurnPhase.Resolution;
        endTurnButton.interactable = canInteract;
        if (swoopButton)
        {
            swoopButton.interactable = canInteract;
        }


        // banner.text = cur
        //     ? $"ROUND {GameManager.Round}  -  {cur.name.ToUpperInvariant()}'S TURN"
        //     : "RESOLVING...";
    }
}
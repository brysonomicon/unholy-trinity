using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleHUD : MonoBehaviour
{
    [SerializeField] private TurnManager turns;

    [Header("Panels")] [Header("Center")] [SerializeField]
    private TMP_Text roundNumberText;

    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private Button endTurnButton;

    [Header("Optional swoop toggle")] 
    public CameraHandler cameraHandler;
    public Button swoopButton;

    // private void OnEnable() => TurnManager.NewPhaseEvent += Refresh;
    // private void OnDisable() => TurnManager.NewPhaseEvent -= Refresh;

    private void Awake()
    {
        TurnManager.NewPhaseEvent += OnNewPhase;
        GameManager.NewRoundEvent += OnNewRound;
    }

    private void Start()
    {
        if (swoopButton)
        {
            bool isEnabled = cameraHandler && cameraHandler.allowSwoop;
            swoopButton.gameObject.SetActive(isEnabled);
            if (isEnabled)
            {
                swoopButton.onClick.AddListener(cameraHandler.ToggleSwoop);
            }
        }

        OnNewRound(GameManager.Instance.Round);
        OnNewPhase(TurnManager.Instance.CurrentPhase);
    }
    
    private void OnDestroy()
    {
        TurnManager.NewPhaseEvent -= OnNewPhase;
        GameManager.NewRoundEvent -= OnNewRound;       
    }

    private void OnNewRound(int roundNumber)
    {
        roundNumberText.text = "ROUND " + roundNumber;
    }

    private void OnNewPhase(TurnPhase newPhase)
    {
        bool isResolution = newPhase == TurnPhase.Resolution;
        endTurnButton.interactable = !isResolution;
        if (swoopButton != null)
        {
            swoopButton.interactable = !isResolution;
        }

        phaseText.text =
            newPhase.ToString().PascalToString().ToUpperInvariant();

        Debug.Log($"Phase: {newPhase}\nInteract: {!isResolution}");
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
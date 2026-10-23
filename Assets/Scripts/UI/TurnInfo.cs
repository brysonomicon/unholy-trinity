
using TMPro;
using UnityEngine;

public class TurnInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text roundNumberText;
    [SerializeField] private TMP_Text phaseText;

    private void Awake()
    {
        GameManager.NewRoundEvent += OnNewRound;
        TurnManager.NewPhaseEvent += OnNewPhase;
    }

    private void Start()
    {
        OnNewRound(GameManager.Instance.Round);
        OnNewPhase(TurnManager.Instance.CurrentPhase);
    }

    private void OnDestroy()
    {
        GameManager.NewRoundEvent -= OnNewRound;
        TurnManager.NewPhaseEvent -= OnNewPhase;
    }
    
    private void OnNewPhase(TurnPhase newPhase)
    {
        phaseText.text =
            newPhase.ToString().PascalToString().ToUpperInvariant();
    }

    private void OnNewRound(int roundNumber)
    {
        roundNumberText.text = "ROUND " + roundNumber;
    }

}


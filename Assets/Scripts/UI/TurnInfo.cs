using TMPro;
using UnholyTrinity.Turns;
using UnityEngine;

public class TurnInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text roundNumberText;
    [SerializeField] private TMP_Text phaseText;

    private void OnEnable()
    {
        TurnManager.NewPhaseEvent += OnNewPhase;
        TurnManager.NewRoundEvent += OnNewRound;
    }

    private void Start()
    {
        OnNewRound(TurnManager.Round);
        OnNewPhase(TurnManager.CurrentPhase);
    }

    private void OnDisable()
    {
        TurnManager.NewPhaseEvent -= OnNewPhase;
        TurnManager.NewRoundEvent -= OnNewRound;
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
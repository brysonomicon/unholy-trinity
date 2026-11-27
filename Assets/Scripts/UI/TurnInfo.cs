using TMPro;
using UnholyTrinity.Helpers;
using UnholyTrinity.Managers;
using UnityEngine;

public class TurnInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text roundNumberText;
    [SerializeField] private TMP_Text phaseText;

    private void Awake()
    {
        TurnManager.NewPhaseEvent += OnNewPhase;
        TurnManager.NewRoundEvent += OnNewRound;
    }

    private void Start()
    {
        OnNewRound(TurnManager.Round);
        OnNewPhase(TurnManager.CurrentPhase);
    }

    private void OnDestroy()
    {
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
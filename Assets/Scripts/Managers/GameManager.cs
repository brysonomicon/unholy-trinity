using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event Action<int> RoundChangedEvent;
    public int Round { get; private set; }
    
    [SerializeField] private float resolutionDuration = 4;

    private void Start()
    {
        TurnManager.PhaseChangedEvent += OnPhaseChange;
        Debug.Log($"Current phase: {TurnManager.Instance.CurrentPhase}");
    }

    private void OnPhaseChange(TurnPhase newPhase)
    {
        if (newPhase.Equals(TurnPhase.Resolution))
        {
            StartCoroutine(ResolutionRoutine());
        }
    }

    private IEnumerator ResolutionRoutine()
    {
        // TODO: actual resolution goes here.
        yield return new WaitForSeconds(resolutionDuration);
        Round++;
        Debug.Log($"New round: {Round}");
        RoundChangedEvent?.Invoke(Round);
    }
    
}

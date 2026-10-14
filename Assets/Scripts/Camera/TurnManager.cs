using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TurnPhase { PlayerOne, PlayerTwo, Resolution }

/// P1 plans -> P2 plans -> resolution (just a pause for now) -> repeat.
public class TurnManager : MonoBehaviour
{
    public GameObject playerOne;
    public GameObject playerTwo;
    public float resolutionDuration = 1f;

    public TurnPhase Phase { get; private set; } = TurnPhase.PlayerOne;
    public int Round { get; private set; } = 1;

    /// The player currently planning, or null during resolution.
    public GameObject Current =>
        Phase == TurnPhase.PlayerOne ? playerOne :
        Phase == TurnPhase.PlayerTwo ? playerTwo : null;

    public GameObject Other(GameObject c) => c == playerOne ? playerTwo : playerOne;

    public event Action<TurnPhase> PhaseChanged;
    
    void Start() => SetPhase(TurnPhase.PlayerOne);

    void OnEndTurn(InputAction.CallbackContext _) => EndTurn();
    public void EndTurn()
    {
        switch (Phase)
        {
            case TurnPhase.PlayerOne: SetPhase(TurnPhase.PlayerTwo); break;
            case TurnPhase.PlayerTwo: SetPhase(TurnPhase.Resolution); break;
            // ignored during resolution
        }
    }

    void SetPhase(TurnPhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
        if (phase == TurnPhase.Resolution) StartCoroutine(ResolutionRoutine());
    }

    IEnumerator ResolutionRoutine()
    {
        // TODO: actual resolution goes here.
        yield return new WaitForSeconds(resolutionDuration);
        Round++;
        SetPhase(TurnPhase.PlayerOne);
    }
}
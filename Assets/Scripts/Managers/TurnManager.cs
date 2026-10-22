using System;
using System.Collections.Generic;
using UnityEngine;


public enum TurnPhase
{
    PlayerOne,
    PlayerTwo,
    Resolution
}

/// P1 plans -> P2 plans -> resolution (just a pause for now) -> repeat.
[CreateAssetMenu(fileName = "NewTurnManager",
    menuName = "Managers/TurnManager")]
public class TurnManager : ScriptableObject
{
    public static TurnManager Instance { get; private set; }
    
    public static event Action<TurnPhase> NewPhaseEvent;

    [field: SerializeField][Min(0.1f)] public int TicksPerTurn { get; private set; } = 3;
    
    public TurnPhase CurrentPhase => _turnPhase.Peek();
    public bool IsPlayerTurn => CurrentPhase != TurnPhase.Resolution;

    private readonly Queue<TurnPhase> _turnPhase = new(new List<TurnPhase>
    {
        TurnPhase.PlayerOne,
        TurnPhase.PlayerTwo,
        TurnPhase.Resolution,
    });

    /// The player currently planning, or null during resolution.
    

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        GameManager.NewRoundEvent += OnRoundChanged;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        GameManager.NewRoundEvent -= OnRoundChanged;
    }

    public void EndTurn()
    {
        TurnPhase previous = _turnPhase.Dequeue();
        _turnPhase.Enqueue(previous);
        Debug.Log($"Going from {previous} to {CurrentPhase}");
        NewPhaseEvent?.Invoke(CurrentPhase);
    }
    
    private void OnRoundChanged(int _)
    {
        EndTurn();
    }


}
using System;
using System.Collections.Generic;
using System.Linq;
using UnholyTrinity.Players;
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

    private readonly Player[] _players = new Player[2];

    private readonly Queue<TurnPhase> _turnPhase = new(new List<TurnPhase>
    {
        TurnPhase.PlayerOne,
        TurnPhase.PlayerTwo,
        TurnPhase.Resolution,
    });

    public TurnPhase CurrentPhase => _turnPhase.Peek();

    /// The player currently planning, or null during resolution.
    public Player CurrentPlayer =>
        CurrentPhase switch
        {
            TurnPhase.PlayerOne => _players[0],
            TurnPhase.PlayerTwo => _players[1],
            TurnPhase.Resolution => _players[0],
            _ => throw new ArgumentOutOfRangeException()
        };

    public Player OtherPlayer =>
        CurrentPlayer == _players[0] ? _players[0] : _players[1];

    public static event Action<TurnPhase> PhaseChangedEvent;

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        GameManager.RoundChangedEvent += OnRoundChanged;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        GameManager.RoundChangedEvent -= OnRoundChanged;
    }

    public void EndTurn()
    {
        TurnPhase current = _turnPhase.Dequeue();
        _turnPhase.Enqueue(current);
        PhaseChangedEvent?.Invoke(CurrentPhase);
    }
    
    private void OnRoundChanged(int _)
    {
        EndTurn();
    }

    public TurnPhase RegisterPlayer(Player player)
    {
        if(_players.Contains(player))
        {
            throw new ArgumentException($"{player.gameObject.name} is already registered.");
        }
        int nextSpot = Array.IndexOf(_players, null);
        if (nextSpot == -1)
        {
            throw new ArgumentOutOfRangeException($"There are already 2 registered players.");
        }

        _players[nextSpot] = player;
        Debug.Log($"Registering {player.gameObject.name}");

        return nextSpot == 0 ? TurnPhase.PlayerOne : TurnPhase.PlayerTwo;
    }


}
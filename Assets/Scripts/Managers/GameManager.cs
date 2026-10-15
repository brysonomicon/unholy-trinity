using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnholyTrinity.Players;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameManager",
    menuName = "Managers/GameManager")]
public class GameManager : ScriptableObject
{
    public static GameManager Instance {get; private set;}
    public static event Action<int> NewRoundEvent;
    public static event Action<Player> NewTurnEvent;
    public int Round { get; private set; }
    
    [SerializeField]
    private int maxPlayers = 2;
    
    [SerializeField]
    private TurnManager turnManager;
        
    private readonly List<Player> _players = new();

    [CanBeNull]
    public Player CurrentPlayer =>
        turnManager.CurrentPhase switch
        {
            TurnPhase.PlayerOne => _players.First(),
            TurnPhase.PlayerTwo => _players.Last(),
            TurnPhase.Resolution => null,
            _ => throw new ArgumentOutOfRangeException()
        };

    public Player OtherPlayer =>
        turnManager.CurrentPhase switch
        {
            TurnPhase.PlayerOne => _players.Last(),
            TurnPhase.PlayerTwo => _players.First(),
            TurnPhase.Resolution => null,
            _ => throw new ArgumentOutOfRangeException()
        };

    public List<Vector3> CurrentPlayerPositions => new (_players.Select(x => x.transform.position).ToList());

    [SerializeField][Min(0)] private int resolutionDuration = 4;

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        Round = 1;
        TurnManager.NewPhaseEvent += OnPhaseChanged;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        TurnManager.NewPhaseEvent -= OnPhaseChanged;
    }

    public void RegisterPlayer(Player player)
    {
        if(_players.Contains(player))
        {
            throw new ArgumentException($"{player.gameObject.name} is already registered.");
        }
        if(_players.Count >= maxPlayers)
        {
            throw new ArgumentOutOfRangeException($"There are already {maxPlayers} registered players.");
        }

        _players.Add(player);
        Debug.Log($"Registering {player.gameObject.name}");
        if(_players.Count == maxPlayers)
        {
            NewTurnEvent?.Invoke(_players.First());
        }
    }

    private void OnPhaseChanged(TurnPhase newPhase)
    {
        if (newPhase == TurnPhase.Resolution)
        {
            // [CB] We can't do Coroutines within a SO, but this is a useful marker.
            ResolutionRoutine();
        }
        else
        {
            Debug.Log($"Invoking new phase in GM: {newPhase}");
            NewTurnEvent?.Invoke(newPhase == TurnPhase.PlayerOne ? _players.First() : _players.Last());
        }
    }

    private async void ResolutionRoutine()
    {
        //! [CB] Fun fact, there's a race condition if you don't wait long enough
        await Task.Delay(resolutionDuration * 1000);
        Round++;
        Debug.Log($"New round: {Round}");
        NewRoundEvent?.Invoke(Round);
    }
}
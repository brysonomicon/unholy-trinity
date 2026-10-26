using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnholyTrinity.Players;
using UnholyTrinity.Weapons;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameManager",
    menuName = "Managers/GameManager")]
public class GameManager : ScriptableObject
{
    #region Fields

    public static GameManager Instance {get; private set;}
    public static event Action<int> NewRoundEvent;
    public static event Action<Player> NewTurnEvent;
    
    [SerializeField]
    private int maxPlayers = 2;
    
    [SerializeField]
    private TurnManager turnManager;

    // second between ticks so resolution can be watched
    [SerializeField][Min(0.1f)] private float tickDuration = 1f;
        
    private readonly List<Player> _players = new();

    public int Round { get; private set; }
    // counts every tick of the game and only resets with ResetGame.
    // cooldowns are stored as the tick they expire on
    public int CurrentTick { get; private set; }

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

    #endregion

    #region Unity Lifecycle

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

    #endregion

    #region Setup

    public void ResetGame()
    {
        Round = 1;
        CurrentTick = 0;
        _players.Clear();
        turnManager.ResetPhases();
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
        //Start the game when we get enough players.
        if(_players.Count == maxPlayers)
        {
            NewTurnEvent?.Invoke(_players.First());
        }
    }

    #endregion

    #region Resolution

    private async void ResolutionRoutine()
    {
        for (int i = 0; i < turnManager.TicksPerTurn; i ++)
        {
            CurrentTick++;
            ResolveTick(i);
            await Task.Delay((int)(tickDuration * 1000));
        }
        //! [CB] Fun fact, there's a race condition if you don't wait long enough
        Round++;
        Debug.Log($"New round: {Round}");
        NewRoundEvent?.Invoke(Round);
    }

    // each player gets a shoot, aim, and move "lane" with one slot per tick
    // a phase runs for every player before the next phase starts. both players
    // actions in the same phase happen at the same time. tickIndex is the slot
    // within this turn's lanes
    private void ResolveTick(int tickIndex)
    {
        Debug.Log($"Resolving tick {CurrentTick}");
        // shot -> aim -> move -> LG damage so that LG can be applied according to 
        // opponent move path relative to shooter aim cone

        // shoot phase
        foreach (Player player in _players)
        {
            player.GetPlannedAction(tickIndex)?.Shoot?.Invoke();
        }

        // aim phase
        foreach (Player player in _players)
        {
            player.GetPlannedAction(tickIndex)?.Aim?.Invoke();
        }

        // move phase
        foreach (Player player in _players)
        {
            player.GetPlannedAction(tickIndex)?.Move?.Invoke();
        }
    }

    #endregion

    #region Combat

    public void ResolveShot(Player shooter, WeaponData weaponData, List<AimTile> aimTiles)
    {
        Player opponent = GetOpponent(shooter);

        // shoot the opponent if they are in the aim cone of the shooter
        foreach (AimTile at in aimTiles)
        {
            if (at.Coord != opponent.Coord)
            {
                continue;
            }

            int damageAmount = weaponData.GetDamage(at);

            if (damageAmount > 0)
            {
                Debug.Log($"{shooter.name} hit {opponent.name} with {weaponData.DisplayName} for {damageAmount} damage");
                opponent.TakeDamage(damageAmount);
            }
            else 
            {
                Debug.Log($"{shooter.name} missed");
            }
        }
    }

    private Player GetOpponent(Player shooter)
    {
        return _players.First(player => player != shooter);
    }

    #endregion

    #region Event Handlers

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
            // [CB] We could cache the above Task and check if it's done
            // If it isn't we create another Task and check in that thread
            // until it's done and *then* invoke this.
            NewTurnEvent?.Invoke(newPhase == TurnPhase.PlayerOne ? _players.First() : _players.Last());
        }
    }

    #endregion
}
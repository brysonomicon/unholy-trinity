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

    // second between ticks so resolution can be watched
    [SerializeField][Min(0.1f)] private float tickDuration = 1f;

    public int CurrentTick { get; private set; }

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
        //Start the game when we get enough players.
        if(_players.Count == maxPlayers)
        {
            NewTurnEvent?.Invoke(_players.First());
        }
    }

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

    private async void ResolutionRoutine()
    {
        for (int i = 0; i < turnManager.TicksPerTurn; i ++)
        {
            CurrentTick++;
            ResolveTick();
            await Task.Delay((int)(tickDuration * 1000));
        }
        //! [CB] Fun fact, there's a race condition if you don't wait long enough
        Round++;
        Debug.Log($"New round: {Round}");
        NewRoundEvent?.Invoke(Round);
    }

    private void ResolveTick()
    {
        Debug.Log($"Resolving tick {CurrentTick}");
        // shot -> aim -> move -> LG damage so that LG can be applied according to 
        // opponent move path relative to shooter aim cone
    }
}
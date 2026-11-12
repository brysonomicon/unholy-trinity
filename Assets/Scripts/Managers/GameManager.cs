using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NaughtyAttributes;
using Nrjwolf.Tools.AttachAttributes;
using UnholyTrinity.Players;
using UnholyTrinity.Weapons;
using UnholyTrinity.Hex;
using UnholyTrinity.Tiles;
using UnholyTrinity.Camera;
using UnholyTrinity.Helpers;
using UnityEngine;

namespace UnholyTrinity.Managers
{
    [RequireComponent(typeof(MovementManager))]
    public class GameManager : Singleton<GameManager>
    {
        #region Fields

        public static event Action<int> NewRoundEvent;
        public static event Action<Player> NewTurnEvent;

        [SerializeField] private int maxPlayers = 2;

        [SerializeField] [GetComponent] private MovementManager moveManager;

        [SerializeField] private TurnManager turnManager = TurnManager.Instance;

        // second between ticks so resolution can be watched
        private float TickDuration => turnManager.TickDuration;

        [MinValue(0.1f)] [MaxValue(nameof(TickDuration))]
        private float shootDuration;

        [MinValue(0.1f)] [MaxValue(nameof(TickDuration))]
        private float aimDuration;

        [SerializeField] [Min(0.1f)] private float turnStartDelay = 0.1f;

        private readonly List<Player> _players = new();

        // in registration order, so index 0 is player 1
        public IReadOnlyList<Player> Players => _players;

        [field: SerializeField]
        [CanBeNull]
        public PickupManager PickupManager { get; private set; }

        [CanBeNull]
        private PickupField LivePickups =>
            PickupManager != null ? PickupManager.Field : null;

        private readonly List<(Player shooter, Rocket rocket)> _rockets = new();

        private readonly List<(
            Player shooter,
            WeaponData Data,
            AimDirection firedAim,
            List<AimTile> firedTiles)> _beams = new();

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

        // false until the spawner has registered every player
        public bool AllPlayersRegistered => _players.Count == maxPlayers;

        public List<Vector3> CurrentPlayerPositions =>
            new(_players.Select(x => x.transform.position).ToList());

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            Round = 1;
            TurnManager.NewPhaseEvent += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            TurnManager.NewPhaseEvent -= OnPhaseChanged;
        }

        #endregion

        #region Setup

        public void ResetGame()
        {
            Round = 1;
            CurrentTick = 0;
            _players.Clear();
            _rockets.Clear();
            _beams.Clear();
            turnManager.ResetPhases();
        }

        public void RegisterPlayer(Player player)
        {
            if (_players.Contains(player))
            {
                throw new ArgumentException(
                    $"{player.gameObject.name} is already registered.");
            }

            if (_players.Count >= maxPlayers)
            {
                throw new ArgumentOutOfRangeException(
                    $"There are already {maxPlayers} registered players.");
            }

            _players.Add(player);
            CameraManager.Instance.RegisterPlayerTracked(player.transform, 1,
                1);
            //Start the game when we get enough players.
            if (_players.Count == maxPlayers)
            {
                NewTurnEvent?.Invoke(_players.First());
            }
        }

        #endregion

        #region Resolution

        private IEnumerator ResolutionRoutine()
        {
            for (int i = 0; i < turnManager.TicksPerTurn; i++)
            {
                CurrentTick++;
                yield return ResolveTick(i);
            }

            yield return (turnStartDelay);

            Round++;
            Debug.Log($"New round: {Round}");
            NewRoundEvent?.Invoke(Round);
        }

        // each player gets a shoot, aim, and move "lane" with one slot per tick
        // a phase runs for every player before the next phase starts. both players
        // actions in the same phase happen at the same time. tickIndex is the slot
        // within this turn's lanes
        private IEnumerator ResolveTick(int tickIndex)
        {
            // shot -> aim -> move -> LG damage so that LG can be applied according to 
            // opponent move path relative to shooter aim cone

            #region shoot phase

            foreach (Player player in _players)
            {
                player.GetPlannedAction(tickIndex)?.Shoot?.Invoke();
            }

            yield return new WaitForSeconds(shootDuration);

            #endregion

            #region aim phase

            foreach (Player player in _players)
            {
                player.GetPlannedAction(tickIndex)?.Aim?.Invoke();
            }

            yield return new WaitForSeconds(aimDuration);

            #endregion

            #region move phase

            // respawns come before anyone moves, so a pickup that comes back this tick can be
            // collected this tick. RespawnTicks = 1 means a pickup collected on tick N is
            // available again on tick N + 1
            PickupManager?.AdvanceTicks(1);

            HashSet<MoveData> movingObjects = new();

            // [CB] TODO: Make a common interface for movement.
            foreach (Player player in _players)
            {
                player.BeginMovePhase();
                IEnumerator<(int, HexCoord)> playerMove =
                    player.GetPlannedAction(tickIndex)?.Move.Invoke();
                if (playerMove == null) continue;
                movingObjects.Add(new MoveData(
                    WithTileEffects(player, playerMove), player.gameObject));
            }

            _rockets.ForEach(rocket =>
            {
                var rocketMover = new MoveData(
                    AdvanceRocket(rocket.shooter, rocket.rocket),
                    rocket.rocket.gameObject);
                movingObjects.Add(rocketMover);
            });

            if (movingObjects.Count > 0)
            {
                yield return moveManager.HandleTick(movingObjects.ToList());
            }

            // OnStop tile effects fire where each player ends up, including players who didn't move
            foreach (Player player in _players)
            {
                player.EndMovePhase();
                TileEffects.OnStop(player.Map, LivePickups, player.Coord,
                    player);
            }

            // LG needs both players end position, so it resolves after everyone moves
            ResolveBeams();

            #endregion

            // absorb ghosts after each turn
            foreach (Player player in _players)
            {
                player.PickUpGhost();
            }
        }

        // runs OnEnter tile effects on each hex the player enters, on the subtick they enter it, so
        // pickups vanish as the player reaches them. the first yield is where the player starts. ends
        // with 0 moves left when the move finishes, is blocked, or the player dies, so the movement
        // manager knows the player is done. a portal entrance moves the player on to its target
        private IEnumerator<(int, HexCoord)> WithTileEffects(Player player,
            IEnumerator<(int, HexCoord)> move)
        {
            if (!move.MoveNext())
            {
                yield break;
            }

            yield return move.Current;

            while (move.MoveNext())
            {
                (int remainingMoves, HexCoord entered) = move.Current;

                HexCoord endsOn = TileEffects.OnEnter(player.Map, LivePickups,
                    entered, player);

                if (player.IsDead)
                {
                    yield return (0, entered);
                    yield break;
                }

                if (endsOn != entered)
                {
                    player.MoveThroughPortal(endsOn);
                    entered = endsOn;
                }

                yield return (remainingMoves, entered);
            }

            // a blocked move stops without saying it has no moves left
            yield return (0, player.Coord);
        }

        #endregion

        #region Combat

        // damages every player standing on a hit tile. cones never include the shooter's tile
        // so players can't hit themselves with rail but can hit themselves with rocket splash
        public void ResolveShot(Player shooter, WeaponData weaponData,
            List<AimTile> aimTiles)
        {
            foreach (AimTile at in aimTiles)
            {
                foreach (Player player in _players)
                {
                    if (at.Coord != player.Coord)
                    {
                        continue;
                    }

                    int damageAmount = weaponData.GetDamage(at);

                    if (damageAmount > 0)
                    {
                        Debug.Log(
                            $"{shooter.name} hit {player.name} with {weaponData.DisplayName} for {damageAmount} damage");
                        player.TakeDamage(damageAmount);
                    }
                    else
                    {
                        Debug.Log($"{shooter.name} missed");
                    }
                }
            }
        }

        public void LaunchRocket(Player shooter, RocketLauncherData rocketData,
            MapManager mapManager, HexCoord target)
        {
            Rocket rocket = Instantiate(rocketData.RocketPrefab);
            rocket.Launch(rocketData, mapManager, shooter.Coord, target);
            _rockets.Add((shooter, rocket));
            Debug.Log($"{shooter.name} launched a rocket at {target}");
        }

        // moves all rockets on the map
        // private void AdvanceRockets()
        // {
        //     foreach ((Player shooter, Rocket rocket) in _rockets.ToList())
        //     {
        //         AdvanceRocket(shooter, rocket);
        //     }
        // }

        // advances a single rocket forward by the speed defined in the weapon data
        private IEnumerator<(int, HexCoord)> AdvanceRocket(Player shooter,
            Rocket rocket)
        {
            bool shouldDetonate = false;

            yield return (rocket.Data.Speed, rocket.Coord);

            for (int step = 0;
                 step < rocket.Data.Speed && !shouldDetonate;
                 step++)
            {
                rocket.StepForward();
                shouldDetonate = rocket.HasArrived || IsDirectHit(shooter, rocket);
                
                if (shouldDetonate)
                {
                    DetonateRocket(shooter, rocket);
                }

                yield return (shouldDetonate ? 0 : rocket.Data.Speed - (step + 1),
                    rocket.Coord);
            }

           
        }

        private bool IsDirectHit(Player shooter, Rocket rocket)
        {
            return _players.Any(player =>
                player != shooter &&
                player.Coord == rocket.Coord
            );
        }

        private void DetonateRocket(Player shooter, Rocket rocket)
        {
            Debug.Log($"{shooter.name}'s rocket exploded at {rocket.Coord}");
            ResolveShot(shooter, rocket.Data, rocket.GetBlastTiles());
            _rockets.Remove((shooter, rocket));
            Destroy(rocket.gameObject);
        }

        public void StartBeam(Player shooter, WeaponData weaponData,
            AimDirection firedAim, List<AimTile> firedTiles)
        {
            _beams.Add((shooter, weaponData, firedAim, firedTiles));
        }

        // LG deals damage based on "tracking" so it needs opponent path before it's calculated.
        // shoot: the beam is created from the shooters current tile and aim
        // aim: a rotation only affects later ticks, so the beam keeps firedAim for the whole tick
        // move: both players move and the beam moves with the shooter
        // damage is then based on the average of two samples:
        // start: target's start tile in the cone as fired, just like the railgun
        // end: target's end tile in firedAim's cone from the shooter's end tile
        // this way moving in lockstep deals full damage as expected.
        // TODO with momentum: the two samples work for one hex per tick, with more
        // tiles crossed mid-tick would get skipped and players go different distances, so
        // this will need to sample both paths by a fraction of the tick
        private void ResolveBeams()
        {
            foreach ((Player shooter, WeaponData weaponData,
                         AimDirection firedAim,
                         List<AimTile> firedTiles) in _beams)
            {
                List<AimTile> endTiles =
                    shooter.GetBeamTilesFromCurrentCoord(weaponData, firedAim);

                foreach (Player target in _players.Where(player =>
                             player != shooter))
                {
                    float startFalloff =
                        GetFalloff(target.MoveStartCoord, firedTiles);
                    float endFalloff = GetFalloff(target.Coord, endTiles);
                    float averageFalloff = (startFalloff + endFalloff) / 2f;
                    int damageAmount =
                        weaponData.GetDamage(new AimTile(target.Coord,
                            averageFalloff));

                    if (damageAmount > 0)
                    {
                        Debug.Log(
                            $"{shooter.name} hit {target.name} with {weaponData.DisplayName} for {damageAmount} damage");
                        target.TakeDamage(damageAmount);
                    }
                    else
                    {
                        Debug.Log($"{shooter.name} missed");
                    }
                }
            }

            _beams.Clear();
        }

        // tiles outside the cone count as 0, FirstOrDefault gives a default AimTile with 0 falloff
        private float GetFalloff(HexCoord coord, List<AimTile> aimTiles)
        {
            return aimTiles.FirstOrDefault(aimTile => aimTile.Coord == coord)
                .Falloff;
        }

        #endregion

        #region Event Handlers

        private void OnPhaseChanged(TurnPhase newPhase)
        {
            if (newPhase == TurnPhase.Resolution)
            {
                StartCoroutine(ResolutionRoutine());
            }
            else
            { 
                NewTurnEvent?.Invoke(newPhase == TurnPhase.PlayerOne
                    ? _players.First()
                    : _players.Last());
            }
        }

        #endregion
    }
}
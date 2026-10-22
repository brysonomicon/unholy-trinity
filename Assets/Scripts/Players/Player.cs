using System.Linq;
using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameManager gm;

        // unserialized, passed to player by spawner instead
        private MapManager mapManager;

        [Header("Map")]
        [field: SerializeField] public HexCoord Coord { get; private set; }
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Combat")]
        [SerializeField] private WeaponData startingWeapon;
        [SerializeField] private AimPreview aimPreview;

        // ticks counted for the whole game, so a cooldown can block ticks
        // subsequent turns
        private int nextFireTick;

        // the "shoot lane". one slot per tick of the turn. 0 index resolves
        // on the first tick of a turn, null slot holds fire for that tick
        private readonly List<Weapon> plannedShots = new();

        // copy of the nexFireTick that increments as shots are planned so
        // planning follows the same cooldown rules without changing the real cooldown
        private int plannedNextFireTick;

        [field: SerializeField] 
        public Weapon CurrentWeapon { get; private set; }

        [field: SerializeField]
        public AimDirection CurrentAimDirection { get; private set; }

        [Header("Health")]
        [SerializeField, Min(1)] private int startingHealth = 100;

        [field: SerializeField]
        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        // the game tick the next queued slot will resolve on. planning happens between
        // ticks, so the turns first tick is CurrentTick + 1
        private int NextPlannedTick => gm.CurrentTick + 1 + plannedShots.Count;

        // Unity Lifecycle

        private void Awake()
        {
            CurrentHealth = startingHealth;
            CurrentWeapon = new Weapon(startingWeapon);
            GameManager.NewTurnEvent += OnNewTurn;
        }

        private void OnDestroy()
        {
            GameManager.NewTurnEvent -= OnNewTurn;

            if (mapManager != null)
            {
                mapManager.LayoutChanged -= SnapToCoord;
            }
        }

        private void OnDrawGizmos()
        {
            if (mapManager == null ||
                mapManager.Map == null ||
                CurrentWeapon == null ||
                CurrentWeapon.Data == null)
            {
                return;
            }

            foreach (AimTile aimTile in CurrentWeapon.Data.GetAimTiles(
                         mapManager.Map, Coord, CurrentAimDirection))
            {
                Gizmos.color =
                    Color.Lerp(Color.yellow, Color.red, aimTile.Falloff);
                Gizmos.DrawSphere(
                    mapManager.Layout.HexToWorld(aimTile.Coord) +
                    Vector3.up * 0.6f, 0.2f);
            }
        }

        // Setup

        // called by the spawner 
        public void Initialize(MapManager mapManager, HexCoord spawnCoord)
        {
            this.mapManager = mapManager;
            Coord = spawnCoord;
            mapManager.LayoutChanged += SnapToCoord;
            SnapToCoord();
        }

        private void SnapToCoord()
        {
            transform.position = mapManager.Layout.HexToWorld(Coord) +
                                 Vector3.up * heightOffset;

            if (mapManager.Map != null &&
                !mapManager.Map.IsWalkable(Coord))
            {
                Debug.LogWarning($"{name} is on non walkable coord {Coord}");
            }
        }

        // Movement

        public void MovePlayer()
        {
            Debug.Log($"{name} is moving");
        }

        // Aim

        public void RotatePlayer(bool clockwise)
        {
            // [CB] TODO: Actually rotate the player.
            Debug.Log($"{name} was facing {CurrentAimDirection}");
            CurrentAimDirection = clockwise
                ? CurrentAimDirection.Next()
                : CurrentAimDirection.Previous();
            Debug.Log($"{name} is now facing {CurrentAimDirection}");

            RefreshAimPreview();
        }

        private void RefreshAimPreview()
        {
            List<AimTile> aimTiles = CurrentWeapon.Data.GetAimTiles(
                mapManager.Map, Coord, CurrentAimDirection);
            aimPreview.Show(aimTiles, mapManager.Layout);
        }

        // Planning

        // actions gets queued into lanes during the players turn and only take
        // effect during resolution. nothing in planning changes ammo, aim, or position

        // a shot can be queued if the lane has a free tick, there is ammo left
        // and the planned cooldown has expired by that tick
        public bool CanQueueShot(Weapon weapon)
        {
            return plannedShots.Count < TurnManager.Instance.TicksPerTurn &&
                    GetUnplannedAmmo(weapon) > 0 &&
                    NextPlannedTick >= plannedNextFireTick;
        }

        public void QueueShot(Weapon weapon)
        {
            if (!CanQueueShot(weapon))
            {
                return;
            }

            Debug.Log($"{name} queued {weapon.Data.DisplayName} on tick {NextPlannedTick}");
            plannedNextFireTick = NextPlannedTick + 1 + weapon.Data.Cooldown;
            plannedShots.Add(weapon);
        }

        public int GetUnplannedAmmo(Weapon weapon)
        {
            return weapon.Ammo - plannedShots.Count(plannedWeapon => plannedWeapon == weapon);
        }

        private void ClearPlannedShots()
        {
            plannedShots.Clear();
            plannedNextFireTick = nextFireTick;
        }

        // Resolution

        // planned actions get carried out one tick at a time. Fire() checks
        // cooldown and ammo against the real tick so planning shouldn't break
        // the rules

        // called by the game manager in the shoot phase of each tick
        public void ResolvePlannedShot(int tickIndex)
        {
            if (tickIndex < plannedShots.Count &&
                plannedShots[tickIndex] != null)
            {
                Fire(plannedShots[tickIndex]);
            }
        }

        private void Fire(Weapon weapon)
        {
            if (!CanFire(weapon))
            {
                return;
            }

            weapon.Fire();
            nextFireTick = gm.CurrentTick + 1 + weapon.Data.Cooldown;

            List<AimTile> aimTiles = weapon.Data.GetAimTiles(mapManager.Map, Coord, CurrentAimDirection);
            gm.ResolveShot(this, weapon.Data, aimTiles);
        }

        // weapon cooldowns are global, no weapon can be fired until the cooldown of the last
        // fired weapon has expired
        private bool CanFire(Weapon weapon)
        {
            return weapon.HasAmmo && gm.CurrentTick >= nextFireTick;
        }

        // Health

        public void TakeDamage(int damageAmount)
        {
             if (IsDead)
             {
                return;
             }

             CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0);
             Debug.Log($"{name} took {damageAmount} damage (ouch), {CurrentHealth} health remaining");

             if (IsDead)
             {
                Debug.Log($"{name} died. RIP");
             }
        }

        // Event Handlers

        private void OnNewTurn(Player obj)
        {
            // [CB] This might be entirely handled by the UI manager. IDK.
            if(obj == this)
            {
                // Do stuff to enable actions
                ClearPlannedShots();
                RefreshAimPreview();
            }
            else
            {
                // Do stuff to disable actions
                aimPreview.Hide();
            }
        }
    }
}
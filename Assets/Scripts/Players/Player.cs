using System.Linq;
using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class Player : MonoBehaviour
    {
        #region Fields

        [SerializeField] private GameManager gm;

        // unserialized, passed to player by spawner instead
        private MapManager mapManager;

        [Header("Map")]
        [field: SerializeField] public HexCoord Coord { get; private set; }
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Combat")]
        [SerializeField] private WeaponData startingWeapon;
        [SerializeField] private AimPreview aimPreview;
        // how many 30 degree steps a player can aim in one tick
        [SerializeField][Min(1)] private int aimStepsPerTick = 1;

        // ticks counted for the whole game, so a cooldown can block ticks
        // in subsequent turns
        private int nextFireTick;

        // this turn's plan, one UnholyAction per tick
        private readonly List<UnholyAction> plannedActions = new();

        [field: SerializeField] 
        public Weapon CurrentWeapon { get; private set; }

        [field: SerializeField]
        public AimDirection CurrentAimDirection { get; private set; }

        [Header("Health")]
        [SerializeField, Min(1)] private int startingHealth = 100;

        [field: SerializeField]
        public int CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        public int PlanningTickOfTurn => plannedActions.Count;

        public bool CanPlanNextTick => plannedActions.Count < TurnManager.Instance.TicksPerTurn;

        private UnholyAction PlanningAction => plannedActions[^1];

        // the game tick the action being planned will resolve on
        private int PlanningTick => gm.CurrentTick + plannedActions.Count;

        // aim from the end of the previous tick, which this tick starts from
        private AimDirection PreviousPlannedAim => plannedActions.Count > 1
            ? plannedActions[^2].PlannedAim
            : CurrentAimDirection;
        
        private static readonly int AimDirectionCount = System.Enum.GetValues(typeof(AimDirection)).Length;

        private int PlannedAimSteps
        {
            get
            {
                int steps = 
                    ((int)PlanningAction.PlannedAim - (int)PreviousPlannedAim + AimDirectionCount) % AimDirectionCount;
                
                return steps > AimDirectionCount / 2
                    ? steps - AimDirectionCount
                    : steps;
            }
        }
        
        public bool IsHoldingFire => PlanningAction.PlannedShot == null;
        public bool IsHoldingAim => PlanningAction.PlannedAim == PreviousPlannedAim;
        public bool IsHoldingMove => PlanningAction.PlannedMove == null;

        private bool HasTickChanges => !IsHoldingFire || !IsHoldingAim || !IsHoldingMove;

        #endregion

        #region Unity Lifecycle

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

        #endregion

        #region Setup

        // called by the spawner 
        public void Initialize(MapManager mapManager, HexCoord spawnCoord)
        {
            this.mapManager = mapManager;
            Coord = spawnCoord;
            mapManager.LayoutChanged += SnapToCoord;
            SnapToCoord();
            ClearPlan();
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

        #endregion

        #region Movement

        private HexCoord GetStepDestination(HexCoord from, HexDirection direction)
        {
            HexCoord next = from.Neighbor(direction);
            return mapManager.Map.IsWalkable(next)
                ? next
                : from;
        }

        private HexCoord GetPlannedCoord()
        {
            HexCoord plannedCoord = Coord;

            foreach (UnholyAction action in plannedActions)
            {
                if (action.PlannedMove != null)
                {
                    plannedCoord = GetStepDestination(plannedCoord, action.PlannedMove.Value);
                }
            }

            return plannedCoord;
        }

        #endregion

        #region Aim

        private void RefreshAimPreview()
        {
            List<AimTile> aimTiles = CurrentWeapon.Data.GetAimTiles(
                mapManager.Map, GetPlannedCoord(), PlanningAction.PlannedAim);
            aimPreview.Show(aimTiles, mapManager.Layout);
        }

        #endregion

        #region Planning

        // actions gets queued into lanes during the players turn and only take
        // effect during resolution. nothing in planning changes ammo, aim, or position

        // a shot can be queued if the lane has a free tick, there is ammo left
        // and the planned cooldown has expired by that tick
        public bool CanPlanShot(Weapon weapon)
        {
            return GetUnplannedAmmo(weapon) > 0 &&
                    PlanningTick >= GetPlannedNextFireTick();
        }

        public void PlanShot(Weapon weapon)
        {
            if (!CanPlanShot(weapon))
            {
                return;
            }

            Debug.Log($"{name} plans {weapon.Data.DisplayName} on tick {PlanningTick}");
            PlanningAction.PlannedShot = weapon;
            PlanningAction.Shoot = () => Fire(weapon);
        }

        // fills the next tick of the shoot lane without firing
        public void PlanHoldFire()
        {
            Debug.Log($"{name} holds fire on tick {PlanningTick}");
            PlanningAction.PlannedShot = null;
            PlanningAction.Shoot = null;
        }

        public bool CanPlanRotation(bool clockwise)
        {
            int stepsAfterRotation = PlannedAimSteps + (clockwise ? -1 : 1);
            return Mathf.Abs(stepsAfterRotation) <= aimStepsPerTick;
        }

        public void PlanRotation(bool clockwise)
        {
            if (!CanPlanRotation(clockwise))
            {
                return;
            }

            AimDirection plannedAim = PlanningAction.PlannedAim;
            SetPlannedAim(clockwise
                ? plannedAim.Previous()
                : plannedAim.Next());
        }

        public void PlanHoldAim()
        {
            SetPlannedAim(PreviousPlannedAim);
        }

        public bool IsPlanningShot(Weapon weapon)
        {
            return PlanningAction.PlannedShot == weapon;
        }

        public bool IsPlanningRotation(bool clockwise)
        {
            return clockwise
                ? PlannedAimSteps < 0
                : PlannedAimSteps > 0;
        }

        public void PlanMove(HexDirection direction)
        {
            Debug.Log($"{name} plans move {direction} on tick {PlanningTick}");
            PlanningAction.PlannedMove = direction;
            PlanningAction.Move = () => Step(direction);
            RefreshAimPreview();
        }

        public void PlanHoldMove()
        {
            Debug.Log($"{name} hold position on tick {PlanningTick}");
            PlanningAction.PlannedMove = null;
            PlanningAction.Move = null;
            RefreshAimPreview();
        }

        public bool IsPlanningMove(HexDirection direction)
        {
            return PlanningAction.PlannedMove == direction;
        }

        public void PlanNextTick()
        {
            if (!CanPlanNextTick)
            {
                return;
            }

            plannedActions.Add(new UnholyAction(PlanningAction.PlannedAim));
            Debug.Log($"{name} is planning tick {PlanningTick}");
        }

        public int GetUnplannedAmmo(Weapon weapon)
        {
            return weapon.Ammo - plannedActions.Count(action => action.PlannedShot == weapon);
        }

        // clears the tick being planned currently. if already clear, clears previous tick
        public void ClearTick()
        {
            if (!HasTickChanges && plannedActions.Count > 1)
            {
                plannedActions.RemoveAt(plannedActions.Count - 1);
            }

            plannedActions[^1] = new UnholyAction(PreviousPlannedAim);
            RefreshAimPreview();
        }

        // clears the entire plan, used for turn start
        private void ClearPlan()
        {
            plannedActions.Clear();
            plannedActions.Add(new UnholyAction(CurrentAimDirection));
            RefreshAimPreview();
        }

        private void SetPlannedAim(AimDirection aim)
        {
            Debug.Log($"{name} plans aim {aim} on tick {PlanningTick}");
            PlanningAction.PlannedAim = aim;
            PlanningAction.Aim = () => SetAim(aim);
            RefreshAimPreview();
        }

        private int GetPlannedNextFireTick()
        {
            int plannedNextFireTick = nextFireTick;

            for (int i = 0; i < plannedActions.Count - 1; i++)
            {
                Weapon plannedShot = plannedActions[i].PlannedShot;

                if (plannedShot != null)
                {
                    int shotTick = gm.CurrentTick + 1 + i;
                    plannedNextFireTick = shotTick + 1 + plannedShot.Data.Cooldown;
                }
            }

            return plannedNextFireTick;
        }

        #endregion

        #region Resolution

        // game manager runs each tick's callback phase by phase. Fire() checks
        // cooldown and ammo again the real tick so planning doesn't break rules

        // the plan for a tick of this turn, or null if the player didn't plan that far
        public UnholyAction GetPlannedAction(int tickIndex)
        {
            return tickIndex < plannedActions.Count
                ? plannedActions[tickIndex]
                : null;
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

        private void SetAim(AimDirection aim)
        {
            CurrentAimDirection = aim;
            Debug.Log($"{name} is now aiming {CurrentAimDirection}");
        }

        private void Step(HexDirection direction)
        {
            HexCoord destination = GetStepDestination(Coord, direction);

            if (destination == Coord)
            {
                Debug.Log($"{name} is blocked moving {direction}");
                return;
            }

            Coord = destination;
            SnapToCoord();
            Debug.Log($"{name} moved to {Coord}");
        }

        #endregion

        #region Health

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

        #endregion

        #region Event Handlers

        private void OnNewTurn(Player obj)
        {
            // [CB] This might be entirely handled by the UI manager. IDK.
            if(obj == this)
            {
                // Do stuff to enable actions
                ClearPlan();
            }
            else
            {
                // Do stuff to disable actions
                aimPreview.Hide();
            }
        }

        #endregion
    }
}
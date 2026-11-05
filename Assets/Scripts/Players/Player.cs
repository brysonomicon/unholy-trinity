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
        [SerializeField] private PlanGhosts planGhosts;
        [SerializeField] private AimPreview selectionPreview;
        [SerializeField] private AimPreview plannedBlastPreview;
        // how many 30 degree steps a player can aim in one tick
        [SerializeField][Min(1)] private int aimStepsPerTick = 1;

        // ticks counted for the whole game, so a cooldown can block ticks
        // in subsequent turns
        private int nextFireTick;

        // this turn's plan, one UnholyAction per tick
        private readonly List<UnholyAction> plannedActions = new();

        // the selected tile that rocket launcher fires at
        public HexCoord? SelectedTile { get; private set; }

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
        private AimDirection PreviousPlannedAim => GetPlannedAim(plannedActions.Count - 1);
        
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
        private bool HasTargetInShotCone => SelectedTile != null && IsInShotCone(SelectedTile.Value);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CurrentHealth = startingHealth;
            CurrentWeapon = new Weapon(startingWeapon);
            GameManager.NewTurnEvent += OnNewTurn;
            TurnManager.NewPhaseEvent += OnNewPhase;
        }

        private void OnDestroy()
        {
            GameManager.NewTurnEvent -= OnNewTurn;
            TurnManager.NewPhaseEvent -= OnNewPhase;

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
            Pose pose = GetPose(Coord, CurrentAimDirection);
            transform.SetPositionAndRotation(pose.position, pose.rotation);

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

        // where the player stands after the first tickCount planned ticks, 0 = real coord
        private HexCoord GetPlannedCoord(int tickCount)
        {
            HexCoord plannedCoord = Coord;

            for (int i = 0; i < tickCount; i++)
            {
                HexDirection? plannedMove = plannedActions[i].PlannedMove;

                if (plannedMove != null)
                {
                    plannedCoord = GetStepDestination(plannedCoord, plannedMove.Value);
                }
            }

            return plannedCoord;
        }


        #endregion

        #region Aim

        // aim after the first tickCount planned ticks, 0 = real aim
        private AimDirection GetPlannedAim(int tickCount)
        {
            return tickCount > 0
                ? plannedActions[tickCount - 1].PlannedAim
                : CurrentAimDirection;
        }

        private List<AimTile> GetPlannedShotTiles()
        {
            int shotTickCount = plannedActions.Count - 1;
            return CurrentWeapon.Data.GetAimTiles(
                mapManager.Map, GetPlannedCoord(shotTickCount), GetPlannedAim(shotTickCount)
            );
        }

        public bool IsInShotCone(HexCoord coord)
        {
            return GetPlannedShotTiles().Any(aimTile => aimTile.Coord == coord);
        }

        private Quaternion GetAimRotation(AimDirection aim)
        {
            Vector3 aimVector = mapManager.Layout.HexToWorld(aim.ToOffset()) -
                                mapManager.Layout.HexToWorld(HexCoord.Zero);
            
            return Quaternion.LookRotation(aimVector);
        }

        #endregion

        #region Plan Projection

        private void ProjectPlan()
        {
            int tickCount = plannedActions.Count;

            List<Pose> ghostPoses = new();

            for (int i = 0; i < tickCount; i++)
            {
                ghostPoses.Add(GetPlannedPose(i));
            }
            planGhosts.Show(ghostPoses);

            Pose plannedPose = GetPlannedPose(tickCount);
            transform.SetPositionAndRotation(plannedPose.position, plannedPose.rotation);

            aimPreview.Show(GetPlannedShotTiles(), mapManager.Layout);
            plannedBlastPreview.Show(GetPlannedBlastTiles(), mapManager.Layout);
        }

        private void StopProjecting()
        {
            planGhosts.Hide();
            aimPreview.Hide();
            plannedBlastPreview.Hide();
            ClearSelectedTile();
            SnapToCoord();
        }

        private void RevealPlan()
        {
            aimPreview.Hide();
            plannedBlastPreview.Hide();
            ClearSelectedTile();
            SnapToCoord();

            List<Pose> ghostPoses = new();

            for (int i = 1; i <= plannedActions.Count; i++)
            {
                ghostPoses.Add(GetPlannedPose(i));
            }

            planGhosts.Show(ghostPoses);
        }

        private Pose GetPose(HexCoord coord, AimDirection aim)
        {
            return new Pose(
                mapManager.Layout.HexToWorld(coord) + Vector3.up * heightOffset,
                GetAimRotation(aim)
            );
        }

        private Pose GetPlannedPose(int tickCount)
        {
            return GetPose(GetPlannedCoord(tickCount), GetPlannedAim(tickCount));
        }

        private List<AimTile> GetPlannedBlastTiles()
        {
            List<AimTile> blastTiles = new();

            foreach (UnholyAction action in plannedActions)
            {
                if (action.PlannedTarget != null &&
                    action.PlannedShot.Data is RocketLauncherData rocketData)
                {
                    blastTiles.AddRange(
                        rocketData.GetBlastTiles(mapManager.Map, action.PlannedTarget.Value)
                    );
                }
            }

            return blastTiles;
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
                    PlanningTick >= GetPlannedNextFireTick() &&
                    (!weapon.Data.RequiresTarget || HasTargetInShotCone);
        }

        public void PlanShot(Weapon weapon)
        {
            if (!CanPlanShot(weapon))
            {
                return;
            }

            // ensures the correct Fire() method is used when rocketlauncher equipped
            if (weapon.Data.RequiresTarget)
            {
                PlanShot(weapon, SelectedTile.Value);
                return;
            }

            Debug.Log($"{name} plans {weapon.Data.DisplayName} on tick {PlanningTick}");
            PlanningAction.PlannedShot = weapon;
            PlanningAction.PlannedTarget = null;
            PlanningAction.Shoot = () => Fire(weapon);
            ProjectPlan();
        }

        private void PlanShot(Weapon weapon, HexCoord target)
        {
            Debug.Log($"{name} plans {weapon.Data.DisplayName} at {target} on tick {PlanningTick}");
            PlanningAction.PlannedShot = weapon;
            PlanningAction.PlannedTarget = target;
            PlanningAction.Shoot = () => Fire(weapon, target);
            ProjectPlan();
        }

        // fills the next tick of the shoot lane without firing
        public void PlanHoldFire()
        {
            Debug.Log($"{name} holds fire on tick {PlanningTick}");
            PlanningAction.PlannedShot = null;
            PlanningAction.PlannedTarget = null;
            PlanningAction.Shoot = null;
            ProjectPlan();
        }

        public void SelectTile(HexCoord coord)
        {
            SelectedTile = coord;
            selectionPreview.Show(new List<AimTile> { new AimTile(coord, 1f) }, mapManager.Layout);
        }

        private void ClearSelectedTile()
        {
            SelectedTile = null;
            selectionPreview.Hide();
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
            ProjectPlan();
        }

        public void PlanHoldMove()
        {
            Debug.Log($"{name} hold position on tick {PlanningTick}");
            PlanningAction.PlannedMove = null;
            PlanningAction.Move = null;
            ProjectPlan();
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
            ProjectPlan();
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
            ProjectPlan();
        }

        // clears the entire plan, used for turn start
        private void ClearPlan()
        {
            plannedActions.Clear();
            plannedActions.Add(new UnholyAction(CurrentAimDirection));
            ProjectPlan();
        }

        private void SetPlannedAim(AimDirection aim)
        {
            Debug.Log($"{name} plans aim {aim} on tick {PlanningTick}");
            PlanningAction.PlannedAim = aim;
            PlanningAction.Aim = () => SetAim(aim);
            ProjectPlan();
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

        // called by game manager once this ticks move has resolved
        public void PickUpGhost()
        {
            planGhosts.PickUpNext();
        }

        // for rail and LG
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

        // for rocket launcher
        private void Fire(Weapon weapon, HexCoord target)
        {
            RocketLauncherData rocketData = (RocketLauncherData)weapon.Data;
            List<AimTile> aimTiles = rocketData.GetAimTiles(mapManager.Map, Coord, CurrentAimDirection);
            int targetIndex = aimTiles.FindIndex(aimTile => aimTile.Coord == target);
            
            if (targetIndex < 0)
            {
                Debug.Log($"{name} holds fire, {target} is no longer in the shot cone");
                return;
            }

            if (!CanFire(weapon))
            {
                return;
            }

            weapon.Fire();
            nextFireTick = gm.CurrentTick + 1 + weapon.Data.Cooldown;

            HexCoord landingTile = rocketData.GetLandingTile(aimTiles, aimTiles[targetIndex]);

            if (landingTile != target)
            {
                Debug.Log($"{name}'s rocket scattered from {target} to {landingTile}");
            }

            gm.LaunchRocket(this, (RocketLauncherData)weapon.Data, mapManager, target);
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
            SnapToCoord();
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
                StopProjecting();
            }
        }

        // player two gets no new turn event on turn end, so resolution
        // hides the plan as well
        private void OnNewPhase(TurnPhase newPhase)
        {
            if (newPhase == TurnPhase.Resolution)
            {
                RevealPlan();
            }
        }

        #endregion
    }
}
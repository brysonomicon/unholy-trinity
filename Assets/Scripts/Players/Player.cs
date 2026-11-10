using System.Linq;
using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Simulation;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Look")]
        [SerializeField, Tooltip("Tinted with the player's colour. Defaults to the renderer on this object.")]
        private Renderer bodyRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // momentum carried out of the last resolved tick
        public HexVelocity Velocity { get; private set; }

        // where this player stood before the last resolved move phase. beams read it for their start sample
        public HexCoord MoveStartCoord { get; private set; }

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

        // all the weapons the player holds
        [SerializeField] private List<Weapon> weapons = new();
        public IReadOnlyList<Weapon> Weapons => weapons;

        // the equipped weapon used for planning
        public Weapon CurrentWeapon { get; private set; }

        [field: SerializeField]
        public AimDirection CurrentAimDirection { get; private set; }

        [Header("Health")]
        [SerializeField, Min(1), FormerlySerializedAs("startingHealth")]
        private int maxHealth = 100;

        [field: SerializeField] public int CurrentHealth { get; private set; }

        public int MaxHealth => maxHealth;

        [Header("Armour")]
        [SerializeField]
        private int maxArmour = 100;

        [SerializeField]
        // The percentage of damage that is absorbed by the armour (out of ArmourProtection.FullPercent)
        private int armourDamageReduction = 50;

        private bool damageReductionRoundedUp = true;

        private ArmourProtection ArmourProtection => new(armourDamageReduction, damageReductionRoundedUp);

        [field: SerializeField]
        public int CurrentArmour { get; private set; }

        public int MaxArmour => maxArmour;

        public bool IsDead => CurrentHealth <= 0;

        public int PlanningTickOfTurn => plannedActions.Count;

        public bool CanPlanNextTick =>
            plannedActions.Count < TurnManager.Instance.TicksPerTurn;

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
                    ((int)PlanningAction.PlannedAim - (int)PreviousPlannedAim +
                     AimDirectionCount) % AimDirectionCount;

                return steps > AimDirectionCount / 2
                    ? steps - AimDirectionCount
                    : steps;
            }
        }

        public bool IsHoldingFire => PlanningAction.PlannedShot == null;

        public bool IsHoldingAim =>
            PlanningAction.PlannedAim == PreviousPlannedAim;

        public bool IsHoldingMove => PlanningAction.PlannedMove == null;

        private bool HasTickChanges =>
            !IsHoldingFire || !IsHoldingAim || !IsHoldingMove;

        private bool HasTargetInShotCone => SelectedTile != null && IsInShotCone(SelectedTile.Value);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentArmour = 0;
            CurrentWeapon = new Weapon(startingWeapon);
            weapons.Insert(0, CurrentWeapon);
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
            MoveStartCoord = spawnCoord;
            Velocity = HexVelocity.Zero;
            mapManager.LayoutChanged += SnapToCoord;
            SnapToCoord();
            ClearPlan();
        }

        // no longer just blue da-ba-dee, can be removed once we're using player models.
        public void SetColour(Color colour)
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<Renderer>();
            }

            MaterialPropertyBlock propertyBlock = new();
            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, colour);
            propertyBlock.SetColor(ColorId, colour);
            bodyRenderer.SetPropertyBlock(propertyBlock);
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

        // runs the whole plan through the same simulation resolution uses. entry i is the
        // player at the end of planned tick i + 1. empty until the map is known
        private List<PreviewTick> PreviewPlan()
        {
            List<MoveInput> plan = plannedActions.Select(action => action.MoveInput).ToList();
            return gm.PreviewPlan(this, plan);
        }

        // where the player stands after the first tickCount planned ticks, 0 = real coord
        private HexCoord GetPlannedCoord(List<PreviewTick> preview, int tickCount)
        {
            return tickCount > 0 && tickCount <= preview.Count
                ? preview[tickCount - 1].Unit.Coord
                : Coord;
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

        // the shot fires before this tick's aim and move, so it comes from where the previous tick left off
        private List<AimTile> GetPlannedShotTiles(List<PreviewTick> preview)
        {
            int shotTickCount = plannedActions.Count - 1;
            return CurrentWeapon.Data.GetAimTiles(
                mapManager.Map, GetPlannedCoord(preview, shotTickCount), GetPlannedAim(shotTickCount)
            );
        }

        // the cone of the shot planned on tickIndex, or null if that tick doesn't fire. it fires from
        // where the plan stands after tickIndex ticks, before that tick's aim and move
        private List<AimTile> GetPlannedShotCone(List<PreviewTick> preview, int tickIndex)
        {
            Weapon shot = tickIndex < plannedActions.Count
                ? plannedActions[tickIndex].PlannedShot
                : null;

            return shot?.Data.GetAimTiles(
                mapManager.Map, GetPlannedCoord(preview, tickIndex), GetPlannedAim(tickIndex)
            );
        }

        // the real cone, shown during resolution so it follows every aim change and move
        private void ShowLiveAim()
        {
            aimPreview.Show(
                CurrentWeapon.Data.GetAimTiles(mapManager.Map, Coord, CurrentAimDirection),
                mapManager.Layout
            );
        }

        public bool IsInShotCone(HexCoord coord)
        {
            return GetPlannedShotTiles(PreviewPlan()).Any(aimTile => aimTile.Coord == coord);
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
            List<PreviewTick> preview = PreviewPlan();
            int tickCount = plannedActions.Count;

            // ghost i stands where tick i + 1 starts, which is where that tick's shot fires from.
            // the tick being planned already has the main aim preview, so its ghost skips the cone
            List<PlanGhost> ghosts = new();

            for (int i = 0; i < tickCount; i++)
            {
                List<AimTile> cone = i < tickCount - 1 ? GetPlannedShotCone(preview, i) : null;
                ghosts.Add(new PlanGhost(GetPlannedPose(preview, i), cone));
            }
            planGhosts.Show(ghosts, mapManager.Layout, aimPreview);

            Pose plannedPose = GetPlannedPose(preview, tickCount);
            transform.SetPositionAndRotation(plannedPose.position, plannedPose.rotation);

            aimPreview.Show(GetPlannedShotTiles(preview), mapManager.Layout);
            plannedBlastPreview.Show(GetPlannedBlastTiles(), mapManager.Layout);
            gm.ShowPlannedPickups(preview);
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
            plannedBlastPreview.Hide();
            ClearSelectedTile();
            SnapToCoord();
            ShowLiveAim();
            gm.ShowLivePickups();

            // ghost i stands where the player ends tick i, which is where tick i + 1's shot fires
            // from. each is picked up as its tick resolves, taking its cone with it
            List<PreviewTick> preview = PreviewPlan();
            List<PlanGhost> ghosts = new();

            for (int i = 1; i <= plannedActions.Count; i++)
            {
                ghosts.Add(new PlanGhost(GetPlannedPose(preview, i), GetPlannedShotCone(preview, i)));
            }

            planGhosts.Show(ghosts, mapManager.Layout, aimPreview);
        }

        private Pose GetPose(HexCoord coord, AimDirection aim)
        {
            return new Pose(
                mapManager.Layout.HexToWorld(coord) + Vector3.up * heightOffset,
                GetAimRotation(aim)
            );
        }

        private Pose GetPlannedPose(List<PreviewTick> preview, int tickCount)
        {
            return GetPose(GetPlannedCoord(preview, tickCount), GetPlannedAim(tickCount));
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

        // only changes which weapon the shoot card fires and which weapons aim cone is whoen
        // selecting a weapon doesn't have an action cost. ammo/cooldown are checked when a shot
        // is planned
        public void SelectWeapon(Weapon weapon)
        {
            if (weapon == CurrentWeapon)
            {
                return;
            }

            Debug.Log($"{name} selects {weapon.Data.DisplayName}");
            CurrentWeapon = weapon;
            ClearSelectedTile();

            PlanHoldFire();
        }

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

        // null holds, which stops the player and zeroes momentum
        public void PlanMove(HexDirection? direction)
        {
            Debug.Log(direction is { } d
                ? $"{name} plans move {d} on tick {PlanningTick}"
                : $"{name} holds move on tick {PlanningTick}");
            PlanningAction.PlannedMove = direction;
            ProjectPlan();
        }

        // null asks whether this tick holds
        public bool IsPlanningMove(HexDirection? direction)
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
            return weapon.Ammo -
                   plannedActions.Count(action => action.PlannedShot == weapon);
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

        // the preview reads this tick's aim, so a planned move re-checks acceleration on its own
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
                    plannedNextFireTick =
                        shotTick + 1 + plannedShot.Data.Cooldown;
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
            if (weapon.Data.IsContinuous)
            {
                gm.StartBeam(this, weapon.Data, CurrentAimDirection, aimTiles);
            }
            else
            {
                gm.ResolveShot(this, weapon.Data, aimTiles);
            }
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

            gm.LaunchRocket(this, (RocketLauncherData)weapon.Data, mapManager, landingTile);
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
            ShowLiveAim();
            Debug.Log($"{name} is now aiming {CurrentAimDirection}");
        }

        // the fired aim's cone from where the player stands now, so a beam follows its shooter
        // only meaningful after the move phase
        public List<AimTile> GetBeamTilesFromCurrentCoord(WeaponData weaponData, AimDirection firedAim)
        {
            return weaponData.GetAimTiles(mapManager.Map, Coord, firedAim);
        }

        // snapshot for the simulator. id is how the game manager tells units apart
        public UnitState ToUnitState(int id)
        {
            return new UnitState(id, Coord, Velocity, CurrentHealth, maxHealth, MaxArmour, CurrentArmour, ArmourProtection);
        }

        // writes the result of a simulated move phase back to the player
        public void ApplyUnitState(UnitState unit)
        {
            MoveStartCoord = Coord;
            Velocity = unit.Velocity;

            if (unit.Coord != Coord)
            {
                Coord = unit.Coord;
                SnapToCoord();
                ShowLiveAim();
            }

            if (unit.Health != CurrentHealth || unit.Armour != CurrentArmour)
            {
                CurrentHealth = unit.Health;
                CurrentArmour = unit.Armour;
                Debug.Log($"{name} now at {CurrentHealth}/{maxHealth} health, {CurrentArmour} armour");
            }
        }

        #endregion

        #region Health

        // shots still resolve outside the simulator, so they borrow its damage maths
        // to keep armour working the same as it does for pools
        public void TakeDamage(int damageAmount, bool ignoresArmour = false)
        {
            if (IsDead)
            {
                return;
            }

            UnitState unit = ToUnitState(0);
            int healthLost = unit.TakeDamage(damageAmount, ignoresArmour);
            int absorbed = CurrentArmour - unit.Armour;

            CurrentHealth = unit.Health;
            CurrentArmour = unit.Armour;
            Debug.Log($"{name} took {healthLost} damage (ouch), {absorbed} absorbed by armour, {CurrentHealth} health remaining");

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
            if (obj == this)
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

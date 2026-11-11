using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnholyTrinity.Players
{
    public partial class Player : MonoBehaviour, IVitals, IWeaponCollector
    {
        #region Fields

        [SerializeField] private GameManager gm;

        // unserialized, passed to player by spawner instead
        private MapManager mapManager;

        public HexMap Map => mapManager.Map;

        [Header("Map")]
        [SerializeField] private float heightOffset = 0.5f;
        
        [field: SerializeField] public PlayerCameraRig CameraRig { get; private set; }

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

        // set up by BeginMovePhase each tick. beams read MoveStartCoord for their start sample,
        // tickMovePath will be needed for pickups
        private readonly List<HexCoord> tickMovePath = new();

        public HexCoord MoveStartCoord { get; private set; }

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

        public bool IsDead => CurrentHealth <= 0;

        [Header("Armour")]
        [SerializeField, Min(0)]
        private int maxArmour = 100;

        [Tooltip("Percent of each hit the armour absorbs, spending one armour per point absorbed.")]
        [SerializeField, Range(0, 100)]
        private int armourAbsorbPercent = 50;

        [Tooltip("Round the absorbed share up, so even small hits use armour first.")]
        [SerializeField]
        private bool roundAbsorbUp = true;

        [field: SerializeField] public int CurrentArmour { get; private set; }

        public int MaxArmour => maxArmour;

        public int MaxHealth => maxHealth;

        // raised whenever a player's health or armour changes, so UI can update
        public static event System.Action<Player> HealthChangedEvent;

        public IReadOnlyList<HexCoord> TickMovePath => tickMovePath;

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

        public bool IsHoldingMove => PlanningAction.PlannedMove.Count == 0;

        private bool HasTickChanges =>
            !IsHoldingFire || !IsHoldingAim || !IsHoldingMove;

        private bool HasTargetInShotCone => SelectedTile != null && IsInShotCone(SelectedTile.Value);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (CameraRig == null)
            {
                throw new MissingComponentException($"{name} has no CameraRig assigned.");
            }
            CameraRig.SetTarget(transform);
            
            CurrentHealth = maxHealth;
            CurrentArmour = 0;
            CurrentWeapon = new Weapon(startingWeapon);
            weapons.Insert(0, CurrentWeapon);
            GameManager.NewTurnEvent += OnNewTurn;
            TurnManager.NewPhaseEvent += OnNewPhase;

            gm ??= GameManager.Instance;
            
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
            momentum = HexCoord.Zero;
            mapManager.LayoutChanged += SnapToCoord;
            SnapToCoord(Coord);
            ClearPlan();
        }

        private void SnapToCoord() => SnapToCoord(Coord);

        private void SnapToCoord(HexCoord coord)
        {
            Pose pose = GetPose(coord, CurrentAimDirection);
            transform.SetPositionAndRotation(pose.position, pose.rotation);

            if (!mapManager.Map.IsWalkable(Coord))
            {
                Debug.LogWarning($"{name} is on non walkable coord {Coord}");
            }
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

        // the cone of the shot planned on tick tickIndex, fired from where the player starts that
        // tick. null when that tick doesn't fire
        private List<AimTile> GetPlannedShotCone(int tickIndex)
        {
            Weapon shot = tickIndex < plannedActions.Count
                ? plannedActions[tickIndex].PlannedShot
                : null;

            return shot?.Data.GetAimTiles(
                mapManager.Map, GetPlannedCoord(tickIndex), GetPlannedAim(tickIndex)
            );
        }

        // during resolution the aim preview follows where the player really is and aims
        private void ShowLiveAim()
        {
            aimPreview.Show(
                CurrentWeapon.Data.GetAimTiles(mapManager.Map, Coord, CurrentAimDirection),
                mapManager.Layout
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

            // ghost i stands where tick i + 1 starts, which is where that tick's shot fires from.
            // the tick being planned already has the main aim preview, so its ghost skips the cone
            List<PlanGhost> ghosts = new();

            for (int i = 0; i < tickCount; i++)
            {
                List<AimTile> cone = i < tickCount - 1 ? GetPlannedShotCone(i) : null;
                ghosts.Add(new PlanGhost(GetPlannedPose(i), cone));
            }
            planGhosts.Show(ghosts, mapManager.Layout, aimPreview);

            Pose plannedPose = GetPlannedPose(tickCount);
            transform.SetPositionAndRotation(plannedPose.position, plannedPose.rotation);

            aimPreview.Show(GetPlannedShotTiles(), mapManager.Layout);
            plannedBlastPreview.Show(GetPlannedBlastTiles(), mapManager.Layout);

            UpdateForecast();
        }

        private void StopProjecting()
        {
            planGhosts.Hide();
            aimPreview.Hide();
            plannedBlastPreview.Hide();
            ClearSelectedTile();
            SnapToCoord();
            ClearForecast();
        }

        // resolution plays out for real from here, so the forecast and planned pickups are cleared
        private void RevealPlan()
        {
            plannedBlastPreview.Hide();
            ClearSelectedTile();
            SnapToCoord();
            ClearForecast();
            ShowLiveAim();

            // ghost i stands where the player ends tick i, which is where tick i + 1's shot fires
            // from. each is picked up as its tick resolves, taking its cone with it
            List<PlanGhost> ghosts = new();

            for (int i = 1; i <= plannedActions.Count; i++)
            {
                ghosts.Add(new PlanGhost(GetPlannedPose(i), GetPlannedShotCone(i)));
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

        public bool IsPlanningMove(HexDirection? direction)
        {
            return PlanningAction.PlannedDirection == direction;
        }

        public void PlanNextTick()
        {
            if (!CanPlanNextTick)
            {
                return;
            }

            plannedActions.Add(CreateNewDefaultAction(PlanningAction.PlannedAim));
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

            plannedActions[^1] = CreateNewDefaultAction(PreviousPlannedAim);
            ProjectPlan();
        }

        // clears the entire plan, used for turn start
        private void ClearPlan()
        {
            plannedActions.Clear();
            plannedActions.Add(CreateNewDefaultAction(CurrentAimDirection));
            ProjectPlan();
        }

        private void SetPlannedAim(AimDirection aim)
        {
            Debug.Log($"{name} plans aim {aim} on tick {PlanningTick}");
            PlanningAction.PlannedAim = aim;
            PlanningAction.Aim = () => SetAim(aim);
            // aim resolves before move, so changing this tick's aim can change whether its move
            // accelerates. this recalculates the move already planned for the tick
            UpdatePlannedMove();
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
        [CanBeNull]
        public UnholyAction GetPlannedAction(int tickIndex)
        {
            return tickIndex < plannedActions.Count
                ? plannedActions[tickIndex]
                : null;
        }

        // called by game manager once this ticks move has resolved
        // also moves the live aim preview to where the player ended up
        public void PickUpGhost()
        {
            planGhosts.PickUpNext();
            ShowLiveAim();
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
        public void BeginMovePhase()
        {
            MoveStartCoord = Coord;
            tickMovePath.Clear();
        }

        // the fired aim's cone from where the player stands now, so a beam follows its shooter
        // only meaningful after the move phase
        public List<AimTile> GetBeamTilesFromCurrentCoord(WeaponData weaponData, AimDirection firedAim)
        {
            return weaponData.GetAimTiles(mapManager.Map, Coord, firedAim);
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
            tickMovePath.Add(Coord);
            SnapToCoord();
            Debug.Log($"{name} moved to {Coord}");
        }

        #endregion

        #region Health

        // armour absorbs its share of the hit first, then the rest comes off health. every damage
        // source (weapons, beams, harm tiles) comes through here, so they all go through armour
        public void TakeDamage(int damageAmount)
        {
            if (IsDead || damageAmount <= 0)
            {
                return;
            }

            int absorbed = GetArmourAbsorbed(damageAmount, CurrentArmour, armourAbsorbPercent, roundAbsorbUp);
            CurrentArmour -= absorbed;
            damageAmount -= absorbed;

            CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0);
            Debug.Log(
                $"{name} took {damageAmount} damage (ouch), {absorbed} absorbed by armour, " +
                $"{CurrentHealth} health and {CurrentArmour} armour remaining");

            if (IsDead)
            {
                Debug.Log($"{name} died. RIP");
            }

            HealthChangedEvent?.Invoke(this);
        }

        public bool TryHeal(int amount)
        {
            if (IsDead || amount <= 0 || CurrentHealth >= maxHealth)
            {
                Debug.Log($"{name} cannot be healed (dead: {IsDead}, amount: {amount}, current: {CurrentHealth}/{maxHealth})");
                return false;
            }

            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            Debug.Log($"{name} healed {amount} health, now at {CurrentHealth}/{maxHealth}");
            HealthChangedEvent?.Invoke(this);
            return true;
        }

        public bool TryAddArmour(int amount)
        {
            if (IsDead || amount <= 0 || CurrentArmour >= maxArmour)
            {
                Debug.Log($"{name} cannot gain armour (dead: {IsDead}, amount: {amount}, current: {CurrentArmour}/{maxArmour})");
                return false;
            }

            CurrentArmour = Mathf.Min(CurrentArmour + amount, maxArmour);
            Debug.Log($"{name} gained {amount} armour, now at {CurrentArmour}/{maxArmour}");
            HealthChangedEvent?.Invoke(this);
            return true;
        }

        // whole-number math so results don't drift. never absorbs more armour than there is.
        // static so the plan forecast absorbs damage exactly the same way
        private static int GetArmourAbsorbed(int damageAmount, int armour, int absorbPercent, bool roundUp)
        {
            if (armour <= 0)
            {
                return 0;
            }

            int rounding = roundUp ? 99 : 0;
            int share = (damageAmount * absorbPercent + rounding) / 100;
            return Mathf.Min(share, armour);
        }

        // kills outright, e.g. lava or a fall into the void
        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = 0;
            Debug.Log($"{name} died. RIP");
            HealthChangedEvent?.Invoke(this);
        }

        #endregion

        #region Weapon Pickups

        public bool TryGiveWeapon(WeaponData data)
        {
            if (IsDead)
            {
                return false;
            }

            Weapon heldWeapon = weapons.Find(weapon => weapon.Data == data);

            if (heldWeapon == null)
            {
                weapons.Add(new Weapon(data));
                Debug.Log($"{name} picked up {data.DisplayName}");
                return true;
            }

            // weapons are still picked up if ammo is full, weapon.cs clamps at full
            // the idea is that a weapon can be "denied" for a time by triggering its respawn
            heldWeapon.AddAmmo(data.PickupAmmo);
            Debug.Log($"{name} picked up {data.DisplayName} ammo, now at {heldWeapon.Ammo}/{data.MaxAmmo}");
            return true;
        }

        // ammo only goes into a weapon the player already holds. the ammo pickup decides whether
        // it's still taken when the weapon isn't held or is already full
        public AmmoGiven TryGiveAmmo(WeaponData data, int amount)
        {
            if (IsDead || amount <= 0)
            {
                return AmmoGiven.Refused;
            }

            Weapon heldWeapon = weapons.Find(weapon => weapon.Data == data);

            if (heldWeapon == null)
            {
                Debug.Log($"{name} can't use {data.DisplayName} ammo without the weapon");
                return AmmoGiven.WeaponNotHeld;
            }

            if (heldWeapon.Ammo >= data.MaxAmmo)
            {
                Debug.Log($"{name} can't take {data.DisplayName} ammo, already full at {heldWeapon.Ammo}/{data.MaxAmmo}");
                return AmmoGiven.AmmoFull;
            }

            heldWeapon.AddAmmo(amount);
            Debug.Log($"{name} picked up {amount} {data.DisplayName} ammo, now at {heldWeapon.Ammo}/{data.MaxAmmo}");
            return AmmoGiven.Added;
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

        // the lambda lets each ResolveMove remember which PlannedAction it's associated with. the
        // previous version was always pointing at the same PlannedAction.PlannedAim for each tick
        private UnholyAction CreateNewDefaultAction(AimDirection baseDirection)
        {
            UnholyAction action = new(baseDirection);
            action.Move = () => ResolveMove(action);
            return action;
        }
    }
}
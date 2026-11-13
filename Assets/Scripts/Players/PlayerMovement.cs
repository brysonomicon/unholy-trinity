using System.Collections.Generic;
using NaughtyAttributes;
using UnholyTrinity.Helpers;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public partial class Player
    {
        [Header("Movement")] [SerializeField, Min(1)]
        private int baseMomentumMax = 1;

        [SerializeField, MinValue("baseMomentumMax")]
        private int aimMomentumMax;

        [SerializeField] [Min(1)] private int momentumGainPerTick = 1;

        [field: SerializeField]
        [Tooltip("Used to display where the player will be during the turn.")]
        public HexCoord Coord { get; private set; }

        //? [CB] Should this be public so the wall get interact with it?
        [SerializeField] private HexCoord momentum = new(0, 0);

        // momentum from the end of the previous planned tick from the planning model
        // lastMomentum only updated once per turn and didn't work for calculating momentum
        // between ticks
        private HexCoord PreviousPlannedMomentum =>
            GetPlannedMomentum(plannedActions.Count - 1);
        // [SerializeField] private HexCoord lastMomentum = new(0, 0);

        #region Planning

        // where the player stands after the first tickCount planned ticks, 0 = real coord
        private HexCoord GetPlannedCoord(int tickCount)
        {
            HexCoord plannedCoord = Coord;

            for (int i = 0; i < tickCount; i++)
            {
                plannedCoord = GetMoveEnd(plannedCoord,
                    plannedActions[i].PlannedMove);
            }

            return plannedCoord;
        }

        // a tick can hold a bunch of steps instead of one tile per tick, so this works out where
        // they end without touching the real state. it stops at the first blocked hex so the plan
        // shows the same result ResolveMove will execute at resolution
        private HexCoord GetMoveEnd(HexCoord start, List<HexDirection> steps)
        {
            HexCoord current = start;
            bool blocked = false;

            for (int i = 0; i < steps.Count && !blocked; i++)
            {
                HexCoord next = GetStepDestination(current, steps[i]);
                blocked = next == current;
                current = blocked ? next : mapManager.Map.ThroughPortal(next);
            }

            return current;
        }

        // only records the choice on this ticks action, ProjectPlan handles showing it,
        // so planning no longer runs ResolveMove or change Coord which is real state that
        // planning shouldn't touch
        public void PlanMove(HexDirection? direction)
        {
            if (PlanningStep != SAMType.Move)
            {
                return;
            }
            
            PlanningAction.PlannedDirection = direction;
            UpdatePlannedMove();
            ProjectPlan();
        }

        private void UpdatePlannedMove()
        {
            HexDirection? direction = PlanningAction.PlannedDirection;

            //Don't change momentum if holding
            HexCoord plannedMomentum = direction.HasValue
                ? UpdatePlannedMomentum(direction.Value)
                : HexCoord.Zero;

            HexCoord start = GetPlannedCoord(plannedActions.Count - 1);
            List<HexDirection> steps = GetStepListFromMomentum(start, plannedMomentum);

            // a portal on the way turns the rest of the move and the momentum. it's baked into the
            // plan here so the preview, the next tick's planning and resolution all agree
            mapManager.Map.RedirectThroughPortals(start, steps, out int portalRotation);

            PlanningAction.PlannedMomentum =
                plannedMomentum.RotateAround(HexCoord.Zero, portalRotation);
            PlanningAction.PlannedMove = steps;
        }

        private HexCoord UpdatePlannedMomentum(HexDirection direction)
        {
            int previousSpeed =
                HexCoord.Distance(HexCoord.Zero, PreviousPlannedMomentum);
            bool isMoving = previousSpeed > 0;

            // reads this ticks planned aim because aim resolves before move. only even aims line
            // up exactly with a hex direction, so a diagonal aim never count as looking that way
            bool isAimingAlongMove =
                (int)PlanningAction.PlannedAim == (int)direction * 2;
            bool isSameDirection = isMoving &&
                                   PreviousPlannedMomentum.ToDirection() ==
                                   direction;
            bool isReversing = isMoving &&
                               PreviousPlannedMomentum.ToDirection()
                                   .Opposite() == direction;

            int speed = baseMomentumMax;

            if (isMoving && isAimingAlongMove && !isReversing)
            {
                speed = Mathf.Min(previousSpeed + momentumGainPerTick,
                    aimMomentumMax);
            }
            else if (isSameDirection)
            {
                speed = previousSpeed;
            }

            return direction.ToOffset() * speed;
        }

        // momentum after the first tickCount planned ticks, 0 = real momentum
        // same thing as GetPlannedAim, so each tick builds on the one before
        private HexCoord GetPlannedMomentum(int tickCount)
        {
            return tickCount > 0
                ? plannedActions[tickCount - 1].PlannedMomentum
                : momentum;
        }

        // takes its start and momentum as inputs so each tick can be planned from the
        // planning model, instead of reading real state of Coord and momentum
        private static List<HexDirection> GetStepListFromMomentum(
            HexCoord start, HexCoord plannedMomentum)
        {
            var target = start + plannedMomentum;

            List<HexDirection> directions = new List<HexDirection>();

            if (target == start)
            {
                return directions;
            }

            List<HexCoord> stepList = start.LineTo(target);

            for (int i = 0; i < stepList.Count - 1; i++)
            {
                HexDirection next = stepList[i].DirectionTo(stepList[i + 1]);
                directions.Add(next);
            }

            return directions;
        }

        #endregion

        #region Resolution

        // takes the tick's own action because PlanningAction is always the last planned tick by 
        // the time resolution happens. this is now the only place where Coord and momentum change
        // and it records each hex entered in tickMovePath, which replaces what Step was doing on
        // aiming_weapons
        private IEnumerator<(int, HexCoord)> ResolveMove(UnholyAction action)
        {
            var moveSteps = action.PlannedMove;
            HexCoord current = Coord;
            // the planned momentum from the planning model becomes the real momentum now
            // that resolution is happening
            momentum = action.PlannedMomentum;
            //First, tell the system how many steps we plan to take, and where we start
            // This lets the system know our intent.
            yield return (moveSteps.Count, current);

            //Then iterate through the steps
            for (int i = 0; i < moveSteps.Count; i++)
            {
                var direction = moveSteps[i];
                //If, for some reason, we can't move any more,
                //break out and exit the coroutine.
                if (!TryStep(direction, out current))
                {
                    break;
                }
                
                Coord = current;
                tickMovePath.Add(Coord);
                SnapToCoord(Coord);
                //Otherwise, tell the system how many more steps there are,
                //and where we currently expect to be.
                yield return (moveSteps.Count - i - 1, current);
            }
        }

        private bool TryStep(HexDirection direction, out HexCoord next)
        {
            next = GetStepDestination(Coord, direction);

            if (next == Coord)
            {
                Debug.Log($"{name} is blocked from moving {direction}");
                return false;
            }
            
            return true;
        }

        private HexCoord GetStepDestination(HexCoord from,
            HexDirection direction)
        {
            HexCoord next = from.Neighbor(direction);
            return mapManager.Map.IsWalkable(next)
                ? next
                : from;
        }

        #endregion
    }
}
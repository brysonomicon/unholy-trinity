using System.Collections.Generic;
using NaughtyAttributes;
using UnholyTrinity.Hex;
using UnholyTrinity.Hex.Interfaces;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public partial class Player : IMovable
    {
        public int MoveIndex { get; set; }

        public HexTile Tile { get; set; }

        public List<HexDirection> MoveSteps { get; private set; } = new();

        [Header("Movement")] [SerializeField, Min(1)]
        private int baseMomentumMax = 1;

        [SerializeField, MinValue("baseMomentumMax")]
        private int aimMomentumMax;

        [field: SerializeField] [Min(1)] public int MomentumGainPerTick { get; private set; }= 1;

        [field: SerializeField]
        [Tooltip("Used to display where the player will be during the turn.")]
        public HexCoord Coord { get; private set; }

        //? [CB] Should this be public so the wall get interact with it?
        [field: SerializeField] public HexCoord Momentum { get; private set; }

        // momentum from the end of the previous planned tick from the planning model
        // lastMomentum only updated once per turn and didn't work for calculating momentum
        // between ticks
        private HexCoord PreviousPlannedMomentum =>
            GetPlannedMomentum(_plannedActions.Count - 1);

        #region Planning

        // where the player stands after the first tickCount planned ticks, 0 = real coord
        private HexCoord GetPlannedCoord(int tickCount)
        {
            HexCoord plannedCoord = Coord;

            for (int i = 0; i < tickCount; i++)
            {
                plannedCoord = GetMoveEnd(plannedCoord,
                    _plannedActions[i].PlannedMove);
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
                current = blocked ? next : _mapManager.Map.ThroughPortal(next);
            }

            return current;
        }

        // only records the choice on this ticks action, ProjectPlan handles showing it,
        // so planning no longer runs ResolveMove or change Coord which is real state that
        // planning shouldn't touch
        public void PlanMove(HexDirection? direction)
        {
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

            HexCoord start = GetPlannedCoord(_plannedActions.Count - 1);
            List<HexDirection> steps =
                GetStepListFromMomentum(start, plannedMomentum);

            // a portal on the way turns the rest of the move and the momentum. it's baked into the
            // plan here so the preview, the next tick's planning and resolution all agree
            _mapManager.Map.RedirectThroughPortals(start, steps,
                out int portalRotation);

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
                speed = Mathf.Min(previousSpeed + MomentumGainPerTick,
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
                ? _plannedActions[tickCount - 1].PlannedMomentum
                : Momentum;
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

        private HexCoord GetStepDestination(HexCoord from,
            HexDirection direction)
        {
            HexCoord next = from.Neighbor(direction);
            return _mapManager.Map.IsWalkable(next)
                ? next
                : from;
        }

        public void Move()
        {
            HexMap map = MapManager.Instance.Map;
            map.Step(Coord, MoveSteps[MoveIndex], this);
            _tickMovePath.Add(Coord);
            LerpToCoord(Coord);
            MoveIndex++;
        }

        #endregion
    }
}

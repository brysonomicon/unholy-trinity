using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;


namespace UnholyTrinity.Players
{
    public partial class Player
    {
        
        [Header("Movement")] 
        [SerializeField, Min(1)]
        private int baseMomentumMax = 1;

        [SerializeField, MinValue("baseMomentumMax")]
        private int aimMomentumMax;

        [SerializeField][Min(1)]
        private int momentumGainPerTick = 1;

        [field: SerializeField]
        [Tooltip("Used to display where the player will be during the turn.")]
        public HexCoord Coord { get; private set; }

        //? [CB] Should this be public so the wall get interact with it?
        [SerializeField] private HexCoord momentum = new(0, 0);

        // momentum from the end of the previous planned tick from the planning model
        // lastMomentum only updated once per turn and didn't work for calculating momentum
        // between ticks
        private HexCoord PreviousPlannedMomentum => GetPlannedMomentum(plannedActions.Count - 1);
        // [SerializeField] private HexCoord lastMomentum = new(0, 0);

        #region Planning

        // where the player stands after the first tickCount planned ticks, 0 = real coord
        private HexCoord GetPlannedCoord(int tickCount)
        {
            HexCoord plannedCoord = Coord;

            for (int i = 0; i < tickCount; i++)
            {
                plannedCoord = GetMoveEnd(plannedCoord, plannedActions[i].PlannedMove);
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
                current = next;
            }

            return current;
        }

        // public void PlanMove(HexDirection? direction)
        // {
        //     //We're changing plans; so clear the current one.
        //     PlanningAction.PlannedMove.Clear();
        //     Coord = lastCoord;
        //     //Don't change momentum if holding
        //     momentum = direction.HasValue
        //         ? UpdatePlannedMomentum(direction.Value)
        //         : PreviousPlannedMomentum;

        //     //Get a list of all steps
        //     PlanningAction.PlannedMove = GetStepListFromMomentum();
        //     // Updated expected location
        //     var next = ResolveMove();
        //     while (next.MoveNext())
        //     {
        //         Coord = next.Current.Item2;
        //     }
            
        //     Debug.Log(string.Join(", ", PlanningAction.PlannedMove));

        //     RefreshAimPreview();
        // }

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
            
            PlanningAction.PlannedMomentum = plannedMomentum;

            PlanningAction.PlannedMove = GetStepListFromMomentum(
                GetPlannedCoord(plannedActions.Count - 1), plannedMomentum
            );

            Debug.Log(string.Join(", ", PlanningAction.PlannedMove));
        }

        private HexCoord UpdatePlannedMomentum(HexDirection direction)
        {
            int previousSpeed = HexCoord.Distance(HexCoord.Zero, PreviousPlannedMomentum);
            bool isMoving = previousSpeed > 0;

            // reads this ticks planned aim because aim resolves before move. only even aims line
            // up exactly with a hex direction, so a diagonal aim never count as looking that way
            bool isAimingAlongMove = (int)PlanningAction.PlannedAim == (int)direction * 2;
            bool isSameDirection = isMoving && PreviousPlannedMomentum.ToDirection() == direction;
            bool isReversing  = isMoving && PreviousPlannedMomentum.ToDirection().Opposite() == direction;

            int speed = baseMomentumMax;

            if (isMoving && isAimingAlongMove && !isReversing)
            {
                speed = Mathf.Min(previousSpeed + momentumGainPerTick, aimMomentumMax);
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

        private bool IsFasterThan(HexDirection direction, int maxSpeed)
        {
            HexCoord maxVal = direction.ToOffset() * maxSpeed;
            return direction switch
            {
                HexDirection.D0 => maxVal.Q <= PreviousPlannedMomentum.Q,
                HexDirection.D1 => maxVal.Q <= PreviousPlannedMomentum.Q &&
                                   maxVal.R >= PreviousPlannedMomentum.R,
                // > R because R is negative in this direction, so we're
                // faster if we're LESS than R
                HexDirection.D2 => maxVal.R >= PreviousPlannedMomentum.R,
                HexDirection.D3 => maxVal.Q >= PreviousPlannedMomentum.Q,
                HexDirection.D4 => maxVal.Q >= PreviousPlannedMomentum.Q &&
                                   maxVal.R <= PreviousPlannedMomentum.R,
                HexDirection.D5 => maxVal.R <= PreviousPlannedMomentum.R,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction), direction, null)
            };
        }

        // takes its start and momentum as inputs so each tick can be planned from the
        // planning model, instead of reading real state of Coord and momentum
        private List<HexDirection> GetStepListFromMomentum(HexCoord start, HexCoord plannedMomentum)
        {
            var target = start + plannedMomentum;
            Debug.Log(
                $"{gameObject.name} wants to move from {start} to {plannedMomentum}");

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
                
                Debug.Log($"Moving from {Coord} to {current}");
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
            
            
            Debug.Log($"{name} moved to {Coord}");
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
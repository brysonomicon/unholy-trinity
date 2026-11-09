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

        [field: SerializeField]
        [Tooltip("Used to display where the player will be during the turn.")]
        public HexCoord Coord { get; private set; }
        
        [SerializeField]
        [Tooltip("The coordinate the player had at the beginning of the turn.")]
        private HexCoord lastCoord;

        //? [CB] Should this be public so the wall get interact with it?
        [SerializeField] private HexCoord momentum = new(0, 0);
        [SerializeField] private HexCoord lastMomentum = new(0, 0);

        private List<HexDirection> GetStepListFromMomentum()
        {
            var target = lastCoord + momentum;
            Debug.Log(
                $"{gameObject.name} wants to move from {Coord} to {target}");
            List<HexDirection> directions = new List<HexDirection>();
            if (target == Coord)
            {
                return directions;
            }

            List<HexCoord> stepList = Coord.LineTo(target);

            for (int i = 0; i < stepList.Count - 1; i++)
            {
                HexDirection next = stepList[i].DirectionTo(stepList[i + 1]);
                directions.Add(next);
            }

            return directions;
        }

        private HexCoord GetPlannedCoord(in List<HexDirection> steps) =>
            steps.Count == 0
                ? lastCoord
                : GetStepListFromMomentum().Last().ToOffset();

        #region Planning

        public void PlanMove(HexDirection? direction)
        {
            //We're changing plans; so clear the current one.
            PlanningAction.PlannedMove.Clear();
            Coord = lastCoord;
            //Don't change momentum if holding
            momentum = direction.HasValue
                ? UpdatePlannedMomentum(direction.Value)
                : lastMomentum;

            //Get a list of all steps
            var steps = GetStepListFromMomentum();
            // Updated expected location
            Coord = GetPlannedCoord(steps);
            //Update planned move
            PlanningAction.PlannedMove = steps;
            Debug.Log(string.Join(", ", steps));

            RefreshAimPreview();
        }

        private HexCoord UpdatePlannedMomentum(HexDirection direction)
        {
            if (lastMomentum == HexCoord.Zero)
            {
                momentum = direction.ToOffset() * baseMomentumMax;
            }

            // If we're aiming in the same direction
            if (direction == CurrentAimDirection.ToHexDirection())
            {
                Debug.Log("Aiming the same way I'm moving");
                //If we're faster than base, then crank to max
                //If we're not, go to base.
                HexCoord dir = IsFasterThan(direction, baseMomentumMax)
                    ? (lastMomentum * aimMomentumMax).AbsClamp(aimMomentumMax)
                    : (lastMomentum * baseMomentumMax)
                    .AbsClamp(baseMomentumMax);
                Debug.LogWarning($"Going from {lastMomentum} to {dir}");
            }

            //Check if we're slowing down
            if (lastMomentum != HexCoord.Zero &&
                direction == lastMomentum.ToDirection().Opposite())
            {
                return IsFasterThan(direction, baseMomentumMax)
                    ? momentum = direction.ToOffset() * baseMomentumMax
                    : HexCoord.Zero;
            }

            return direction.ToOffset() * baseMomentumMax;
        }

        private bool IsFasterThan(HexDirection direction, int maxSpeed)
        {
            HexCoord maxVal = direction.ToOffset() * maxSpeed;
            return direction switch
            {
                HexDirection.D0 => maxVal.Q <= lastMomentum.Q,
                HexDirection.D1 => maxVal.Q <= lastMomentum.Q &&
                                   maxVal.R >= lastMomentum.R,
                // > R because R is negative in this direction, so we're
                // faster if we're LESS than R
                HexDirection.D2 => maxVal.R >= lastMomentum.R,
                HexDirection.D3 => maxVal.Q >= lastMomentum.Q,
                HexDirection.D4 => maxVal.Q >= lastMomentum.Q &&
                                   maxVal.R <= lastMomentum.R,
                HexDirection.D5 => maxVal.R <= lastMomentum.R,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction), direction, null)
            };
        }

        #endregion

        #region Resolution

        private IEnumerator<(int, HexCoord)> ResolveMove()
        {
            var moveSteps = PlanningAction.PlannedMove;
            HexCoord current = lastCoord;
            //First, tell the system how many steps we plan to take, and where we start
            // This lets the system know our intent.
            yield return (moveSteps.Count, current);

            //Then iterate through the steps
            for (int i = 0; i < moveSteps.Count; i++)
            {
                var direction = moveSteps[i];
                //If, for some reason, we can't move any more,
                //break out and exit the coroutine.
                if (!TryStep(direction, ref current))
                {
                    break;
                }

                //Otherwise, tell the system how many more steps there are,
                //and where we currently expect to be.
                Coord = current;
                yield return (moveSteps.Count - i, current);
            }
        }

        private bool TryStep(HexDirection direction, ref HexCoord current)
        {
            HexCoord destination = GetStepDestination(current, direction);

            if (destination == current)
            {
                Debug.Log($"{name} is blocked moving {direction}");
                return false;
            }

            current = destination;
            SnapToCoord(current);
            Debug.Log($"{name} moved to {current}");
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
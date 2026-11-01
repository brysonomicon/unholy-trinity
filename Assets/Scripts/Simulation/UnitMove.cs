using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// A unit's movement through one tick. <see cref="MoveModel"/> creates it, then the simulator uses it up one step
    /// at a time. Tile rules may change it on the way, e.g. a boost turning the unit or adding steps.
    /// </summary>
    public class UnitMove
    {
        public HexDirection Direction { get; set; }
        public int StepsRemaining { get; set; }
        public int StepsTaken { get; private set; }
        /// <summary>True once something ended the move early: a wall, another unit or death.</summary>
        public bool IsHalted { get; private set; }

        public bool IsMoving => !IsHalted && StepsRemaining > 0;

        public UnitMove(HexDirection direction, int steps)
        {
            Direction = direction;
            StepsRemaining = steps;
        }

        internal void CompleteStep()
        {
            StepsRemaining--;
            StepsTaken++;
        }

        public void Halt()
        {
            IsHalted = true;
            StepsRemaining = 0;
        }
    }
}

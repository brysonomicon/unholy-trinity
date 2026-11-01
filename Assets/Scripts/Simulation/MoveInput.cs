using System;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// What a unit's plan asks its movement to do on one tick. This is the recorded intent, not the result:
    /// <see cref="MoveModel"/> combines it with the unit's momentum to work out the actual move.
    /// </summary>
    public readonly struct MoveInput : IEquatable<MoveInput>
    {
        /// <summary>Null when the plan gives no movement input for the tick.</summary>
        public readonly HexDirection? Direction;

        public MoveInput(HexDirection? direction)
        {
            Direction = direction;
        }

        public static readonly MoveInput None = default;

        public bool Equals(MoveInput other) => Direction == other.Direction;
        public override bool Equals(object obj) => obj is MoveInput other && Equals(other);
        public override int GetHashCode() => Direction.GetHashCode();
        public override string ToString() => Direction?.ToString() ?? "None";
    }
}

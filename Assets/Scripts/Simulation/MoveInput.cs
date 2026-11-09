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

        /// <summary>
        /// The unit's aim when it moves (aim resolves before move), which decides whether the move accelerates.
        /// Null when unknown, which never counts as lined up.
        /// </summary>
        public readonly Facing? Facing;

        public MoveInput(HexDirection? direction, Facing? facing = null)
        {
            Direction = direction;
            Facing = facing;
        }

        public static readonly MoveInput None = default;

        public bool Equals(MoveInput other) => Direction == other.Direction && Nullable.Equals(Facing, other.Facing);
        public override bool Equals(object obj) => obj is MoveInput other && Equals(other);
        public override int GetHashCode() => (Direction.GetHashCode() * 397) ^ Facing.GetHashCode();
        public override string ToString() =>
            Facing is { } facing ? $"{Direction?.ToString() ?? "None"} facing {facing}" : Direction?.ToString() ?? "None";
    }
}

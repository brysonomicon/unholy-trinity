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
        /// <summary>Null on a planned tick holds, which stops the unit.</summary>
        public readonly HexDirection? Direction;

        /// <summary>
        /// The unit's aim when it moves (aim resolves before move), which decides whether the move accelerates.
        /// Null when unknown, which never counts as lined up.
        /// </summary>
        public readonly Facing? Facing;

        /// <summary>
        /// False only for <see cref="None"/>: the unit has no plan for this tick, so it stays put and keeps its
        /// momentum. Any constructed input is planned, including a hold.
        /// </summary>
        public readonly bool IsPlanned;

        public MoveInput(HexDirection? direction, Facing? facing = null)
        {
            Direction = direction;
            Facing = facing;
            IsPlanned = true;
        }

        /// <summary>No plan for this tick. Not the same as a hold, which is <c>new MoveInput(null)</c>.</summary>
        public static readonly MoveInput None = default;

        public bool Equals(MoveInput other) =>
            Direction == other.Direction && Nullable.Equals(Facing, other.Facing) && IsPlanned == other.IsPlanned;
        public override bool Equals(object obj) => obj is MoveInput other && Equals(other);
        public override int GetHashCode() => (Direction.GetHashCode() * 397) ^ Facing.GetHashCode() ^ (IsPlanned ? 1 : 0);
        public override string ToString()
        {
            if (!IsPlanned)
                return "Unplanned";
            string move = Direction?.ToString() ?? "Hold";
            return Facing is { } facing ? $"{move} facing {facing}" : move;
        }
    }
}

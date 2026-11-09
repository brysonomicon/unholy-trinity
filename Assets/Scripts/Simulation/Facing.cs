using System;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Which way a unit faces on the 12-way aim ring: straight down <see cref="Axis"/>, or halfway between
    /// <see cref="Axis"/> and the next direction round (<c>Axis.Rotate(1)</c>). Lives here rather than using the
    /// weapons' aim type so the simulation doesn't depend on weapons.
    /// </summary>
    public readonly struct Facing : IEquatable<Facing>
    {
        public readonly HexDirection Axis;
        public readonly bool IsBetweenAxes;

        public Facing(HexDirection axis, bool isBetweenAxes = false)
        {
            Axis = axis;
            IsBetweenAxes = isBetweenAxes;
        }

        /// <summary>
        /// True if moving in <paramref name="direction"/> counts as moving the way this faces. A facing between
        /// two axes only lines up with them when <paramref name="betweenCountsForBoth"/> is set.
        /// </summary>
        public bool LinesUpWith(HexDirection direction, bool betweenCountsForBoth) =>
            IsBetweenAxes
                ? betweenCountsForBoth && (direction == Axis || direction == Axis.Rotate(1))
                : direction == Axis;

        public bool Equals(Facing other) => Axis == other.Axis && IsBetweenAxes == other.IsBetweenAxes;
        public override bool Equals(object obj) => obj is Facing other && Equals(other);
        public override int GetHashCode() => ((int)Axis * 2) + (IsBetweenAxes ? 1 : 0);
        public override string ToString() => IsBetweenAxes ? $"{Axis}+{Axis.Rotate(1)}" : Axis.ToString();
    }
}

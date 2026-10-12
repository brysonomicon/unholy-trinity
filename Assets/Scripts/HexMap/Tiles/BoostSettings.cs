using System;

namespace UnholyTrinity.Hex
{
    public enum BoostMode
    {
        /// <summary>Changes speed along whatever direction the unit is already moving.</summary>
        AlongMovement = 0,

        /// <summary>Redirects the unit onto <see cref="BoostSettings.Direction"/>, then changes speed.</summary>
        Fixed = 1,
    }

    /// <summary>
    /// Boost / friction data for a <see cref="TileType.Boost"/> tile.
    /// Positive strength speeds units up, negative strength (friction) slows them down.
    /// </summary>
    [Serializable]
    public struct BoostSettings : IEquatable<BoostSettings>
    {
        public int Strength;
        public BoostMode Mode;

        /// <summary>Only used when <see cref="Mode"/> is <see cref="BoostMode.Fixed"/>.</summary>
        public HexDirection Direction;

        public BoostSettings(int strength, BoostMode mode,
            HexDirection direction = HexDirection.D0)
        {
            Strength = strength;
            Mode = mode;
            Direction = direction;
        }

        public static BoostSettings Along(int strength) =>
            new BoostSettings(strength, BoostMode.AlongMovement);

        public static BoostSettings
            Fixed(int strength, HexDirection direction) =>
            new BoostSettings(strength, BoostMode.Fixed, direction);

        /// <summary>
        /// Velocity after passing over this tile. Speed never drops below zero.
        /// Along-movement tiles do nothing to a stationary unit (it has no direction);
        /// a positive fixed tile will push a stationary unit, conveyor-style.
        /// </summary>
        public readonly HexVelocity Apply(HexVelocity velocity)
        {
            if (Mode != BoostMode.AlongMovement)
            {
                return new HexVelocity(Direction,
                    Math.Max(0, velocity.Speed + Strength));
            }

            return !velocity.IsMoving
                ? velocity
                : new HexVelocity(velocity.Direction,
                    Math.Max(0, velocity.Speed + Strength));
        }

        public bool Equals(BoostSettings other)
        {
            return Strength == other.Strength 
                   && Mode == other.Mode 
                   && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is BoostSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Strength, (int)Mode, (int)Direction);
        }
    }
}
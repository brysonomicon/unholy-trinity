using System;

namespace UnholyTrinity.Hex
{
    /// <summary>Momentum carried between turns: a direction plus a speed in hexes per turn. May not be an effective 
    /// way to do movement here, iterate.</summary>
    public readonly struct HexVelocity : IEquatable<HexVelocity>
    {
        public readonly HexDirection Direction;
        public readonly int Speed;

        public HexVelocity(HexDirection direction, int speed)
        {
            Direction = direction;
            Speed = Math.Max(0, speed);
        }

        public static readonly HexVelocity Zero = default;

        public bool IsMoving => Speed > 0;

        public HexVelocity WithSpeed(int speed) => new HexVelocity(Direction, speed);
        public HexVelocity WithDirection(HexDirection direction) => new HexVelocity(direction, Speed);

        public bool Equals(HexVelocity other) => Direction == other.Direction && Speed == other.Speed;
        public override bool Equals(object obj) => obj is HexVelocity other && Equals(other);
        public override int GetHashCode() => ((int)Direction * 397) ^ Speed;
        public override string ToString() => $"{Direction} x{Speed}";
    }
}

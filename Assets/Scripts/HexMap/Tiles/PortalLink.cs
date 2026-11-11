using System;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Payload of a <see cref="TileType.Portal"/> tile. An entrance sends whatever steps onto it to
    /// <see cref="Target"/>; an exit only receives. Two entrances targeting each other make a two-way pair,
    /// and any number of entrances may share one target.
    /// </summary>
    [Serializable]
    public struct PortalLink : IEquatable<PortalLink>
    {
        public HexCoord Target;
        /// <summary>60 degree steps (counter-clockwise) applied to a unit's direction when it exits at <see cref="Target"/>.</summary>
        public int RotationSteps;
        /// <summary>True for a receive-only exit; <see cref="Target"/> and <see cref="RotationSteps"/> are ignored.</summary>
        public bool ExitOnly;

        public PortalLink(HexCoord target, int rotationSteps = 0)
        {
            Target = target;
            RotationSteps = rotationSteps;
            ExitOnly = false;
        }

        public static PortalLink Exit => new PortalLink { ExitOnly = true };

        public bool IsEntrance => !ExitOnly;

        public bool Equals(PortalLink other)
        {
            return Target.Equals(other.Target) && RotationSteps == other.RotationSteps && ExitOnly == other.ExitOnly;
        }

        public override bool Equals(object obj)
        {
            return obj is PortalLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Target, RotationSteps, ExitOnly);
        }
    }
}

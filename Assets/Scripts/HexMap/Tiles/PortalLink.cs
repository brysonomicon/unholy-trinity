using System;

namespace UnholyTrinity.Hex
{
    /// <summary>Destination of a <see cref="TileType.Portal"/> tile. Portals come in reciprocal pairs.</summary>
    [Serializable]
    public struct PortalLink : IEquatable<PortalLink>
    {
        public HexCoord Target;
        /// <summary>60 degree steps (counter-clockwise) applied to a unit's direction when it exits at <see cref="Target"/>.</summary>
        public int RotationSteps;

        public PortalLink(HexCoord target, int rotationSteps = 0)
        {
            Target = target;
            RotationSteps = rotationSteps;
        }

        public bool Equals(PortalLink other)
        {
            return Target.Equals(other.Target) && RotationSteps == other.RotationSteps;
        }

        public override bool Equals(object obj)
        {
            return obj is PortalLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Target, RotationSteps);
        }
    }
}

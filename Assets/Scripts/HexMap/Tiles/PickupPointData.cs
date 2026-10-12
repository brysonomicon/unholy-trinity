using System;

namespace UnholyTrinity.Hex
{
    /// <summary>Placeholder data for a <see cref="TileType.PickupPoint"/>; the pickup system will decide what spawns.</summary>
    [Serializable]
    public struct PickupPointData : IEquatable<PickupPointData>
    {
        /// <summary>Lets the pickup system treat groups of points differently (e.g. weapon vs. health spawns).</summary>
        public int SpawnGroup;

        public PickupPointData(int spawnGroup)
        {
            SpawnGroup = spawnGroup;
        }

        public bool Equals(PickupPointData other)
        {
            return SpawnGroup == other.SpawnGroup;
        }

        public override bool Equals(object obj)
        {
            return obj is PickupPointData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return SpawnGroup;
        }
    }
}

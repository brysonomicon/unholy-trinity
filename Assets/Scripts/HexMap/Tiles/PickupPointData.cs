using System;

namespace UnholyTrinity.Hex
{
    /// <summary>Placeholder data for a <see cref="TileType.PickupPoint"/>; the pickup system will decide what spawns.</summary>
    [Serializable]
    public struct PickupPointData
    {
        /// <summary>Lets the pickup system treat groups of points differently (e.g. weapon vs. health spawns).</summary>
        public int SpawnGroup;

        public PickupPointData(int spawnGroup)
        {
            SpawnGroup = spawnGroup;
        }
    }
}

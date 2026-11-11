using System;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Hex
{
    /// <summary>Data for a <see cref="TileType.PickupPoint"/>: the pickup it spawns, which also sets the effect, trigger and respawn time.</summary>
    [Serializable]
    public struct PickupPointData : IEquatable<PickupPointData>
    {
        public PickupDefinition Pickup;

        public PickupPointData(PickupDefinition pickup)
        {
            Pickup = pickup;
        }

        public bool HasPickup => Pickup != null;

        public bool Equals(PickupPointData other)
        {
            return Pickup == other.Pickup;
        }

        public override bool Equals(object obj)
        {
            return obj is PickupPointData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HasPickup ? Pickup.GetHashCode() : 0;
        }
    }
}

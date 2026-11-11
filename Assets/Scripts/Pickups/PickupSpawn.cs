using System;

namespace UnholyTrinity.Pickups
{
    /// <summary>
    /// Runtime state of one pickup point: available until collected, then unavailable for
    /// <see cref="PickupDefinition.RespawnTicks"/> ticks. Starts available.
    /// </summary>
    public class PickupSpawn
    {
        public PickupDefinition Definition { get; }
        public int TicksUntilRespawn { get; private set; }
        public bool IsAvailable => TicksUntilRespawn == 0;

        public PickupSpawn(PickupDefinition definition)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>Applies the effect and starts the respawn countdown; false if unavailable or the collector couldn't use it.</summary>
        public bool TryCollect(IPickupCollector collector)
        {
            if (!IsAvailable || !Definition.TryApply(collector))
                return false;

            TicksUntilRespawn = Definition.RespawnTicks;
            return true;
        }

        /// <summary>Counts the respawn timer down; true only when this call made the pickup available again.</summary>
        public bool AdvanceTicks(int ticks)
        {
            if (IsAvailable || ticks <= 0)
                return false;

            TicksUntilRespawn = Math.Max(0, TicksUntilRespawn - ticks);
            return IsAvailable;
        }
    }
}

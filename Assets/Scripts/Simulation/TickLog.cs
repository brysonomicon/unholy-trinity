using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Simulation
{
    /// <summary>Everything that happened during one simulated tick, in order.</summary>
    public class TickLog
    {
        readonly List<TickEvent> events = new List<TickEvent>();

        public IReadOnlyList<TickEvent> Events => events;

        /// <summary>The step new events are stamped with; advanced by the simulator, so rules never set it.</summary>
        public int Step { get; internal set; }

        public void Add(TickEventType type, int unitId, HexCoord coord, PickupDefinition pickup = null) =>
            Add(type, unitId, coord, coord, pickup);

        public void Add(TickEventType type, int unitId, HexCoord coord, HexCoord from, PickupDefinition pickup = null) =>
            events.Add(new TickEvent(type, Step, unitId, coord, from, pickup));
    }
}

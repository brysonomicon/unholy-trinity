using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Simulation
{
    public enum TickEventType
    {
        /// <summary>The unit moved from <see cref="TickEvent.From"/> into <see cref="TickEvent.Coord"/>.</summary>
        Moved,
        /// <summary>The unit at <see cref="TickEvent.From"/> couldn't enter <see cref="TickEvent.Coord"/> and stopped.</summary>
        Blocked,
        PickupCollected,
        PickupRespawned,
        Died,
    }

    /// <summary>
    /// Something that happened during a simulated tick. The log of these drives playback, previews and
    /// (later) replays, so views never need to re-derive what the simulation decided.
    /// </summary>
    public readonly struct TickEvent
    {
        /// <summary><see cref="UnitId"/> for events not caused by a unit, like a pickup respawning.</summary>
        public const int NoUnit = -1;

        public readonly TickEventType Type;
        /// <summary>0 at tick start, 1..n for each movement step, n + 1 for where units end up.</summary>
        public readonly int Step;
        public readonly int UnitId;
        /// <summary>The hex the event is about.</summary>
        public readonly HexCoord Coord;
        /// <summary>The unit's hex before a move or block; the same as <see cref="Coord"/> otherwise.</summary>
        public readonly HexCoord From;
        /// <summary>The pickup collected or respawned; null for other events.</summary>
        public readonly PickupDefinition Pickup;

        public TickEvent(TickEventType type, int step, int unitId, HexCoord coord, HexCoord from, PickupDefinition pickup)
        {
            Type = type;
            Step = step;
            UnitId = unitId;
            Coord = coord;
            From = from;
            Pickup = pickup;
        }

        public override string ToString() => Type switch
        {
            TickEventType.Moved => $"[{Step}] unit {UnitId} moved {From} -> {Coord}",
            TickEventType.Blocked => $"[{Step}] unit {UnitId} at {From} blocked entering {Coord}",
            TickEventType.PickupCollected => $"[{Step}] unit {UnitId} collected {Pickup.name} at {Coord}",
            TickEventType.PickupRespawned => $"[{Step}] {Pickup.name} respawned at {Coord}",
            _ => $"[{Step}] unit {UnitId} {Type} at {Coord}",
        };
    }
}

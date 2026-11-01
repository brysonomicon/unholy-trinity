using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Simulation
{
    /// <summary>The unit moved from <see cref="From"/> into <see cref="To"/>.</summary>
    public sealed class MovedEvent : TickEvent
    {
        public HexCoord From { get; }
        public HexCoord To { get; }

        public MovedEvent(int unitId, HexCoord from, HexCoord to) : base(unitId)
        {
            From = from;
            To = to;
        }

        protected override string Describe() => $"unit {UnitId} moved {From} -> {To}";
    }

    /// <summary>The unit at <see cref="From"/> couldn't enter <see cref="Into"/> and stopped.</summary>
    public sealed class BlockedEvent : TickEvent
    {
        public HexCoord From { get; }
        public HexCoord Into { get; }

        public BlockedEvent(int unitId, HexCoord from, HexCoord into) : base(unitId)
        {
            From = from;
            Into = into;
        }

        protected override string Describe() => $"unit {UnitId} at {From} blocked entering {Into}";
    }

    public sealed class PickupCollectedEvent : TickEvent
    {
        public HexCoord At { get; }
        public PickupDefinition Pickup { get; }

        public PickupCollectedEvent(int unitId, HexCoord at, PickupDefinition pickup) : base(unitId)
        {
            At = at;
            Pickup = pickup;
        }

        protected override string Describe() => $"unit {UnitId} collected {Pickup.name} at {At}";
    }

    /// <summary>A collected pickup became available again; not caused by any unit.</summary>
    public sealed class PickupRespawnedEvent : TickEvent
    {
        public HexCoord At { get; }
        public PickupDefinition Pickup { get; }

        public PickupRespawnedEvent(HexCoord at, PickupDefinition pickup) : base(NoUnit)
        {
            At = at;
            Pickup = pickup;
        }

        protected override string Describe() => $"{Pickup.name} respawned at {At}";
    }

    /// <summary>A tile at <see cref="At"/> restored <see cref="Amount"/> HP.</summary>
    public sealed class HealedEvent : TickEvent
    {
        public HexCoord At { get; }
        public int Amount { get; }

        public HealedEvent(int unitId, HexCoord at, int amount) : base(unitId)
        {
            At = at;
            Amount = amount;
        }

        protected override string Describe() => $"unit {UnitId} healed {Amount} at {At}";
    }

    /// <summary>A tile at <see cref="At"/> cost <see cref="HealthLost"/> HP, after armour.</summary>
    public sealed class DamagedEvent : TickEvent
    {
        public HexCoord At { get; }
        public int HealthLost { get; }

        public DamagedEvent(int unitId, HexCoord at, int healthLost) : base(unitId)
        {
            At = at;
            HealthLost = healthLost;
        }

        protected override string Describe() => $"unit {UnitId} took {HealthLost} damage at {At}";
    }

    public sealed class DiedEvent : TickEvent
    {
        public HexCoord At { get; }

        public DiedEvent(int unitId, HexCoord at) : base(unitId)
        {
            At = at;
        }

        protected override string Describe() => $"unit {UnitId} died at {At}";
    }
}

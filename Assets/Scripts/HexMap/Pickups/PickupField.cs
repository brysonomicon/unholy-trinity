using System.Collections.Generic;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Runtime pickup state for a map: one <see cref="PickupSpawn"/> per assigned pickup point.
    /// </summary>
    public class PickupField
    {
        readonly Dictionary<HexCoord, PickupSpawn> spawns = new Dictionary<HexCoord, PickupSpawn>();

        public IReadOnlyDictionary<HexCoord, PickupSpawn> Spawns => spawns;

        /// <summary>Pickup points without an assigned pickup are skipped; <see cref="HexMap.Validate"/> reports them.</summary>
        public PickupField(HexMap map)
        {
            foreach (var tile in map.PickupPoints)
                if (tile.Pickup.HasPickup)
                    spawns.Add(tile.Coord, new PickupSpawn(tile.Pickup.Pickup));
        }

        PickupField()
        {
        }

        /// <summary>An independent copy of every point's state, for simulating without changing this field.</summary>
        public PickupField Clone()
        {
            var clone = new PickupField();
            foreach (var pair in spawns)
                clone.spawns.Add(pair.Key, pair.Value.Clone());
            return clone;
        }

        public bool IsAvailable(HexCoord coord) => spawns.TryGetValue(coord, out var spawn) && spawn.IsAvailable;

        /// <summary>Collects the pickup at <paramref name="coord"/> if its trigger matches how the hex was <paramref name="reached"/>.</summary>
        public bool TryCollect(HexCoord coord, PickupTrigger reached, IPickupCollector collector) =>
            spawns.TryGetValue(coord, out var spawn)
            && spawn.Definition.Trigger == reached
            && spawn.TryCollect(collector);

        /// <summary>Collects OnEnter pickups along the slide's path, then the OnStop pickup where it ends.</summary>
        // public void CollectAlong() =>
            //todo

        /// <summary>Collects OnEnter pickups on each hex of <paramref name="path"/>, then the OnStop pickup at <paramref name="end"/>.</summary>
        public void CollectAlong(IEnumerable<HexCoord> path, HexCoord end, IPickupCollector collector)
        {
            foreach (var coord in path)
                TryCollect(coord, PickupTrigger.OnEnter, collector);
            TryCollect(end, PickupTrigger.OnStop, collector);
        }

        /// <summary>Counts every respawn timer down; returns the points that became available again.</summary>
        public List<HexCoord> AdvanceTicks(int ticks)
        {
            var respawned = new List<HexCoord>();
            foreach (var pair in spawns)
                if (pair.Value.AdvanceTicks(ticks))
                    respawned.Add(pair.Key);
            return respawned;
        }
    }
}

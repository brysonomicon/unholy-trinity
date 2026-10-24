using System;
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

        /// <summary>Raised after a pickup is collected at the coord.</summary>
        public event Action<HexCoord, PickupSpawn> Collected;
        /// <summary>Raised when a collected pickup becomes available again at the coord.</summary>
        public event Action<HexCoord, PickupSpawn> Respawned;

        public IReadOnlyDictionary<HexCoord, PickupSpawn> Spawns => spawns;

        /// <summary>Pickup points without an assigned pickup are skipped; <see cref="HexMap.Validate"/> reports them.</summary>
        public PickupField(HexMap map)
        {
            foreach (var tile in map.PickupPoints)
                if (tile.Pickup.HasPickup)
                    spawns.Add(tile.Coord, new PickupSpawn(tile.Pickup.Pickup));
        }

        public bool IsAvailable(HexCoord coord) => spawns.TryGetValue(coord, out var spawn) && spawn.IsAvailable;

        /// <summary>Collects the pickup at <paramref name="coord"/> if its trigger matches how the hex was <paramref name="reached"/>.</summary>
        public bool TryCollect(HexCoord coord, PickupTrigger reached, IPickupCollector collector)
        {
            if (!spawns.TryGetValue(coord, out var spawn)
                || spawn.Definition.Trigger != reached
                || !spawn.TryCollect(collector))
                return false;

            Collected?.Invoke(coord, spawn);
            return true;
        }

        /// <summary>Collects OnEnter pickups along the slide's path, then the OnStop pickup where it ends.</summary>
        public void CollectAlong(SlideResult slide, IPickupCollector collector)
        {
            foreach (var coord in slide.Path)
                TryCollect(coord, PickupTrigger.OnEnter, collector);
            TryCollect(slide.End, PickupTrigger.OnStop, collector);
        }

        public void AdvanceTicks(int ticks)
        {
            foreach (var pair in spawns)
                if (pair.Value.AdvanceTicks(ticks))
                    Respawned?.Invoke(pair.Key, pair.Value);
        }
    }
}

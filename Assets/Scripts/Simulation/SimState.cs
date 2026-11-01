using System;
using System.Collections.Generic;
using System.Linq;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Everything a tick simulation reads and changes. Clones are independent, so previews can't leak into the
    /// real game; the map is shared because simulations never change it.
    /// </summary>
    public class SimState
    {
        readonly List<UnitState> units;

        public HexMap Map { get; }
        /// <summary>Null when there are no pickups to simulate.</summary>
        public PickupField Pickups { get; }
        public IReadOnlyList<UnitState> Units => units;

        public SimState(HexMap map, IEnumerable<UnitState> units, PickupField pickups = null)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            this.units = units.ToList();
            Pickups = pickups;

            if (this.units.Select(unit => unit.Id).Distinct().Count() != this.units.Count)
                throw new ArgumentException("Unit ids must be unique.", nameof(units));
        }

        public SimState Clone() => new SimState(Map, units.Select(unit => unit.Clone()), Pickups?.Clone());

        /// <summary>A clone holding only <paramref name="unitId"/>, for previewing one plan without guessing at anyone else's.</summary>
        public SimState CloneWithOnly(int unitId) => new SimState(Map, new[] { GetUnit(unitId).Clone() }, Pickups?.Clone());

        public UnitState GetUnit(int id) =>
            units.Find(unit => unit.Id == id) ?? throw new ArgumentException($"No unit with id {id}.", nameof(id));

        /// <summary>True if a living unit stands on <paramref name="coord"/>.</summary>
        public bool IsOccupied(HexCoord coord) => units.Exists(unit => !unit.IsDead && unit.Coord == coord);
    }
}

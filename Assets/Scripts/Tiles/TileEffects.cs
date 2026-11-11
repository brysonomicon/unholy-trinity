using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;
using UnholyTrinity.Players;

namespace UnholyTrinity.Tiles
{
    // what a hex does to a unit that enters it or stops on it. resolution (GameManager) runs these on the
    // real player and the plan forecast (Player) runs them on a copy.
    // pickups is null when the scene has none
    public static class TileEffects
    {
        // runs when a unit steps into coord: its health tile, then its pickup, then its portal.
        // returns where the unit ends up: the portal's target on an entrance, otherwise coord.
        // a unit that dies here stays on coord
        public static HexCoord OnEnter(HexMap map, PickupField pickups, HexCoord coord, IVitals unit)
        {
            ApplyHealthTile(map, coord, HealthTrigger.OnEnter, unit);
            pickups?.TryCollect(coord, PickupTrigger.OnEnter, unit);

            if (unit.IsDead)
            {
                return coord;
            }

            // arriving on a portal's target isn't a step, so the target's own OnEnter effects don't
            // run. portals only lead to portals for now, which have none. revisit if that changes
            return map.ThroughPortal(coord);
        }

        public static void OnStop(HexMap map, PickupField pickups, HexCoord coord, IVitals unit)
        {
            ApplyHealthTile(map, coord, HealthTrigger.OnStop, unit);
            pickups?.TryCollect(coord, PickupTrigger.OnStop, unit);
        }

        // heals, harms or kills if coord is a health tile with a matching trigger
        private static void ApplyHealthTile(HexMap map, HexCoord coord, HealthTrigger reached, IVitals unit)
        {
            if (unit.IsDead ||
                !map.TryGetTile(coord, out HexTile tile) ||
                tile.Type != TileType.Health ||
                tile.Health.Trigger != reached)
            {
                return;
            }

            HealthTileSettings health = tile.Health;

            if (health.Lethal)
            {
                unit.Kill();
            }
            else if (health.Amount > 0)
            {
                unit.TryHeal(health.Amount);
            }
            else if (health.Amount < 0)
            {
                unit.TakeDamage(-health.Amount);
            }
        }
    }
}

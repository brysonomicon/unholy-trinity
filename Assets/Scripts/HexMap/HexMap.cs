using System.Collections.Generic;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// The logical map: a sparse set of tiles, so any shape is allowed. Hexes with no tile are off-map
    /// and behave like walls. Pure data with no Unity dependencies, so it is safe for simulation and tests.
    /// </summary>
    public class HexMap
    {
        readonly Dictionary<HexCoord, HexTile> tiles = new Dictionary<HexCoord, HexTile>();

        public IReadOnlyCollection<HexTile> Tiles => tiles.Values;
        public int Count => tiles.Count;

        public HexTile this[HexCoord coord] => tiles[coord];

        public bool Contains(HexCoord coord) => tiles.ContainsKey(coord);

        public bool TryGetTile(HexCoord coord, out HexTile tile) => tiles.TryGetValue(coord, out tile);

        public bool IsWalkable(HexCoord coord) => tiles.TryGetValue(coord, out var tile) && tile.IsWalkable;

        public bool BlocksLineOfFire(HexCoord coord) => !tiles.TryGetValue(coord, out var tile) || tile.BlocksLineOfFire;

        /// <summary>
        /// The wall variant at <paramref name="coord"/>. False for non-wall tiles and for off-map hexes,
        /// which callers should treat as normal walls.
        /// </summary>
        public bool TryGetWall(HexCoord coord, out WallSettings wall)
        {
            if (tiles.TryGetValue(coord, out var tile) && tile.Type == TileType.Wall)
            {
                wall = tile.WallSettings;
                return true;
            }
            wall = default;
            return false;
        }

        public void SetTile(HexTile tile) => tiles[tile.Coord] = tile;

        public bool RemoveTile(HexCoord coord) => tiles.Remove(coord);

        public void Clear() => tiles.Clear();

        public IEnumerable<HexCoord> Neighbors(HexCoord coord, bool walkableOnly = false)
        {
            for (int i = 0; i < HexDirectionExtensions.Count; i++)
            {
                var n = coord.Neighbor((HexDirection)i);
                if (walkableOnly ? IsWalkable(n) : Contains(n))
                    yield return n;
            }
        }

        public bool TryGetPortalExit(HexCoord coord, out HexCoord exit, out int rotationSteps)
        {
            if (tiles.TryGetValue(coord, out var tile) && tile.Type == TileType.Portal
                && tiles.TryGetValue(tile.Portal.Target, out var target) && target.Type == TileType.Portal
                && tile.Portal.Target != coord)
            {
                exit = tile.Portal.Target;
                rotationSteps = tile.Portal.RotationSteps;
                return true;
            }

            exit = coord;
            rotationSteps = 0;
            return false;
        }

        public IEnumerable<HexTile> TilesOfType(TileType type)
        {
            foreach (var tile in tiles.Values)
                if (tile.Type == type)
                    yield return tile;
        }

        public IEnumerable<HexTile> PickupPoints => TilesOfType(TileType.PickupPoint);

        /// <summary>Returns human-readable problems with the map; empty when valid.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            foreach (var tile in tiles.Values)
            {
                if (tile.Type == TileType.Portal)
                    ValidatePortal(tile, problems);
                else if (tile.Type == TileType.PickupPoint)
                    ValidatePickupPoint(tile, problems);
            }
            return problems;
        }

        void ValidatePortal(HexTile tile, List<string> problems)
        {
            var target = tile.Portal.Target;
            if (target == tile.Coord)
                problems.Add($"Portal {tile.Coord} links to itself.");
            else if (!tiles.TryGetValue(target, out var other))
                problems.Add($"Portal {tile.Coord} links to {target}, which is off the map.");
            else if (other.Type != TileType.Portal)
                problems.Add($"Portal {tile.Coord} links to {target}, which is a {other.Type}, not a portal.");
            else if (other.Portal.Target != tile.Coord)
                problems.Add($"Portal {tile.Coord} links to {target}, but that portal links to {other.Portal.Target}.");
        }

        static void ValidatePickupPoint(HexTile tile, List<string> problems)
        {
            if (!tile.Pickup.HasPickup)
                problems.Add($"Pickup point {tile.Coord} has no pickup assigned.");
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Serialized map data. Hand-painted in the Scene view via MapManager for now; procedural generation can
    /// later build a <see cref="HexMap"/> and <see cref="Write"/> it here. Holds gameplay data only:
    /// orientation and hex size are view settings on MapManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Unholy Trinity/Hex Map", fileName = "HexMap")]
    public class HexMapDefinition : ScriptableObject
    {
        [Serializable]
        public struct TileEntry
        {
            public HexCoord Coord;
            public TileType Type;
            public BoostSettings Boost;
            public PortalLink Portal;
            public PickupPointData Pickup;

            public TileEntry(HexTile tile)
            {
                Coord = tile.Coord;
                Type = tile.Type;
                Boost = tile.Boost;
                Portal = tile.Portal;
                Pickup = tile.Pickup;
            }

            public HexTile ToTile() => new HexTile(Coord, Type, Boost, Portal, Pickup);
        }

        [SerializeField] List<TileEntry> tiles = new List<TileEntry>();

        public IReadOnlyList<TileEntry> Entries => tiles;

        public HexMap Build()
        {
            var map = new HexMap();
            foreach (var entry in tiles)
                map.SetTile(entry.ToTile());
            return map;
        }

        /// <summary>Replaces the stored tiles with the contents of <paramref name="map"/>.</summary>
        public void Write(HexMap map)
        {
            tiles.Clear();
            foreach (var tile in map.Tiles)
                tiles.Add(new TileEntry(tile));
            tiles.Sort((a, b) => a.Coord.R != b.Coord.R ? a.Coord.R.CompareTo(b.Coord.R) : a.Coord.Q.CompareTo(b.Coord.Q));
        }

        public bool TryGetTile(HexCoord coord, out HexTile tile)
        {
            int index = IndexOf(coord);
            tile = index >= 0 ? tiles[index].ToTile() : default;
            return index >= 0;
        }

        public void SetTile(HexTile tile)
        {
            int index = IndexOf(tile.Coord);
            if (index >= 0)
                tiles[index] = new TileEntry(tile);
            else
                tiles.Add(new TileEntry(tile));
        }

        public bool RemoveTile(HexCoord coord)
        {
            int index = IndexOf(coord);
            if (index < 0)
                return false;
            tiles.RemoveAt(index);
            return true;
        }

        public void Clear() => tiles.Clear();

        /// <summary>Validation problems, including duplicate coordinates that <see cref="HexMap"/> can't represent.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<HexCoord>();
            foreach (var entry in tiles)
                if (!seen.Add(entry.Coord))
                    problems.Add($"Duplicate tile at {entry.Coord}; the last one wins.");
            problems.AddRange(Build().Validate());
            return problems;
        }

        int IndexOf(HexCoord coord)
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].Coord == coord)
                    return i;
            return -1;
        }
    }
}

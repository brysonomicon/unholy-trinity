using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>Marks a spawned tile GameObject with the hex it represents.</summary>
    public class HexTileView : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public TileType Type { get; private set; }

        public void Init(HexTile tile)
        {
            Coord = tile.Coord;
            Type = tile.Type;
        }
    }
}

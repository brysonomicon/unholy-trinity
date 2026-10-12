using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    public readonly struct AimTile
    {
        public HexCoord Coord { get; }
        public float Falloff { get; }

        public AimTile(HexCoord coord, float falloff)
        {
            Coord = coord;
            Falloff = falloff;
        }
    }
}


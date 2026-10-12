using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RailgunData", menuName = "Unholy Trinity/RailgunData")]
    public class RailgunData : WeaponData
    {
        private static readonly HexLayout AngleLayout = new HexLayout(HexOrientation.PointyTop, 1f, Vector3.zero);

        [SerializeField] private float[] tierMaxAngles = { 1f, 20f, 30f };
        [SerializeField] private float[] tierFalloff = { 1f, 0.66f, 0.33f };

        public override List<AimTile> GetAimTiles(HexMap map, HexCoord origin, HexDirection direction)
        {
            List<AimTile> aimTiles = new List<AimTile>();
            Vector3 aim = AngleLayout.HexToWorld(direction.ToOffset());

            foreach (HexTile tile in map.Tiles)
            {
                if (tile.Coord == origin ||
                    tile.BlocksLineOfFire)
                {
                    continue;
                }

                float angle = Vector3.Angle(aim, AngleLayout.HexToWorld(tile.Coord - origin));
                int tier = GetTier(angle);

                if (tier < 0 ||
                    !ShotTracer.HasLineOfSight(map, origin, tile.Coord))
                {
                    continue;
                }

                aimTiles.Add(new AimTile(tile.Coord, tierFalloff[tier]));
            }

            return aimTiles;
        }

        public override void Fire()
        {
            // TODO: roll hit chance per aimed tile depending on falloff, full damage on hit
        }

        private int GetTier(float angle)
        {
            for (int i = 0; i < tierMaxAngles.Length; i++)
            {
                if (angle <= tierMaxAngles[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

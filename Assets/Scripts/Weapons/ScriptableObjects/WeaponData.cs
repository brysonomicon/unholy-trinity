using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    public abstract class WeaponData : ScriptableObject
    {
        private static readonly HexLayout AngleLayout = new HexLayout(HexOrientation.PointyTop, 1f, Vector3.zero);

        [Header("Properties")]
        [SerializeField] private string displayName;
        [SerializeField] private int maxAmmo = 10;
        [SerializeField] private int pickupAmmo = 5;
        [SerializeField] private int cooldown = 0;

        [Header("Aim Cone")]
        [SerializeField] private float[] tierMaxAngles = { 1f, 20f, 30f };
        [SerializeField] private float[] tierFalloff = { 1f, 0.66f, 0.33f };

        public string DisplayName => displayName;
        public int MaxAmmo => maxAmmo;
        public int PickupAmmo => pickupAmmo;
        public int Cooldown => cooldown;

        public virtual List<AimTile> GetAimTiles(HexMap map, HexCoord origin, HexDirection direction)
        {
            List<AimTile> aimTiles = new List<AimTile>();
            Vector3 aim = AngleLayout.HexToWorld(direction.ToOffset());

            foreach (HexTile tile in map.Tiles)
            {
                if (tile.Coord == origin ||
                    tile.BlocksLineOfFire ||
                    !IsInRange(origin, tile.Coord))
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

        public abstract void Fire();

        // weapons reach anything in line of sight unless they override this
        protected virtual bool IsInRange(HexCoord origin, HexCoord target)
        {
            return true;
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


using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RocketLauncherData", menuName = "Unholy Trinity/RocketLauncherData")]
    public class RocketLauncherData : WeaponData
    {
        [SerializeField][Min(0)] private int damage = 100;
        [field: SerializeField] public Rocket RocketPrefab { get; private set; }
        // hexes per tick, should keep this above max player speed
        [field: SerializeField][Min(1)] public int Speed { get; private set; } = 2;
        // falloff by distance from impact tile, 0 is direct hit
        [SerializeField] private float[] blastFalloff = { 1f, 0.66f, 0.33f };

        public override bool RequiresTarget => true;

        public override int GetDamage(AimTile aimTile)
        {
            return Mathf.RoundToInt(damage * aimTile.Falloff);
        }

        // tiles caught in the explosion
        // TODO: make walls shield tiles behind them
        public List<AimTile> GetBlastTiles(HexMap map, HexCoord impact)
        {
            List<AimTile> blastTiles = new();

            for (int distance = 0; distance < blastFalloff.Length; distance++)
            {
                foreach (HexCoord coord in HexCoord.Ring(impact, distance))
                {
                    if (!map.BlocksLineOfFire(coord))
                    {
                        blastTiles.Add(new AimTile(coord, blastFalloff[distance]));
                    }
                }
            }

            return blastTiles;
        }
    }
}


using System.Collections.Generic;
using System.Linq;
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
        public List<AimTile> GetBlastTiles(HexMap map, HexCoord impact)
        {
            List<AimTile> blastTiles = new();

            for (int distance = 0; distance < blastFalloff.Length; distance++)
            {
                foreach (HexCoord coord in HexCoord.Ring(impact, distance))
                {
                    if (!map.BlocksLineOfFire(coord) &&
                        ShotTracer.HasLineOfSight(map, impact, coord)
                    ){
                        blastTiles.Add(new AimTile(coord, blastFalloff[distance]));
                    }
                }
            }

            return blastTiles;
        }

        // the aim cones falloff is accuracy for rockets. a failed roll moves the
        // target tile to a random neighbour that is also in the aim cone
        public HexCoord GetLandingTile(List<AimTile> aimTiles, AimTile target)
        {
            if (Random.value <= target.Falloff)
            {
                return target.Coord;
            }

            List<HexCoord> scatterTiles = aimTiles
                .Select(aimTile => aimTile.Coord)
                .Where(coord => coord.DistanceTo(target.Coord) == 1)
                .ToList();

            return scatterTiles.Count > 0
                ? scatterTiles[Random.Range(0, scatterTiles.Count)]
                : target.Coord;
        }
    }
}


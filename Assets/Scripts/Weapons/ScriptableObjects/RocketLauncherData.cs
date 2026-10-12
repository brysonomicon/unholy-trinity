using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RocketLauncherData", menuName = "Unholy Trinity/RocketLauncherData")]
    public class RocketLauncherData : WeaponData
    {
        public override List<AimTile> GetAimTiles(HexMap map, HexCoord origin, HexDirection direction)
        {
            // TODO: launch projectile in direction, splash tiers at detonation tile
            return new List<AimTile>();
        }
        public override void Fire()
        {
            // TODO: spawn projectile toward target, splash falloff at impact
        }
    }
}


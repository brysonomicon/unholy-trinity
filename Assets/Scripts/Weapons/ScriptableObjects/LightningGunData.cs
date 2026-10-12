using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "LightningGunData", menuName = "Unholy Trinity/LightningGunData")]
    public class LightningGunData : WeaponData
    {
        public override List<AimTile> GetAimTiles(HexMap map, HexCoord origin, HexDirection direction)
        {
            // TODO: cone like rail gun, damage falloff instead of accuracy, limited by range
            return new List<AimTile>();
        }

        public override void Fire()
        {
            //TODO: damage per AimTile scaled by falloff percent
        }
    }
}


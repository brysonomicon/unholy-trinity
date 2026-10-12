using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "LightningGunData", menuName = "Unholy Trinity/LightningGunData")]
    public class LightningGunData : WeaponData
    {
        [SerializeField, Min(1)] private int range = 4;

        protected override bool IsInRange(HexCoord origin, HexCoord target)
        {
            return origin.DistanceTo(target) <= range;
        }

        public override void Fire()
        {
            //TODO: damage per AimTile scaled by falloff percent
        }
    }
}


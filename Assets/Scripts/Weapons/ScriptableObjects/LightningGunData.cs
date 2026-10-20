using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "LightningGunData", menuName = "Unholy Trinity/LightningGunData")]
    public class LightningGunData : WeaponData
    {
        [SerializeField][Min(1)] private int range = 4;
        [SerializeField][Min(0)] private int damage = 21;

        protected override bool IsInRange(HexCoord origin, HexCoord target)
        {
            return origin.DistanceTo(target) <= range;
        }

        public override int GetDamage(AimTile aimTile)
        {
            return Mathf.RoundToInt(damage * aimTile.Falloff);
        }
    }
}


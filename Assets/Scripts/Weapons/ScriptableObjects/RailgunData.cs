using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RailgunData", menuName = "Unholy Trinity/RailgunData")]
    public class RailgunData : WeaponData
    {
        [SerializeField][Min(0)] private int damage = 80;

        public override int GetDamage(AimTile aimTile)
        {
            bool shotHit = Random.value <= aimTile.Falloff;

            if (shotHit)
            {
                return damage;
            }

            return 0;
        }
    }
}

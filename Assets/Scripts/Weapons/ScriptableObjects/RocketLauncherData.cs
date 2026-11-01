using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RocketLauncherData", menuName = "Unholy Trinity/RocketLauncherData")]
    public class RocketLauncherData : WeaponData
    {
        [SerializeField][Min(0)] private int damage = 100;

        public override bool RequiresTarget => true;

        public override int GetDamage(AimTile aimTile)
        {
            // TODO: spawn projectile toward target, splash falloff at impact
            return 0;
        }
    }
}


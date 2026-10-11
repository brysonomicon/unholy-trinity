using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "LightningGunData", menuName = "Unholy Trinity/LightningGunData")]
    public class LightningGunData : WeaponData
    {
        public override void Fire()
        {
            //TODO: damage per AimTile scaled by falloff percent
        }
    }
}


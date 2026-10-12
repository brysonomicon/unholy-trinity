using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RailgunData", menuName = "Unholy Trinity/RailgunData")]
    public class RailgunData : WeaponData
    {
        public override void Fire()
        {
            // TODO: roll hit chance per aimed tile depending on falloff, full damage on hit
        }
    }
}

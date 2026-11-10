using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(menuName = "Unholy Trinity/Pickups/Weapon", fileName = "WeaponPickup")]
    public class WeaponPickup : PickupDefinition
    {
        [field: SerializeField] public WeaponData Weapon { get; private set; }

        public override bool TryApply(IPickupCollector collector)
        {
            return collector is IWeaponCollector weaponCollector &&
                   Weapon != null &&
                   weaponCollector.TryGiveWeapon(Weapon);
        }
    }
}


using UnholyTrinity.Pickups;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    // ammo for one weapon. only adds ammo to a weapon the collector already holds
    [CreateAssetMenu(menuName = "Unholy Trinity/Pickups/Ammo", fileName = "AmmoPickup")]
    public class AmmoPickup : PickupDefinition
    {
        [field: SerializeField] public WeaponData Weapon { get; private set; }

        [field: SerializeField][Min(1)] public int Amount { get; private set; } = 1;

        [field: SerializeField]
        [Tooltip("When the collector doesn't hold the weapon or its ammo is already full: on, the pickup is " +
                 "taken anyway with no effect (and starts respawning, so it can be denied). Off, it stays " +
                 "on the map for someone who can use it.")]
        public bool TakenWhenUnusable { get; private set; }

        public override bool TryApply(IPickupCollector collector)
        {
            if (collector is not IWeaponCollector weaponCollector || Weapon == null)
            {
                return false;
            }

            return weaponCollector.TryGiveAmmo(Weapon, Amount) switch
            {
                AmmoGiven.Added => true,
                AmmoGiven.WeaponNotHeld or AmmoGiven.AmmoFull => TakenWhenUnusable,
                _ => false,
            };
        }
    }
}

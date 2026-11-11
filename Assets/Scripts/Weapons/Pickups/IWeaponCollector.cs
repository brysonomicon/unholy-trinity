using UnholyTrinity.Pickups;

namespace UnholyTrinity.Weapons
{
    // same pattern as IHealable but lives with weapons because the pickup assembly
    // can't see WeaponData
    public interface IWeaponCollector : IPickupCollector
    {
        bool TryGiveWeapon(WeaponData data);

        // adds ammo to a weapon the collector already holds
        AmmoGiven TryGiveAmmo(WeaponData data, int amount);
    }
}

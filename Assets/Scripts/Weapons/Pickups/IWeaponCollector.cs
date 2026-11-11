using UnholyTrinity.Pickups;

namespace UnholyTrinity.Weapons
{
    // same pattern as IHealable but lives with weapons because the pickup assembly
    // can't see WeaponData
    public interface IWeaponCollector : IPickupCollector
    {
        bool TryGiveWeapon(WeaponData data);
    }
}

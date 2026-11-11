namespace UnholyTrinity.Weapons
{
    // what happened when a collector was offered ammo, so an ammo pickup can decide whether
    // it was taken
    public enum AmmoGiven
    {
        // ammo was added to the held weapon
        Added,

        // the collector doesn't hold that weapon, so there's nothing to load the ammo into
        WeaponNotHeld,

        // the weapon is held but its ammo is already at max
        AmmoFull,

        // the collector can't pick anything up right now, e.g. it's dead
        Refused,
    }
}

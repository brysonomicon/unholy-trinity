using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int pickupAmmo = 5;
    [SerializeField] private int cooldown = 0;

    public string DisplayName => displayName;
    public int MaxAmmo => maxAmmo;
    public int PickupAmmo => pickupAmmo;
    public int Cooldown => cooldown;

    //TODO: once hex map exists
    // public abstract List<AimTile> GetAimTiles(HexTile origin, HexTile target);

    public abstract void Fire();
}

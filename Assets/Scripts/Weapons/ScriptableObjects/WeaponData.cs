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
}

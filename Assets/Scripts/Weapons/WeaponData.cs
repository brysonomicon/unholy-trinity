using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    public string DisplayName;
    public int MaxAmmo = 10;
    public int PickupAmmo = 5;
    public int Cooldown = 0;
}

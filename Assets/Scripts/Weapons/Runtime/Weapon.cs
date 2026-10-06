using UnityEngine;

[System.Serializable]
public class Weapon
{
    [SerializeField] private WeaponData data;
    [SerializeField] private int ammo;
    [SerializeField] private int cooldown;

    public WeaponData Data => data;
    public int Ammo => ammo;

    public Weapon (WeaponData data)
    {
        this.data = data;
        ammo = data.PickupAmmo;
    }

    public bool CanFire()
    {
        return ammo > 0 && cooldown == 0;
    }

    public bool Fire()
    {
        if (!CanFire())
        {
            return false;
        }

        ammo--;
        cooldown = data.Cooldown;
        data.Fire();
        return true;
    }

    public void AddAmmo(int amount)
    {
        ammo = Mathf.Min(ammo + amount, data.MaxAmmo);
    }

    public void TickCooldown()
    {
        if (cooldown > 0)
        {
            cooldown--;
        }
    }
}

using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [System.Serializable]
    public class Weapon
    {
        [field: SerializeField] public WeaponData Data { get; private set; }
        [field: SerializeField] public int Ammo {get; private set;}
        [SerializeField] private int cooldown;
        
        public bool CanFire => Ammo > 0 && cooldown <= 0;

        public Weapon (WeaponData data)
        {
            this.Data = data;
            Ammo = data.PickupAmmo;
        }

        public bool Fire()
        {
            if (!CanFire)
            {
                return false;
            }
            Debug.Log($"Firing {Data.DisplayName}");
            Ammo--;
            cooldown = Data.Cooldown;
            return true;
        }

        public void AddAmmo(int amount)
        {
            Ammo = Mathf.Min(Ammo + amount, Data.MaxAmmo);
        }

        public void TickCooldown()
        {
            if (cooldown > 0)
            {
                cooldown--;
            }
        }
    }
}


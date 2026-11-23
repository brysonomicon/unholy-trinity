using System;
using UnityEngine;

namespace UnholyTrinity.Weapons
{
    [Serializable]
    public class Weapon
    {
        [field: SerializeField] public WeaponData Data { get; private set; }
        [field: SerializeField] public int Ammo {get; private set;}

        public bool HasAmmo => Ammo > 0;

        public Weapon (WeaponData data)
        {
            Data = data;
            Ammo = data.PickupAmmo;
        }

        public bool Fire()
        {
            if (!HasAmmo)
            {
                return false;
            }

            Ammo--;
            return true;
        }

        public void AddAmmo(int amount)
        {
            Ammo = Mathf.Min(Ammo + amount, Data.MaxAmmo);
        }
    }
}


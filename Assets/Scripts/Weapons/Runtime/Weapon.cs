using System;
using UnholyTrinity.Hex.Interfaces;
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

        public bool Fire(IMovable shooter, IAimer aimer)
        {
            if (!HasAmmo)
            {
                return false;
            }
            bool result = Data.Fire(shooter, aimer);
            if (result)
            {
                Ammo--;
            }

            return result;
        }

        public void AddAmmo(int amount)
        {
            Ammo = Mathf.Min(Ammo + amount, Data.MaxAmmo);
        }
    }
}


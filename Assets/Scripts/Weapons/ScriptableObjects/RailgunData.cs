using System;
using System.Collections.Generic;
using UnholyTrinity.Hex.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnholyTrinity.Weapons
{
    [CreateAssetMenu(fileName = "RailgunData",
        menuName = "Unholy Trinity/RailgunData")]
    public class RailgunData : WeaponData
    {
        public static event Action<IMovable, WeaponData, IAimer> BeamFired;

        [SerializeField] [Min(0)] private int damage = 80;

        public override bool Fire(IMovable shooter, IAimer aimer)
        {
            BeamFired?.Invoke(shooter, this, aimer);
            return true;
        }

        public override int GetDamage(AimTile aimTile)
        {
            bool shotHit = Random.value <= aimTile.Falloff;

            return shotHit ? damage : 0;
        }
    }
}

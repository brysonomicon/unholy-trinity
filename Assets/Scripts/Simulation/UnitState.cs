using System;
using UnholyTrinity.Hex;
using UnholyTrinity.Pickups;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// A unit as the simulation sees it. Simulations change these copies, never the scene: a preview throws
    /// them away and resolution writes them back to the players.
    /// </summary>
    public class UnitState : IHealable
    {
        public int Id { get; }
        public HexCoord Coord { get; internal set; }
        /// <summary>Momentum carried out of the last simulated tick.</summary>
        public HexVelocity Velocity { get; internal set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public int Armour { get; private set; }
        public ArmourProtection ArmourProtection { get; }
        public bool IsDead => Health <= 0;

        public UnitState(int id, HexCoord coord, HexVelocity velocity, int health, int maxHealth,
            int armour = 0, ArmourProtection armourProtection = default)
        {
            if (maxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be at least 1.");

            Id = id;
            Coord = coord;
            Velocity = velocity;
            MaxHealth = maxHealth;
            Health = Math.Clamp(health, 0, maxHealth);
            Armour = Math.Max(armour, 0);
            ArmourProtection = armourProtection;
        }

        public UnitState Clone() => new UnitState(Id, Coord, Velocity, Health, MaxHealth, Armour, ArmourProtection);

        public bool TryHeal(int amount) => Heal(amount) > 0;

        /// <summary>Restores up to <paramref name="amount"/> HP; returns the HP actually restored.</summary>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return 0;

            int healed = Math.Min(amount, MaxHealth - Health);
            Health += healed;
            return healed;
        }

        /// <summary>
        /// Deals <paramref name="amount"/> damage, absorbed by armour first unless the source
        /// <paramref name="ignoresArmour"/>. Returns the HP actually lost.
        /// </summary>
        public int TakeDamage(int amount, bool ignoresArmour = false)
        {
            if (IsDead || amount <= 0)
                return 0;

            if (!ignoresArmour)
            {
                int absorbed = ArmourProtection.Absorbed(amount, Armour);
                Armour -= absorbed;
                amount -= absorbed;
            }

            int lost = Math.Min(amount, Health);
            Health -= lost;
            return lost;
        }

        /// <summary>Kills outright, ignoring armour, e.g. lava or a fall into the void.</summary>
        public void Kill() => Health = 0;
    }
}

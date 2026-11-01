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
        public bool IsDead => Health <= 0;

        public UnitState(int id, HexCoord coord, HexVelocity velocity, int health, int maxHealth)
        {
            if (maxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be at least 1.");

            Id = id;
            Coord = coord;
            Velocity = velocity;
            MaxHealth = maxHealth;
            Health = Math.Clamp(health, 0, maxHealth);
        }

        public UnitState Clone() => new UnitState(Id, Coord, Velocity, Health, MaxHealth);

        public bool TryHeal(int amount)
        {
            if (IsDead || amount <= 0 || Health >= MaxHealth)
                return false;

            Health = Math.Min(Health + amount, MaxHealth);
            return true;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            Health = Math.Max(Health - amount, 0);
        }
    }
}

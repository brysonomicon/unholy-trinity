using UnholyTrinity.Pickups;

namespace UnholyTrinity.Players
{
    // health and armour that damage, heals, armour pickups and health tiles can change. the real
    // player is one, and so is the forecast of a player's plan, so both run the same tile rules
    public interface IVitals : IHealable, IArmourable
    {
        bool IsDead { get; }

        void TakeDamage(int damageAmount);

        void Kill();
    }
}

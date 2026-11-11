namespace UnholyTrinity.Pickups
{
    public interface IHealable : IPickupCollector
    {
        /// <summary>Restores up to <paramref name="amount"/> HP; false if nothing was healed (e.g. already at full HP).</summary>
        bool TryHeal(int amount);
    }
}

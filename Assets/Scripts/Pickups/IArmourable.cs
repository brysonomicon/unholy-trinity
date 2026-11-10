namespace UnholyTrinity.Pickups
{
    public interface IArmourable : IPickupCollector
    {
        /// <summary>Restores up to <paramref name="amount"/> armour; false if no armour was added (e.g. already at full armour).</summary>
        bool TryAddArmour(int amount);
    }
}

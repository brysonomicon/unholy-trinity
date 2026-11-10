namespace UnholyTrinity.Pickups
{
    /// <summary>
    /// Anything that can collect pickups. Effects look for the capability interfaces they need
    /// (e.g. <see cref="IHealable"/>), so a pickup never depends on a concrete unit type.
    /// </summary>
    public interface IPickupCollector
    {
    }
}

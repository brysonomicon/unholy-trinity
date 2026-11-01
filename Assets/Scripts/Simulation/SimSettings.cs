namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Game-wide rule settings. They live on the <see cref="SimState"/> so previews, resolution and replays always
    /// simulate under the same rules.
    /// </summary>
    public readonly struct SimSettings
    {
        /// <summary>Environmental damage that comes straight off health instead of being absorbed by armour.</summary>
        public readonly EnvironmentalDamage ArmourIgnoredBy;

        public SimSettings(EnvironmentalDamage armourIgnoredBy)
        {
            ArmourIgnoredBy = armourIgnoredBy;
        }

        public bool IgnoresArmour(EnvironmentalDamage source) => (ArmourIgnoredBy & source) != 0;
    }
}

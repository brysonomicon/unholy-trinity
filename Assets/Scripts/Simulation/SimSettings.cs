using System;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Game-wide rule settings. They live on the <see cref="SimState"/> so previews, resolution and replays always
    /// simulate under the same rules. <c>default</c> is valid: speeds fall back to 1 hex per tick.
    /// </summary>
    public readonly struct SimSettings
    {
        /// <summary>Environmental damage that comes straight off health instead of being absorbed by armour.</summary>
        public readonly EnvironmentalDamage ArmourIgnoredBy;

        /// <summary>A diagonal aim (between two hex directions) counts as lined up with both of them.</summary>
        public readonly bool BetweenAimsAccelerate;

        readonly int baseSpeed;
        readonly int aimedMaxSpeed;

        public SimSettings(EnvironmentalDamage armourIgnoredBy, int baseSpeed = 1, int aimedMaxSpeed = 1,
            bool betweenAimsAccelerate = false)
        {
            ArmourIgnoredBy = armourIgnoredBy;
            this.baseSpeed = baseSpeed;
            this.aimedMaxSpeed = aimedMaxSpeed;
            BetweenAimsAccelerate = betweenAimsAccelerate;
        }

        /// <summary>Speed of a move that isn't lined up with the unit's aim. At least 1.</summary>
        public int BaseSpeed => Math.Max(1, baseSpeed);

        /// <summary>Top speed when moving the way the unit aims; each aimed tick adds 1 up to this. At least <see cref="BaseSpeed"/>.</summary>
        public int AimedMaxSpeed => Math.Max(BaseSpeed, aimedMaxSpeed);

        public bool IgnoresArmour(EnvironmentalDamage source) => (ArmourIgnoredBy & source) != 0;
    }
}

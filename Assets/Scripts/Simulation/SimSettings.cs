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
        readonly int aimedGain;

        public SimSettings(EnvironmentalDamage armourIgnoredBy, int baseSpeed = 1, int aimedMaxSpeed = 1,
            bool betweenAimsAccelerate = false, int aimedGain = 1)
        {
            ArmourIgnoredBy = armourIgnoredBy;
            this.baseSpeed = baseSpeed;
            this.aimedMaxSpeed = aimedMaxSpeed;
            BetweenAimsAccelerate = betweenAimsAccelerate;
            this.aimedGain = aimedGain;
        }

        /// <summary>Speed when starting, reversing, or changing direction without aiming along the move. At least 1.</summary>
        public int BaseSpeed => Math.Max(1, baseSpeed);

        /// <summary>Top speed reachable by aimed moves. At least <see cref="BaseSpeed"/>.</summary>
        public int AimedMaxSpeed => Math.Max(BaseSpeed, aimedMaxSpeed);

        /// <summary>Speed added per tick while moving and aiming along the move. At least 1.</summary>
        public int AimedGain => Math.Max(1, aimedGain);

        public bool IgnoresArmour(EnvironmentalDamage source) => (ArmourIgnoredBy & source) != 0;
    }
}

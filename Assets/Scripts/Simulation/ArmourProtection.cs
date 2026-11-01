using System;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Absorbs a percentage of each hit, spending one armour point per point absorbed, until it
    /// runs out. Whole-number math because floating points are spooky. 
    /// Rounds up by default so even small hits use armour first.
    /// </summary>
    public readonly struct ArmourProtection : IEquatable<ArmourProtection>
    {
        public const int FullPercent = 100;

        /// <summary>Share of each hit the armour absorbs, out of <see cref="FullPercent"/>.</summary>
        public readonly int Percent;
        /// <summary>Round the absorbed share up, so even small hits use armour first.</summary>
        public readonly bool RoundUp;

        public ArmourProtection(int percent, bool roundUp = true)
        {
            Percent = Math.Clamp(percent, 0, FullPercent);
            RoundUp = roundUp;
        }

        /// <summary>How much of <paramref name="damage"/> the armour absorbs; never more than <paramref name="armour"/>.</summary>
        public int Absorbed(int damage, int armour)
        {
            if (damage <= 0 || armour <= 0)
                return 0;

            int rounding = RoundUp ? FullPercent - 1 : 0;
            return Math.Min((damage * Percent + rounding) / FullPercent, armour);
        }

        public bool Equals(ArmourProtection other) => Percent == other.Percent && RoundUp == other.RoundUp;
        public override bool Equals(object obj) => obj is ArmourProtection other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Percent, RoundUp);
        public override string ToString() => $"{Percent}%{(RoundUp ? " rounded up" : "")}";
    }
}

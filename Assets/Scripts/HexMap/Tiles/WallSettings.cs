using System;

namespace UnholyTrinity.Hex
{
    public enum WallKind
    {
        /// <summary>Stops a sliding unit and zeroes its speed.</summary>
        Normal = 0,
        /// <summary>Reverses a sliding unit, which carries on with its remaining steps and keeps its speed.</summary>
        Bouncy = 1,
        /// <summary>Stops a sliding unit like a normal wall and reports damage.</summary>
        Spiked = 2,
    }

    public enum SpikeDamageMode
    {
        /// <summary>Always deals <see cref="WallSettings.Damage"/>.</summary>
        Fixed = 0,
        /// <summary>Deals <see cref="WallSettings.Damage"/> per point of speed at impact.</summary>
        ScaledBySpeed = 1,
    }

    /// <summary>
    /// Variant data for a <see cref="TileType.Wall"/> tile. The default value is a normal wall,
    /// so walls saved before variants existed load unchanged.
    /// </summary>
    [Serializable]
    public struct WallSettings
    {
        public WallKind Kind;
        /// <summary>Only used when <see cref="Kind"/> is <see cref="WallKind.Spiked"/>.</summary>
        public int Damage;
        /// <summary>Only used when <see cref="Kind"/> is <see cref="WallKind.Spiked"/>.</summary>
        public SpikeDamageMode DamageMode;

        public WallSettings(WallKind kind, int damage = 0, SpikeDamageMode damageMode = SpikeDamageMode.Fixed)
        {
            Kind = kind;
            Damage = damage;
            DamageMode = damageMode;
        }

        public static WallSettings Normal => new WallSettings(WallKind.Normal);
        public static WallSettings Bouncy => new WallSettings(WallKind.Bouncy);
        public static WallSettings Spiked(int damage, SpikeDamageMode mode = SpikeDamageMode.Fixed) =>
            new WallSettings(WallKind.Spiked, damage, mode);

        /// <summary>Damage dealt by hitting this wall at <paramref name="impactSpeed"/>; 0 unless spiked.</summary>
        public int DamageFor(int impactSpeed)
        {
            if (Kind != WallKind.Spiked)
                return 0;
            return DamageMode == SpikeDamageMode.ScaledBySpeed ? Damage * impactSpeed : Damage;
        }
    }
}

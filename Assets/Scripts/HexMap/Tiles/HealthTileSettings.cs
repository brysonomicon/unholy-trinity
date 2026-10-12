using System;

namespace UnholyTrinity.Hex
{
    public enum HealthTrigger
    {
        /// <summary>Fires every time a unit enters the hex during a slide, including sliding straight over it.</summary>
        OnEnter = 0,
        /// <summary>Fires only when a slide ends on the hex, including a unit that didn't move.</summary>
        OnStop = 1,
    }

    /// <summary>
    /// Heal / harm / kill data for a <see cref="TileType.Health"/> tile.
    /// Positive amounts heal, negative amounts harm; a lethal tile kills outright and ends the slide.
    /// </summary>
    [Serializable]
    public struct HealthTileSettings
    {
        /// <summary>+ heals, - harms. Ignored when <see cref="Lethal"/>.</summary>
        public int Amount;
        public HealthTrigger Trigger;
        /// <summary>Kills the unit outright, e.g. lava or a fall into the void.</summary>
        public bool Lethal;

        public HealthTileSettings(int amount, HealthTrigger trigger = HealthTrigger.OnEnter, bool lethal = false)
        {
            Amount = amount;
            Trigger = trigger;
            Lethal = lethal;
        }

        public static HealthTileSettings Heal(int amount, HealthTrigger trigger = HealthTrigger.OnEnter) =>
            new HealthTileSettings(amount, trigger);
        public static HealthTileSettings Harm(int damage, HealthTrigger trigger = HealthTrigger.OnEnter) =>
            new HealthTileSettings(-damage, trigger);
        public static HealthTileSettings Kill(HealthTrigger trigger = HealthTrigger.OnEnter) =>
            new HealthTileSettings(0, trigger, lethal: true);
    }
}

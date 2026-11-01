using System;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Kinds of damage that come from the map rather than another player. Flags, so settings can pick any combination.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum#enumeration-types-as-bit-flags">
    [Flags]
    public enum EnvironmentalDamage
    {
        None = 0,
        /// <summary>Health tiles with a negative amount.</summary>
        HarmTile = 1 << 0,
        // SpikedWall = 1 << 1, once spiked walls are implemented
    }
}

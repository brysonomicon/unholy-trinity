namespace UnholyTrinity.Hex
{
    /// <summary>
    /// One of the six hex directions, indexed counter-clockwise (viewed from above).
    /// Orientation-neutral: gameplay only ever stores the index; <see cref="HexDirectionExtensions.DisplayName"/>
    /// gives the compass name for the current layout.
    /// </summary>
    public enum HexDirection
    {
        D0 = 0,
        D1 = 1,
        D2 = 2,
        D3 = 3,
        D4 = 4,
        D5 = 5,
    }

    public static class HexDirectionExtensions
    {
        public const int Count = 6;

        // Axial offsets, counter-clockwise starting from +q.
        static readonly HexCoord[] Offsets =
        {
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1),
        };

        static readonly string[] PointyNames = { "E", "NE", "NW", "W", "SW", "SE" };
        static readonly string[] FlatNames = { "SE", "NE", "N", "NW", "SW", "S" };

        public static HexCoord ToOffset(this HexDirection direction) => Offsets[(int)direction];

        public static HexDirection Opposite(this HexDirection direction) => direction.Rotate(3);

        /// <summary>Rotates by 60 degree steps; positive is counter-clockwise.</summary>
        public static HexDirection Rotate(this HexDirection direction, int steps) =>
            (HexDirection)Mod((int)direction + steps);

        public static string DisplayName(this HexDirection direction, HexOrientation orientation) =>
            orientation == HexOrientation.PointyTop ? PointyNames[(int)direction] : FlatNames[(int)direction];

        internal static int Mod(int value) => ((value % Count) + Count) % Count;
    }
}

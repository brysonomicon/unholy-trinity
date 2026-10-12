namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Immutable tile data. Only the payload matching <see cref="Type"/> is meaningful;
    /// use the static factories to build tiles.
    /// </summary>
    public readonly struct HexTile
    {
        public readonly HexCoord Coord;
        public readonly TileType Type;
        public readonly BoostSettings Boost;
        public readonly PortalLink Portal;
        public readonly PickupPointData Pickup;
        public readonly WallSettings WallSettings;

        public HexTile(HexCoord coord, TileType type, BoostSettings boost = default, PortalLink portal = default, PickupPointData pickup = default, WallSettings wall = default)
        {
            Coord = coord;
            Type = type;
            Boost = boost;
            Portal = portal;
            Pickup = pickup;
            WallSettings = wall;
        }

        public static HexTile Floor(HexCoord coord) => new HexTile(coord, TileType.Floor);
        public static HexTile Wall(HexCoord coord) => new HexTile(coord, TileType.Wall);
        public static HexTile Wall(HexCoord coord, WallSettings wall) => new HexTile(coord, TileType.Wall, wall: wall);
        public static HexTile BouncyWall(HexCoord coord) => Wall(coord, WallSettings.Bouncy);
        public static HexTile SpikedWall(HexCoord coord, int damage, SpikeDamageMode mode = SpikeDamageMode.Fixed) => Wall(coord, WallSettings.Spiked(damage, mode));
        public static HexTile PickupPoint(HexCoord coord, int spawnGroup = 0) => new HexTile(coord, TileType.PickupPoint, pickup: new PickupPointData(spawnGroup));
        public static HexTile BoostTile(HexCoord coord, BoostSettings boost) => new HexTile(coord, TileType.Boost, boost: boost);
        public static HexTile PortalTile(HexCoord coord, HexCoord target, int rotationSteps = 0) => new HexTile(coord, TileType.Portal, portal: new PortalLink(target, rotationSteps));

        public bool IsWalkable => Type != TileType.Wall;
        public bool BlocksLineOfFire => Type == TileType.Wall;

        public override string ToString() => $"{Type} {Coord}";
    }
}

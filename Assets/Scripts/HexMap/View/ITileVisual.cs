namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Implemented by components on a tile prefab. MapManager calls <see cref="Configure"/> after spawning the prefab
    /// (and again after a layout change) so the visual can set itself up from the tile's data.
    /// Visual only: never change gameplay state from here.
    /// </summary>
    public interface ITileVisual
    {
        void Configure(HexTile tile, HexLayout layout);
    }
}

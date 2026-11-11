using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Shows the same seal on both portals of a pair, so players can see which portals connect.
    /// MapManager calls Configure when the tile is spawned.
    /// </summary>
    public class PortalVisual : MonoBehaviour, ITileVisual
    {
        [SerializeField] private Renderer glyphRenderer;
        [SerializeField] private Sprite[] glyphs;

        private static readonly int GlyphTexId = Shader.PropertyToID("_GlyphTex");
        private static MaterialPropertyBlock _block;

        public void Configure(HexTile tile, HexLayout layout)
        {
            if (tile.Type != TileType.Portal || glyphRenderer == null || glyphs == null || glyphs.Length == 0)
                return;


            var glyph = glyphs[PairIndex(tile.Coord, tile.Portal.Target) % glyphs.Length];

            _block ??= new MaterialPropertyBlock();
            glyphRenderer.GetPropertyBlock(_block);
            _block.SetTexture(GlyphTexId, glyph.texture);
            glyphRenderer.SetPropertyBlock(_block);
        }

        // Same result from either end of the pair, and the same every run.
        private static int PairIndex(HexCoord a, HexCoord b)
        {
            // Sort so A→B and B→A give the same answer.
            if (a.Q > b.Q || (a.Q == b.Q && a.R > b.R))
                (a, b) = (b, a);
            unchecked
            {
                int h = a.Q * 73856093 ^ a.R * 19349663 ^ b.Q * 83492791 ^ b.R * 2971215;
                return h & int.MaxValue;
            }
        }
    }
}

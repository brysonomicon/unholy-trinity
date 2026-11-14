using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Shows the same seal on every portal in a linked network, so players can see which portals connect,
    /// and swaps the swirl floor for a plain exit floor on receive-only exits.
    /// MapManager calls Configure when the tile is spawned.
    /// </summary>
    public class PortalVisual : MonoBehaviour, ITileVisual
    {
        [SerializeField, Tooltip("The billboarded glyph quad using the GlyphSmoke material.")]
        private Renderer glyphRenderer;

        [SerializeField, Tooltip("Each network gets the next seal in this order; seals only repeat once all are used.")]
        private Sprite[] glyphs;

        [SerializeField, Tooltip("The swirl floor. Shown on entrances, hidden on exit-only portals.")]
        private Renderer swirlSurface;

        [SerializeField, Tooltip("Optional floor and effects shown only on exit-only portals.")]
        private GameObject exitFloor;

        private static readonly int GlyphTexId = Shader.PropertyToID("_GlyphTex");
        private static MaterialPropertyBlock _block;

        public void Configure(HexTile tile, HexLayout layout)
        {
            if (tile.Type != TileType.Portal)
            {
                return;
            }

            if (swirlSurface != null)
            {
                swirlSurface.enabled = tile.Portal.IsEntrance;
            }

            if (exitFloor != null)
            {
                exitFloor.SetActive(tile.Portal.ExitOnly);
            }

            if (glyphRenderer == null || glyphs == null || glyphs.Length == 0)
            {
                return;
            }

            // Tile prefabs are spawned under the MapManager, which numbers the portal networks.
            var mapManager = GetComponentInParent<MapManager>();
            if (mapManager == null || !mapManager.TryGetPortalGroup(tile.Coord, out int group))
            {
                return;
            }

            var glyph = glyphs[group % glyphs.Length];
            if (glyph == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            glyphRenderer.GetPropertyBlock(_block);
            _block.SetTexture(GlyphTexId, glyph.texture);
            glyphRenderer.SetPropertyBlock(_block);
        }
    }
}

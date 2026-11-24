using UnityEngine;

namespace UnholyTrinity.Hex
{
    public class HealPoolVisual : MonoBehaviour, ITileVisual
    {
        [SerializeField] private Renderer waterRenderer;

        private static readonly int EdgeMaskId = Shader.PropertyToID("_EdgeMask");
        private static MaterialPropertyBlock _block;

        private const int tileSideCount = HexDirectionExtensions.Count;
        private const float DegreesPerSide = 360f / tileSideCount;

        public void Configure(HexTile tile, HexLayout layout)
        {
            var mapManager = GetComponentInParent<MapManager>();
            if (waterRenderer == null)
            {
                return;
            }

            var style = mapManager.ActiveStyle;
            var selfPrefab = style.PrefabFor(tile);
            int mask = 0;

            for (int d = 0; d < tileSideCount; d++)
            {
                var dir = (HexDirection)d;
                bool sameLook = mapManager.Map.TryGetTile(tile.Coord.Neighbor(dir), out HexTile neighbor) &&
                                style.PrefabFor(neighbor) == selfPrefab;
                if (sameLook)
                {
                    mask |= 1 << LocalEdgeIndex(layout.DirectionToWorld(dir));
                }
            }

            _block ??= new MaterialPropertyBlock();
            waterRenderer.GetPropertyBlock(_block);
            _block.SetFloat(EdgeMaskId, mask);
            waterRenderer.SetPropertyBlock(_block);
        }

        private int LocalEdgeIndex(Vector3 worldDir)
        {
            var local = waterRenderer.transform.InverseTransformDirection(worldDir);
            float deg = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;  
            return (int)Mathf.Repeat(Mathf.RoundToInt(deg / DegreesPerSide), tileSideCount);
        }
    }
}

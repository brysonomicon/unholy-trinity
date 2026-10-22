using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnholyTrinity.Players
{
    public class AimPreview : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float highlightHeight = 0.12f;
        [SerializeField][Range(0.5f, 1f)] private float highlightScale = 0.95f;
        [SerializeField][Range(0f, 1f)] private float tintAlpha = 0.35f;
        [SerializeField] private Color weakFalloffColor = Color.yellow;
        [SerializeField] private Color strongFalloffColor = Color.red;

        private Transform highlightRoot;
        private MaterialPropertyBlock propertyBlock;

        public void Show(List<AimTile> aimTiles, HexLayout layout)
        {
            Hide();

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            highlightRoot = new GameObject($"{name} Aim Preview").transform;

            Mesh hexMesh = HexMeshFactory.Prism(layout.Orientation, layout.Size * highlightScale, 0.001f);

            foreach (AimTile aimTile in aimTiles)
            {
                GameObject highlight = new GameObject($"Aim {aimTile.Coord}");
                highlight.transform.SetParent(highlightRoot, false);
                highlight.transform.position = layout.HexToWorld(aimTile.Coord) +
                                               Vector3.up * highlightHeight;
                highlight.AddComponent<MeshFilter>().sharedMesh = hexMesh;

                MeshRenderer highlightRenderer = highlight.AddComponent<MeshRenderer>();
                highlightRenderer.sharedMaterial = highlightMaterial;
                highlightRenderer.shadowCastingMode = ShadowCastingMode.Off;

                Color falloffColor = Color.Lerp(weakFalloffColor, strongFalloffColor, aimTile.Falloff);
                falloffColor.a = tintAlpha;
                propertyBlock.SetColor(BaseColorId, falloffColor);
                highlightRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        public void Hide()
        {
            if (highlightRoot != null)
            {
                Destroy(highlightRoot.gameObject);
            }
        }
    }
}



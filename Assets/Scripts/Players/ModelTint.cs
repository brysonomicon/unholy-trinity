using UnityEngine;

namespace UnholyTrinity.Players
{
    public class ModelTint : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;

        private void EnsureCached()
        {
            _renderers ??= GetComponentsInChildren<Renderer>();
            _block ??= new MaterialPropertyBlock();
        }

        /// Multiplies the existing material colour. Keeps textures.
        public void SetTint(Color color)
        {
            EnsureCached();
            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                r.SetPropertyBlock(_block);
            }
        }

        /// Swaps every material for one shared ghost material, then tints it.
        /// Use a transparent material so the alpha of 'color' fades the ghost.
        public void MakeGhost(Material ghostMaterial, Color color)
        {
            EnsureCached();
            foreach (Renderer r in _renderers)
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                r.sharedMaterials = mats;
            }
            SetTint(color);
        }
    }
}
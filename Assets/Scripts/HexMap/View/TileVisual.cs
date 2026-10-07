using UnityEngine;
using UnityEngine.Events;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// General-purpose <see cref="ITileVisual"/> for tile prefabs: points a direction marker at a fixed boost's
    /// direction, tints renderers for boost vs. friction, and forwards the boost strength to effects.
    /// Write a custom <see cref="ITileVisual"/> for anything this doesn't cover.
    /// </summary>
    public class TileVisual : MonoBehaviour, ITileVisual
    {
        [SerializeField, Tooltip("Rotated to face a fixed boost's direction (+Z forward). Hidden for other tiles.")]
        Transform directionPivot;

        [SerializeField, Tooltip("Tinted with boostTint / frictionTint on boost tiles via _BaseColor.")]
        Renderer[] tintRenderers = new Renderer[0];
        [SerializeField] bool tintBoostTiles = true;
        [SerializeField] Color boostTint = new Color(0.25f, 0.85f, 0.45f);
        [SerializeField] Color frictionTint = new Color(0.65f, 0.4f, 0.2f);

        [SerializeField, Tooltip("Invoked with the boost strength, e.g. to scale particle emission or animation speed.")]
        UnityEvent<int> onStrength = new UnityEvent<int>();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock propertyBlock;

        public void Configure(HexTile tile, HexLayout layout)
        {
            bool isBoost = tile.Type == TileType.Boost;

            if (directionPivot != null)
            {
                bool directional = isBoost && tile.Boost.Mode == BoostMode.Fixed;
                directionPivot.gameObject.SetActive(directional);
                if (directional)
                    directionPivot.rotation = Quaternion.LookRotation(layout.DirectionToWorld(tile.Boost.Direction), Vector3.up);
            }

            if (isBoost && tintBoostTiles)
                Tint(tile.Boost.Strength >= 0 ? boostTint : frictionTint);

            if (isBoost)
                onStrength.Invoke(tile.Boost.Strength);
        }

        void Tint(Color color)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            foreach (var tintRenderer in tintRenderers)
            {
                if (tintRenderer == null)
                    continue;
                tintRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                tintRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}

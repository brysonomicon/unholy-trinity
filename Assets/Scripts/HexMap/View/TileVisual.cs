using System;
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
        private Transform directionPivot;

        [SerializeField, Tooltip("Tinted with boostTint / frictionTint on boost tiles via _BaseColor.")]
        private Renderer[] tintRenderers = Array.Empty<Renderer>();
        [SerializeField] private bool tintBoostTiles = true;
        [SerializeField] private Color boostTint = HexTileStyle.WongBluishGreen;
        [SerializeField] private Color frictionTint = HexTileStyle.WongOrange;

        [SerializeField, Tooltip("Invoked with the boost strength, e.g. to scale particle emission or animation speed.")]
        private UnityEvent<int> onStrength = new();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock _propertyBlock;

        public void Configure(HexTile tile, HexLayout layout)
        {
            bool isBoost = tile.Type == TileType.Boost;

            if (directionPivot is not null)
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

        private void Tint(Color color)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (var tintRenderer in tintRenderers)
            {
                if (tintRenderer is null)
                    continue;
                tintRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                tintRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}

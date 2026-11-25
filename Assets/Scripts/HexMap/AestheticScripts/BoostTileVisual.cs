using TMPro;
using UnityEngine;

namespace UnholyTrinity.Hex
{
/// <summary>
    /// Boost tile look: turns the chevron decal to the boost direction (Fixed) or to D0 so the six
    /// chevrons line up with the faces (AlongMovement), passes strength and mode to BoostChevrons.shadergraph,
    /// and shows a number when strength is more than the chevrons can show.
    /// </summary>
    public class BoostTileVisual : MonoBehaviour, ITileVisual
    {
        // Must match the ring count in BoostChevrons.hlsl.
        private const int MaxChevrons = 3;

        [SerializeField] private Transform chevronPivot;
        [SerializeField] private Renderer chevronRenderer;
        [SerializeField] private TMP_Text strengthLabel;

        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        // 1 = Unidirectional, 0 = Omnidirectional
        private static readonly int UnidirectionalId = Shader.PropertyToID("_Unidirectional"); 

        // Easier than bool in this case in hlsl
        private static readonly int unidirectionalInt = 1;
        private static readonly int omniInt = 0;
        private static MaterialPropertyBlock _block;
        

        public void Configure(HexTile tile, HexLayout layout)
        {
            if (tile.Type != TileType.Boost)
            {
                return;
            }

            var boost = tile.Boost;
            bool isFixed = boost.Mode == BoostMode.Fixed;

            if (chevronPivot != null)
            {
                var facing = isFixed ? boost.Direction : HexDirection.D0;
                chevronPivot.rotation = Quaternion.LookRotation(layout.DirectionToWorld(facing), Vector3.up);
            }

            if (chevronRenderer != null)
            {
                _block ??= new MaterialPropertyBlock();
                chevronRenderer.GetPropertyBlock(_block);
                _block.SetFloat(StrengthId, boost.Strength);
                _block.SetFloat(UnidirectionalId, isFixed ? unidirectionalInt : omniInt);
                chevronRenderer.SetPropertyBlock(_block);
            }

            if (strengthLabel != null)
            {
                bool showNumber = boost.Strength > MaxChevrons;
                strengthLabel.gameObject.SetActive(showNumber);
                if (showNumber)
                {
                    strengthLabel.text = boost.Strength.ToString();
                }
            }
        }
    }
}

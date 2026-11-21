using UnityEngine;
using UnityEngine.Rendering;

namespace UnholyTrinity.Hex
{
    /// <summary>Swaps a pickup model to a translucent ghost material while it is respawning. Visual only.</summary>
    public class PickupGhost : MonoBehaviour
    {
        private Renderer[] _renderers;
        private Material[][] _originals;
        private Material[][] _ghosts;
        private ShadowCastingMode[] _shadowModes;
        private PickupSpin _spin;

        public void Init(Material ghostMaterial)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originals = new Material[_renderers.Length][];
            _ghosts = new Material[_renderers.Length][];
            _shadowModes = new ShadowCastingMode[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _originals[i] = _renderers[i].sharedMaterials;
                _shadowModes[i] = _renderers[i].shadowCastingMode;

                _ghosts[i] = new Material[_originals[i].Length];
                for (int m = 0; m < _ghosts[i].Length; m++)
                {
                    _ghosts[i][m] = ghostMaterial;
                }
            }

            _spin = GetComponent<PickupSpin>();
        }

        public void SetGhosted(bool ghosted)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = ghosted ? _ghosts[i] : _originals[i];
                _renderers[i].shadowCastingMode = ghosted ? ShadowCastingMode.Off : _shadowModes[i];
            }

            if (_spin != null)
            {
                _spin.enabled = !ghosted;
            }
        }
    }
}

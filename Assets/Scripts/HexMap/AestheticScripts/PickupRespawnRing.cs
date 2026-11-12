using UnityEngine;
using UnholyTrinity.Pickups;


namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Segmented ring that shows progress to pickup respawn.
    /// </summary>
    public class PickupRespawnRing : MonoBehaviour
    {
        [SerializeField] private Renderer ring;
        [SerializeField, Tooltip("Above this many ticks we share segments.")]
        private int maxSegments = 24;
        // private Renderer[] segments = new Renderer[24];
        [SerializeField]
        private Renderer innerRing;
        [SerializeField] private Material innerLitMaterial;
        [SerializeField] private Material innerUnlitMaterial;
        private static readonly int SegmentsId = Shader.PropertyToID("_Segments");
        private static readonly int LitId = Shader.PropertyToID("_Lit");
        private MaterialPropertyBlock mpb;

        private void Awake()
        {
            if (ring == null)
            {
                Debug.LogError($"{nameof(PickupRespawnRing)}: ring not assigned.", this);
                enabled = false;
            }
        }

        public void Show(PickupSpawn spawn)
        {
            int total = spawn.Definition.RespawnTicks;
            int done = spawn.IsAvailable ? total : total - spawn.TicksUntilRespawn;
            int segs = Mathf.Min(total, maxSegments);

            mpb ??= new MaterialPropertyBlock();
            ring.GetPropertyBlock(mpb);
            mpb.SetFloat(SegmentsId, segs);
            mpb.SetFloat(LitId, (float)done / total * segs);
            ring.SetPropertyBlock(mpb);

            if (innerRing != null)
            {
                innerRing.sharedMaterial = spawn.IsAvailable ? innerLitMaterial : innerUnlitMaterial;
            }
        }


    }
}

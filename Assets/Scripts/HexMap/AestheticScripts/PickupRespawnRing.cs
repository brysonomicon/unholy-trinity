using UnityEngine;
using UnholyTrinity.Pickups;


namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Segmented ring that shows progress to pickup respawn.
    /// </summary>
    public class PickupRespawnRing : MonoBehaviour
        {
            [SerializeField, Tooltip("Clockwise from 12.)")]
            private Renderer[] segments = new Renderer[12];
            [SerializeField] private Material litMaterial;
            [SerializeField] private Material unlitMaterial;

            public void Show (PickupSpawn spawn)
        {
            int total = spawn.Definition.RespawnTicks;
            float progress = spawn.IsAvailable ? 1f : (float)(total-spawn.TicksUntilRespawn) / total;
            int lit = Mathf.RoundToInt(progress * segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null)
                {
                    Debug.LogWarning($"PickupRespawnRing {name} has no segment at index {i}");
                    continue;
                }
                segments[i].sharedMaterial = i < lit ? litMaterial : unlitMaterial;
            }
        }


    }
}

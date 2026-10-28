using Unity.Cinemachine;
using UnityEngine;

namespace UnholyTrinity.Players
{
    /// Lives on a child of the player prefab. Just holds this player's two cameras.
    public class PlayerCameraRig : MonoBehaviour
    {
        [field: SerializeField] public CinemachineCamera TopDown { get; private set; }
        [field: SerializeField] public CinemachineCamera CloseThird { get; private set; }

        private void Awake()
        {
            // Point both cameras at the player root so nothing needs wiring after Instantiate.
            Player player = GetComponentInParent<Player>();
            Transform target = player != null ? player.transform : transform.parent;
            Assign(TopDown, target);
            Assign(CloseThird, target);
        }

        private static void Assign(CinemachineCamera cam, Transform target)
        {
            if (cam != null && cam.Target.TrackingTarget == null)
            {
                cam.Target.TrackingTarget = target;
            }
        }
    }
}
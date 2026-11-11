using Unity.Cinemachine;
using UnityEngine;

namespace UnholyTrinity.Players
{
    /// Lives on a child of the player prefab. Holds this player's two cameras.
    /// The owning Player calls SetTarget; the rig doesn't look anything up itself.
    public class PlayerCameraRig : MonoBehaviour
    {
        [field: SerializeField] public CinemachineCamera TopDown { get; private set; }
        [field: SerializeField] public CinemachineCamera CloseThird { get; private set; }

        public void SetTarget(Transform target)
        {
            TopDown.Target.TrackingTarget = target;
            CloseThird.Target.TrackingTarget = target;
        }
    }
}
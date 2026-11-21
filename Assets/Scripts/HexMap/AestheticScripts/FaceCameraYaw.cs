using Mono.Cecil.Cil;
using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Rotate on Y to face camera.
    /// </summary>
    public class FaceCameraYaw : MonoBehaviour
    {
        private Transform _camera;

        private static readonly float XAngle = 90;        
        private static readonly float ZAngle = 0;


        private void LateUpdate()
        {
            if (_camera == null)
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
                _camera = cam.transform;
            }

            transform.rotation = Quaternion.Euler(XAngle, _camera.eulerAngles.y, ZAngle);
        }
    }
}

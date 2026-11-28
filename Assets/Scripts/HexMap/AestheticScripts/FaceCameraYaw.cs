using UnityEngine;

namespace UnholyTrinity.Hex
{
/// <summary>
/// Rotate on Y to face camera.
/// </summary>
public class FaceCameraYaw : MonoBehaviour
{
    private Transform _camera;

    private const float XAngle = 90;
    private const float ZAngle = 0;


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
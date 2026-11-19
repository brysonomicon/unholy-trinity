using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>Keeps this object parallel to the screen, so it reads clearly from any camera angle. Visual only.</summary>
    /// <remarks>Currently unused. Will delete once UI is further developed.</remarks>
    public class Billboard : MonoBehaviour
    {
        private Transform _camera;

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

            transform.rotation = _camera.rotation;
        }
    }
}

using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Spins and gently bobs a pickup's model. On the actual model, not the prefab.
    /// </summary>
    public class PickupSpin : MonoBehaviour
    {
        [SerializeField, Tooltip("Degrees per second around the vertical axis.")]
        private float spinSpeed = 60f;

        [SerializeField, Tooltip("Bob height in world units. 0 disables bobbing.")]
        private float bobHeight = 0.05f;

        [SerializeField, Tooltip("Bob cycles per second.")]
        private float bobSpeed = 0.8f;

        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private float _phase;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            // Random phase so pickups next to each other don't move in sync.
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time + _phase;
            transform.localRotation = Quaternion.Euler(0f, t * spinSpeed, 0f) * _baseLocalRotation;
            transform.localPosition = _baseLocalPosition + Vector3.up * (Mathf.Sin(t * bobSpeed * 2f * Mathf.PI) * bobHeight);
        }
    }
}

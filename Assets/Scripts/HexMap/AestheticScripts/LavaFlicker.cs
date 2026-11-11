using UnityEngine;

namespace UnholyTrinity.Hex
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float amount = 0.2f;
        [SerializeField] private float speed = 1.5f;

        private Light _light;
        private float _baseIntensity;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            var p = transform.position;
            _seed = p.x * 1.37f + p.z * 2.11f;   // varies per tile, same every run
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(_seed, Time.time * speed);   // 0..1
            _light.intensity = _baseIntensity * (1f + (noise - 0.5f) * 2f * amount);
        }
    }
}

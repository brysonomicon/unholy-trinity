using UnityEngine;

namespace UnholyTrinity.Hex
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float amount = 0.2f;
        [SerializeField] private float speed = 1.5f;

        private float xMult = 1.37f;
        private float zMult = 2.11f;

        private Light _light;
        private float _baseIntensity;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            var p = transform.position;
            _seed = p.x * xMult + p.z * zMult;   // varies per tile, deterministic
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(_seed, Time.time * speed);   // 0..1
            // noise is 0..1, we want to map it to -1..1, then multiply by amount, then add 1 to get a multiplier for the base intensity
            _light.intensity = _baseIntensity * (1f + (noise - 0.5f) * 2f * amount);
        }
    }
}

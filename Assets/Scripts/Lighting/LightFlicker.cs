using UnityEngine;

namespace UltimaCinta.Lighting
{
    [DisallowMultipleComponent]
    public sealed class LightFlicker : MonoBehaviour
    {
        private const float MinimumIntensityFactor = 0.55f;
        private const float MaximumIntensityFactor = 1.05f;
        private const float MinimumChangeInterval = 0.08f;
        private const float MaximumChangeInterval = 0.35f;

        [SerializeField] private Light targetLight;
        private float _baseIntensity;
        private float _timeToChange;

        private void Awake()
        {
            if (targetLight == null) targetLight = GetComponent<Light>();
            if (targetLight == null) return;
            _baseIntensity = targetLight.intensity;
            _timeToChange = Random.Range(MinimumChangeInterval, MaximumChangeInterval);
        }

        private void Update()
        {
            if (targetLight == null) return;
            _timeToChange -= Time.deltaTime;
            if (_timeToChange > 0f) return;
            targetLight.intensity = _baseIntensity * Random.Range(MinimumIntensityFactor, MaximumIntensityFactor);
            _timeToChange = Random.Range(MinimumChangeInterval, MaximumChangeInterval);
        }
    }
}

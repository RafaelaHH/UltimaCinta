using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    public sealed class FlashlightController : MonoBehaviour
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Light flashlight;
        private void Awake()
        {
            if (flashlight == null) flashlight = GetComponentInChildren<Light>();
            if (flashlight != null && gameConfig != null) { flashlight.range = gameConfig.FlashlightRange; flashlight.intensity = gameConfig.FlashlightIntensity; }
        }
        public void Toggle() { if (flashlight != null) flashlight.enabled = !flashlight.enabled; }
    }
}

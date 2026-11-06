using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    public sealed class FlashlightController : MonoBehaviour
    {
        private const float FlashlightRange = 18f;
        private const float FlashlightIntensity = 5f;
        private const float FlashlightSpotAngle = 70f;
        private const float FlashlightInnerSpotAngle = 40f;
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Light flashlight;
        private void Awake()
        {
            if (flashlight == null) flashlight = GetComponentInChildren<Light>();
            if (flashlight == null) return;
            if (gameConfig != null)
            {
                flashlight.range = gameConfig.FlashlightRange;
                flashlight.intensity = gameConfig.FlashlightIntensity;
            }
            flashlight.intensity = FlashlightIntensity;
            if (flashlight.type == LightType.Spot)
            {
                flashlight.spotAngle = FlashlightSpotAngle;
                flashlight.innerSpotAngle = FlashlightInnerSpotAngle;
                flashlight.range = FlashlightRange;
            }
            else if (flashlight.type == LightType.Point)
            {
                flashlight.range = FlashlightRange;
            }
            flashlight.shadows = LightShadows.None;
        }
        public void Toggle() { if (flashlight != null) flashlight.enabled = !flashlight.enabled; }
    }
}

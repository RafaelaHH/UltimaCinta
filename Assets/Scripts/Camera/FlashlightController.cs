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
        private UltimaCinta.Player.PlayerInventory _inventory;
        private bool _available;
        private void Awake()
        {
            if (flashlight == null) flashlight = GetComponentInChildren<Light>();
            if (flashlight == null) return;
            _inventory = GetComponent<UltimaCinta.Player.PlayerInventory>();
            if (_inventory != null)
            {
                _inventory.SlotChanged += OnInventoryChanged;
                _available = _inventory.ContainsItem("linterna");
            }
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
            flashlight.enabled = _available;
        }
        private void OnDestroy() { if (_inventory != null) _inventory.SlotChanged -= OnInventoryChanged; }
        private void OnInventoryChanged(int slot, UltimaCinta.Data.InventoryItem item)
        {
            _available = _inventory != null && _inventory.ContainsItem("linterna");
            if (!_available && flashlight != null) flashlight.enabled = false;
        }
        public void Toggle()
        {
            if (flashlight == null) return;
            if (!_available)
            {
                _inventory?.ShowMessage("Recoge la linterna en la camioneta");
                return;
            }
            flashlight.enabled = !flashlight.enabled;
        }
    }
}

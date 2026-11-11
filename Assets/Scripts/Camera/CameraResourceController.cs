using System;
using UltimaCinta.Data;
using UnityEngine;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraResourceController : MonoBehaviour, ICameraResource
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Camera playerCamera;
        private float _batteryMinutes, _tapeMinutes, _zoom = 1f;
        private UltimaCinta.Player.PlayerInventory _inventory;
        private bool _nightVision, _powered;
        public event Action ResourcesChanged;
        public float BatteryNormalized => gameConfig == null || gameConfig.BatteryDurationMinutes <= 0 ? 0 : _batteryMinutes / gameConfig.BatteryDurationMinutes;
        public float TapeNormalized => gameConfig == null || gameConfig.TapeDurationMinutes <= 0 ? 0 : _tapeMinutes / gameConfig.TapeDurationMinutes;
        public float WarningThreshold => gameConfig != null ? gameConfig.CameraLowResourceThreshold : 0.2f;
        public bool IsPowered => _powered;
        public bool NightVisionEnabled => _nightVision;
        private void Awake()
        {
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("GameConfig");
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            _inventory = GetComponent<UltimaCinta.Player.PlayerInventory>();
            if (_inventory != null)
            {
                _powered = _inventory.HasCamcorder;
                _inventory.CamcorderChanged += OnCamcorderChanged;
            }
            if (gameConfig != null) { _batteryMinutes = gameConfig.BatteryDurationMinutes; _tapeMinutes = gameConfig.TapeDurationMinutes; }
            ApplyZoom();
        }
        private void OnDestroy() { if (_inventory != null) _inventory.CamcorderChanged -= OnCamcorderChanged; }
        private void OnCamcorderChanged(bool available) { _powered = available; if (!available) _nightVision = false; ResourcesChanged?.Invoke(); }
        public void ToggleNightVision() { if (!_powered) return; _nightVision = !_nightVision; ResourcesChanged?.Invoke(); }
        public void SetZoom(float inputDelta)
        {
            if (!_powered || gameConfig == null || Mathf.Approximately(inputDelta, 0f)) return;
            _zoom = Mathf.Clamp(_zoom + inputDelta * gameConfig.CameraZoomSensitivity, 1f, gameConfig.CameraMaximumZoom);
            ApplyZoom();
        }
        public void Tick(float deltaTime)
        {
            if (!_powered || gameConfig == null) return;
            var drainMultiplier = _nightVision ? gameConfig.NightVisionBatteryMultiplier : 1f;
            _batteryMinutes = Mathf.Max(0f, _batteryMinutes - deltaTime / 60f * drainMultiplier);
            _tapeMinutes = Mathf.Max(0f, _tapeMinutes - deltaTime / 60f);
            if (_batteryMinutes <= 0f || _tapeMinutes <= 0f) { _powered = false; _nightVision = false; }
            ResourcesChanged?.Invoke();
        }
        public void DebugSetBatteryNormalized(float normalized)
        {
            if (gameConfig == null) return;
            _batteryMinutes = gameConfig.BatteryDurationMinutes * Mathf.Clamp01(normalized);
            ResourcesChanged?.Invoke();
        }
        public void DebugSetTapeNormalized(float normalized)
        {
            if (gameConfig == null) return;
            _tapeMinutes = gameConfig.TapeDurationMinutes * Mathf.Clamp01(normalized);
            ResourcesChanged?.Invoke();
        }
        public void DebugRestoreResources()
        {
            if (gameConfig == null) return;
            _batteryMinutes = gameConfig.BatteryDurationMinutes;
            _tapeMinutes = gameConfig.TapeDurationMinutes;
            _nightVision = false;
            _powered = true;
            ResourcesChanged?.Invoke();
        }
        public void DrainBatteryForGhost(float seconds)
        {
            if (gameConfig == null || seconds <= 0f || !_powered) return;
            _batteryMinutes = Mathf.Max(0f, _batteryMinutes - seconds / 60f);
            if (_batteryMinutes <= 0f) { _powered = false; _nightVision = false; }
            ResourcesChanged?.Invoke();
        }
        public bool RestoreBattery()
        {
            if (gameConfig == null || _inventory == null || !_inventory.HasCamcorder || _tapeMinutes <= 0f ||
                _batteryMinutes >= gameConfig.BatteryDurationMinutes) return false;
            _batteryMinutes = Mathf.Min(gameConfig.BatteryDurationMinutes,
                _batteryMinutes + gameConfig.BatteryDurationMinutes * gameConfig.CameraBatteryRefillFraction);
            _powered = _batteryMinutes > 0f;
            ResourcesChanged?.Invoke();
            return true;
        }
        private void ApplyZoom() { if (playerCamera != null && gameConfig != null) playerCamera.fieldOfView = gameConfig.CameraFieldOfView / _zoom; }
    }
}






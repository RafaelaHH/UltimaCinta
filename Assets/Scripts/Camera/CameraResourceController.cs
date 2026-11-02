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
        private bool _nightVision, _powered = true;
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
            if (gameConfig != null) { _batteryMinutes = gameConfig.BatteryDurationMinutes; _tapeMinutes = gameConfig.TapeDurationMinutes; }
            ApplyZoom();
        }
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
        private void ApplyZoom() { if (playerCamera != null && gameConfig != null) playerCamera.fieldOfView = gameConfig.CameraFieldOfView / _zoom; }
    }
}






using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class FoundFootageEffects : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CameraResourceController resources;
        private ColorAdjustments _color;
        private int _baseCullingMask;
        private int _ghostLayer;
        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (resources == null) resources = GetComponent<CameraResourceController>();
            volume?.profile?.TryGet(out _color);
            _ghostLayer = LayerMask.NameToLayer("Ghost");
            if (playerCamera != null) _baseCullingMask = _ghostLayer >= 0 ? playerCamera.cullingMask & ~(1 << _ghostLayer) : playerCamera.cullingMask;
            if (resources != null) resources.ResourcesChanged += RefreshCameraState;
            RefreshCameraState();
        }
        private void OnDestroy()
        {
            if (resources != null) resources.ResourcesChanged -= RefreshCameraState;
        }
        private void RefreshCameraState()
        {
            if (resources == null) return;
            var nightVision = resources.IsPowered && resources.NightVisionEnabled;
            if (_color != null)
            {
                _color.active = nightVision;
                if (nightVision) _color.colorFilter.value = new Color(0.2f, 1f, 0.3f);
            }
            if (playerCamera != null && _ghostLayer >= 0)
                playerCamera.cullingMask = resources.IsPowered ? _baseCullingMask | (1 << _ghostLayer) : _baseCullingMask;
        }
    }
}

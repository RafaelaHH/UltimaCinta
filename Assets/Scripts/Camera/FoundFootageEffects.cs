using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UltimaCinta.Ghost;
namespace UltimaCinta.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class FoundFootageEffects : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CameraResourceController resources;
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        private ColorAdjustments _color;
        private DepthOfField _depthOfField;
        private SanityController _sanity;
        private int _baseCullingMask;
        private int _ghostLayer;
        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (resources == null) resources = GetComponent<CameraResourceController>();
            _sanity = GetComponent<SanityController>();
            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGet(out _color);
                volume.profile.TryGet(out _depthOfField);
            }
            if (_depthOfField != null)
            {
                _depthOfField.mode.overrideState = true;
                _depthOfField.mode.value = DepthOfFieldMode.Gaussian;
                _depthOfField.gaussianStart.overrideState = true;
                _depthOfField.gaussianEnd.overrideState = true;
                _depthOfField.gaussianMaxRadius.overrideState = true;
                _depthOfField.highQualitySampling.overrideState = false;
                _depthOfField.active = false;
            }
            _ghostLayer = LayerMask.NameToLayer("Ghost");
            if (playerCamera != null) _baseCullingMask = _ghostLayer >= 0 ? playerCamera.cullingMask & ~(1 << _ghostLayer) : playerCamera.cullingMask;
            if (resources != null) resources.ResourcesChanged += RefreshCameraState;
            RefreshCameraState();
        }
        private void Start()
        {
            if (_sanity == null) return;
            _sanity.SanityChanged += RefreshSanityEffects;
            RefreshSanityEffects(_sanity.Value);
        }
        private void OnDestroy()
        {
            if (resources != null) resources.ResourcesChanged -= RefreshCameraState;
            if (_sanity != null) _sanity.SanityChanged -= RefreshSanityEffects;
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

        private void RefreshSanityEffects(float sanity)
        {
            if (_depthOfField == null || _sanity == null) return;
            var config = gameConfig != null ? gameConfig : Resources.Load<UltimaCinta.Data.GameConfig>("GameConfig");
            if (config == null || config.SanityBlurStartThreshold <= 0f) return;
            var blur = 1f - Mathf.Clamp01(sanity / config.SanityBlurStartThreshold);
            _depthOfField.active = blur > 0.001f;
            if (!_depthOfField.active) return;
            _depthOfField.gaussianStart.value = Mathf.Lerp(config.SanityBlurFarEndMeters, config.SanityBlurFarStartMeters, blur);
            _depthOfField.gaussianEnd.value = Mathf.Lerp(config.SanityBlurFarEndMeters * 2f, config.SanityBlurFarEndMeters, blur);
            _depthOfField.gaussianMaxRadius.value = Mathf.Lerp(0.5f, config.SanityBlurMaximumRadius, blur);
        }
    }
}

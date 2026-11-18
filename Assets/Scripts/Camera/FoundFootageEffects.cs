using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UltimaCinta.Ghost;
using UltimaCinta.Player;
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
        private FilmGrain _filmGrain;
        private ChromaticAberration _chromaticAberration;
        private LensDistortion _lensDistortion;
        private SanityController _sanity;
        private GhostCameraInterference _interference;
        private NetworkPlayerAvatar _networkAvatar;
        private int _baseCullingMask;
        private int _ghostLayer;
        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (volume == null) volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.enabled = false;
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (resources == null) resources = GetComponent<CameraResourceController>();
            _networkAvatar = GetComponent<NetworkPlayerAvatar>();
            _sanity = GetComponent<SanityController>();
            _interference = GetComponent<GhostCameraInterference>();
            if (volume != null)
            {
                var runtimeProfile = volume.profile;
                if (runtimeProfile == null)
                {
                    runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
                    volume.profile = runtimeProfile;
                }
                if (runtimeProfile != null)
                {
                    if (!runtimeProfile.TryGet(out _color)) _color = runtimeProfile.Add<ColorAdjustments>(true);
                    if (!runtimeProfile.TryGet(out _depthOfField)) _depthOfField = runtimeProfile.Add<DepthOfField>(true);
                    if (!runtimeProfile.TryGet(out _filmGrain)) _filmGrain = runtimeProfile.Add<FilmGrain>(true);
                    if (!runtimeProfile.TryGet(out _chromaticAberration)) _chromaticAberration = runtimeProfile.Add<ChromaticAberration>(true);
                    if (!runtimeProfile.TryGet(out _lensDistortion)) _lensDistortion = runtimeProfile.Add<LensDistortion>(true);
                    _color.colorFilter.overrideState = true;
                }
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
            RefreshInterferenceEffects();
        }
        private void Start()
        {
            if (_networkAvatar == null || !_networkAvatar.IsNetworkStarted) ConfigureRole(true);
            else ConfigureRole(_networkAvatar.IsLocalPlayer);
            if (_sanity == null) return;
            _sanity.SanityChanged += RefreshSanityEffects;
            RefreshSanityEffects(_sanity.Value);
        }
        private void OnDestroy()
        {
            if (resources != null) resources.ResourcesChanged -= RefreshCameraState;
            if (_sanity != null) _sanity.SanityChanged -= RefreshSanityEffects;
        }

        public void ConfigureRole(bool isLocalPlayer)
        {
            if (volume != null) volume.enabled = isLocalPlayer;
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

        private void Update() => RefreshInterferenceEffects();

        private void RefreshInterferenceEffects()
        {
            var config = gameConfig != null ? gameConfig : Resources.Load<UltimaCinta.Data.GameConfig>("GameConfig");
            var amount = _interference != null && (resources == null || resources.IsPowered)
                ? Mathf.Clamp01(_interference.InterferenceAmount) : 0f;
            var tuning = config != null ? config.Audio : null;
            if (tuning == null) amount = 0f;

            if (_filmGrain != null)
            {
                _filmGrain.active = amount > 0.001f;
                _filmGrain.intensity.value = amount * (tuning != null ? tuning.MaximumFilmGrain : 0f);
            }
            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = amount > 0.001f;
                _chromaticAberration.intensity.value = amount * (tuning != null ? tuning.MaximumChromaticAberration : 0f);
            }
            if (_lensDistortion != null)
            {
                _lensDistortion.active = amount > 0.001f;
                _lensDistortion.intensity.value = amount * (tuning != null ? tuning.MaximumLensDistortion : 0f);
            }
        }
    }
}

using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostCameraInterference : MonoBehaviour
    {
        private const int ScreamerSampleRate = 22050;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        private GhostAgent _ghost;
        private UltimaCinta.CameraSystem.CameraResourceController _cameraResource;
        private GameObject _screamer;
        private AudioSource _audioSource;
        private AudioClip _screamerClip;
        private float _screamerTimer;
        private float _screamerStartTime;
        private float _screamerBaseFov;
        private bool _scaredThisHunt;
        private float _scanTimer;
        private float _amount;

        public float InterferenceAmount => _amount;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            _cameraResource = GetComponent<UltimaCinta.CameraSystem.CameraResourceController>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _screamerClip = CreateScreamerClip(gameConfig != null ? gameConfig.GhostScreamerDurationSeconds : 0.9f);
        }

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                var found = FindFirstObjectByType<GhostAgent>();
                if (found != _ghost)
                {
                    if (_ghost != null) _ghost.StateChanged -= OnGhostStateChanged;
                    _ghost = found;
                    if (_ghost != null) _ghost.StateChanged += OnGhostStateChanged;
                }
                _scanTimer = gameConfig != null ? gameConfig.GhostTargetRefreshSeconds : 0.5f;
            }

            _amount = _ghost == null ? 0f : _ghost.InterferenceAt(transform.position);
            if (_ghost != null && _ghost.State == GhostState.Hunting)
                TryShowScreamer(_ghost);

            if (_screamerTimer <= 0f) return;
            _screamerTimer = Mathf.Max(0f, _screamerTimer - Time.deltaTime);
            if (playerCamera != null && gameConfig != null)
            {
                var progress = 1f - _screamerTimer / gameConfig.GhostScreamerDurationSeconds;
                var kick = Mathf.Sin(progress * Mathf.PI) * gameConfig.GhostScreamerFovKickDegrees;
                playerCamera.fieldOfView = _screamerBaseFov - kick;
            }
            if (_screamerTimer <= 0f)
            {
                if (playerCamera != null) playerCamera.fieldOfView = _screamerBaseFov;
                if (_screamer != null) Destroy(_screamer);
                _screamer = null;
            }
        }

        private void OnGhostStateChanged(GhostAgent ghost, GhostState state)
        {
            if (state == GhostState.Resting)
            {
                _scaredThisHunt = false;
                return;
            }
            if (state == GhostState.Hunting) TryShowScreamer(ghost);
        }

        private void TryShowScreamer(GhostAgent ghost)
        {
            if (_scaredThisHunt || ghost == null || playerCamera == null || gameConfig == null ||
                !_playerCameraPowered() ||
                Vector3.Distance(transform.position, ghost.transform.position) > gameConfig.GhostScreamerRangeMeters)
                return;

            var prefab = ghost.Type != null ? ghost.Type.GhostPrefab : null;
            if (prefab == null) return;
            if (_screamer != null) Destroy(_screamer);
            _screamer = Instantiate(prefab, playerCamera.transform);
            _screamer.name = "Aparición de cacería";
            _screamer.transform.localPosition = Vector3.forward * 0.75f;
            _screamer.transform.localRotation = Quaternion.identity;
            _screamer.transform.localScale = Vector3.one * 0.9f;
            foreach (var navigation in _screamer.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
                navigation.enabled = false;

            _screamerStartTime = Time.time;
            _screamerTimer = gameConfig.GhostScreamerDurationSeconds;
            _screamerBaseFov = playerCamera.fieldOfView;
            if (_screamerClip != null)
                _audioSource.PlayOneShot(_screamerClip, gameConfig.GhostScreamerAudioVolume);
            _scaredThisHunt = true;
        }

        private static AudioClip CreateScreamerClip(float duration)
        {
            duration = Mathf.Max(0.1f, duration);
            var sampleCount = Mathf.CeilToInt(ScreamerSampleRate * duration);
            var samples = new float[sampleCount];
            var noise = 0f;
            var phase = 0f;
            for (var index = 0; index < sampleCount; index++)
            {
                var progress = (float)index / sampleCount;
                var envelope = Mathf.Sin(progress * Mathf.PI);
                var frequency = Mathf.Lerp(150f, 920f, progress) + Mathf.Sin(progress * Mathf.PI * 9f) * 90f;
                phase += frequency * (2f * Mathf.PI / ScreamerSampleRate);
                noise = noise * 0.82f + (Random.value * 2f - 1f) * 0.18f;
                var growl = Mathf.Sin(phase) * 0.42f + Mathf.Sin(phase * 2.03f) * 0.24f;
                var shriek = Mathf.Sin(phase * 3.1f + Mathf.Sin(phase * 0.17f)) * 0.2f;
                var impact = progress < 0.09f ? (Random.value * 2f - 1f) * (1f - progress / 0.09f) * 0.42f : 0f;
                samples[index] = Mathf.Clamp((growl + shriek + noise * 0.34f + impact) * envelope, -1f, 1f);
            }

            var clip = AudioClip.Create("Aullido de aparición", sampleCount, 1, ScreamerSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDisable()
        {
            if (_ghost != null) _ghost.StateChanged -= OnGhostStateChanged;
            if (_screamer != null) Destroy(_screamer);
            if (playerCamera != null && _screamerTimer > 0f) playerCamera.fieldOfView = _screamerBaseFov;
        }

        private void OnDestroy()
        {
            if (_screamerClip != null) Destroy(_screamerClip);
        }

        private void OnGUI()
        {
            if (playerCamera == null || !_playerCameraPowered()) return;
            if (_amount > 0.01f)
            {
                var alpha = Mathf.Clamp01(_amount * 0.34f);
                GUI.color = new Color(0.82f, 0.9f, 1f, alpha * (0.6f + Mathf.PingPong(Time.unscaledTime * 4f, 0.4f)));
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            if (_screamerTimer > 0f && gameConfig != null)
            {
                var progress = Mathf.Clamp01((Time.time - _screamerStartTime) / gameConfig.GhostScreamerDurationSeconds);
                var flash = Mathf.Sin(progress * Mathf.PI) * 0.22f;
                GUI.color = new Color(0.35f, 0.015f, 0.025f, flash);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private bool _playerCameraPowered()
        {
            return _cameraResource == null || _cameraResource.IsPowered;
        }
    }
}

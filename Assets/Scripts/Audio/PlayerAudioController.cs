using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.Player;
using UltimaCinta.UI;
using UnityEngine;

namespace UltimaCinta.Audio
{
    [DisallowMultipleComponent]
    public sealed class PlayerAudioController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private SanityController sanityController;
        [SerializeField] private NetworkPlayerAvatar networkAvatar;

        private CharacterController _characterController;
        private GhostWorldController _world;
        private AudioSource _footstepSource;
        private AudioSource _heartbeatSource;
        private AudioSource _ambientSource;
        private bool _isLocalPlayer;
        private bool _networkRoleKnown;
        private bool _ambientStarted;
        private float _nextFootstepTime;
        private float _nextHeartbeatTime;
        private int _footstepIndex;

        public static PlayerAudioController LocalInstance { get; private set; }

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (sanityController == null) sanityController = GetComponent<SanityController>();
            if (networkAvatar == null) networkAvatar = GetComponent<NetworkPlayerAvatar>();
            _characterController = GetComponent<CharacterController>();
            _footstepSource = CreateSource("Audio 3D del investigador", 1f);
            _heartbeatSource = CreateSource("Latidos del investigador", 0f);
            _ambientSource = CreateSource("Ambiente interior del investigador", 0f);
            _ambientSource.loop = true;
            _ambientSource.clip = AudioCueLibrary.RoomTone();
            ApplyDistanceSettings();
        }

        private void Start()
        {
            if (networkAvatar == null || !networkAvatar.IsNetworkStarted) ConfigureRole(isLocalPlayer: true);
            GameSettingsStore.Apply(gameConfig);
            _world = FindAnyObjectByType<GhostWorldController>();
        }

        private void Update()
        {
            if (!_networkRoleKnown) return;
            UpdateFootsteps();
            if (_isLocalPlayer) UpdateFearAudio();
        }

        public void ConfigureRole(bool isLocalPlayer)
        {
            _networkRoleKnown = true;
            _isLocalPlayer = isLocalPlayer;
            if (isLocalPlayer)
            {
                LocalInstance = this;
                GameSettingsStore.Apply(gameConfig);
                StartAmbientBed();
            }
            else
            {
                StopAmbientBed();
                if (LocalInstance == this) LocalInstance = null;
            }

            GetComponent<SubtitleOverlay>()?.SetLocalRole(isLocalPlayer);
        }

        public void PlayPickupCue()
        {
            if (_isLocalPlayer && _heartbeatSource != null && gameConfig != null)
                _heartbeatSource.PlayOneShot(AudioCueLibrary.Pickup(), gameConfig.Audio.PickupVolume);
        }

        public void PlayMilestoneCue()
        {
            if (_isLocalPlayer && _heartbeatSource != null && gameConfig != null)
                _heartbeatSource.PlayOneShot(AudioCueLibrary.Milestone(), gameConfig.Audio.MilestoneVolume);
        }

        public void PlayUiCue()
        {
            if (_isLocalPlayer && _heartbeatSource != null && gameConfig != null)
                _heartbeatSource.PlayOneShot(AudioCueLibrary.UiClick(), gameConfig.Audio.UiVolume);
        }

        private void UpdateFootsteps()
        {
            var movementState = networkAvatar != null && networkAvatar.IsNetworkStarted
                ? networkAvatar.MovementState
                : inputReader != null && inputReader.Move.sqrMagnitude > 0.01f
                    ? (byte)(inputReader.SprintHeld ? 2 : inputReader.CrouchHeld ? 3 : 1)
                    : (byte)0;
            if (movementState == 0 || (_characterController != null && !_characterController.isGrounded))
            {
                _nextFootstepTime = 0f;
                return;
            }

            if (Time.time < _nextFootstepTime || gameConfig == null) return;
            var interval = movementState == 2 ? gameConfig.Audio.SprintFootstepIntervalSeconds : gameConfig.Audio.FootstepIntervalSeconds;
            _nextFootstepTime = Time.time + interval;
            _footstepSource.PlayOneShot(AudioCueLibrary.Footstep(_footstepIndex++), gameConfig.Audio.FootstepVolume);
        }

        private void UpdateFearAudio()
        {
            if (gameConfig == null || sanityController == null || sanityController.IsDead) return;
            var threshold = gameConfig.Audio.HeartbeatSanityThreshold;
            var sanity = sanityController.Value;
            if (Time.time < _nextHeartbeatTime) return;

            var distress = 1f - Mathf.Clamp01(sanity / Mathf.Max(0.01f, threshold));
            var ghost = _world != null ? _world.Agent : null;
            if (ghost != null) distress = Mathf.Max(distress, 1f - Mathf.Clamp01(
                Vector3.Distance(transform.position, ghost.transform.position) / gameConfig.Atmosphere.HeartbeatRange));
            if (distress <= 0f) return;
            var interval = Mathf.Lerp(gameConfig.Audio.HeartbeatIntervalAtThresholdSeconds,
                gameConfig.Audio.HeartbeatIntervalAtZeroSeconds, distress);
            _nextHeartbeatTime = Time.time + interval;
            _heartbeatSource.PlayOneShot(AudioCueLibrary.Heartbeat(), gameConfig.Audio.HeartbeatVolume * distress);
        }

        private void StartAmbientBed()
        {
            if (_ambientStarted || _ambientSource == null || gameConfig == null) return;
            _ambientSource.volume = gameConfig.Audio.RoomToneVolume;
            _ambientSource.Play();
            _ambientStarted = true;
        }

        private void StopAmbientBed()
        {
            if (_ambientSource != null) _ambientSource.Stop();
            _ambientStarted = false;
        }

        private void ApplyDistanceSettings()
        {
            if (gameConfig == null) return;
            _footstepSource.minDistance = gameConfig.Audio.FootstepMinDistanceMeters;
            _footstepSource.maxDistance = gameConfig.Audio.FootstepMaxDistanceMeters;
            _footstepSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private AudioSource CreateSource(string objectName, float spatialBlend)
        {
            var sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private void OnDestroy()
        {
            if (LocalInstance == this) LocalInstance = null;
            if (_ambientSource != null) _ambientSource.Stop();
        }
    }
}

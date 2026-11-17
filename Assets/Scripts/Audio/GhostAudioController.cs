using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.UI;
using UnityEngine;

namespace UltimaCinta.Audio
{
    [DisallowMultipleComponent]
    public sealed class GhostAudioController : MonoBehaviour
    {
        [SerializeField] private GhostAgent ghostAgent;
        [SerializeField] private GameConfig gameConfig;
        private AudioSource _source;
        private float _nextHuntCueTime;

        private void Awake()
        {
            if (ghostAgent == null) ghostAgent = GetComponent<GhostAgent>();
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1f;
            _source.dopplerLevel = 0f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            if (ghostAgent != null) ghostAgent.StateChanged += OnStateChanged;
        }

        private void Start()
        {
            if (ghostAgent != null && ghostAgent.IsInitialized) PlayCue(ghostAgent.State);
            ApplyDistanceSettings();
        }

        private void Update()
        {
            if (ghostAgent == null || gameConfig == null || ghostAgent.State != GhostState.Hunting || Time.time < _nextHuntCueTime) return;
            _nextHuntCueTime = Time.time + gameConfig.Audio.GhostHuntCueIntervalSeconds;
            PlayCue(GhostState.Hunting, repeat: true);
        }

        private void OnStateChanged(GhostAgent agent, GhostState state)
        {
            if (agent != ghostAgent) return;
            PlayCue(state);
            if (state == GhostState.Hunting && gameConfig != null)
                _nextHuntCueTime = Time.time + gameConfig.Audio.GhostHuntCueIntervalSeconds;
        }

        private void PlayCue(GhostState state, bool repeat = false)
        {
            if (ghostAgent == null || ghostAgent.Type == null || gameConfig == null || _source == null) return;
            var profile = ghostAgent.Type.SoundProfile;
            _source.PlayOneShot(AudioCueLibrary.GhostCue(profile, state),
                gameConfig.Audio.GhostVolume * (repeat ? 0.72f : 1f));
            if (state != GhostState.Wandering)
                SubtitleOverlay.ShowAt(transform.position, gameConfig.Audio.GhostMaxDistanceMeters, SubtitleFor(state));
        }

        private void ApplyDistanceSettings()
        {
            if (gameConfig == null || _source == null) return;
            _source.minDistance = gameConfig.Audio.GhostMinDistanceMeters;
            _source.maxDistance = gameConfig.Audio.GhostMaxDistanceMeters;
        }

        private static string SubtitleFor(GhostState state) => state switch
        {
            GhostState.Activity => "Un sonido áspero resuena en la habitación.",
            GhostState.Hunting => "El fantasma ruge y empieza a cazar.",
            GhostState.Resting => "El sonido del fantasma se aleja.",
            _ => ""
        };

        private void OnDestroy()
        {
            if (ghostAgent != null) ghostAgent.StateChanged -= OnStateChanged;
        }
    }
}

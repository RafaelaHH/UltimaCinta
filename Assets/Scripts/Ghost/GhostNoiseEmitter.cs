using UnityEngine;
using UltimaCinta.Data;
using UltimaCinta.Player;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        private PlayerInputReader _input;
        private float _timer;
        private float _ghostRefreshTimer;
        private GhostAgent[] _ghosts = System.Array.Empty<GhostAgent>();
        private UltimaCinta.Interaction.HideoutInteractable[] _hideouts = System.Array.Empty<UltimaCinta.Interaction.HideoutInteractable>();
        private void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("GameConfig");
            _ghosts = FindObjectsByType<GhostAgent>(FindObjectsSortMode.None);
            _hideouts = FindObjectsByType<UltimaCinta.Interaction.HideoutInteractable>(FindObjectsSortMode.None);
        }
        private void Update()
        {
            if (_input == null || gameConfig == null || _input.Move.sqrMagnitude < 0.01f) return;
            _ghostRefreshTimer -= Time.deltaTime;
            if (_ghostRefreshTimer <= 0f)
            {
                _ghosts = FindObjectsByType<GhostAgent>(FindObjectsSortMode.None);
                _hideouts = FindObjectsByType<UltimaCinta.Interaction.HideoutInteractable>(FindObjectsSortMode.None);
                _ghostRefreshTimer = gameConfig.GhostTargetRefreshSeconds;
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = gameConfig.PlayerNoiseIntervalSeconds;
            var loudness = _input.CrouchHeld ? 0f : _input.SprintHeld ? gameConfig.PlayerRunNoiseLoudness : gameConfig.PlayerWalkNoiseLoudness;
            if (loudness <= 0f) return;
            foreach (var hideout in _hideouts)
                if (hideout != null && hideout.IsOccupant(gameObject)) return;
            foreach (var ghost in _ghosts) if (ghost != null) ghost.HearNoise(transform.position, loudness);
        }
    }
}

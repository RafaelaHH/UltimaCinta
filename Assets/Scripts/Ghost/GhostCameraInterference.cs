using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostCameraInterference : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        private GhostAgent _ghost;
        private GameObject _screamer;
        private float _screamerTimer;
        private bool _scaredThisHunt;
        private float _scanTimer;
        private float _amount;
        private void Awake() { if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(); }
        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_screamerTimer > 0f)
            {
                _screamerTimer -= Time.deltaTime;
                if (_screamerTimer <= 0f && _screamer != null) Destroy(_screamer);
            }
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
        }

        private void OnGhostStateChanged(GhostAgent ghost, GhostState state)
        {
            if (state != GhostState.Hunting)
            {
                if (state == GhostState.Resting) _scaredThisHunt = false;
                return;
            }
            if (_scaredThisHunt || ghost == null || playerCamera == null || gameConfig == null ||
                !_playerCameraPowered() || Vector3.Distance(transform.position, ghost.transform.position) > gameConfig.GhostScreamerRangeMeters)
                return;

            var prefab = ghost.Type != null ? ghost.Type.GhostPrefab : null;
            if (prefab == null) return;
            if (_screamer != null) Destroy(_screamer);
            _screamer = Instantiate(prefab, playerCamera.transform);
            _screamer.name = "Aparición de cacería";
            _screamer.transform.localPosition = Vector3.forward * 0.75f;
            _screamer.transform.localRotation = Quaternion.identity;
            _screamer.transform.localScale = Vector3.one * 0.9f;
            var navigation = _screamer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navigation != null) navigation.enabled = false;
            _screamerTimer = gameConfig.GhostScreamerDurationSeconds;
            _scaredThisHunt = true;
        }

        private void OnDisable()
        {
            if (_ghost != null) _ghost.StateChanged -= OnGhostStateChanged;
            if (_screamer != null) Destroy(_screamer);
        }
        private void OnGUI()
        {
            if (playerCamera == null || !_playerCameraPowered() || _amount <= 0.01f) return;
            var alpha = Mathf.Clamp01(_amount * 0.34f);
            GUI.color = new Color(0.82f, 0.9f, 1f, alpha * (0.6f + Mathf.PingPong(Time.unscaledTime * 4f, 0.4f)));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        private bool _playerCameraPowered()
        {
            var resource = GetComponent<UltimaCinta.CameraSystem.CameraResourceController>();
            return resource == null || resource.IsPowered;
        }
    }
}

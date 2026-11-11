using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostModelMotion : MonoBehaviour
    {
        [SerializeField] private float floatAmplitude = 0.12f;
        [SerializeField] private float floatCyclesPerSecond = 0.7f;
        [SerializeField] private float crawlHeight = 0.45f;
        [SerializeField] private float blinkIntervalSeconds = 8f;
        [SerializeField] private float blinkDurationSeconds = 0.12f;
        private GhostAgent _agent;
        private Renderer[] _renderers;
        private Transform _visualRoot;
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _baseScale;
        private float _blinkTimer;
        private float _blinkRemaining;

        private void Awake()
        {
            _agent = GetComponent<GhostAgent>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in _renderers)
                if (renderer != null && renderer.transform != transform) { _visualRoot = renderer.transform; break; }
            _visualRoot = _visualRoot != null ? _visualRoot : transform;
            _basePosition = _visualRoot.localPosition;
            _baseRotation = _visualRoot.localRotation;
            _baseScale = _visualRoot.localScale;
        }

        private void Update()
        {
            var crawling = _agent != null && _agent.Type != null && (_agent.Type.Behavior & GhostBehavior.Crawls) != 0;
            var y = Mathf.Sin(Time.time * floatCyclesPerSecond * Mathf.PI * 2f) * floatAmplitude;
            if (_visualRoot != transform)
                _visualRoot.localPosition = _basePosition + Vector3.up * (y - (crawling ? crawlHeight : 0f));
            _visualRoot.localRotation = _baseRotation * Quaternion.Euler(0f, Mathf.Sin(Time.time * floatCyclesPerSecond) * 3f, 0f);
            _visualRoot.localScale = Vector3.Scale(_baseScale, crawling ? new Vector3(1f, 0.55f, 1f) : Vector3.one);

            _blinkTimer += Time.deltaTime;
            _blinkRemaining = Mathf.Max(0f, _blinkRemaining - Time.deltaTime);
            if (_blinkTimer >= blinkIntervalSeconds)
            {
                _blinkTimer = 0f;
                _blinkRemaining = blinkDurationSeconds;
            }
            var visible = _blinkRemaining <= 0f;
            foreach (var renderer in _renderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}

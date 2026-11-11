using System.Linq;
using UnityEngine;

namespace UltimaCinta.Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostOrbField : MonoBehaviour
    {
        [SerializeField, Min(1)] private int orbCount = 4;
        [SerializeField, Min(0.01f)] private float orbitRadius = 1.1f;
        [SerializeField, Min(0.01f)] private float orbitSpeed = 0.45f;
        [SerializeField, Min(0.01f)] private float orbSize = 0.07f;
        private Transform[] _orbs;
        private void Start()
        {
            var agent = GetComponent<GhostAgent>();
            if (agent == null || agent.Type == null || !agent.Type.Evidences.Contains(GhostEvidence.Orbs)) return;
            _orbs = new Transform[orbCount];
            for (var index = 0; index < _orbs.Length; index++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "EvidenceOrb";
                orb.transform.SetParent(transform, false);
                orb.transform.localScale = Vector3.one * orbSize;
                var collider = orb.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                var renderer = orb.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                if (shader != null) renderer.material = new Material(shader) { color = new Color(0.6f, 0.85f, 1f, 0.8f) };
                orb.layer = gameObject.layer;
                _orbs[index] = orb.transform;
            }
        }
        private void Update()
        {
            if (_orbs == null) return;
            for (var index = 0; index < _orbs.Length; index++)
            {
                var angle = Time.time * orbitSpeed + Mathf.PI * 2f * index / _orbs.Length;
                _orbs[index].localPosition = new Vector3(Mathf.Cos(angle), 0.6f + Mathf.Sin(angle * 1.7f) * 0.3f, Mathf.Sin(angle)) * orbitRadius;
            }
        }
        private void OnDestroy()
        {
            if (_orbs == null) return;
            foreach (var orb in _orbs)
                if (orb != null) Destroy(orb.gameObject);
        }
    }
}

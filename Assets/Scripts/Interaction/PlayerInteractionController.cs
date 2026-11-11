using System;
using System.Collections.Generic;
using UnityEngine;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Transform view;
        [SerializeField] private UltimaCinta.Player.PlayerInputReader input;
        [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;
        public event Action<string> PromptChanged;
        private string _prompt;
        private Collider lastTargetCollider;
        private IInteractable lastInteractable;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[32];
        private static readonly IComparer<RaycastHit> HitDistanceComparer = new RaycastHitDistanceComparer();
        private void OnGUI() { if (!string.IsNullOrEmpty(_prompt)) GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.72f, 240f, 28f), _prompt); }
        public float InteractionDistance => gameConfig != null ? gameConfig.InteractionReachMeters : 0f;

        private void Awake()
        {
            if (view == null) view = GetComponentInChildren<Camera>()?.transform;
            if (input == null) input = GetComponent<UltimaCinta.Player.PlayerInputReader>();
            if (gameConfig == null) Debug.LogError($"PlayerInteractionController {name}: falta GameConfig; no hay distancia de interacción.", this);
            if (view == null) Debug.LogError($"PlayerInteractionController {name}: no encuentra la cámara de interacción.", this);
            Debug.Log($"[DIAG] interacción: cámara={(view != null ? view.name : "FALTA")}, " +
                      $"distancia={InteractionDistance:0.00} m, LayerMask={interactionLayers.value}.", this);
        }
        private void Update()
        {
            IInteractable target = null;
            Collider targetCollider = null;
            if (gameConfig != null && view != null)
            {
                var hitCount = Physics.RaycastNonAlloc(view.position, view.forward, _hitBuffer, gameConfig.InteractionReachMeters,
                    interactionLayers, QueryTriggerInteraction.Collide);
                Array.Sort(_hitBuffer, 0, hitCount, HitDistanceComparer);
                for (var index = 0; index < hitCount; index++)
                {
                    var hit = _hitBuffer[index];
                    var collider = hit.collider;
                    if (collider == null || collider.transform == transform || collider.transform.IsChildOf(transform)) continue;
                    var candidate = collider.GetComponentInParent<IInteractable>();
                    if (candidate != null)
                    {
                        target = candidate;
                        targetCollider = collider;
                        break;
                    }
                    if (IsStructuralCollider(collider))
                    {
                        if (IsFloorOrCeiling(collider)) continue;
                        targetCollider = collider;
                        break;
                    }
                }
            }

            if (targetCollider != lastTargetCollider || !ReferenceEquals(target, lastInteractable))
            {
                Debug.Log($"Objetivo apuntado: {targetCollider?.name ?? "ninguno"}; interactuable={target != null} " +
                          $"({target?.GetType().Name ?? "NULL"}).", targetCollider);
                lastTargetCollider = targetCollider;
                lastInteractable = target;
            }

            var prompt = target is FuseBoxInteractable fuseBox ? fuseBox.GetPrompt(gameObject) : target?.Prompt;
            if (prompt != _prompt) { _prompt = prompt; PromptChanged?.Invoke(_prompt); }
            if (input != null && input.InteractPressed)
            {
                if (target != null) target.Interact(gameObject);
                else Debug.Log("Interacción: pulsé E sin un objetivo interactuable.", this);
            }
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit left, RaycastHit right) => left.distance.CompareTo(right.distance);
        }

        private static bool IsStructuralCollider(Collider collider)
        {
            var objectName = collider.name.ToLowerInvariant();
            return objectName.Contains("wall") || objectName.Contains("muro") || objectName.Contains("doorframe") ||
                   objectName.Contains("doorlintel") || objectName.Contains("doorjamb") ||
                   objectName.Contains("doorframeheader") ||
                   objectName.Contains("jamba") || objectName.Contains("doorleaf") || objectName.Contains("hinge") ||
                   objectName.Contains("floor") || objectName.Contains("suelo") || objectName.Contains("ceiling") ||
                   objectName.Contains("techo") || objectName.Contains("roof") || objectName.Contains("ramp") ||
                   objectName.Contains("rampa") || objectName.Contains("rail") || objectName.Contains("baranda") ||
                   objectName.Contains("pasamanos") || objectName.Contains("step") || objectName.Contains("escalon") ||
                   objectName.Contains("huella") || objectName.Contains("contrahuella") || objectName.Contains("pozo") ||
                   objectName.Contains("stair") || objectName.Contains("escalera");
        }

        private static bool IsFloorOrCeiling(Collider collider)
        {
            var objectName = collider.name.ToLowerInvariant();
            return objectName.Contains("floor") || objectName.Contains("suelo") || objectName.Contains("ceiling") ||
                   objectName.Contains("techo") || objectName.Contains("roof");
        }
    }
}

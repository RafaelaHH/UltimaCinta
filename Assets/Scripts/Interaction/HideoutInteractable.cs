using System.Collections.Generic;
using UnityEngine;
using UltimaCinta.Player;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class HideoutInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool underBed;
        [SerializeField] private string hideoutType = "armario";
        [SerializeField] private Vector2 roomMin;
        [SerializeField] private Vector2 roomMax;
        [SerializeField] private bool hasRoomBounds;

        private GameObject occupant;
        private PlayerInputReader input;
        private CharacterController characterController;
        private FirstPersonMovementController movement;
        private FirstPersonLookController look;
        private PlayerInteractionController interaction;
        private Camera playerCamera;
        private Quaternion originalRotation;
        private Vector3 originalCameraLocalPosition;
        private bool characterControllerWasEnabled;
        private bool movementWasEnabled;
        private bool lookWasEnabled;
        private bool interactionWasEnabled;
        private readonly List<(Renderer Renderer, bool Enabled)> occupantRenderers = new List<(Renderer, bool)>();
        private readonly List<(Renderer Renderer, bool Enabled)> hideoutRenderers = new List<(Renderer, bool)>();
        private Bounds hideoutBounds;
        private int enteredFrame;

        public string Prompt => "E — esconderse";

        public void SetUnderBed(bool value)
        {
            underBed = value;
            if (underBed) hideoutType = "cama";
        }

        public void SetHideoutType(string value)
        {
            hideoutType = string.IsNullOrWhiteSpace(value) ? "otro" : value;
        }

        public void SetRoomBounds(Vector2 min, Vector2 max)
        {
            roomMin = min;
            roomMax = max;
            hasRoomBounds = true;
        }

        public bool Interact(GameObject interactor)
        {
            if (occupant != null || interactor == null)
            {
                LogInteraction(false);
                return false;
            }

            var interactionCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);
            if (interactionCollider == null || !interactionCollider.enabled)
            {
                Debug.LogError($"Escondite {name}: no encuentra su collider de interacción.", this);
                LogInteraction(false);
                return false;
            }
            if (!TryGetRendererBounds(out hideoutBounds))
            {
                Debug.LogError($"Escondite {name}: no encuentra su punto de ocultación.", this);
                LogInteraction(false);
                return false;
            }

            input = interactor.GetComponent<PlayerInputReader>();
            if (input == null)
            {
                LogInteraction(false);
                return false;
            }
            characterController = interactor.GetComponent<CharacterController>();
            movement = interactor.GetComponent<FirstPersonMovementController>();
            look = interactor.GetComponent<FirstPersonLookController>();
            interaction = interactor.GetComponent<PlayerInteractionController>();
            playerCamera = interactor.GetComponentInChildren<Camera>(true);
            if (underBed && playerCamera == null)
            {
                Debug.LogError($"Escondite {name}: no encuentra la cámara para el punto de ocultación de la cama.", this);
                LogInteraction(false);
                return false;
            }

            originalRotation = interactor.transform.rotation;
            originalCameraLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : Vector3.zero;
            characterControllerWasEnabled = characterController != null && characterController.enabled;
            movementWasEnabled = movement != null && movement.enabled;
            lookWasEnabled = look != null && look.enabled;
            interactionWasEnabled = interaction != null && interaction.enabled;

            if (characterController != null) characterController.enabled = false;
            if (movement != null) movement.enabled = false;
            if (look != null) look.enabled = false;
            if (interaction != null) interaction.enabled = false;

            var hidePosition = new Vector3(hideoutBounds.center.x, hideoutBounds.min.y + 0.02f, hideoutBounds.center.z);
            interactor.transform.SetPositionAndRotation(hidePosition, transform.rotation);
            if (playerCamera != null && underBed)
            {
                var cameraPosition = playerCamera.transform.position;
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, hideoutBounds.min.x + 0.1f, hideoutBounds.max.x - 0.1f);
                cameraPosition.z = Mathf.Clamp(cameraPosition.z, hideoutBounds.min.z + 0.1f, hideoutBounds.max.z - 0.1f);
                cameraPosition.y = hideoutBounds.min.y + 0.3f;
                playerCamera.transform.position = cameraPosition;
            }

            occupant = interactor;
            enteredFrame = Time.frameCount;
            SetRenderers(interactor.GetComponentsInChildren<Renderer>(true), occupantRenderers, false);
            SetRenderers(GetComponentsInChildren<Renderer>(true), hideoutRenderers, false);
            LogInteraction(true);
            return true;
        }

        private void Update()
        {
            if (occupant == null || input == null || Time.frameCount == enteredFrame) return;
            if (input.InteractPressed) Exit();
        }

        private void OnGUI()
        {
            if (occupant == null) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.72f, 200f, 28f), "E — salir del escondite");
        }

        private void Exit()
        {
            Vector3 position;
            if (underBed)
            {
                if (!TryFindFreeBedExit(out position))
                {
                    Debug.LogError($"Escondite {name}: no encuentra un lado libre y seguro para salir de la cama.", this);
                    return;
                }
            }
            else
            {
                var distance = hideoutBounds.extents.z + 0.7f;
                position = transform.position + transform.forward * distance;
                position.y = hideoutBounds.min.y + 0.02f;
            }

            occupant.transform.SetPositionAndRotation(position, originalRotation);
            if (playerCamera != null) playerCamera.transform.localPosition = originalCameraLocalPosition;
            RestoreRenderers(occupantRenderers);
            RestoreRenderers(hideoutRenderers);
            if (characterController != null) characterController.enabled = characterControllerWasEnabled;
            if (movement != null) movement.enabled = movementWasEnabled;
            if (look != null) look.enabled = lookWasEnabled;
            if (interaction != null) interaction.enabled = interactionWasEnabled;
            LogInteraction(false);
            occupant = null;
            input = null;
        }

        private bool TryFindFreeBedExit(out Vector3 position)
        {
            var radius = characterController != null ? characterController.radius : 0.3f;
            var height = characterController != null ? characterController.height : 1.8f;
            var directions = new[] { transform.right, -transform.right, transform.forward, -transform.forward };
            foreach (var direction in directions)
            {
                var extent = Mathf.Abs(direction.x) > 0.5f ? hideoutBounds.extents.x : hideoutBounds.extents.z;
                var candidate = hideoutBounds.center + direction * (extent + radius + 0.15f);
                candidate.y = hideoutBounds.min.y + 0.02f;
                if (hasRoomBounds && (candidate.x - radius < roomMin.x || candidate.x + radius > roomMax.x ||
                                      candidate.z - radius < roomMin.y || candidate.z + radius > roomMax.y)) continue;

                var rayOrigin = candidate + Vector3.up * 1.4f;
                if (!Physics.Raycast(rayOrigin, Vector3.down, out _, 1.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (IsCapsuleBlocked(candidate, radius, height)) continue;
                position = candidate;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        private bool IsCapsuleBlocked(Vector3 feetPosition, float radius, float height)
        {
            var center = feetPosition + originalRotation * (characterController != null ? characterController.center : Vector3.up * (height * 0.5f));
            var halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            var pointA = center + Vector3.up * halfSegment;
            var pointB = center - Vector3.up * halfSegment;
            return Physics.CheckCapsule(pointA, pointB, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = default;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private void LogInteraction(bool entering)
        {
            Debug.Log($"Escondite {name} tipo={hideoutType} entrar={entering.ToString().ToLowerInvariant()}", this);
        }

        private static void SetRenderers(Renderer[] renderers, List<(Renderer Renderer, bool Enabled)> states, bool enabled)
        {
            states.Clear();
            foreach (var renderer in renderers)
            {
                states.Add((renderer, renderer.enabled));
                renderer.enabled = enabled;
            }
        }

        private static void RestoreRenderers(List<(Renderer Renderer, bool Enabled)> states)
        {
            foreach (var state in states)
                if (state.Renderer != null) state.Renderer.enabled = state.Enabled;
            states.Clear();
        }
    }
}

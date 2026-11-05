using System.Collections.Generic;
using UnityEngine;
using UltimaCinta.Player;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class HideoutInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool underBed;

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
        private int enteredFrame;

        public string Prompt => "E — esconderse";

        public void SetUnderBed(bool value)
        {
            underBed = value;
        }

        public bool Interact(GameObject interactor)
        {
            if (occupant != null || interactor == null) return false;
            input = interactor.GetComponent<PlayerInputReader>();
            if (input == null) return false;

            characterController = interactor.GetComponent<CharacterController>();
            movement = interactor.GetComponent<FirstPersonMovementController>();
            look = interactor.GetComponent<FirstPersonLookController>();
            interaction = interactor.GetComponent<PlayerInteractionController>();
            playerCamera = interactor.GetComponentInChildren<Camera>(true);
            originalRotation = interactor.transform.rotation;
            originalCameraLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : Vector3.zero;
            characterControllerWasEnabled = characterController != null && characterController.enabled;
            movementWasEnabled = movement != null && movement.enabled;
            lookWasEnabled = look != null && look.enabled;
            interactionWasEnabled = interaction != null && interaction.enabled;

            var bounds = GetRendererBounds();
            var floorY = bounds.min.y;
            interactor.transform.SetPositionAndRotation(
                new Vector3(bounds.center.x, floorY + 0.02f, bounds.center.z), transform.rotation);
            if (playerCamera != null && underBed)
            {
                var cameraPosition = playerCamera.transform.position;
                cameraPosition.y = bounds.min.y + 0.3f;
                playerCamera.transform.position = cameraPosition;
            }

            occupant = interactor;
            enteredFrame = Time.frameCount;
            SetRenderers(interactor.GetComponentsInChildren<Renderer>(true), occupantRenderers, false);
            SetRenderers(GetComponentsInChildren<Renderer>(true), hideoutRenderers, false);
            if (characterController != null) characterController.enabled = false;
            if (movement != null) movement.enabled = false;
            if (look != null) look.enabled = false;
            if (interaction != null) interaction.enabled = false;
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
            var bounds = GetRendererBounds();
            var distance = bounds.extents.z + 0.7f;
            var position = transform.position + transform.forward * distance;
            position.y = bounds.min.y + 0.02f;
            occupant.transform.SetPositionAndRotation(position, originalRotation);
            if (playerCamera != null) playerCamera.transform.localPosition = originalCameraLocalPosition;
            RestoreRenderers(occupantRenderers);
            RestoreRenderers(hideoutRenderers);
            if (characterController != null) characterController.enabled = characterControllerWasEnabled;
            if (movement != null) movement.enabled = movementWasEnabled;
            if (look != null) look.enabled = lookWasEnabled;
            if (interaction != null) interaction.enabled = interactionWasEnabled;
            occupant = null;
            input = null;
        }

        private Bounds GetRendererBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = default(Bounds);
            foreach (var renderer in renderers)
            {
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : new Bounds(transform.position, Vector3.one);
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

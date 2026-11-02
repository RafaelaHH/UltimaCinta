using System;
using UnityEngine;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Transform view;
        [SerializeField] private UltimaCinta.Player.PlayerInputReader input;
        public event Action<string> PromptChanged;
        private string _prompt;
        private void OnGUI() { if (!string.IsNullOrEmpty(_prompt)) GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.72f, 240f, 28f), _prompt); }
        private void Awake() { if (view == null) view = GetComponentInChildren<Camera>()?.transform; if (input == null) input = GetComponent<UltimaCinta.Player.PlayerInputReader>(); }
        private void Update()
        {
            IInteractable target = null;
            if (gameConfig != null && view != null && Physics.Raycast(view.position, view.forward, out var hit, gameConfig.InteractionReachMeters)) target = hit.collider.GetComponentInParent<IInteractable>();
            var prompt = target?.Prompt;
            if (prompt != _prompt) { _prompt = prompt; PromptChanged?.Invoke(_prompt); }
            if (target != null && input != null && input.InteractPressed) target.Interact(gameObject);
        }
    }
}

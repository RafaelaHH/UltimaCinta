using UnityEngine;
namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private UltimaCinta.Data.GameConfig gameConfig;
        [SerializeField] private Transform hinge;
        [SerializeField] private string keyId = "NONE";
        [SerializeField] private float swingSign = 1f;
        [SerializeField] private float openAngle = -1f;
        private bool _open;
        private bool _unlocked;
        private Quaternion _closedRotation;
        public string Prompt => _open ? "E — close door" : !_unlocked ? $"E — unlock with {keyId}" : "E — open door";
        private void Awake() { if (hinge == null) hinge = transform; _closedRotation = hinge.localRotation; _unlocked = keyId == "NONE"; }
        private void Update()
        {
            if (gameConfig == null) return;
            var angle = openAngle > 0f ? openAngle : gameConfig.DoorOpenAngle;
            var target = _open ? _closedRotation * Quaternion.Euler(0f, angle * swingSign, 0f) : _closedRotation;
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, gameConfig.DoorOpenSpeed * Time.deltaTime);
        }
        public bool Interact(GameObject interactor)
        {
            if (!_unlocked && !HasKey(interactor, keyId))
            {
                interactor?.GetComponent<UltimaCinta.Player.PlayerInventory>()?.ShowMessage($"Se necesita {keyId}");
                return false;
            }
            _unlocked = true;
            _open = !_open;
            return true;
        }

        private static bool HasKey(GameObject interactor, string requiredKey)
        {
            var inventory = interactor == null ? null : interactor.GetComponent<UltimaCinta.Player.PlayerInventory>();
            if (inventory == null) return false;
            for (var slot = 0; slot < inventory.Capacity; slot++)
            {
                var item = inventory.GetItem(slot);
                if (item != null && item.DisplayName == requiredKey) return true;
            }
            return false;
        }
    }
}

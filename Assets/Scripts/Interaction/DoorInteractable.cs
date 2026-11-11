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
        public void ForceSlamFromGhost() { if (_unlocked) _open = false; }
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
            var inventory = interactor == null ? null : interactor.GetComponent<UltimaCinta.Player.PlayerInventory>();
            var consumesKey = !_unlocked && keyId != "NONE";
            if (consumesKey && !HasKey(inventory, keyId))
            {
                inventory?.ShowMessage($"Se necesita {keyId}");
                return false;
            }
            _unlocked = true;
            _open = !_open;
            if (consumesKey)
            {
                var remainingUses = CountLockedDoors(keyId);
                if (remainingUses == 0)
                {
                    if (inventory == null || !inventory.TryRemove(keyId))
                    {
                        Debug.LogError($"Door {name}: no se pudo retirar {keyId} del inventario tras desbloquear.", this);
                    }
                    else Debug.Log($"Llave {keyId} usada en puerta {DoorId}; eliminada del inventario", this);
                }
                else Debug.Log($"Llave {keyId} usada en puerta {DoorId}; quedan {remainingUses} usos", this);
            }
            return true;
        }

        private string DoorId
        {
            get
            {
                var separator = name.IndexOf(" [", System.StringComparison.Ordinal);
                return separator < 0 ? name : name.Substring(0, separator);
            }
        }

        private static bool HasKey(UltimaCinta.Player.PlayerInventory inventory, string requiredKey)
        {
            if (inventory == null) return false;
            for (var slot = 0; slot < inventory.Capacity; slot++)
            {
                var item = inventory.GetItem(slot);
                if (item != null && item.DisplayName == requiredKey) return true;
            }
            return false;
        }

        private static int CountLockedDoors(string requiredKey)
        {
            var count = 0;
            foreach (var door in FindObjectsOfType<DoorInteractable>(true))
                if (!door._unlocked && door.keyId == requiredKey) count++;
            return count;
        }
    }
}

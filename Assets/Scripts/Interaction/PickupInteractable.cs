using UnityEngine;
namespace UltimaCinta.Interaction
{
    public sealed class PickupInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private UltimaCinta.Data.InventoryItem item;
        public UltimaCinta.Data.InventoryItem Item => item;
        public string Prompt => item != null ? $"E — recoger {item.DisplayName}" : "E — recoger [objeto sin definir]";
        private void Awake() { EnsureVisual(); }
        public void SetItem(UltimaCinta.Data.InventoryItem definition) { item = definition; EnsureVisual(); }
        private void EnsureVisual()
        {
            if (item == null || GetComponentInChildren<Renderer>(true) != null) return;
            ItemVisualFactory.CreateVisual(item, transform, 0.4f);
        }
        public bool Interact(GameObject interactor)
        {
            if (item == null)
            {
                Debug.LogWarning($"Pickup failed: '{name}' has no item definition.", this);
                return false;
            }
            if (interactor == null)
            {
                Debug.LogWarning($"Pickup failed: '{name}' received no player object.", this);
                return false;
            }
            var inventory = interactor.GetComponent<UltimaCinta.Player.PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning($"Pickup failed: player '{interactor.name}' has no inventory.", interactor);
                return false;
            }
            if (!inventory.TryAdd(item))
            {
                inventory.ShowMessage("Inventario lleno");
                Debug.LogWarning($"Pickup failed: inventario lleno; no se recogió '{item.DisplayName}'.", interactor);
                return false;
            }
            Debug.Log($"Picked up '{item.DisplayName}'.", interactor);
            Destroy(gameObject);
            return true;
        }
    }
}

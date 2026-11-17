using UnityEngine;
using Fusion;
namespace UltimaCinta.Interaction
{
    public sealed class PickupInteractable : NetworkBehaviour, IInteractable
    {
        [SerializeField] private UltimaCinta.Data.InventoryItem item;
        [Networked] private NetworkBool NetworkPicked { get; set; }
        private bool _networkStarted;
        private bool _hidden;
        private bool _ritualPickupPending;
        private UltimaCinta.Ghost.GhostWorldController _ritualWorld;
        private int _ritualItemIndex = -1;
        public UltimaCinta.Data.InventoryItem Item => item;
        public string Prompt => item != null ? $"E — recoger {item.DisplayName}" : "E — recoger [objeto sin definir]";
        private void Awake() { EnsureVisual(); }
        private void Update()
        {
            if (_ritualWorld != null && _ritualWorld.IsRitualItemPicked(_ritualItemIndex)) HideTakenItem();
        }

        public void ConfigureNetworkRitualItem(UltimaCinta.Ghost.GhostWorldController world, int index)
        {
            _ritualWorld = world;
            _ritualItemIndex = index;
        }
        public override void Spawned()
        {
            _networkStarted = true;
            if (HasStateAuthority) NetworkPicked = false;
            ApplyNetworkState();
        }
        public override void Render() { if (_networkStarted) ApplyNetworkState(); }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SetPicked() { NetworkPicked = true; }

        private void ApplyNetworkState()
        {
            if (_hidden == NetworkPicked) return;
            _hidden = NetworkPicked;
            SetVisible(!_hidden);
        }

        private void HideTakenItem()
        {
            if (_hidden) return;
            _hidden = true;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = visible;
        }
        public void SetItem(UltimaCinta.Data.InventoryItem definition) { item = definition; EnsureVisual(); }
        private void EnsureVisual()
        {
            if (item == null || GetComponentInChildren<Renderer>(true) != null) return;
            ItemVisualFactory.CreateVisual(item, transform, 0.4f);
        }
        public bool Interact(GameObject interactor)
        {
            if (_ritualPickupPending || (_ritualWorld != null && _ritualWorld.IsRitualItemPicked(_ritualItemIndex))) return false;
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
            interactor.GetComponent<UltimaCinta.Audio.PlayerAudioController>()?.PlayPickupCue();
            Debug.Log($"Picked up '{item.DisplayName}'.", interactor);
            if (_ritualWorld != null)
            {
                _ritualPickupPending = true;
                _ritualWorld.RequestRitualItemPickup(_ritualItemIndex);
            }
            else if (_networkStarted)
            {
                if (HasStateAuthority) NetworkPicked = true;
                else RPC_SetPicked();
            }
            else Destroy(gameObject);
            return true;
        }
    }
}

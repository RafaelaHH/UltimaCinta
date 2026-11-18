using System;
using Fusion;
using UltimaCinta.Audio;
using UnityEngine;
using UltimaCinta.Data;
using UltimaCinta.Player;

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class NetworkDroppedItem : NetworkBehaviour, IInteractable
    {
        public static event Action<NetworkDroppedItem> NetworkItemSpawned;

        [Networked] private int NetworkItemIndex { get; set; }
        [Networked] private NetworkBool NetworkPicked { get; set; }

        private NetworkItemCatalog _catalog;
        private InventoryItem _item;
        private int _renderedItemIndex = -1;
        private bool _renderedPicked;

        public string Prompt => _item != null ? $"E — recoger {_item.DisplayName}" : "E — recoger objeto";

        public static bool TrySpawn(InventoryItem item, Transform dropper, NetworkRunner runner)
        {
            if (item == null || dropper == null || runner == null || !runner.IsRunning) return false;
            var catalog = Resources.Load<NetworkItemCatalog>("NetworkItemCatalog");
            if (catalog == null || catalog.DroppedItemPrefab == null) return false;
            var itemIndex = catalog.GetIndex(item);
            if (itemIndex < 0) return false;

            var position = dropper.position + dropper.forward * 0.8f + Vector3.up * 0.25f;
            var networkObject = runner.Spawn(catalog.DroppedItemPrefab, position, dropper.rotation, runner.LocalPlayer);
            var droppedItem = networkObject != null ? networkObject.GetComponent<NetworkDroppedItem>() : null;
            if (droppedItem == null) return false;
            droppedItem.InitializeItem(itemIndex);
            return true;
        }

        public override void Spawned()
        {
            _catalog = Resources.Load<NetworkItemCatalog>("NetworkItemCatalog");
            if (HasStateAuthority) NetworkPicked = false;
            ApplyNetworkState();
        }

        public override void Render() => ApplyNetworkState();

        public void InitializeItem(int itemIndex)
        {
            if (HasStateAuthority) NetworkItemIndex = itemIndex;
            else RPC_SetItemIndex(itemIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SetItemIndex(int itemIndex)
        {
            if (_catalog != null && _catalog.GetItem(itemIndex) != null) NetworkItemIndex = itemIndex;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_Pickup() => NetworkPicked = true;

        private void ApplyNetworkState()
        {
            if (_catalog == null) _catalog = Resources.Load<NetworkItemCatalog>("NetworkItemCatalog");
            if (_catalog != null && _renderedItemIndex != NetworkItemIndex)
            {
                _renderedItemIndex = NetworkItemIndex;
                _item = _catalog.GetItem(_renderedItemIndex);
                BuildVisual();
                if (_item != null)
                {
                    if (HasQaFlag("-ucDropTest") || HasQaFlag("-ucPickTest"))
                        Debug.Log($"[Fusion QA] Objeto de red visible: {_item.DisplayName}, índice={_renderedItemIndex}.");
                    NetworkItemSpawned?.Invoke(this);
                }
            }
            if (_renderedPicked == NetworkPicked) return;
            _renderedPicked = NetworkPicked;
            if (_renderedPicked && (HasQaFlag("-ucDropTest") || HasQaFlag("-ucPickTest")))
                Debug.Log($"[Fusion QA] Objeto de red marcado como recogido: {_item?.DisplayName ?? "desconocido"}.");
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if (renderer.transform != transform) renderer.enabled = !_renderedPicked;
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = !_renderedPicked;
        }

        private void BuildVisual()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
                Destroy(transform.GetChild(index).gameObject);
            if (_item == null) return;
            var visual = ItemVisualFactory.CreateVisual(_item, transform, 0.4f);
            if (visual == null) return;
            var bounds = ItemVisualFactory.GetVisualBounds(visual);
            var collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.center = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            collider.enabled = !_renderedPicked;
        }

        public bool Interact(GameObject interactor)
        {
            if (_item == null || NetworkPicked || interactor == null) return false;
            var inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null || !inventory.TryAdd(_item))
            {
                inventory?.ShowMessage("Inventario lleno");
                return false;
            }

            interactor.GetComponent<PlayerAudioController>()?.PlayPickupCue();
            if (HasStateAuthority) NetworkPicked = true;
            else RPC_Pickup();
            return true;
        }

        private static bool HasQaFlag(string flag)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}

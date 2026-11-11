using System;
using UnityEngine;
namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 3;
        [SerializeField, Min(0.1f)] private float notificationDurationSeconds = 2.5f;
        private UltimaCinta.Data.InventoryItem[] _items;
        private string _notification;
        private float _notificationExpiresAt;
        private Transform _heldProp;
        public event Action<int, UltimaCinta.Data.InventoryItem> SlotChanged;
        public event Action<bool> CamcorderChanged;
        public int SelectedSlot { get; private set; }
        public int Capacity => capacity;
        public bool HasCamcorder { get; private set; }
        public bool HasJournal => ContainsItem("diario") || ContainsItem("journal");
        private void Awake() { _items = new UltimaCinta.Data.InventoryItem[Mathf.Max(1, capacity)]; }
        private void Start() { RefreshHeldProp(); }
        private void OnGUI()
        {
            const float left = 24f;
            const float width = 180f;
            var staminaY = Screen.height - 96f;
            var barY = Screen.height - 70f;
            var toolsY = Screen.height - 38f;
            if (TryGetComponent<StaminaResource>(out var stamina))
            {
                GUI.Label(new Rect(left, staminaY, width, 22f), "STAMINA");
                GUI.color = new Color(0f, 0f, 0f, 0.75f);
                GUI.Box(new Rect(left, barY, width, 16f), GUIContent.none);
                GUI.color = new Color(0.25f, 0.9f, 0.4f, 1f);
                GUI.Box(new Rect(left + 2f, barY + 2f, (width - 4f) * stamina.NormalizedValue, 12f), GUIContent.none);
                GUI.color = Color.white;
            }
            var tools = $"TOOLS [1] {GetItem(0)?.DisplayName ?? "Vacío"}  [2] {GetItem(1)?.DisplayName ?? "Vacío"}  [3] {GetItem(2)?.DisplayName ?? "Vacío"}  |  {(HasCamcorder ? "CAMCORDER" : "SIN CAMCORDER")}  [Q] Usar [G] Soltar";
            GUI.Label(new Rect(left, toolsY, Screen.width - left * 2f, 24f), tools);
            if (!string.IsNullOrEmpty(_notification) && Time.unscaledTime < _notificationExpiresAt)
            {
                var notification = new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.78f, 300f, 40f);
                GUI.color = new Color(0f, 0f, 0f, 0.9f);
                GUI.Box(notification, GUIContent.none);
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
                GUI.Label(notification, _notification, style);
            }
        }
        public UltimaCinta.Data.InventoryItem GetItem(int slot) => _items != null && slot >= 0 && slot < _items.Length ? _items[slot] : null;
        public bool TryAdd(UltimaCinta.Data.InventoryItem item)
        {
            if (item == null || _items == null) return false;
            if (string.Equals(item.DisplayName, "camcorder", StringComparison.OrdinalIgnoreCase))
            {
                if (HasCamcorder) return false;
                HasCamcorder = true;
                CamcorderChanged?.Invoke(true);
                ShowMessage("Camcorder recogida");
                return true;
            }
            for (var i = 0; i < _items.Length; i++)
                if (_items[i] == null) { _items[i] = item; SlotChanged?.Invoke(i, item); if (i == SelectedSlot) RefreshHeldProp(); return true; }
            return false;
        }

        public bool Equip(UltimaCinta.Data.InventoryItem item, int slot)
        {
            if (item == null || _items == null || slot < 0 || slot >= _items.Length) return false;
            var existingSlot = -1;
            for (var index = 0; index < _items.Length; index++)
                if (_items[index] != null && string.Equals(_items[index].DisplayName, item.DisplayName, StringComparison.OrdinalIgnoreCase))
                { existingSlot = index; break; }
            if (existingSlot == slot) return true;

            var displaced = _items[slot];
            _items[slot] = item;
            if (existingSlot >= 0) _items[existingSlot] = displaced;
            SlotChanged?.Invoke(slot, item);
            if (existingSlot >= 0) SlotChanged?.Invoke(existingSlot, displaced);
            if (slot == SelectedSlot || existingSlot == SelectedSlot) RefreshHeldProp();
            return true;
        }

        public bool ContainsItem(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName) || _items == null) return false;
            foreach (var item in _items)
                if (item != null && string.Equals(item.DisplayName, displayName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        public bool TryRemove(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _items == null) return false;
            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null || !string.Equals(_items[i].DisplayName, itemId, StringComparison.Ordinal)) continue;
                _items[i] = null;
                SlotChanged?.Invoke(i, null);
                if (i == SelectedSlot) RefreshHeldProp();
                return true;
            }
            return false;
        }
        public void ShowMessage(string message)
        {
            _notification = message;
            _notificationExpiresAt = Time.unscaledTime + notificationDurationSeconds;
        }
        public void Select(int slot) { if (slot >= 0 && slot < capacity) { SelectedSlot = slot; RefreshHeldProp(); } }

        public bool DropSelected()
        {
            var item = GetItem(SelectedSlot);
            if (item == null) { ShowMessage("No hay ningún objeto en esta ranura"); return false; }
            if (!UltimaCinta.Interaction.DroppedItemSpawner.Spawn(item, transform))
            {
                ShowMessage("No se pudo soltar el objeto");
                return false;
            }
            _items[SelectedSlot] = null;
            SlotChanged?.Invoke(SelectedSlot, null);
            RefreshHeldProp();
            ShowMessage($"Soltaste {item.DisplayName}");
            return true;
        }

        public bool UseSelected()
        {
            var item = GetItem(SelectedSlot);
            if (item == null) { ShowMessage("No hay ningún objeto en esta ranura"); return false; }
            var used = item.Kind switch
            {
                UltimaCinta.Data.InventoryItemKind.SanityPills when TryGetComponent<UltimaCinta.Ghost.SanityController>(out var sanity)
                    => sanity.RestoreFromPills(),
                UltimaCinta.Data.InventoryItemKind.CameraBattery when TryGetComponent<UltimaCinta.CameraSystem.CameraResourceController>(out var camera)
                    => camera.RestoreBattery(),
                _ => false
            };
            if (!used) { ShowMessage("No puedes usar este objeto ahora"); return false; }
            _items[SelectedSlot] = null;
            SlotChanged?.Invoke(SelectedSlot, null);
            RefreshHeldProp();
            ShowMessage($"Usaste {item.DisplayName}");
            return true;
        }

        private void RefreshHeldProp()
        {
            if (_heldProp != null) Destroy(_heldProp.gameObject);
            var cameraTransform = GetComponentInChildren<Camera>()?.transform;
            var item = GetItem(SelectedSlot);
            if (cameraTransform == null || item == null) return;

            var prop = new GameObject($"ObjetoEnMano_{item.DisplayName}");
            prop.name = $"ObjetoEnMano_{item.DisplayName}";
            prop.transform.SetParent(cameraTransform, false);
            prop.transform.localPosition = new Vector3(0.32f, -0.28f, 0.55f);
            prop.transform.localRotation = Quaternion.Euler(15f, 20f, 0f);
            var visual = UltimaCinta.Interaction.ItemVisualFactory.CreateVisual(item, prop.transform, 0.24f);
            if (visual == null) { Destroy(prop); return; }
            _heldProp = prop.transform;
        }
    }
}

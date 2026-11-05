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
        public event Action<int, UltimaCinta.Data.InventoryItem> SlotChanged;
        public int SelectedSlot { get; private set; }
        public int Capacity => capacity;
        private void Awake() { _items = new UltimaCinta.Data.InventoryItem[Mathf.Max(1, capacity)]; }
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
            var tools = $"TOOLS [1] {GetItem(0)?.DisplayName ?? "Empty"}  [2] {GetItem(1)?.DisplayName ?? "Empty"}  [3] {GetItem(2)?.DisplayName ?? "Empty"}  |  CAMCORDER";
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
            for (var i = 0; i < _items.Length; i++)
                if (_items[i] == null) { _items[i] = item; SlotChanged?.Invoke(i, item); return true; }
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
                return true;
            }
            return false;
        }
        public void ShowMessage(string message)
        {
            _notification = message;
            _notificationExpiresAt = Time.unscaledTime + notificationDurationSeconds;
        }
        public void Select(int slot) { if (slot >= 0 && slot < capacity) SelectedSlot = slot; }
    }
}

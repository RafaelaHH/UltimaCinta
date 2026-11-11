using UnityEngine;
namespace UltimaCinta.Data
{
    public enum InventoryItemKind { Generic, SanityPills, CameraBattery }

    [CreateAssetMenu(menuName="UltimaCinta/Inventory Item", fileName="InventoryItem")]
    public sealed class InventoryItem : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private InventoryItemKind kind;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public InventoryItemKind Kind => kind;
    }
}

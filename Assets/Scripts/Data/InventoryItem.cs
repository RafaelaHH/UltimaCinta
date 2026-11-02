using UnityEngine;
namespace UltimaCinta.Data
{
    [CreateAssetMenu(menuName="UltimaCinta/Inventory Item", fileName="InventoryItem")]
    public sealed class InventoryItem : ScriptableObject
    {
        [SerializeField] private string displayName;
        public string DisplayName => displayName;
    }
}

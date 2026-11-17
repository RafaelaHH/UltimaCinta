using System;
using UnityEngine;

namespace UltimaCinta.Data
{
    [CreateAssetMenu(menuName = "UltimaCinta/Network Item Catalog", fileName = "NetworkItemCatalog")]
    public sealed class NetworkItemCatalog : ScriptableObject
    {
        [SerializeField] private InventoryItem[] items = Array.Empty<InventoryItem>();
        [SerializeField] private GameObject droppedItemPrefab;

        public GameObject DroppedItemPrefab => droppedItemPrefab;

        public int GetIndex(InventoryItem item)
        {
            if (item == null || items == null) return -1;
            for (var index = 0; index < items.Length; index++)
                if (items[index] == item) return index;
            return -1;
        }

        public InventoryItem GetItem(int index) => items != null && index >= 0 && index < items.Length ? items[index] : null;

#if UNITY_EDITOR
        public void Configure(InventoryItem[] definitions, GameObject prefab)
        {
            items = definitions ?? Array.Empty<InventoryItem>();
            droppedItemPrefab = prefab;
        }
#endif
    }
}

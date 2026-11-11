using UnityEngine;

namespace UltimaCinta.Interaction
{
    public static class DroppedItemSpawner
    {
        public static bool Spawn(UltimaCinta.Data.InventoryItem item, Transform dropper)
        {
            if (item == null || dropper == null) return false;

            var dropped = new GameObject();
            dropped.name = $"Objeto soltado · {item.DisplayName}";
            dropped.transform.position = dropper.position + dropper.forward * 0.8f + Vector3.up * 0.25f;
            var visual = ItemVisualFactory.CreateVisual(item, dropped.transform, 0.4f);
            if (visual == null) { Object.Destroy(dropped); return false; }
            var bounds = ItemVisualFactory.GetVisualBounds(visual);
            var collider = dropped.AddComponent<BoxCollider>();
            collider.center = dropped.transform.InverseTransformPoint(bounds.center);
            collider.size = dropped.transform.InverseTransformVector(bounds.size);
            var pickup = dropped.AddComponent<PickupInteractable>();
            pickup.SetItem(item);
            var body = dropped.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return true;
        }
    }
}

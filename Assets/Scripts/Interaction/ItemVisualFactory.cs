using UnityEngine;

namespace UltimaCinta.Interaction
{
    public static class ItemVisualFactory
    {
        public static GameObject CreateVisual(UltimaCinta.Data.InventoryItem item, Transform parent, float maximumExtent)
        {
            if (item == null || parent == null) return null;
            GameObject visual;
            if (item.VisualPrefab != null)
                visual = Object.Instantiate(item.VisualPrefab, parent, false);
            else
                visual = CreateFallback(item, parent);
            if (visual == null) return null;

            visual.name = $"Modelo_{item.DisplayName}";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            DisablePhysics(visual);
            FitToExtent(visual.transform, maximumExtent);
            return visual;
        }

        public static Bounds GetVisualBounds(GameObject visual)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(visual.transform.position, Vector3.zero);
            var hasBounds = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static void FitToExtent(Transform visual, float maximumExtent)
        {
            var bounds = GetVisualBounds(visual.gameObject);
            var extent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (extent <= 0.0001f) return;
            visual.localScale *= maximumExtent / extent;
            bounds = GetVisualBounds(visual.gameObject);
            visual.localPosition -= visual.parent.InverseTransformVector(bounds.center - visual.parent.position);
        }

        private static GameObject CreateFallback(UltimaCinta.Data.InventoryItem item, Transform parent)
        {
            var root = new GameObject(item.Kind == UltimaCinta.Data.InventoryItemKind.SanityPills
                ? "Frasco de pastillas provisional"
                : $"Representación provisional · {item.DisplayName}");
            root.transform.SetParent(parent, false);
            if (item.Kind == UltimaCinta.Data.InventoryItemKind.SanityPills)
            {
                var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bottle.name = "Frasco";
                bottle.transform.SetParent(root.transform, false);
                bottle.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Tapa";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = Vector3.up * 0.12f;
                cap.transform.localScale = new Vector3(0.13f, 0.025f, 0.13f);
            }
            else
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(root.transform, false);
                capsule.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            }
            return root;
        }

        private static void DisablePhysics(GameObject visual)
        {
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }
    }
}

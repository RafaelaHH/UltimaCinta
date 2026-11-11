using System.Collections.Generic;
using UnityEngine;

namespace UltimaCinta.Interaction
{
    internal static class VanCabinBuilder
    {
        private const float BoundsExpansion = 0.8f;
        private const float WallThickness = 0.12f;
        private const float ExitOpeningWidth = 1.05f;
        private const float ShelfThickness = 0.08f;
        private const float EntranceRampLength = 0.9f;
        private const float EntranceRise = 0.4f;
        public static GameObject Build(VanSupplyInteractable van, BoxCollider trigger, List<PickupInteractable> supplies)
        {
            var bounds = trigger.bounds;
            var lengthRunsAlongZ = bounds.extents.z > bounds.extents.x;
            var halfLength = Mathf.Max(1.25f, (lengthRunsAlongZ ? bounds.extents.z : bounds.extents.x) - BoundsExpansion * 0.5f);
            var halfWidth = Mathf.Max(0.72f, (lengthRunsAlongZ ? bounds.extents.x : bounds.extents.z) - BoundsExpansion * 0.5f);
            var floorY = bounds.min.y + BoundsExpansion * 0.5f + WallThickness * 0.5f;
            var height = Mathf.Clamp(bounds.size.y - BoundsExpansion - 0.2f, 1.9f, 2.15f);
            var interior = new GameObject("Interior físico de camioneta");
            interior.transform.SetPositionAndRotation(bounds.center,
                lengthRunsAlongZ ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity);
            var floorLocalY = floorY - bounds.center.y;
            var center = Vector3.zero;
            var minX = -halfLength;
            var maxX = halfLength;
            var minZ = -halfWidth;
            var maxZ = halfWidth;

            CreatePart(interior.transform, "Piso interior", new Vector3(center.x, floorLocalY, center.z),
                new Vector3(halfLength * 2f, WallThickness, halfWidth * 2f), CabinColor(0.12f, 0.14f, 0.15f), true);
            CreatePart(interior.transform, "Techo interior", new Vector3(center.x, floorLocalY + height, center.z),
                new Vector3(halfLength * 2f, WallThickness, halfWidth * 2f), CabinColor(0.20f, 0.22f, 0.22f), false);
            CreatePart(interior.transform, "Pared izquierda", new Vector3(center.x, floorLocalY + height * 0.5f, minZ),
                new Vector3(halfLength * 2f, height, WallThickness), CabinColor(0.25f, 0.27f, 0.26f), true);
            CreatePart(interior.transform, "Pared derecha", new Vector3(center.x, floorLocalY + height * 0.5f, maxZ),
                new Vector3(halfLength * 2f, height, WallThickness), CabinColor(0.25f, 0.27f, 0.26f), true);
            CreatePart(interior.transform, "Cabina frontal", new Vector3(minX, floorLocalY + height * 0.5f, center.z),
                new Vector3(WallThickness, height, halfWidth * 2f), CabinColor(0.16f, 0.18f, 0.18f), true);

            var openingHalfWidth = Mathf.Min(ExitOpeningWidth * 0.5f, halfWidth * 0.55f);
            var sideWidth = halfWidth - openingHalfWidth;
            CreatePart(interior.transform, "Marco trasero izquierdo", new Vector3(maxX, floorLocalY + height * 0.5f, center.z - (openingHalfWidth + sideWidth * 0.5f)),
                new Vector3(WallThickness, height, sideWidth), CabinColor(0.16f, 0.18f, 0.18f), true);
            CreatePart(interior.transform, "Marco trasero derecho", new Vector3(maxX, floorLocalY + height * 0.5f, center.z + (openingHalfWidth + sideWidth * 0.5f)),
                new Vector3(WallThickness, height, sideWidth), CabinColor(0.16f, 0.18f, 0.18f), true);
            CreatePart(interior.transform, "Rampa de acceso", new Vector3(maxX + EntranceRampLength * 0.5f,
                floorLocalY - EntranceRise * 0.5f, center.z),
                new Vector3(EntranceRampLength, 0.12f, openingHalfWidth * 2f), CabinColor(0.20f, 0.21f, 0.20f), true,
                -Mathf.Atan2(EntranceRise, EntranceRampLength) * Mathf.Rad2Deg);

            var shelfZOffset = halfWidth - 0.18f;
            var lowerShelfY = floorLocalY + 0.72f;
            var upperShelfY = floorLocalY + 1.26f;
            var shelfSize = new Vector3(halfLength * 1.35f, ShelfThickness, 0.32f);
            CreatePart(interior.transform, "Estante equipo inferior izquierdo", new Vector3(center.x - 0.1f, lowerShelfY, minZ + 0.18f), shelfSize, CabinColor(0.29f, 0.20f, 0.13f), true);
            CreatePart(interior.transform, "Estante equipo superior izquierdo", new Vector3(center.x - 0.1f, upperShelfY, minZ + 0.18f), shelfSize, CabinColor(0.29f, 0.20f, 0.13f), true);
            CreatePart(interior.transform, "Estante equipo inferior derecho", new Vector3(center.x - 0.1f, lowerShelfY, maxZ - 0.18f), shelfSize, CabinColor(0.29f, 0.20f, 0.13f), true);
            CreatePart(interior.transform, "Estante equipo superior derecho", new Vector3(center.x - 0.1f, upperShelfY, maxZ - 0.18f), shelfSize, CabinColor(0.29f, 0.20f, 0.13f), true);
            CreatePart(interior.transform, "Banco de trabajo", new Vector3(center.x - halfLength * 0.15f, floorLocalY + 0.78f, maxZ - 0.24f),
                new Vector3(halfLength * 0.9f, 0.12f, 0.42f), CabinColor(0.34f, 0.24f, 0.16f), true);
            CreatePart(interior.transform, "Lámpara de cabina", new Vector3(center.x, floorLocalY + height - 0.16f, center.z),
                new Vector3(0.42f, 0.08f, 0.18f), CabinColor(0.70f, 0.64f, 0.47f), false);

            var cabinLightObject = new GameObject("Luz cálida de cabina");
            cabinLightObject.transform.SetParent(interior.transform, false);
            cabinLightObject.transform.localPosition = new Vector3(center.x, floorLocalY + height - 0.28f, center.z);
            var cabinLight = cabinLightObject.AddComponent<Light>();
            cabinLight.type = LightType.Point;
            cabinLight.color = new Color(1f, 0.78f, 0.48f);
            cabinLight.intensity = 1.6f;
            cabinLight.range = halfLength + halfWidth;

            var exit = new GameObject("Salida camioneta · E");
            exit.transform.SetParent(interior.transform, false);
            exit.transform.localPosition = new Vector3(maxX - WallThickness, floorLocalY + 0.9f, center.z);
            var exitTrigger = exit.AddComponent<BoxCollider>();
            exitTrigger.isTrigger = true;
            exitTrigger.size = new Vector3(0.8f, 1.8f, ExitOpeningWidth);
            exit.AddComponent<VanExitInteractable>().SetVan(van);

            PlaceSupplies(supplies, interior.transform, center, lowerShelfY, upperShelfY, minX, maxX, shelfZOffset);
            interior.SetActive(false);
            return interior;
        }

        public static Vector3 EntryPosition(BoxCollider trigger)
        {
            var bounds = trigger.bounds;
            var lengthRunsAlongZ = bounds.extents.z > bounds.extents.x;
            var halfLength = Mathf.Max(1.25f, (lengthRunsAlongZ ? bounds.extents.z : bounds.extents.x) - BoundsExpansion * 0.5f);
            var floorLocalY = bounds.min.y + BoundsExpansion * 0.5f + WallThickness * 0.5f - bounds.center.y;
            var localEntry = new Vector3(halfLength + EntranceRampLength * 0.5f,
                floorLocalY - EntranceRise * 0.5f, 0f);
            var rotation = lengthRunsAlongZ ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity;
            return bounds.center + rotation * localEntry;
        }

        public static Quaternion EntryRotation(BoxCollider trigger) =>
            trigger.bounds.extents.z > trigger.bounds.extents.x ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(0f, -90f, 0f);

        public static Vector3 InteriorPosition(BoxCollider trigger, int occupantIndex)
        {
            var bounds = trigger.bounds;
            var lengthRunsAlongZ = bounds.extents.z > bounds.extents.x;
            var floorLocalY = bounds.min.y + BoundsExpansion * 0.5f + WallThickness * 0.5f - bounds.center.y;
            var rotation = lengthRunsAlongZ ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity;
            var lateralOffset = (occupantIndex - 1) * 0.65f;
            return bounds.center + rotation * new Vector3(lateralOffset, floorLocalY + 0.95f, 0f);
        }

        private static void PlaceSupplies(List<PickupInteractable> supplies, Transform interior, Vector3 center, float lowerShelfY,
            float upperShelfY, float minX, float maxX, float shelfZOffset)
        {
            var shelfWidth = (maxX - minX) * 0.675f;
            var shelfHalfWidth = shelfWidth * 0.5f;
            var bayWidth = shelfWidth / 5f;
            for (var index = 0; index < supplies.Count; index++)
            {
                var pickup = supplies[index];
                if (pickup == null) continue;
                var side = index % 2 == 0 ? -1f : 1f;
                var row = index / 2;
                var column = row % 5;
                var level = row / 5;
                pickup.transform.rotation = interior.rotation;
                if (!TryGetRendererBounds(pickup.transform, out var itemBounds)) continue;
                var fitWidth = Mathf.Max(0.01f, bayWidth - 0.08f);
                var fitDepth = ShelfThickness * 4f - 0.08f;
                var fitScale = Mathf.Min(1f,
                    Mathf.Min(fitWidth / Mathf.Max(itemBounds.size.x, 0.01f), fitDepth / Mathf.Max(itemBounds.size.z, 0.01f)));
                if (fitScale < 1f)
                {
                    pickup.transform.localScale *= fitScale;
                    TryGetRendererBounds(pickup.transform, out itemBounds);
                }

                var x = center.x - shelfHalfWidth + bayWidth * (column + 0.5f);
                var shelfY = level == 0 ? lowerShelfY : upperShelfY;
                var z = center.z + side * shelfZOffset;
                var worldCenter = interior.TransformPoint(new Vector3(x, shelfY + ShelfThickness * 0.5f + itemBounds.extents.y + 0.01f, z));
                pickup.transform.position = worldCenter - (itemBounds.center - pickup.transform.position);
                foreach (var collider in pickup.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static void CreatePart(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid,
            float localRotationZ = 0f)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.Destroy(part.GetComponent<Collider>());
        }

        private static Material CabinColor(float red, float green, float blue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = new Color(red, green, blue) };
        }
    }
}

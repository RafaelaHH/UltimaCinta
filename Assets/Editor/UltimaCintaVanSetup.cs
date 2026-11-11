#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaVanSetup
    {
        [MenuItem("Tools/UltimaCinta/Fase 3/Configurar entrada y equipo de camioneta")]
        public static void ConfigureActiveSceneVan()
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject equipmentRoot = null;
            foreach (var root in roots)
            {
                equipmentRoot = Find(root.transform, "Camioneta_Equipo");
                if (equipmentRoot != null) break;
            }
            if (equipmentRoot == null)
            {
                Debug.LogError("Fase 3: no se encontró Camioneta_Equipo en la escena activa.");
                return;
            }

            if (equipmentRoot.GetComponent<UltimaCinta.Interaction.VanSupplyInteractable>() == null)
                Undo.AddComponent<UltimaCinta.Interaction.VanSupplyInteractable>(equipmentRoot);
            var box = equipmentRoot.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(equipmentRoot);
            box.isTrigger = true;

            var bounds = new Bounds(equipmentRoot.transform.position, Vector3.zero);
            var hasBounds = false;
            var parent = equipmentRoot.transform.parent;
            if (parent != null)
                foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.transform == equipmentRoot.transform || renderer.transform.IsChildOf(equipmentRoot.transform)) continue;
                    if (renderer.name.IndexOf("camioneta", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }

            if (!hasBounds)
                foreach (var renderer in equipmentRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }

            if (!hasBounds) bounds = new Bounds(equipmentRoot.transform.position, new Vector3(3f, 2.4f, 3f));
            bounds.Expand(new Vector3(0.8f, 0.8f, 0.8f));
            box.center = equipmentRoot.transform.InverseTransformPoint(bounds.center);
            var scale = equipmentRoot.transform.lossyScale;
            box.size = new Vector3(bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));

            ConfigureCamcorderPickup(equipmentRoot);
            EditorUtility.SetDirty(equipmentRoot);
            EditorSceneManager.MarkSceneDirty(equipmentRoot.scene);
            Debug.Log("Fase 3: camioneta configurada; el camcorder y el equipo se recogen con E.", equipmentRoot);
        }

        private static void ConfigureCamcorderPickup(GameObject equipmentRoot)
        {
            var camcorder = FindByNormalizedName(equipmentRoot.transform, "camcorder");
            if (camcorder == null)
            {
                Debug.LogWarning("Fase 3: no se encontró el objeto camcorder bajo Camioneta_Equipo.", equipmentRoot);
                return;
            }

            const string itemPath = "Assets/Generated/KeyItems/camcorder.asset";
            var item = AssetDatabase.LoadAssetAtPath<UltimaCinta.Data.InventoryItem>(itemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<UltimaCinta.Data.InventoryItem>();
                var serializedItem = new SerializedObject(item);
                serializedItem.FindProperty("displayName").stringValue = "camcorder";
                serializedItem.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(item, itemPath);
            }

            var pickup = camcorder.GetComponent<UltimaCinta.Interaction.PickupInteractable>();
            if (pickup == null) pickup = Undo.AddComponent<UltimaCinta.Interaction.PickupInteractable>(camcorder);
            pickup.SetItem(item);
            EditorUtility.SetDirty(pickup);
        }

        private static GameObject FindByNormalizedName(Transform parent, string target)
        {
            var normalized = Normalize(parent.name);
            if (normalized == target) return parent.gameObject;
            for (var index = 0; index < parent.childCount; index++)
            {
                var match = FindByNormalizedName(parent.GetChild(index), target);
                if (match != null) return match;
            }
            return null;
        }

        private static string Normalize(string value)
        {
            var characters = value.ToLowerInvariant().ToCharArray();
            var result = new System.Text.StringBuilder(characters.Length);
            foreach (var character in characters)
                if (char.IsLetterOrDigit(character)) result.Append(character);
            return result.ToString();
        }

        private static GameObject Find(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (var index = 0; index < parent.childCount; index++)
            {
                var match = Find(parent.GetChild(index), name);
                if (match != null) return match;
            }
            return null;
        }
    }
}
#endif

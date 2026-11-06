#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    public static class MansionNavMeshBaker
    {
        private const string MenuPath = "Tools/UltimaCinta/4. Hornear NavMesh de mansión";
        private const string ScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private const string SurfaceName = "Mansion_NavMeshSurface";
        private const string NavMeshFolder = "Assets/Generated/NavMeshData";
        private const string NavMeshAssetPath = NavMeshFolder + "/Mansion_NavMeshData.asset";
        private const int WalkableLayer = 8;

        [MenuItem(MenuPath)]
        public static void BakeMansionNavMesh()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null || !File.Exists(Path.Combine(projectRoot.FullName, ScenePath)))
            {
                Debug.LogError($"MansionNavMeshBaker: no existe {ScenePath}; genera la mansión primero.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var walkableCount = MarkWalkableGeometry(scene);
            var root = FindSurfaceRoot(scene);
            if (root == null) root = new GameObject(SurfaceName);
            root.name = SurfaceName;
            var surface = root.GetComponent<NavMeshSurface>() ?? root.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = 1 << WalkableLayer;
            surface.defaultArea = NavMesh.GetAreaFromName("Walkable");
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                Debug.LogError("MansionNavMeshBaker: BuildNavMesh no produjo NavMeshData.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder(NavMeshFolder)) AssetDatabase.CreateFolder("Assets/Generated", "NavMeshData");
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null && !AssetDatabase.DeleteAsset(NavMeshAssetPath))
            {
                Debug.LogError($"MansionNavMeshBaker: no se pudo reemplazar {NavMeshAssetPath}.");
                return;
            }
            var bakedData = surface.navMeshData;
            AssetDatabase.CreateAsset(bakedData, NavMeshAssetPath);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
            EditorUtility.SetDirty(surface);
            if (surface.navMeshData == null)
            {
                Debug.LogError($"MansionNavMeshBaker: no se pudo asignar el asset {NavMeshAssetPath} a {SurfaceName}.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"MansionNavMeshBaker: no se pudo guardar {ScenePath}.");
                return;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"NavMesh de mansión horneado: agente predeterminado, {walkableCount} superficies caminables, asset {NavMeshAssetPath} asignado a {SurfaceName} y escena guardada en {ScenePath}.");
        }

        private static int MarkWalkableGeometry(Scene scene)
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsWalkableName(transform.name)) continue;
                if (transform.GetComponent<Collider>() == null && transform.GetComponent<Renderer>() == null) continue;
                transform.gameObject.layer = WalkableLayer;
                count++;
            }
            return count;
        }

        private static bool IsWalkableName(string name) =>
            name == "Floor" || name == "PorchFloor" || name == "ExteriorGround" ||
            name.StartsWith("Huella_", System.StringComparison.Ordinal) ||
            name.StartsWith("Escalon_", System.StringComparison.Ordinal) ||
            name.StartsWith("Escalon_P-B7_", System.StringComparison.Ordinal) ||
            name == "Rampa_Colisionable" || name == "RampaColisionable_P-B7";

        private static GameObject FindSurfaceRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == SurfaceName || root.GetComponentInChildren<NavMeshSurface>(true) != null)
                    return root.name == SurfaceName ? root : root.GetComponentInChildren<NavMeshSurface>(true).gameObject;
            return null;
        }
    }
}
#endif

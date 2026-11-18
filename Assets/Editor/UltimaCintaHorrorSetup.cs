#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimaCinta.CameraSystem;
using UltimaCinta.Data;
using UltimaCinta.Interaction;
public static class UltimaCintaHorrorSetup
{
    [MenuItem("Tools/UltimaCinta/Preparar terror y grabaciones")]
    public static void Prepare()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Scripts/Data/GameConfig.asset");
        EnsureVanMonitorMaterial();
        var path = "Assets/Prefabs/Player/Player.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var recorder = root.GetComponent<CamcorderRecording>() ?? root.AddComponent<CamcorderRecording>();
            var serialized = new SerializedObject(recorder);
            serialized.FindProperty("gameConfig").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var entry in new[] { ("Jugador_Verde", "Assets/Generated/Jugadores/Jugador_Verde.fbx"), ("Jugadora_Roja", "Assets/Generated/Jugadores/Jugadora_Roja.fbx"), ("Jugador_Azul", "Assets/Generated/Jugadores/Jugador_Azul.fbx"), ("Jugadora_Ocre", "Assets/Generated/Jugadores/Jugadora_Ocre.fbx") })
        {
            var importer = AssetImporter.GetAtPath(entry.Item2) as ModelImporter;
            if (importer != null) { importer.animationType = ModelImporterAnimationType.None; importer.SaveAndReimport(); }
        }
        var playerRoot = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var body = playerRoot.transform.Find("RemotePlayerBody");
            if (body != null)
            {
                var capsule = body.GetComponent<MeshRenderer>();
                if (capsule != null) capsule.enabled = false;
                foreach (var entry in new[] { ("Jugador_Verde", "Assets/Generated/Jugadores/Jugador_Verde.fbx"), ("Jugadora_Roja", "Assets/Generated/Jugadores/Jugadora_Roja.fbx"), ("Jugador_Azul", "Assets/Generated/Jugadores/Jugador_Azul.fbx"), ("Jugadora_Ocre", "Assets/Generated/Jugadores/Jugadora_Ocre.fbx") })
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Item2);
                    if (model == null || body.Find(entry.Item1) != null) continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, body);
                    instance.name = entry.Item1; instance.transform.localPosition = Vector3.zero;
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var factor = 1.72f / Mathf.Max(0.01f, bounds.size.y);
                    instance.transform.localScale = Vector3.one * factor;
                    instance.SetActive(false);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(playerRoot, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(playerRoot); }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Mansion_Generated.unity");
        foreach (var van in Object.FindObjectsByType<VanSupplyInteractable>(FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(van);
            serialized.FindProperty("gameConfig").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(config);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureVanMonitorMaterial()
    {
        const string folder = "Assets/Resources";
        const string path = folder + "/VanMonitorMaterial.mat";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Resources");
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new System.InvalidOperationException("No se encontró Universal Render Pipeline/Unlit para el monitor.");
        var material = new Material(shader) { name = "VanMonitorMaterial" };
        AssetDatabase.CreateAsset(material, path);
    }
}
#endif

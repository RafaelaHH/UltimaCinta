#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UltimaCinta.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseZeroSetup
    {
        private const string ScenesPath = "Assets/Scenes";
        private const string ConfigPath = "Assets/Scripts/Data/GameConfig.asset";

        [MenuItem("Tools/UltimaCinta/Preparar Fase 0")]
        public static void PreparePhaseZero()
        {
            EnsureFolder("Assets/Scripts");
            EnsureFolder("Assets/Scripts/Player");
            EnsureFolder("Assets/Scripts/Ghost");
            EnsureFolder("Assets/Scripts/Network");
            EnsureFolder("Assets/Scripts/UI");
            EnsureFolder("Assets/Scripts/Data");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScenesPath);

            EnsureGameConfig();

            var scenePaths = new[]
            {
                $"{ScenesPath}/Bootstrap.unity",
                $"{ScenesPath}/Lobby.unity",
                $"{ScenesPath}/Mansion.unity"
            };

            foreach (var scenePath in scenePaths)
            {
                EnsureScene(scenePath);
            }

            AddScenesToBuildSettings(scenePaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fase 0 preparada: carpetas, GameConfig y escenas registradas.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void EnsureGameConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath) != null)
            {
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfig>();
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("realNightDurationMinutes").floatValue = 180f;
            serializedConfig.FindProperty("gameNightDurationHours").floatValue = 8f;
            serializedConfig.FindProperty("batteryDurationMinutes").floatValue = 35f;
            serializedConfig.FindProperty("nightVisionBatteryDurationMinutes").floatValue = 12f;
            serializedConfig.FindProperty("tapeDurationMinutes").floatValue = 45f;
            serializedConfig.FindProperty("startingSanity").floatValue = 100f;
            serializedConfig.FindProperty("maximumSanity").floatValue = 100f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        private static void EnsureScene(string scenePath)
        {
            if (File.Exists(scenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void AddScenesToBuildSettings(IReadOnlyList<string> scenePaths)
        {
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var scenePath in scenePaths)
            {
                var alreadyIncluded = buildScenes.Exists(scene => scene.path == scenePath);
                if (!alreadyIncluded)
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
#endif

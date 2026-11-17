#if UNITY_EDITOR
using UltimaCinta.Audio;
using UltimaCinta.CameraSystem;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UltimaCinta.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseSixSetup
    {
        private const string GameConfigPath = "Assets/Scripts/Data/GameConfig.asset";
        private const string TestConfigPath = "Assets/Scripts/Data/GameConfig_PhaseFourTest.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MansionScenePath = "Assets/Scenes/Mansion_Generated.unity";

        [MenuItem("Tools/UltimaCinta/Preparar Fase 6")]
        public static void PreparePhaseSix()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null) throw new System.InvalidOperationException($"No se encontró GameConfig: {GameConfigPath}");

            ConfigureAudioSettings(config);
            ConfigureAudioSettings(AssetDatabase.LoadAssetAtPath<GameConfig>(TestConfigPath));
            ConfigureGhostAssets(config);
            ConfigurePlayerPrefab(config);
            ConfigureBootstrapScene(config);
            ConfigureMansionScene(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fase 6 preparada: audio, menú, opciones, pausa, guía de camioneta y cámara configurados.");
        }

        private static void ConfigureAudioSettings(GameConfig config)
        {
            if (config == null) return;
            var serialized = new SerializedObject(config);
            var audio = serialized.FindProperty("audioTuning");
            if (audio == null) throw new System.InvalidOperationException($"{config.name} no contiene audioTuning.");

            Set(audio, "defaultMasterVolume", 0.8f);
            Set(audio, "sensitivityMultiplierMinimum", 0.5f);
            Set(audio, "sensitivityMultiplierMaximum", 2f);
            Set(audio, "defaultSensitivityMultiplier", 1f);
            Set(audio, "subtitleDurationSeconds", 2.5f);
            Set(audio, "footstepIntervalSeconds", 0.52f);
            Set(audio, "sprintFootstepIntervalSeconds", 0.34f);
            Set(audio, "footstepVolume", 0.38f);
            Set(audio, "footstepMinDistanceMeters", 1f);
            Set(audio, "footstepMaxDistanceMeters", 12f);
            Set(audio, "pickupVolume", 0.7f);
            Set(audio, "uiVolume", 0.48f);
            Set(audio, "milestoneVolume", 0.62f);
            Set(audio, "doorVolume", 0.72f);
            Set(audio, "doorMinDistanceMeters", 1f);
            Set(audio, "doorMaxDistanceMeters", 16f);
            Set(audio, "ghostVolume", 0.68f);
            Set(audio, "ghostMinDistanceMeters", 1.5f);
            Set(audio, "ghostMaxDistanceMeters", 22f);
            Set(audio, "ghostHuntCueIntervalSeconds", 7f);
            Set(audio, "heartbeatSanityThreshold", 45f);
            Set(audio, "heartbeatIntervalAtThresholdSeconds", 1.35f);
            Set(audio, "heartbeatIntervalAtZeroSeconds", 0.42f);
            Set(audio, "heartbeatVolume", 0.62f);
            Set(audio, "roomToneVolume", 0.07f);
            Set(audio, "maximumFilmGrain", 0.65f);
            Set(audio, "maximumChromaticAberration", 0.22f);
            Set(audio, "maximumLensDistortion", -0.1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void Set(SerializedProperty parent, string name, float value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null) property.floatValue = value;
        }

        private static void ConfigureGhostAssets(GameConfig config)
        {
            var guids = AssetDatabase.FindAssets("t:GhostType", new[] { "Assets/Scripts/Data/Ghosts" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ghostType = AssetDatabase.LoadAssetAtPath<GhostType>(path);
                if (ghostType == null) continue;
                var profile = ghostType.DisplayName.ToLowerInvariant() switch
                {
                    "ghoul" => GhostSoundProfile.Ghoul,
                    "vampiro" => GhostSoundProfile.Vampire,
                    "poltergeist" => GhostSoundProfile.Poltergeist,
                    "espectro" => GhostSoundProfile.Wraith,
                    _ => GhostSoundProfile.Ghoul
                };
                var serializedType = new SerializedObject(ghostType);
                var profileProperty = serializedType.FindProperty("soundProfile");
                if (profileProperty != null) profileProperty.enumValueIndex = (int)profile;
                serializedType.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ghostType);

                ConfigureGhostPrefab(ghostType.GhostPrefab, config);
            }
        }

        private static void ConfigureGhostPrefab(GameObject prefab, GameConfig config)
        {
            if (prefab == null) return;
            var path = AssetDatabase.GetAssetPath(prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var agent = root.GetComponentInChildren<GhostAgent>(true);
                if (agent == null) return;
                var audio = agent.GetComponent<GhostAudioController>();
                if (audio == null) audio = agent.gameObject.AddComponent<GhostAudioController>();
                var serialized = new SerializedObject(audio);
                SetReference(serialized, "ghostAgent", agent);
                SetReference(serialized, "gameConfig", config);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigurePlayerPrefab(GameConfig config)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                AddAndConfigure<PlayerAudioController>(root, "gameConfig", config, "inputReader", root.GetComponent<PlayerInputReader>(), "sanityController", root.GetComponent<SanityController>(), "networkAvatar", root.GetComponent<NetworkPlayerAvatar>());
                AddAndConfigure<SubtitleOverlay>(root, "gameConfig", config);
                AddAndConfigure<PauseMenuController>(root, "gameConfig", config);
                if (root.GetComponent<VanTutorialGuide>() == null) root.AddComponent<VanTutorialGuide>();

                var look = root.GetComponent<FirstPersonLookController>();
                if (look != null) SetComponentReference(look, "gameConfig", config);
                var movement = root.GetComponent<FirstPersonMovementController>();
                if (movement != null) SetComponentReference(movement, "gameConfig", config);
                var interference = root.GetComponent<GhostCameraInterference>();
                if (interference != null) SetComponentReference(interference, "gameConfig", config);
                var footage = root.GetComponent<FoundFootageEffects>();
                if (footage != null) SetComponentReference(footage, "gameConfig", config);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T AddAndConfigure<T>(GameObject root, params object[] nameValuePairs) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component == null) component = root.AddComponent<T>();
            var serialized = new SerializedObject(component);
            for (var index = 0; index + 1 < nameValuePairs.Length; index += 2)
            {
                var property = serialized.FindProperty((string)nameValuePairs[index]);
                if (property != null) property.objectReferenceValue = nameValuePairs[index + 1] as Object;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static void SetComponentReference(Component component, string propertyName, Object value)
        {
            var serialized = new SerializedObject(component);
            SetReference(serialized, propertyName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void ConfigureBootstrapScene(GameConfig config)
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var loaderObject = GameObject.Find("BootstrapLobbyLoader");
            if (loaderObject == null) loaderObject = new GameObject("BootstrapLobbyLoader");
            var loader = loaderObject.GetComponent<BootstrapLobbyLoader>();
            if (loader == null) loader = loaderObject.AddComponent<BootstrapLobbyLoader>();
            SetComponentReference(loader, "gameConfig", config);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureMansionScene(GameConfig config)
        {
            var scene = EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SetComponentReference(door, "gameConfig", config);

            var clock = Object.FindFirstObjectByType<NightClock>(FindObjectsInactive.Include);
            var endScreen = Object.FindFirstObjectByType<GameEndScreen>(FindObjectsInactive.Include);
            if (clock != null)
            {
                if (endScreen == null) endScreen = new GameObject("GameEndScreen").AddComponent<GameEndScreen>();
                var serialized = new SerializedObject(endScreen);
                SetReference(serialized, "nightClock", clock);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif

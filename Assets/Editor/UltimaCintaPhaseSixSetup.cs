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

        public static void VerifyPhaseSix()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Require(config != null, "Falta GameConfig.");
            Require(Mathf.Approximately(config.RealNightDurationMinutes, 180f), "La noche de balance debe durar 180 minutos reales.");
            Require(Mathf.Approximately(config.GameNightDurationHours, 8f), "La noche del juego debe representar ocho horas.");
            Require(config.Audio != null, "Falta GameAudioConfig serializado.");
            var audioClipCount = VerifyAudioCues();

            var player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Require(player.GetComponent<PlayerAudioController>() != null, "Falta audio del jugador en Player.prefab.");
                Require(player.GetComponent<SubtitleOverlay>() != null, "Faltan subtítulos en Player.prefab.");
                Require(player.GetComponent<PauseMenuController>() != null, "Falta menú de pausa en Player.prefab.");
                Require(player.GetComponent<VanTutorialGuide>() != null, "Falta tutorial en Player.prefab.");
                Require(HasReference(player.GetComponent<PlayerAudioController>(), "gameConfig", config), "Audio del jugador no apunta a GameConfig.");
                Require(HasReference(player.GetComponent<PauseMenuController>(), "gameConfig", config), "Pausa no apunta a GameConfig.");
                var recorder = player.GetComponent<CamcorderRecording>();
                Require(recorder != null && HasReference(recorder, "gameConfig", config), "La grabación de camcorder no está configurada en Player.prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }

            var profiles = new System.Collections.Generic.HashSet<GhostSoundProfile>();
            var ghostCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:GhostType", new[] { "Assets/Scripts/Data/Ghosts" }))
            {
                var ghostType = AssetDatabase.LoadAssetAtPath<GhostType>(AssetDatabase.GUIDToAssetPath(guid));
                if (ghostType == null) continue;
                ghostCount++;
                profiles.Add(ghostType.SoundProfile);
                var prefabPath = AssetDatabase.GetAssetPath(ghostType.GhostPrefab);
                var ghostPrefab = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var agent = ghostPrefab.GetComponentInChildren<GhostAgent>(true);
                    var audio = agent != null ? agent.GetComponent<GhostAudioController>() : null;
                    Require(audio != null, $"{ghostType.DisplayName} no tiene GhostAudioController.");
                    Require(HasReference(audio, "gameConfig", config), $"Audio de {ghostType.DisplayName} no apunta a GameConfig.");
                }
                finally { PrefabUtility.UnloadPrefabContents(ghostPrefab); }
            }
            Require(ghostCount == 4 && profiles.Count == 4, "Los cuatro fantasmas deben tener perfiles de sonido distintos.");

            var bootstrap = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            Require(bootstrap.isLoaded, "No se pudo cargar Bootstrap.");
            var loader = Object.FindAnyObjectByType<BootstrapLobbyLoader>(FindObjectsInactive.Include);
            Require(loader != null && HasReference(loader, "gameConfig", config), "Bootstrap no tiene menú principal configurado.");

            var mansion = EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);
            var doors = Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include);
            Require(doors.Length > 0, "La mansión no contiene puertas para probar el audio.");
            foreach (var door in doors) Require(HasReference(door, "gameConfig", config), $"La puerta {door.name} no tiene GameConfig.");
            var clock = Object.FindAnyObjectByType<NightClock>(FindObjectsInactive.Include);
            var endScreen = Object.FindAnyObjectByType<GameEndScreen>(FindObjectsInactive.Include);
            var van = Object.FindAnyObjectByType<VanSupplyInteractable>(FindObjectsInactive.Include);
            Require(clock != null && endScreen != null, "La mansión necesita reloj y pantallas de resultado.");
            Require(HasReference(endScreen, "nightClock", clock), "La pantalla final no está conectada al reloj.");
            Require(van != null && HasReference(van, "gameConfig", config), "La camioneta no tiene GameConfig para crear su cabina y monitor.");
            Require(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VanMonitorMaterial.mat") != null, "Falta el material incluido del monitor de la camioneta.");
            Require(mansion.isLoaded, "No se pudo cargar la mansión.");

            Debug.Log($"[Fase 6 QA] PASS | noche={config.RealNightDurationMinutes} min | clips originales={audioClipCount} | fantasmas={ghostCount} | puertas={doors.Length} | Bootstrap, jugador, pausa, tutorial, subtítulos y pantallas enlazados.");
        }

        private static int VerifyAudioCues()
        {
            var clips = new System.Collections.Generic.List<AudioClip>
            {
                AudioCueLibrary.Heartbeat(), AudioCueLibrary.RoomTone(), AudioCueLibrary.Door(true), AudioCueLibrary.Door(false),
                AudioCueLibrary.Pickup(), AudioCueLibrary.UiClick(), AudioCueLibrary.Milestone()
            };
            for (var index = 0; index < 3; index++) clips.Add(AudioCueLibrary.Footstep(index));

            var ghostClipNames = new System.Collections.Generic.HashSet<string>();
            foreach (GhostSoundProfile profile in System.Enum.GetValues(typeof(GhostSoundProfile)))
                foreach (GhostState state in System.Enum.GetValues(typeof(GhostState)))
                {
                    var clip = AudioCueLibrary.GhostCue(profile, state);
                    Require(clip != null && clip.samples > 0, $"Falta el clip de {profile}/{state}.");
                    Require(ghostClipNames.Add(clip.name), $"El sonido de {profile}/{state} se repite.");
                    clips.Add(clip);
                }

            foreach (var clip in clips) Require(clip != null && clip.samples > 0, "El catálogo de audio tiene un clip vacío.");
            return clips.Count;
        }

        private static bool HasReference(Object component, string propertyName, Object expected)
        {
            if (component == null) return false;
            var property = new SerializedObject(component).FindProperty(propertyName);
            return property != null && property.objectReferenceValue == expected;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException($"[Fase 6 QA] FAIL | {message}");
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
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include))
                SetComponentReference(door, "gameConfig", config);

            var clock = Object.FindAnyObjectByType<NightClock>(FindObjectsInactive.Include);
            var endScreen = Object.FindAnyObjectByType<GameEndScreen>(FindObjectsInactive.Include);
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

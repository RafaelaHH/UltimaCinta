#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.Interaction;
namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseFourSetup
    {
        private const string ScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private const string ConfigPath = "Assets/Scripts/Data/GameConfig.asset";
        private const string TestScenePath = "Assets/Scenes/PhaseFourTest.unity";
        private const string TestConfigPath = "Assets/Scripts/Data/GameConfig_PhaseFourTest.asset";
        [MenuItem("Tools/UltimaCinta/Fase 4/Crear escena acelerada de prueba")]
        public static void CreateAcceleratedTestScene()
        {
            ConfigureMansion();
            var sourceConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (sourceConfig == null) return;
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(TestConfigPath) == null)
            {
                var testConfig = Object.Instantiate(sourceConfig);
                testConfig.name = "GameConfig_PhaseFourTest";
                AssetDatabase.CreateAsset(testConfig, TestConfigPath);
            }
            var accelerated = AssetDatabase.LoadAssetAtPath<GameConfig>(TestConfigPath);
            var configSerialized = new SerializedObject(accelerated);
            SetFloat(configSerialized, "realNightDurationMinutes", 5f);
            SetFloat(configSerialized, "ritualWaveDurationSeconds", 10f);
            configSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(accelerated);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureTestPlayer(scene);
            foreach (var component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component == null || component.gameObject.scene != scene) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.FindProperty("gameConfig");
                if (property == null) continue;
                property.objectReferenceValue = accelerated;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Escena acelerada creada: PhaseFourTest.unity (noche completa en 5 minutos).");
        }
        private static void EnsureTestPlayer(UnityEngine.SceneManagement.Scene scene)
        {
            const string playerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            var van = Object.FindFirstObjectByType<VanSupplyInteractable>();
            if (van == null)
            {
                Debug.LogError("Escena de prueba: falta la camioneta equipada.");
                return;
            }
            if (!van.TryGetApproachPose(out var startPosition, out var startRotation))
            {
                Debug.LogError("Escena de prueba: la camioneta no tiene collider de acceso.");
                return;
            }
            var movement = Object.FindFirstObjectByType<UltimaCinta.Player.FirstPersonMovementController>();
            if (movement == null && playerPrefab == null)
            {
                Debug.LogError("Escena de prueba: falta Player.prefab.");
                return;
            }
            var player = movement != null ? movement.gameObject : PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (player == null) return;
            player.transform.SetPositionAndRotation(startPosition, startRotation);
        }

        public static void BuildAcceleratedTestPlayer()
        {
            var outputPath = "Builds/PhaseFourTest.exe";
            var arguments = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == "-buildOutput") { outputPath = arguments[index + 1]; break; }
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { TestScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("No se pudo compilar PhaseFourTest: " + report.summary.result);
            Debug.Log("Build de prueba creado en " + report.summary.outputPath);
        }

        [MenuItem("Tools/UltimaCinta/Fase 4/Configurar reloj y ritual")]
        public static void ConfigureMansion()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var world = Object.FindFirstObjectByType<GhostWorldController>();
            var itemPoints = FindNamed("POI_ObjetoRitual");
            var ritualRoom = FindNamed("POI_SalaFavorita_Candidata_B-RIT");
            if (config == null || world == null || itemPoints.Count < 3 || ritualRoom.Count == 0)
            {
                Debug.LogError("Fase 4: falta GameConfig, GhostWorldController, POI de objetos o marcador B-RIT.");
                return;
            }
            EnsureNightConfigDefaults(config);
            var root = GameObject.Find("RitualRoomSetup");
            if (root == null) root = new GameObject("RitualRoomSetup");
            var circle = root.transform.Find("RitualCircle");
            if (circle == null) circle = BuildCircle(root.transform, ritualRoom[0].transform.position);
            var circleObject = circle.gameObject;
            var trigger = circleObject.GetComponent<SphereCollider>();
            if (trigger == null) trigger = circleObject.AddComponent<SphereCollider>();
            trigger.radius = 1.2f;
            trigger.isTrigger = true;
            var circleInteraction = circleObject.GetComponent<RitualCircleInteractable>();
            if (circleInteraction == null) circleInteraction = circleObject.AddComponent<RitualCircleInteractable>();
            var candles = new Transform[3];
            for (var index = 0; index < candles.Length; index++)
            {
                var candleName = "Vela_" + (index + 1);
                var candle = circle.Find(candleName);
                if (candle == null)
                {
                    var candleObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    candleObject.name = candleName;
                    candleObject.transform.SetParent(circle, false);
                    candleObject.transform.localPosition = Quaternion.Euler(0f, index * 120f, 0f) * Vector3.forward * 1.05f + Vector3.up * 0.28f;
                    candleObject.transform.localScale = new Vector3(0.08f, 0.28f, 0.08f);
                    Object.DestroyImmediate(candleObject.GetComponent<Collider>());
                    var lightObject = new GameObject("Llama");
                    lightObject.transform.SetParent(candleObject.transform, false);
                    lightObject.transform.localPosition = Vector3.up * 0.75f;
                    var light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.56f, 0.23f);
                    light.range = config.RitualCandleLightRange;
                    light.intensity = config.RitualCandleLightIntensity;
                    light.enabled = false;
                    candle = candleObject.transform;
                }
                candles[index] = candle;
            }
            var clockObject = GameObject.Find("NightClock");
            if (clockObject == null) clockObject = new GameObject("NightClock");
            var clock = clockObject.GetComponent<NightClock>();
            if (clock == null) clock = clockObject.AddComponent<NightClock>();
            var ritual = root.GetComponent<RitualController>();
            if (ritual == null) ritual = root.AddComponent<RitualController>();
            var itemTransforms = new Transform[itemPoints.Count];
            for (var index = 0; index < itemPoints.Count; index++) itemTransforms[index] = itemPoints[index].transform;
            var journal = Object.FindFirstObjectByType<GhostJournal>();
            ritual.Configure(config, world, journal, clock, itemTransforms, circle, candles);
            circleInteraction.Configure(ritual);
            clock.Configure(config);
            var endScreen = clockObject.GetComponent<UltimaCinta.UI.GameEndScreen>();
            if (endScreen == null) endScreen = clockObject.AddComponent<UltimaCinta.UI.GameEndScreen>();
            endScreen.Configure(clock);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Fase 4 configurada en Mansion_Generated: {itemTransforms.Length} puntos rituales.");
        }
        private static void EnsureNightConfigDefaults(GameConfig config)
        {
            var serialized = new SerializedObject(config);
            SetIfZero(serialized, "nightStartHour", 22f);
            SetIfZero(serialized, "groundFloorStartHour", 22.5f);
            SetIfZero(serialized, "upperFloorUnlockHour", 0.5f);
            SetIfZero(serialized, "basementUnlockHour", 3f);
            SetIfZero(serialized, "ritualStartHour", 4f);
            SetIfZero(serialized, "ritualWaveDurationSeconds", 30f);
            SetIfZero(serialized, "ritualWaveHuntDurationMultiplier", 0.5f);
            SetIfZero(serialized, "ritualWaveSpeedMultiplier", 0.75f);
            SetIfZero(serialized, "ritualFailureSpeedMultiplier", 1.5f);
            SetIfZero(serialized, "ritualCandleLightRange", 1.8f);
            SetIfZero(serialized, "ritualCandleLightIntensity", 0.75f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
        private static void SetIfZero(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null && property.floatValue <= 0f) property.floatValue = value;
        }
        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }
        private static List<GameObject> FindNamed(string exactName)
        {
            var result = new List<GameObject>();
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name.StartsWith(exactName, System.StringComparison.OrdinalIgnoreCase)) result.Add(transform.gameObject);
            return result;
        }
        private static Transform BuildCircle(Transform parent, Vector3 markerPosition)
        {
            var circle = new GameObject("RitualCircle").transform;
            circle.SetParent(parent, false);
            circle.position = markerPosition;
            var ring = new GameObject("Piedras del circulo").transform;
            ring.SetParent(circle, false);
            for (var index = 0; index < 12; index++)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "Piedra";
                stone.transform.SetParent(ring, false);
                var angle = index * 30f * Mathf.Deg2Rad;
                stone.transform.localPosition = new Vector3(Mathf.Sin(angle) * 1.25f, 0.04f, Mathf.Cos(angle) * 1.25f);
                stone.transform.localRotation = Quaternion.Euler(0f, -index * 30f, 0f);
                stone.transform.localScale = new Vector3(0.55f, 0.08f, 0.22f);
                Object.DestroyImmediate(stone.GetComponent<Collider>());
            }
            return circle;
        }
    }
}
#endif

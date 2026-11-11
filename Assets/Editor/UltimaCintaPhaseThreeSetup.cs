#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UltimaCinta.Data;
using UltimaCinta.Ghost;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseThreeSetup
    {
        private const string DataRoot = "Assets/Scripts/Data/Ghosts";
        private const string PrefabRoot = "Assets/Prefabs/Generated/Fantasmas";
        private const string RitualRoot = "Assets/Scripts/Data/RitualItems";
        private const string ConfigPath = "Assets/Scripts/Data/GameConfig.asset";
        private const string FootageProfilePath = "Assets/Settings/FoundFootageProfile.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string MansionScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private const float GhostVisualTargetHeight = 2.6f;

        [MenuItem("Tools/UltimaCinta/Fase 3/Activar fantasma en Mansion_Generated")]
        public static void ConfigureMansionGhosts()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var catalog = LoadCatalog();
            if (config == null || catalog.Length != 4)
            {
                EditorUtility.DisplayDialog("Fase 3", "Primero ejecuta 'Crear datos y prefabs de fantasmas'.", "Aceptar");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MansionScenePath)
                scene = EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var roomPoints = FindNamedObjects("POI_SalaFavorita");
            if (roomPoints.Count == 0)
            {
                EditorUtility.DisplayDialog("Fase 3", "Mansion_Generated no contiene POI_SalaFavorita.", "Aceptar");
                return;
            }

            var roomMarkers = new List<GhostRoomMarker>(roomPoints.Count);
            foreach (var roomPoint in roomPoints)
            {
                var marker = roomPoint.GetComponent<GhostRoomMarker>();
                if (marker == null) marker = Undo.AddComponent<GhostRoomMarker>(roomPoint);
                var markerSerialized = new SerializedObject(marker);
                markerSerialized.FindProperty("roomName").stringValue = roomPoint.transform.parent != null
                    ? roomPoint.transform.parent.name
                    : roomPoint.name;
                markerSerialized.ApplyModifiedProperties();
                roomMarkers.Add(marker);
            }

            ConfigureMansionEvidence(config, scene);

            GhostWorldController world = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<GhostWorldController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == scene) { world = candidate; break; }
            if (world == null)
            {
                var worldObject = new GameObject("GhostWorldController");
                Undo.RegisterCreatedObjectUndo(worldObject, "Create ghost world controller");
                world = worldObject.AddComponent<GhostWorldController>();
            }

            var worldSerialized = new SerializedObject(world);
            worldSerialized.FindProperty("gameConfig").objectReferenceValue = config;
            var typesProperty = worldSerialized.FindProperty("ghostTypes");
            typesProperty.arraySize = catalog.Length;
            for (var index = 0; index < catalog.Length; index++)
                typesProperty.GetArrayElementAtIndex(index).objectReferenceValue = catalog[index];
            var roomsProperty = worldSerialized.FindProperty("favoriteRoomMarkers");
            roomsProperty.arraySize = roomMarkers.Count;
            for (var index = 0; index < roomMarkers.Count; index++)
                roomsProperty.GetArrayElementAtIndex(index).objectReferenceValue = roomMarkers[index];
            worldSerialized.FindProperty("randomizeAtStart").boolValue = true;
            worldSerialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Fase 3: Mansion_Generated ya inicia un fantasma al azar en una de {roomMarkers.Count} salas favoritas.", world);
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Completar configuración de escena jugable")]
        public static void CompletePlayableSceneSetup()
        {
            MansionDecorator.DecorateMansion();
            MansionGenerator.ApplyMansionSurfaceTextures();
            MansionGenerator.ProtectStairOpenings();
            CreateGhostAssets();
            ConfigureInventoryItemAssets();
            EnableGhostsInCamcorder();
            ConfigureMansionGhosts();
            ConfigureConsumablePickups();
            ValidateEvidenceTable();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Aplicar consumibles y modelos de objetos")]
        public static void ApplyConsumableAndItemSetup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += ApplyConsumableAndItemSetup;
                return;
            }
            ConfigureGhostGameConfig();
            CreateGhostAssets();
            ConfigureInventoryItemAssets();
            EnableGhostsInCamcorder();
            ConfigureMansionGhosts();
            ConfigureConsumablePickups();
            RefreshGhostVisualScale();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RefreshGhostVisualScale()
        {
            foreach (var prefabPath in new[] { $"{PrefabRoot}/Ghoul.prefab", $"{PrefabRoot}/Vampiro.prefab", $"{PrefabRoot}/Poltergeist.prefab", $"{PrefabRoot}/Espectro.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    NormalizeGhostVisual(root);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static void ConfigureInventoryItemAssets()
        {
            var pillsPath = "Assets/Generated/KeyItems/pastillas.asset";
            var pills = AssetDatabase.LoadAssetAtPath<InventoryItem>(pillsPath);
            if (pills == null)
            {
                pills = ScriptableObject.CreateInstance<InventoryItem>();
                AssetDatabase.CreateAsset(pills, pillsPath);
            }
            SetInventoryItem(pills, "Pastillas de cordura", null, InventoryItemKind.SanityPills);

            var mappings = new (string Path, string Model, InventoryItemKind Kind)[]
            {
                ("Assets/Generated/KeyItems/bateria.asset", "Batería.fbx", InventoryItemKind.CameraBattery),
                ("Assets/Generated/KeyItems/KEY_UPPER.asset", "llave.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/KEY_BASEMENT.asset", "llave.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/keyupper.asset", "llave.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/keybasement.asset", "llave.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/linterna.asset", "linterna.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/camcorder.asset", "Camcorder.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/diario.asset", "Diario.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/cinta.asset", "Cinta.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/botelladeagua.asset", "botella de agua.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/termometro.asset", "Termómetro.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/medidoremf.asset", "Medidor EMF.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/librodeescritura.asset", "Libro de escritura.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/velablanca.asset", "vela blanca.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/incienso.asset", "incienso.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/ajo.asset", "ajo.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/estaca.asset", "estaca.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/amuleto.asset", "amuleto.fbx", InventoryItemKind.Generic),
                ("Assets/Generated/KeyItems/aguabendita.asset", "Agua bendita.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Agua bendita.asset", "Agua bendita.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Ajo.asset", "ajo.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Amuleto.asset", "amuleto.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Campana.asset", "Ritual/campana.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Crucifijo.asset", "Ritual/crusifijo.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Espejo.asset", "Ritual/espejo roto.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Estaca.asset", "estaca.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Incienso.asset", "incienso.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Sal.asset", "Ritual/sal.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Vela blanca.asset", "vela blanca.fbx", InventoryItemKind.Generic),
                ("Assets/Scripts/Data/RitualItems/Vela negra.asset", "Ritual/vela negra.fbx", InventoryItemKind.Generic)
            };
            foreach (var mapping in mappings)
            {
                var item = AssetDatabase.LoadAssetAtPath<InventoryItem>(mapping.Path);
                if (item == null) continue;
                var modelPath = mapping.Model.StartsWith("Ambientacion/", StringComparison.Ordinal) || mapping.Model.StartsWith("Ritual/", StringComparison.Ordinal)
                    ? "Assets/Generated/" + mapping.Model
                    : "Assets/Generated/Herramientas/" + mapping.Model;
                SetInventoryItem(item, item.DisplayName, AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), mapping.Kind);
            }
            AssetDatabase.SaveAssets();
        }

        private static void SetInventoryItem(InventoryItem item, string displayName, GameObject visual, InventoryItemKind kind)
        {
            var serialized = new SerializedObject(item);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("visualPrefab").objectReferenceValue = visual;
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureConsumablePickups()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MansionScenePath) return;
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var pill = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Generated/KeyItems/pastillas.asset");
            var battery = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Generated/KeyItems/bateria.asset");
            var rooms = FindNamedObjects("POI_SalaFavorita");
            if (config == null || rooms.Count == 0) return;
            CreateConsumableSet("Pastillas", pill, config.SanityPillPickupCount, rooms, 0);
            CreateConsumableSet("Batería", battery, config.CameraBatteryPickupCount, rooms, 2);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateConsumableSet(string label, InventoryItem item, int count, List<GameObject> rooms, int offset)
        {
            if (item == null || count <= 0) return;
            for (var index = 0; index < count; index++)
            {
                var pickupName = $"Consumible_{label}_{index + 1:00}";
                var pickup = GameObject.Find(pickupName);
                if (pickup == null) pickup = new GameObject(pickupName);
                var roomIndex = (index * rooms.Count / count + offset) % rooms.Count;
                var room = rooms[roomIndex];
                pickup.transform.SetPositionAndRotation(room.transform.position + Vector3.up * 0.2f,
                    Quaternion.Euler(0f, (index * 97f) % 360f, 0f));
                var collider = pickup.GetComponent<SphereCollider>();
                if (collider == null) collider = pickup.AddComponent<SphereCollider>();
                collider.radius = 0.35f;
                collider.isTrigger = false;
                var interactable = pickup.GetComponent<UltimaCinta.Interaction.PickupInteractable>();
                if (interactable == null) interactable = pickup.AddComponent<UltimaCinta.Interaction.PickupInteractable>();
                interactable.SetItem(item);
                EditorUtility.SetDirty(pickup);
            }
        }

        private static void ConfigureMansionEvidence(GameConfig config, Scene scene)
        {
            var points = FindNamedObjects("POI_Evidencia");
            var evidenceValues = (GhostEvidence[])Enum.GetValues(typeof(GhostEvidence));
            var existing = UnityEngine.Object.FindObjectsByType<EvidenceTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                var terminalName = $"EvidenceTerminal_{point.name}";
                EvidenceTerminal terminal = null;
                foreach (var candidate in existing)
                {
                    if (candidate.gameObject.scene == scene && candidate.name == terminalName)
                    {
                        terminal = candidate;
                        break;
                    }
                }

                if (terminal == null)
                {
                    var terminalObject = new GameObject(terminalName);
                    Undo.RegisterCreatedObjectUndo(terminalObject, "Create evidence terminal");
                    terminalObject.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                    terminal = terminalObject.AddComponent<EvidenceTerminal>();
                    existing = UnityEngine.Object.FindObjectsByType<EvidenceTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                }

                var sphere = terminal.GetComponent<SphereCollider>();
                if (sphere == null) sphere = Undo.AddComponent<SphereCollider>(terminal.gameObject);
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                var serialized = new SerializedObject(terminal);
                serialized.FindProperty("evidence").enumValueIndex = index % evidenceValues.Length;
                serialized.FindProperty("gameConfig").objectReferenceValue = config;
                serialized.ApplyModifiedProperties();
            }

            Debug.Log($"Fase 3: {points.Count} puntos de investigación configurados en Mansion_Generated.");
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Crear datos y prefabs de fantasmas")]
        public static void CreateGhostAssets()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(RitualRoot);
            ConfigureGhostGameConfig();
            EnsureSanityDepthOfField(AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath));
            var ritualItems = CreateRitualItems();
            ConfigureInventoryItemAssets();
            var catalog = new List<GhostType>();
            catalog.Add(CreateGhost("Ghoul", "Ghoul.fbx", new[] { GhostEvidence.Apparition, GhostEvidence.FreezingTemperature, GhostEvidence.Writing },
                GhostBehavior.Slow | GhostBehavior.Resilient | GhostBehavior.HuntsBySound | GhostBehavior.Crawls,
                new[] { "Sal", "Campana", "Vela negra" }, ritualItems, 1.7f));
            catalog.Add(CreateGhost("Vampiro", "Vampiro.fbx", new[] { GhostEvidence.EmfLevelFive, GhostEvidence.FreezingTemperature, GhostEvidence.Orbs },
                GhostBehavior.HuntsOnlyInDarkness | GhostBehavior.DrainsBatteryNearby | GhostBehavior.RepelledByLight | GhostBehavior.RepelledByGarlic | GhostBehavior.DoesNotAppearInMirrors,
                new[] { "Ajo", "Estaca", "Espejo" }, ritualItems, 2.1f));
            catalog.Add(CreateGhost("Poltergeist", "Poltergeist.fbx", new[] { GhostEvidence.EmfLevelFive, GhostEvidence.Writing, GhostEvidence.Orbs },
                GhostBehavior.ThrowsObjects | GhostBehavior.SlamsDoors | GhostBehavior.FrequentShortHunts | GhostBehavior.NoiseReducesSanity,
                new[] { "Incienso", "Crucifijo", "Agua bendita" }, ritualItems, 2.2f));
            catalog.Add(CreateGhost("Espectro", "Espectro.fbx", new[] { GhostEvidence.Apparition, GhostEvidence.EmfLevelFive, GhostEvidence.Orbs },
                GhostBehavior.PassesThroughWalls | GhostBehavior.TeleportsNearPlayers | GhostBehavior.Fast | GhostBehavior.Silent | GhostBehavior.StoppedBySalt,
                new[] { "Sal", "Amuleto", "Vela blanca" }, ritualItems, 3.2f));
            ConfigurePlayerPrefab(catalog.ToArray());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fase 3: creados/actualizados los cuatro GhostType, prefabs, objetos de ritual y componentes locales del jugador.");
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Activar fantasmas en la cámara")]
        public static void EnableGhostsInCamcorder()
        {
            var ghostLayer = LayerMask.NameToLayer("Ghost");
            if (ghostLayer < 0)
            {
                Debug.LogError("Fase 3: no existe la capa Ghost en Project Settings > Tags and Layers.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var cameras = root.GetComponentsInChildren<Camera>(true);
                if (cameras.Length == 0)
                {
                    Debug.LogError("Fase 3: Player.prefab no contiene una cámara.", root);
                    return;
                }

                foreach (var camera in cameras)
                    camera.cullingMask |= 1 << ghostLayer;

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.Refresh();
            Debug.Log("Fase 3: la cámara del jugador ahora renderiza la capa Ghost.");
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Preparar escena de prueba")]
        public static void PrepareTestScene()
        {
            var scenePath = "Assets/Scenes/PhaseThreeTest.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null &&
                !EditorUtility.DisplayDialog("Fase 3", "La escena de prueba ya existe. ¿Sobrescribirla desde Mansion_Generated?", "Sobrescribir", "Cancelar")) return;

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var catalog = LoadCatalog();
            if (config == null || catalog.Length != 4)
            {
                EditorUtility.DisplayDialog("Fase 3", "Primero ejecuta 'Crear datos y prefabs de fantasmas'.", "Aceptar");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);
            var markers = FindNamedObjects("POI_SalaFavorita");
            var roomMarkers = new List<GhostRoomMarker>();
            foreach (var markerObject in markers)
            {
                var marker = markerObject.GetComponentInChildren<GhostRoomMarker>(true);
                if (marker == null)
                {
                    var markerName = $"GhostRoomMarker_{markerObject.name}";
                    foreach (var existingMarker in UnityEngine.Object.FindObjectsByType<GhostRoomMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (existingMarker.gameObject.scene == scene && existingMarker.name == markerName) { marker = existingMarker; break; }
                    if (marker == null)
                    {
                        var markerPoint = new GameObject(markerName);
                        Undo.RegisterCreatedObjectUndo(markerPoint, "Create ghost room marker");
                        markerPoint.transform.SetPositionAndRotation(markerObject.transform.position, markerObject.transform.rotation);
                        marker = markerPoint.AddComponent<GhostRoomMarker>();
                    }
                }
                roomMarkers.Add(marker);
            }
            if (roomMarkers.Count == 0)
            {
                EditorUtility.DisplayDialog("Fase 3", "No se encontraron POI_SalaFavorita en Mansion_Generated.", "Aceptar");
                return;
            }

            var evidencePoints = FindNamedObjects("POI_Evidencia");
            var evidenceValues = (GhostEvidence[])Enum.GetValues(typeof(GhostEvidence));
            var sceneTerminals = UnityEngine.Object.FindObjectsByType<EvidenceTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < evidencePoints.Count; index++)
            {
                var point = evidencePoints[index];
                EvidenceTerminal terminal = null;
                var terminalName = $"EvidenceTerminal_{point.name}";
                foreach (var existingTerminal in sceneTerminals)
                {
                    if (existingTerminal.gameObject.scene != scene) continue;
                    if (existingTerminal.name == terminalName) { terminal = existingTerminal; break; }
                    if (existingTerminal.transform == point.transform)
                    {
                        existingTerminal.enabled = false;
                    }
                }
                if (terminal == null)
                {
                    var terminalObject = new GameObject(terminalName);
                    Undo.RegisterCreatedObjectUndo(terminalObject, "Create evidence terminal");
                    terminalObject.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                    terminal = terminalObject.AddComponent<EvidenceTerminal>();
                    sceneTerminals = UnityEngine.Object.FindObjectsByType<EvidenceTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                }
                var sphere = terminal.GetComponent<SphereCollider>();
                if (sphere == null) sphere = terminal.gameObject.AddComponent<SphereCollider>();
                if (sphere == null)
                {
                    Debug.LogError($"No se pudo crear SphereCollider para {point.name}.", point);
                    continue;
                }
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                var serialized = new SerializedObject(terminal);
                serialized.FindProperty("evidence").enumValueIndex = index % evidenceValues.Length;
                serialized.FindProperty("gameConfig").objectReferenceValue = config;
                serialized.ApplyModifiedProperties();
            }

            var worldObject = new GameObject("GhostWorldController");
            Undo.RegisterCreatedObjectUndo(worldObject, "Create ghost world controller");
            var world = worldObject.AddComponent<GhostWorldController>();
            var worldSerialized = new SerializedObject(world);
            worldSerialized.FindProperty("gameConfig").objectReferenceValue = config;
            worldSerialized.FindProperty("ghostTypes").arraySize = catalog.Length;
            for (var index = 0; index < catalog.Length; index++) worldSerialized.FindProperty("ghostTypes").GetArrayElementAtIndex(index).objectReferenceValue = catalog[index];
            worldSerialized.FindProperty("favoriteRoomMarkers").arraySize = roomMarkers.Count;
            for (var index = 0; index < roomMarkers.Count; index++) worldSerialized.FindProperty("favoriteRoomMarkers").GetArrayElementAtIndex(index).objectReferenceValue = roomMarkers[index];
            worldSerialized.FindProperty("randomizeAtStart").boolValue = true;
            worldSerialized.ApplyModifiedProperties();

            var playerControllers = UnityEngine.Object.FindObjectsByType<UltimaCinta.Player.FirstPersonMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject testPlayer = null;
            foreach (var playerController in playerControllers)
            {
                if (playerController.gameObject.scene != scene) continue;
                if (testPlayer == null)
                {
                    testPlayer = playerController.gameObject;
                    continue;
                }

                Undo.DestroyObjectImmediate(playerController.gameObject);
            }

            if (testPlayer == null)
            {
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (playerPrefab != null)
                {
                    testPlayer = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
                    if (testPlayer != null) Undo.RegisterCreatedObjectUndo(testPlayer, "Create phase three test player");
                }
            }

            if (testPlayer != null)
            {
                Undo.RecordObject(testPlayer.transform, "Position phase three test player");
                testPlayer.transform.SetPositionAndRotation(roomMarkers[0].transform.position + Vector3.right * 3f, roomMarkers[0].transform.rotation);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Fase 3", "Escena creada: Assets/Scenes/PhaseThreeTest.unity", "Aceptar");
        }

        [MenuItem("Tools/UltimaCinta/Fase 3/Validar tabla de evidencias")]
        public static void ValidateEvidenceTable()
        {
            var catalog = LoadCatalog();
            if (catalog.Length != 4) { Debug.LogError("Se esperan cuatro GhostType en Assets/Scripts/Data/Ghosts."); return; }
            var expected = new Dictionary<string, GhostEvidence[]>(StringComparer.Ordinal)
            {
                ["Ghoul"] = new[] { GhostEvidence.Apparition, GhostEvidence.FreezingTemperature, GhostEvidence.Writing },
                ["Vampiro"] = new[] { GhostEvidence.EmfLevelFive, GhostEvidence.FreezingTemperature, GhostEvidence.Orbs },
                ["Poltergeist"] = new[] { GhostEvidence.EmfLevelFive, GhostEvidence.Writing, GhostEvidence.Orbs },
                ["Espectro"] = new[] { GhostEvidence.Apparition, GhostEvidence.EmfLevelFive, GhostEvidence.Orbs }
            };
            var valid = true;
            foreach (var ghost in catalog)
            {
                if (!expected.TryGetValue(ghost.DisplayName, out var expectedEvidence))
                {
                    valid = false;
                    Debug.LogError($"Tipo de fantasma inesperado: {ghost.DisplayName}", ghost);
                    continue;
                }
                var actual = new HashSet<GhostEvidence>(ghost.Evidences);
                var wanted = new HashSet<GhostEvidence>(expectedEvidence);
                if (!actual.SetEquals(wanted))
                {
                    valid = false;
                    Debug.LogError($"Tabla incorrecta para {ghost.DisplayName}. Actual: {string.Join(", ", actual)}. Esperada: {string.Join(", ", wanted)}.", ghost);
                }
            }
            if (valid) Debug.Log("Tabla validada: Ghoul A/C/D, Vampiro B/C/E, Poltergeist B/D/E, Espectro A/B/E.");
        }

        private static GhostType CreateGhost(string displayName, string modelFile, GhostEvidence[] evidence, GhostBehavior behavior, string[] ritualNames,
            Dictionary<string, InventoryItem> items, float speed)
        {
            var assetPath = $"{DataRoot}/{displayName}.asset";
            var ghostType = AssetDatabase.LoadAssetAtPath<GhostType>(assetPath);
            if (ghostType == null)
            {
                ghostType = ScriptableObject.CreateInstance<GhostType>();
                AssetDatabase.CreateAsset(ghostType, assetPath);
            }
            var modelPath = $"Assets/Generated/Fantasmas/{modelFile}";
            var prefabPath = $"{PrefabRoot}/{displayName}.prefab";
            var prefab = CreateGhostPrefab(modelPath, prefabPath);
            var serialized = new SerializedObject(ghostType);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("movementSpeed").floatValue = speed;
            serialized.FindProperty("behavior").intValue = (int)behavior;
            var evidenceProperty = serialized.FindProperty("evidences");
            evidenceProperty.arraySize = evidence.Length;
            for (var index = 0; index < evidence.Length; index++) evidenceProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)evidence[index];
            var ritualProperty = serialized.FindProperty("ritualItems");
            ritualProperty.arraySize = ritualNames.Length;
            for (var index = 0; index < ritualNames.Length; index++) ritualProperty.GetArrayElementAtIndex(index).objectReferenceValue = items[ritualNames[index]];
            serialized.FindProperty("ghostPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ghostType);
            return ghostType;
        }

        private static GameObject CreateGhostPrefab(string modelPath, string prefabPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var isPrefabContents = existing != null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null && existing == null) { Debug.LogError($"Falta modelo {modelPath}; no se creó prefab."); return null; }
            var instance = isPrefabContents ? PrefabUtility.LoadPrefabContents(prefabPath) : PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) return null;
            instance.name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            SetLayerRecursively(instance.transform, LayerMask.NameToLayer("Ghost"));
            if (instance.GetComponent<Collider>() == null) instance.AddComponent<CapsuleCollider>();
            if (instance.GetComponent<GhostAgent>() == null) instance.AddComponent<GhostAgent>();
            var agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null) agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"No se pudo añadir NavMeshAgent al prefab {prefabPath}.", instance);
                if (isPrefabContents) PrefabUtility.UnloadPrefabContents(instance);
                else UnityEngine.Object.DestroyImmediate(instance);
                return null;
            }
            agent.baseOffset = 0f;
            agent.height = GhostVisualTargetHeight;
            agent.updateRotation = true;
            NormalizeGhostVisual(instance);
            if (instance.GetComponent<GhostModelMotion>() == null) instance.AddComponent<GhostModelMotion>();
            if (instance.GetComponent<GhostOrbField>() == null) instance.AddComponent<GhostOrbField>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (isPrefabContents) PrefabUtility.UnloadPrefabContents(instance);
            else UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void NormalizeGhostVisual(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            var visualRoot = renderers[0].transform;
            while (visualRoot.parent != null && visualRoot.parent != root.transform) visualRoot = visualRoot.parent;
            if (visualRoot == root.transform || bounds.size.y <= 0.01f) return;

            visualRoot.localScale *= GhostVisualTargetHeight / bounds.size.y;
            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = GhostVisualTargetHeight;
            capsule.radius = GhostVisualTargetHeight * 0.25f;
            capsule.center = Vector3.up * (GhostVisualTargetHeight * 0.5f);
        }

        private static Dictionary<string, InventoryItem> CreateRitualItems()
        {
            var result = new Dictionary<string, InventoryItem>(StringComparer.Ordinal);
            var names = new[] { "Sal", "Campana", "Vela negra", "Ajo", "Estaca", "Espejo", "Incienso", "Crucifijo", "Agua bendita", "Amuleto", "Vela blanca" };
            foreach (var itemName in names)
            {
                var path = $"{RitualRoot}/{itemName}.asset";
                var item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<InventoryItem>();
                    AssetDatabase.CreateAsset(item, path);
                }
                var serialized = new SerializedObject(item);
                serialized.FindProperty("displayName").stringValue = itemName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
                result[itemName] = item;
            }
            return result;
        }

        private static void ConfigureGhostGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null) { Debug.LogError($"Falta GameConfig: {ConfigPath}"); return; }
            var serialized = new SerializedObject(config);
            SetPositiveDefault(serialized, "ghostWanderIntervalSeconds", 8f);
            SetPositiveDefault(serialized, "ghostActivityIntervalSeconds", 18f);
            SetPositiveDefault(serialized, "ghostRestDurationSeconds", 12f);
            SetPositiveDefault(serialized, "ghostHuntDurationMinimumSeconds", 45f);
            SetPositiveDefault(serialized, "ghostHuntDurationMaximumSeconds", 90f);
            SetPositiveDefault(serialized, "ghostHuntCooldownSeconds", 30f);
            SetPositiveDefault(serialized, "ghostInitialHuntGraceSeconds", 75f);
            SetPositiveDefault(serialized, "ghostMovementSpeedMultiplier", 0.5f);
            SetPositiveDefault(serialized, "ghostPostHuntRetreatSeconds", 50f);
            SetPositiveDefault(serialized, "ghostHuntDistance", 1.4f);
            SetPositiveDefault(serialized, "ghostHuntStartAggressionMinimum", 0.25f);
            SetPositiveDefault(serialized, "ghostHuntStartAggressionMaximum", 0.75f);
            SetPositiveDefault(serialized, "ghostHuntStartDistance", 18f);
            SetPositiveDefault(serialized, "ghostScreamerRangeMeters", 4f);
            SetPositiveDefault(serialized, "ghostScreamerDurationSeconds", 0.45f);
            SetPositiveDefault(serialized, "ghostInterferenceRange", 16f);
            SetPositiveDefault(serialized, "ghostHearingRange", 14f);
            SetPositiveDefault(serialized, "ghostSanityDrainPerSecond", 0.3f);
            SetPositiveDefault(serialized, "sanityDarknessDrainPerSecond", 0.1f);
            SetPositiveDefault(serialized, "sanityLightRecoveryPerSecond", 0.08f);
            SetPositiveDefault(serialized, "sanityCompanionRecoveryPerSecond", 0.04f);
            SetPositiveDefault(serialized, "hallucinationSanityThreshold", 40f);
            SetPositiveDefault(serialized, "extendedDetectionSanityThreshold", 15f);
            SetPositiveDefault(serialized, "hallucinationIntervalSeconds", 24f);
            SetPositiveDefault(serialized, "evidenceConfirmationRange", 4f);
            SetPositiveDefault(serialized, "ghostTargetRefreshSeconds", 0.5f);
            SetPositiveDefault(serialized, "sanityLightRefreshSeconds", 1f);
            SetPositiveDefault(serialized, "sanityCompanionRange", 8f);
            SetPositiveDefault(serialized, "sanityPlayerRefreshSeconds", 1f);
            SetPositiveDefault(serialized, "playerNoiseIntervalSeconds", 0.8f);
            SetPositiveDefault(serialized, "playerWalkNoiseLoudness", 0.8f);
            SetPositiveDefault(serialized, "playerRunNoiseLoudness", 1.5f);
            SetPositiveDefault(serialized, "poltergeistHuntCooldownMultiplier", 0.4f);
            SetPositiveDefault(serialized, "ghostTeleportRadius", 3f);
            SetPositiveDefault(serialized, "poltergeistObjectThrowRadius", 3f);
            SetPositiveDefault(serialized, "poltergeistObjectThrowForce", 2f);
            SetPositiveDefault(serialized, "poltergeistObjectMaximumSize", 0.7f);
            SetPositiveDefault(serialized, "spectatorLightRange", 30f);
            SetPositiveDefault(serialized, "hallucinationDisplaySeconds", 2f);
            SetPositiveDefault(serialized, "ghostRoomActivationRadius", 7f);
            SetPositiveDefault(serialized, "sanityPillRestore", 45f);
            SetPositiveDefault(serialized, "cameraBatteryRefillFraction", 0.5f);
            SetPositiveDefault(serialized, "sanityWhisperIntervalSeconds", 18f);
            SetPositiveDefault(serialized, "zeroSanityHallucinationIntervalSeconds", 7f);
            SetPositiveDefault(serialized, "sanityWhisperVolume", 0.65f);
            SetPositiveDefault(serialized, "hallucinationDistanceMeters", 2.5f);
            SetPositiveDefault(serialized, "hallucinationDisplayAtZeroSeconds", 1.5f);
            SetPositiveDefault(serialized, "sanityBreathingStartThreshold", 15f);
            SetPositiveDefault(serialized, "staminaBreathingStartThreshold", 0.45f);
            SetPositiveDefault(serialized, "staminaBreathingMaximumVolume", 0.58f);
            SetPositiveDefault(serialized, "staminaBreathingMaximumRate", 1.45f);
            SetPositiveDefault(serialized, "sanityBlurStartThreshold", 55f);
            SetPositiveDefault(serialized, "sanityBreathingMaximumVolume", 0.72f);
            SetPositiveDefault(serialized, "sanityBreathingMaximumRate", 1.8f);
            SetPositiveDefault(serialized, "sanityBlurMaximumRadius", 1.35f);
            SetPositiveDefault(serialized, "sanityBlurFarStartMeters", 2f);
            SetPositiveDefault(serialized, "sanityBlurFarEndMeters", 5f);
            var pillCount = serialized.FindProperty("sanityPillPickupCount");
            if (pillCount != null && pillCount.intValue <= 0) pillCount.intValue = 4;
            var batteryCount = serialized.FindProperty("cameraBatteryPickupCount");
            if (batteryCount != null && batteryCount.intValue <= 0) batteryCount.intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void SetPositiveDefault(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null && property.floatValue <= 0f) property.floatValue = value;
        }

        private static void EnsureSanityDepthOfField(GameConfig config)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FootageProfilePath);
            if (profile == null || config == null) return;
            var depthOfField = profile.TryGet<DepthOfField>(out var existing)
                ? existing
                : VolumeProfileFactory.CreateVolumeComponent<DepthOfField>(profile, true, false);
            depthOfField.mode.overrideState = true;
            depthOfField.mode.value = DepthOfFieldMode.Gaussian;
            depthOfField.gaussianStart.overrideState = true;
            depthOfField.gaussianStart.value = config.SanityBlurFarStartMeters;
            depthOfField.gaussianEnd.overrideState = true;
            depthOfField.gaussianEnd.value = config.SanityBlurFarEndMeters;
            depthOfField.gaussianMaxRadius.overrideState = true;
            depthOfField.gaussianMaxRadius.value = Mathf.Min(0.5f, config.SanityBlurMaximumRadius);
            depthOfField.highQualitySampling.overrideState = true;
            depthOfField.highQualitySampling.value = false;
            depthOfField.active = false;
            EditorUtility.SetDirty(profile);
        }

        private static void ConfigurePlayerPrefab(GhostType[] catalog)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var ghostLayer = LayerMask.NameToLayer("Ghost");
                if (ghostLayer >= 0)
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                        camera.cullingMask |= 1 << ghostLayer;
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
                ConfigureComponent(root, root.GetComponent<UltimaCinta.CameraSystem.FoundFootageEffects>() ?? root.AddComponent<UltimaCinta.CameraSystem.FoundFootageEffects>(), config, null);
                ConfigureComponent(root, root.GetComponent<UltimaCinta.Ghost.SanityController>() ?? root.AddComponent<UltimaCinta.Ghost.SanityController>(), config, null);
                ConfigureComponent(root, root.GetComponent<GhostJournal>() ?? root.AddComponent<GhostJournal>(), null, catalog);
                ConfigureComponent(root, root.GetComponent<GhostCameraInterference>() ?? root.AddComponent<GhostCameraInterference>(), config, null);
                ConfigureComponent(root, root.GetComponent<GhostNoiseEmitter>() ?? root.AddComponent<GhostNoiseEmitter>(), config, null);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureComponent(GameObject root, Component component, GameConfig config, GhostType[] catalog)
        {
            var serialized = new SerializedObject(component);
            var configProperty = serialized.FindProperty("gameConfig");
            if (configProperty != null && config != null) configProperty.objectReferenceValue = config;
            var catalogProperty = serialized.FindProperty("catalog");
            if (catalogProperty != null && catalog != null)
            {
                catalogProperty.arraySize = catalog.Length;
                for (var index = 0; index < catalog.Length; index++) catalogProperty.GetArrayElementAtIndex(index).objectReferenceValue = catalog[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GhostType[] LoadCatalog()
        {
            var list = new List<GhostType>();
            foreach (var guid in AssetDatabase.FindAssets("t:GhostType", new[] { DataRoot }))
            {
                var type = AssetDatabase.LoadAssetAtPath<GhostType>(AssetDatabase.GUIDToAssetPath(guid));
                if (type != null) list.Add(type);
            }
            list.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
            return list.ToArray();
        }

        private static List<GameObject> FindNamedObjects(string prefix)
        {
            var results = new List<GameObject>();
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (transform.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) results.Add(transform.gameObject);
            return results;
        }

        private static void SetLayerRecursively(Transform target, int layer)
        {
            if (layer < 0) { Debug.LogError("No existe la capa Ghost en Project Settings > Tags and Layers."); return; }
            target.gameObject.layer = layer;
            foreach (Transform child in target) SetLayerRecursively(child, layer);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
#endif

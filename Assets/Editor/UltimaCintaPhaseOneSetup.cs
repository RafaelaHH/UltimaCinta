using UltimaCinta.CameraSystem;
using UltimaCinta.Data;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseOneSetup
    {
        private const string ScenePath = "Assets/Scenes/PhaseOneTest.unity";
        private const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string ConfigPath = "Assets/Scripts/Data/GameConfig.asset";
        private const string ProfilePath = "Assets/Settings/FoundFootageProfile.asset";
        private const string ItemPath = "Assets/Scripts/Data/TestPickup.asset";
        private const string Item2Path = "Assets/Scripts/Data/TestPickup2.asset";
        private const string Item3Path = "Assets/Scripts/Data/TestPickup3.asset";
        private const string Item4Path = "Assets/Scripts/Data/TestPickup4.asset";
        private const string GhostLayerName = "Ghost";

        [MenuItem("Tools/UltimaCinta/Preparar Fase 1")]
        public static void Prepare()
        {
            EnsureFolder("Assets/Prefabs/Player");
            EnsureFolder("Assets/Settings");
            EnsureLayer();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null) throw new System.InvalidOperationException("No se encontró GameConfig en " + ConfigPath);
            var profile = CreateFootageProfile();
            var player = CreatePlayer(config, profile);
            CreateTestItem(ItemPath, "Test Tool");
            CreateTestItem(Item2Path, "Test Tool 2");
            CreateTestItem(Item3Path, "Test Tool 3");
            CreateTestItem(Item4Path, "Test Tool 4");
            var itemPaths = new[] { ItemPath, Item2Path, Item3Path, Item4Path };

            CreateTestScene(player, profile, itemPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fase 1 preparada. Prefab: " + PrefabPath + " | Escena: " + ScenePath);
        }

        private static GameObject CreatePlayer(GameConfig config, VolumeProfile profile)
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(0f, 0f, -3f);
            var controller = root.AddComponent<CharacterController>();
            controller.height = config.StandingHeight;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            root.AddComponent<PlayerInputReader>();
            var stamina = root.AddComponent<StaminaResource>();
            Set(stamina, "gameConfig", config);
            root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerActionRouter>();
            var move = root.AddComponent<FirstPersonMovementController>();
            Set(move, "gameConfig", config);
            Set(move, "stamina", stamina);
            Set(move, "inputReader", root.GetComponent<PlayerInputReader>());
            var look = root.AddComponent<FirstPersonLookController>();
            Set(look, "gameConfig", config);
            var interaction = root.AddComponent<PlayerInteractionController>();
            Set(interaction, "gameConfig", config);
            var pivot = new GameObject("ViewPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, config.StandingHeight * 0.9f, 0f);
            var camObject = new GameObject("CamcorderCamera");
            camObject.transform.SetParent(pivot, false);
            var camera = camObject.AddComponent<Camera>();
            camera.fieldOfView = config.CameraFieldOfView;
            camera.cullingMask |= 1 << LayerMask.NameToLayer(GhostLayerName);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.nearClipPlane = 0.05f;
            camObject.AddComponent<AudioListener>();
            var resources = root.AddComponent<CameraResourceController>();
            Set(resources, "gameConfig", config);
            Set(resources, "playerCamera", camera);
            root.AddComponent<PlayerCameraInput>();
            root.AddComponent<CamcorderOverlay>();
            var volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            root.AddComponent<FoundFootageEffects>();
            var lightObject = new GameObject("Flashlight");
            lightObject.transform.SetParent(camObject.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 12f;
            light.intensity = 3f;
            var flashlight = root.AddComponent<FlashlightController>();
            Set(flashlight, "gameConfig", config);
            Set(flashlight, "flashlight", light);
            Set(move, "viewPivot", pivot);
            Set(look, "viewPivot", pivot);
            Set(interaction, "view", camObject.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static VolumeProfile CreateFootageProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var grain = GetOrCreateProfileComponent<FilmGrain>(profile);
            grain.active = true;
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Medium1;
            grain.intensity.overrideState = true;
            grain.intensity.value = 0.32f;
            var vignette = GetOrCreateProfileComponent<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.32f;
            var chromatic = GetOrCreateProfileComponent<ChromaticAberration>(profile);
            chromatic.active = true;
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = 0.16f;
            var color = GetOrCreateProfileComponent<ColorAdjustments>(profile);
            color.active = false;
            color.colorFilter.overrideState = true;
            color.postExposure.overrideState = true;
            color.postExposure.value = 1.5f;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrCreateProfileComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            return VolumeProfileFactory.CreateVolumeComponent<T>(profile, true, false);
        }
        private static InventoryItem CreateTestItem(string assetPath, string displayName)
        {
            var item = AssetDatabase.LoadAssetAtPath<InventoryItem>(assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<InventoryItem>();
                item.name = displayName;
                AssetDatabase.CreateAsset(item, assetPath);
            }
            Set(item, "displayName", displayName);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            item = AssetDatabase.LoadAssetAtPath<InventoryItem>(assetPath);
            if (item == null) throw new System.InvalidOperationException($"Could not load InventoryItem asset at {assetPath}.");
            Debug.Log($"Loaded test inventory definition: {item.DisplayName} ({AssetDatabase.GetAssetPath(item)}).");
            return item;
        }

        private static void CreateTestScene(GameObject playerPrefab, VolumeProfile profile, string[] itemPaths)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.22f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test Room Floor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.5f, 12f);
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "Ghost Visibility Test";
            ghost.layer = LayerMask.NameToLayer(GhostLayerName);
            ghost.transform.position = new Vector3(0f, 1f, 2f);
            ghost.GetComponent<Renderer>().material.color = Color.magenta;
            var testDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testDoor.name = "Test Door (E)";
            testDoor.transform.position = new Vector3(3f, 1f, 2f);
            testDoor.transform.localScale = new Vector3(1.8f, 2f, 0.2f);
            var door = testDoor.AddComponent<DoorInteractable>();
            Set(door, "gameConfig", AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath));
            var pickupNames = new[] { "Test Pickup (E)", "Test Pickup 2 (E)", "Test Pickup 3 (E)", "Test Pickup 4 (Full Inventory)" };

            CreateTestPickup(pickupNames[0], new Vector3(2f, 0.4f, 1f), itemPaths[0]);
            CreateTestPickup(pickupNames[1], new Vector3(-2f, 0.4f, 1f), itemPaths[1]);
            CreateTestPickup(pickupNames[2], new Vector3(2f, 0.4f, 4f), itemPaths[2]);
            CreateTestPickup(pickupNames[3], new Vector3(-2f, 0.4f, 4f), itemPaths[3]);
            var switchObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            switchObject.name = "Test Light Switch (E)";
            switchObject.transform.position = new Vector3(-3f, 1.3f, 2f);
            switchObject.transform.localScale = new Vector3(0.35f, 0.45f, 0.12f);
            var switchedLightObject = new GameObject("Switchable Test Light");
            switchedLightObject.transform.position = new Vector3(-3f, 2.6f, 2f);
            var switchedLight = switchedLightObject.AddComponent<Light>();
            switchedLight.type = LightType.Point;
            switchedLight.range = 6f;
            switchedLight.intensity = 1.5f;
            var switchInteractable = switchObject.AddComponent<SwitchInteractable>();
            Set(switchInteractable, "controlledLight", switchedLight);
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Test Room Ceiling";
            ceiling.transform.position = new Vector3(0f, 4f, 0f);
            ceiling.transform.localScale = new Vector3(12f, 0.4f, 12f);
            CreateWall("West Wall", new Vector3(-6f, 2f, 0f), new Vector3(0.4f, 4f, 12f));
            CreateWall("East Wall", new Vector3(6f, 2f, 0f), new Vector3(0.4f, 4f, 12f));
            CreateWall("North Wall", new Vector3(0f, 2f, 6f), new Vector3(12f, 4f, 0.4f));
            CreateWall("South Wall", new Vector3(0f, 2f, -6f), new Vector3(12f, 4f, 0.4f));
            var playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (playerInstance == null) throw new System.InvalidOperationException("Could not instantiate test player.");
            var debugKeys = playerInstance.AddComponent<PhaseOneResourceDebugKeys>();
            Set(debugKeys, "resources", playerInstance.GetComponent<CameraResourceController>());
            var lightObject = new GameObject("Test Room Light");
            lightObject.transform.position = new Vector3(0f, 3f, 1f);
            var point = lightObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 10f;
            point.intensity = 2f;
            EditorSceneManager.SaveScene(scene, ScenePath);
            PersistPickupItemReferences(pickupNames, itemPaths);
            EditorBuildSettings.scenes = AppendTestScene(EditorBuildSettings.scenes, ScenePath);
        }

        private static void CreateTestPickup(string objectName, Vector3 position, string itemPath)
        {
            var definition = AssetDatabase.LoadAssetAtPath<InventoryItem>(itemPath);
            if (definition == null) throw new System.InvalidOperationException($"Could not load item definition for {objectName} from {itemPath}.");
            var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = objectName;
            pickup.transform.position = position;
            var interactable = pickup.AddComponent<PickupInteractable>();
            interactable.SetItem(definition);
            if (pickup.GetComponent<Collider>() == null)
                throw new System.InvalidOperationException($"Test pickup '{objectName}' has no collider.");
            if (pickup.layer != LayerMask.NameToLayer("Default"))
                throw new System.InvalidOperationException($"Test pickup '{objectName}' is not on the Default layer.");
        }
        private static void PersistPickupItemReferences(string[] pickupNames, string[] itemPaths)
        {
            if (pickupNames == null || itemPaths == null || pickupNames.Length != itemPaths.Length)
                throw new System.ArgumentException("Pickup names and item paths must have matching entries.");
            var yaml = System.IO.File.ReadAllText(ScenePath);
            var documents = System.Text.RegularExpressions.Regex.Split(yaml, @"(?=^--- !u!\d+ &)", System.Text.RegularExpressions.RegexOptions.Multiline);
            var pickupScriptGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Interaction/PickupInteractable.cs");
            for (var i = 0; i < pickupNames.Length; i++)
            {
                var metaPath = itemPaths[i] + ".meta";
                if (!System.IO.File.Exists(itemPaths[i]) || !System.IO.File.Exists(metaPath))
                    throw new System.InvalidOperationException($"Cannot persist pickup '{pickupNames[i]}': item asset or meta is missing.");
                var meta = System.IO.File.ReadAllText(metaPath);
                var guidMatch = System.Text.RegularExpressions.Regex.Match(meta, @"(?m)^guid:\s*([a-f0-9]+)\s*$");
                if (!guidMatch.Success) throw new System.InvalidOperationException($"Could not read item GUID for '{pickupNames[i]}'.");
                var itemGuid = guidMatch.Groups[1].Value;
                var gameObjectDocIndex = System.Array.FindIndex(documents, document =>
                    document.StartsWith("--- !u!1 &", System.StringComparison.Ordinal) &&
                    System.Text.RegularExpressions.Regex.IsMatch(document,
                        @"(?m)^  m_Name: " + System.Text.RegularExpressions.Regex.Escape(pickupNames[i]) + @"\s*$"));
                if (gameObjectDocIndex < 0) throw new System.InvalidOperationException($"Pickup GameObject '{pickupNames[i]}' was not found in the saved scene.");
                var ownerMatch = System.Text.RegularExpressions.Regex.Match(documents[gameObjectDocIndex], @"(?m)^--- !u!1 &(?<id>-?\d+)");
                if (!ownerMatch.Success) throw new System.InvalidOperationException($"Could not read GameObject id for '{pickupNames[i]}'.");
                var ownerId = ownerMatch.Groups["id"].Value;
                var componentDocIndex = System.Array.FindIndex(documents, document =>
                    document.StartsWith("--- !u!114 &", System.StringComparison.Ordinal) &&
                    document.Contains("guid: " + pickupScriptGuid) &&
                    System.Text.RegularExpressions.Regex.IsMatch(document,
                        @"(?m)^  m_GameObject: \{fileID: " + ownerId + @"\}$"));
                if (componentDocIndex < 0) throw new System.InvalidOperationException($"PickupInteractable component for '{pickupNames[i]}' was not found.");
                var reference = $"item: {{fileID: 11400000, guid: {itemGuid}, type: 2}}";
                if (documents[componentDocIndex].Contains("item: {fileID: 0}"))
                    documents[componentDocIndex] = documents[componentDocIndex].Replace("item: {fileID: 0}", reference);
                else if (!documents[componentDocIndex].Contains(reference))
                    throw new System.InvalidOperationException($"Pickup '{pickupNames[i]}' has an unexpected item reference.");
                Debug.Log($"Persisted pickup '{pickupNames[i]}' to {itemPaths[i]} (GUID {itemGuid}).");
            }
            System.IO.File.WriteAllText(ScenePath, string.Concat(documents));
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            var verifiedYaml = System.IO.File.ReadAllText(ScenePath);
            for (var i = 0; i < itemPaths.Length; i++)
            {
                var meta = System.IO.File.ReadAllText(itemPaths[i] + ".meta");
                var itemGuid = System.Text.RegularExpressions.Regex.Match(meta, @"(?m)^guid:\s*([a-f0-9]+)\s*$").Groups[1].Value;
                if (string.IsNullOrEmpty(itemGuid) || !verifiedYaml.Contains($"item: {{fileID: 11400000, guid: {itemGuid}, type: 2}}"))
                    throw new System.InvalidOperationException($"Pickup definition for '{pickupNames[i]}' did not persist in the generated scene.");
            }
        }

        private static void CreateWall(string objectName, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = objectName;
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        private static EditorBuildSettingsScene[] AppendTestScene(EditorBuildSettingsScene[] scenes, string path)
        {
            foreach (var scene in scenes) if (scene.path == path) return scenes;
            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(next, 0);
            next[next.Length - 1] = new EditorBuildSettingsScene(path, true);
            return next;
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var parent = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var current = parent + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(current)) AssetDatabase.CreateFolder(parent, segments[i]);
                parent = current;
            }
        }

        private static void EnsureLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (var i = 8; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).stringValue == GhostLayerName) { tagManager.ApplyModifiedProperties(); return; }
            for (var i = 8; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layers.GetArrayElementAtIndex(i).stringValue = GhostLayerName; tagManager.ApplyModifiedProperties(); return; }
            throw new System.InvalidOperationException("No hay una capa libre para Ghost.");
        }

        private static void Set(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}






























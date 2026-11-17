#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Fusion;
using Fusion.Editor;
using Fusion.Photon.Realtime;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UltimaCinta.Interaction;
using UltimaCinta.Player;
using UltimaCinta.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    public static class UltimaCintaPhaseFiveSetup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MansionScenePath = "Assets/Scenes/Mansion_Generated.unity";
        private const string NetworkDroppedItemPrefabPath = "Assets/Resources/NetworkDroppedItem.prefab";
        private const string NetworkItemCatalogPath = "Assets/Resources/NetworkItemCatalog.asset";
        [MenuItem("Tools/UltimaCinta/Preparar Fase 5")]
        public static void PreparePhaseFive()
        {
            ConfigurePhotonAppId();
            ConfigureNetworkDroppedItemAssets();
            ConfigurePlayerPrefab();
            ConfigureGhostPrefabs();
            ConfigureLobbyScene();
            ConfigureBootstrapScene();
            ConfigureMansionScene();
            IncludeRuntimeScenes();
            NetworkProjectConfigUtilities.RebuildPrefabTable();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fase 5 preparada: lobby Shared Mode, prefab de jugador de red y puntos de spawn listos.");
        }

        private static void ConfigurePhotonAppId()
        {
            var settings = PhotonAppSettings.Global;
            if (settings == null) throw new System.InvalidOperationException("No existe el asset PhotonAppSettings.");
            if (string.IsNullOrWhiteSpace(settings.AppSettings.AppIdFusion))
                throw new System.InvalidOperationException("Configura el App ID de Fusion en Fusion Hub antes de preparar la fase 5.");
        }

        private static void ConfigureNetworkDroppedItemAssets()
        {
            Directory.CreateDirectory("Assets/Resources");
            GameObject prefab;
            if (File.Exists(NetworkDroppedItemPrefabPath))
            {
                var root = PrefabUtility.LoadPrefabContents(NetworkDroppedItemPrefabPath);
                try
                {
                    ConfigureDroppedItemRoot(root);
                    PrefabUtility.SaveAsPrefabAsset(root, NetworkDroppedItemPrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkDroppedItemPrefabPath);
            }
            else
            {
                var root = new GameObject("NetworkDroppedItem");
                ConfigureDroppedItemRoot(root);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, NetworkDroppedItemPrefabPath);
                Object.DestroyImmediate(root);
            }

            var definitions = new List<InventoryItem>();
            var itemGuids = AssetDatabase.FindAssets("t:InventoryItem", new[] { "Assets" });
            var itemPaths = new List<string>();
            foreach (var guid in itemGuids) itemPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            itemPaths.Sort(System.StringComparer.Ordinal);
            foreach (var path in itemPaths)
            {
                var definition = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
                if (definition != null) definitions.Add(definition);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<NetworkItemCatalog>(NetworkItemCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NetworkItemCatalog>();
                AssetDatabase.CreateAsset(catalog, NetworkItemCatalogPath);
            }
            catalog.Configure(definitions.ToArray(), prefab);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(prefab);
        }

        private static void ConfigureDroppedItemRoot(GameObject root)
        {
            root.name = "NetworkDroppedItem";
            var networkObject = root.GetComponent<NetworkObject>();
            if (networkObject == null) networkObject = root.AddComponent<NetworkObject>();
            networkObject.Flags |= NetworkObjectFlags.MasterClientObject;
            networkObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            if (root.GetComponent<NetworkTransform>() == null) root.AddComponent<NetworkTransform>();
            if (root.GetComponent<SphereCollider>() == null)
            {
                var collider = root.AddComponent<SphereCollider>();
                collider.radius = 0.35f;
            }
            if (root.GetComponent<NetworkDroppedItem>() == null) root.AddComponent<NetworkDroppedItem>();
        }

        private static void ConfigurePlayerPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
                if (root.GetComponent<NetworkTransform>() == null) root.AddComponent<NetworkTransform>();

                var remoteBody = root.transform.Find("RemotePlayerBody")?.gameObject;
                if (remoteBody == null)
                {
                    remoteBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    remoteBody.name = "RemotePlayerBody";
                    remoteBody.transform.SetParent(root.transform, false);
                    remoteBody.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    remoteBody.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                    Object.DestroyImmediate(remoteBody.GetComponent<CapsuleCollider>());
                    remoteBody.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                }

                var flashlightObject = root.transform.Find("RemoteFlashlight")?.gameObject;
                if (flashlightObject == null)
                {
                    flashlightObject = new GameObject("RemoteFlashlight");
                    flashlightObject.transform.SetParent(root.transform, false);
                    flashlightObject.transform.localPosition = new Vector3(0f, 1.55f, 0.15f);
                    flashlightObject.transform.localRotation = Quaternion.identity;
                    var light = flashlightObject.AddComponent<Light>();
                    light.type = LightType.Spot;
                    light.range = 12f;
                    light.spotAngle = 60f;
                    light.intensity = 3f;
                    light.shadows = LightShadows.None;
                    light.enabled = false;
                }

                var avatar = root.GetComponent<NetworkPlayerAvatar>();
                if (avatar == null) avatar = root.AddComponent<NetworkPlayerAvatar>();
                var serializedAvatar = new SerializedObject(avatar);
                serializedAvatar.FindProperty("remoteBody").objectReferenceValue = remoteBody;
                serializedAvatar.FindProperty("remoteFlashlight").objectReferenceValue = flashlightObject.GetComponent<Light>();
                serializedAvatar.FindProperty("inputReader").objectReferenceValue = root.GetComponent<PlayerInputReader>();
                serializedAvatar.FindProperty("viewPivot").objectReferenceValue = root.transform.Find("ViewPivot");
                serializedAvatar.FindProperty("flashlightController").objectReferenceValue = root.GetComponent<UltimaCinta.CameraSystem.FlashlightController>();
                serializedAvatar.FindProperty("cameraResources").objectReferenceValue = root.GetComponent<UltimaCinta.CameraSystem.CameraResourceController>();
                serializedAvatar.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureLobbyScene()
        {
            var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var lobbyObject = GameObject.Find("NetworkLobby");
            if (lobbyObject == null) lobbyObject = new GameObject("NetworkLobby");
            var lobby = lobbyObject.GetComponent<NetworkLobbyController>();
            if (lobby == null) lobby = lobbyObject.AddComponent<NetworkLobbyController>();
            var serializedLobby = new SerializedObject(lobby);
            serializedLobby.FindProperty("playerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            serializedLobby.FindProperty("mansionScenePath").stringValue = MansionScenePath;
            serializedLobby.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBootstrapScene()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var loaderObject = GameObject.Find("BootstrapLobbyLoader");
            if (loaderObject == null) loaderObject = new GameObject("BootstrapLobbyLoader");
            if (loaderObject.GetComponent<BootstrapLobbyLoader>() == null)
                loaderObject.AddComponent<BootstrapLobbyLoader>();
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureMansionScene()
        {
            var scene = EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);
            var scenePlayers = Object.FindObjectsByType<FirstPersonMovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var player in scenePlayers)
                if (player != null && player.gameObject.scene == scene && player.GetComponent<NetworkObject>() == null)
                    Object.DestroyImmediate(player.gameObject);

            ConfigureNetworkInteractables(scene);

            var spawnRoot = GameObject.Find("PlayerSpawnPoints");
            if (spawnRoot == null) spawnRoot = new GameObject("PlayerSpawnPoints");
            var basePosition = spawnRoot.transform.position;
            var baseRotation = spawnRoot.transform.rotation;
            var vans = Object.FindObjectsByType<VanSupplyInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var van in vans)
            {
                if (van == null || van.gameObject.scene != scene || !van.TryGetApproachPose(out basePosition, out baseRotation)) continue;
                break;
            }
            spawnRoot.transform.SetPositionAndRotation(basePosition, baseRotation);
            var spawnPoints = new Transform[4];
            var offsets = new[]
            {
                new Vector3(-1.1f, 0f, 0f), new Vector3(-0.35f, 0f, 0f),
                new Vector3(0.35f, 0f, 0f), new Vector3(1.1f, 0f, 0f)
            };
            for (var index = 0; index < spawnPoints.Length; index++)
            {
                var point = spawnRoot.transform.Find($"Spawn_{index + 1}");
                if (point == null)
                {
                    var pointObject = new GameObject($"Spawn_{index + 1}");
                    point = pointObject.transform;
                    point.SetParent(spawnRoot.transform, false);
                }
                point.localPosition = offsets[index];
                point.localRotation = Quaternion.identity;
                spawnPoints[index] = point;
            }
            var spawnController = spawnRoot.GetComponent<PlayerSpawnPoint>();
            if (spawnController == null) spawnController = spawnRoot.AddComponent<PlayerSpawnPoint>();
            var serializedSpawn = new SerializedObject(spawnController);
            var pointsProperty = serializedSpawn.FindProperty("points");
            pointsProperty.arraySize = spawnPoints.Length;
            for (var index = 0; index < spawnPoints.Length; index++)
                pointsProperty.GetArrayElementAtIndex(index).objectReferenceValue = spawnPoints[index];
            serializedSpawn.ApplyModifiedPropertiesWithoutUndo();

            var nightClock = Object.FindFirstObjectByType<NightClock>();
            if (nightClock == null) throw new System.InvalidOperationException("Mansion_Generated no contiene NightClock.");
            var clockObject = nightClock.GetComponent<NetworkObject>();
            if (clockObject == null) clockObject = nightClock.gameObject.AddComponent<NetworkObject>();
            clockObject.Flags |= NetworkObjectFlags.MasterClientObject;
            clockObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            EditorUtility.SetDirty(clockObject);
            var ghostWorld = Object.FindFirstObjectByType<GhostWorldController>();
            if (ghostWorld == null) throw new System.InvalidOperationException("Mansion_Generated no contiene GhostWorldController.");
            var worldObject = ghostWorld.GetComponent<NetworkObject>();
            if (worldObject == null) worldObject = ghostWorld.gameObject.AddComponent<NetworkObject>();
            worldObject.Flags |= NetworkObjectFlags.MasterClientObject;
            worldObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            EditorUtility.SetDirty(worldObject);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureNetworkInteractables(Scene scene)
        {
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (door != null && door.gameObject.scene == scene) ConfigureMasterSceneObject(door.gameObject);
            foreach (var lightSwitch in Object.FindObjectsByType<SwitchInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (lightSwitch != null && lightSwitch.gameObject.scene == scene) ConfigureMasterSceneObject(lightSwitch.gameObject);
            foreach (var pickup in Object.FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (pickup != null && pickup.gameObject.scene == scene) ConfigureMasterSceneObject(pickup.gameObject);
        }

        private static void ConfigureMasterSceneObject(GameObject gameObject)
        {
            var networkObject = gameObject.GetComponent<NetworkObject>();
            if (networkObject == null) networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.Flags |= NetworkObjectFlags.MasterClientObject;
            networkObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            EditorUtility.SetDirty(networkObject);
        }

        private static void ConfigureGhostPrefabs()
        {
            foreach (var assetGuid in AssetDatabase.FindAssets("t:GhostType"))
            {
                var ghostType = AssetDatabase.LoadAssetAtPath<GhostType>(AssetDatabase.GUIDToAssetPath(assetGuid));
                if (ghostType == null || ghostType.GhostPrefab == null) continue;
                var prefabPath = AssetDatabase.GetAssetPath(ghostType.GhostPrefab);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var networkObject = root.GetComponent<NetworkObject>();
                    if (networkObject == null) networkObject = root.AddComponent<NetworkObject>();
                    networkObject.Flags |= NetworkObjectFlags.MasterClientObject;
                    networkObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
                    if (root.GetComponent<NetworkTransform>() == null) root.AddComponent<NetworkTransform>();
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void IncludeRuntimeScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddScene(scenes, BootstrapScenePath);
            AddScene(scenes, LobbyScenePath);
            AddScene(scenes, MansionScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path != path) continue;
                if (!scenes[index].enabled) scenes[index] = new EditorBuildSettingsScene(path, true);
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
#endif

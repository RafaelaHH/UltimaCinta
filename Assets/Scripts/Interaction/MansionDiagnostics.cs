using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UltimaCinta.Lighting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimaCinta.Interaction
{
    [DisallowMultipleComponent]
    public sealed class MansionDiagnostics : MonoBehaviour
    {
        private const float SharedWallHalfThickness = 0.09f;
        private const float FuseBoxWallGap = 0.02f;
        private readonly HashSet<int> processedScenes = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<MansionDiagnostics>(FindObjectsInactive.Include) != null) return;
            var diagnostics = new GameObject("MansionDiagnostics_Runtime");
            DontDestroyOnLoad(diagnostics);
            diagnostics.AddComponent<MansionDiagnostics>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
                RepairAndDiagnose(SceneManager.GetSceneAt(index));
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RepairAndDiagnose(scene);
        }

        private void RepairAndDiagnose(Scene scene)
        {
            var sceneId = scene.GetHashCode();
            if (!scene.IsValid() || !scene.isLoaded || processedScenes.Contains(sceneId)) return;
            processedScenes.Add(sceneId);
            var objects = GetSceneObjects(scene);
            if (!objects.Exists(item => IsRoomName(item.name))) return;
            MansionNightLighting.EnsureForScene(scene);

            RepairHideouts(objects);
            RepairPoi(objects);
            RepairElectricalSystem(scene, objects);
            DiagnoseInteraction(objects);
            DiagnosePoi(objects);
            DiagnoseNavMesh(scene, objects);
            DiagnoseLighting(scene);
        }

        private static void RepairHideouts(List<GameObject> objects)
        {
            var repaired = 0;
            foreach (var instance in objects)
            {
                var hideout = instance.GetComponent<HideoutInteractable>();
                var bedObject = IsBedDecorationItem(instance);
                if (hideout == null && !instance.name.StartsWith("Escondite_", StringComparison.OrdinalIgnoreCase) && !bedObject) continue;
                if (hideout == null)
                {
                    hideout = instance.AddComponent<HideoutInteractable>();
                    var type = bedObject ? "cama" : InferHideoutType(instance);
                    hideout.SetHideoutType(type);
                    hideout.SetUnderBed(type == "cama");
                    repaired++;
                }
                if (bedObject)
                {
                    hideout.SetHideoutType("cama");
                    hideout.SetUnderBed(true);
                }
                SetRoomBoundsFromParent(instance.transform, hideout);
                hideout.EnsureRuntimeSetup();
            }
            var hideouts = objects.FindAll(item => item.GetComponent<HideoutInteractable>() != null);
            var beds = hideouts.FindAll(item => item.GetComponent<HideoutInteractable>().IsUnderBed);
            Debug.Log($"[DIAG] escondites={hideouts.Count}; camas escondite={beds.Count} [{string.Join("; ", beds.ConvertAll(item => item.name))}]; reparados={repaired}; " +
                      string.Join("; ", objects.FindAll(item => item.TryGetComponent<HideoutInteractable>(out _))
                          .ConvertAll(item => DescribeHideout(item.GetComponent<HideoutInteractable>()))));
        }

        private static bool IsBedDecorationItem(GameObject instance)
        {
            if (instance.transform.parent == null || instance.transform.parent.name != "Decoracion") return false;
            var name = instance.name.ToLowerInvariant();
            return name.StartsWith("cama", StringComparison.Ordinal) || name.StartsWith("bed", StringComparison.Ordinal);
        }

        private static string DescribeHideout(HideoutInteractable hideout)
        {
            hideout.GetDiagnosticState(out var colliderValid, out var anchorValid);
            return $"{hideout.name}, tipo={hideout.HideoutType}, collider={(colliderValid ? "OK" : "FALTA")}, punto={(anchorValid ? "OK" : "FALTA")}";
        }

        private static string InferHideoutType(GameObject instance)
        {
            var bounds = default(Bounds);
            var found = false;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return "armario";
            return bounds.size.y < 1f && Mathf.Max(bounds.size.x, bounds.size.z) > 1.1f ? "cama" : "armario";
        }

        private static void SetRoomBoundsFromParent(Transform item, HideoutInteractable hideout)
        {
            for (var parent = item.parent; parent != null; parent = parent.parent)
            {
                if (!IsRoomName(parent.name)) continue;
                var bounds = default(Bounds);
                var found = false;
                foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled) continue;
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (found) hideout.SetRoomBounds(new Vector2(bounds.min.x, bounds.min.z), new Vector2(bounds.max.x, bounds.max.z));
                return;
            }
        }

        private static void RepairPoi(List<GameObject> objects)
        {
            foreach (var instance in objects)
            {
                if (!instance.name.StartsWith("POI_", StringComparison.OrdinalIgnoreCase)) continue;
                if (instance.GetComponent<PoiMarker>() == null)
                    instance.AddComponent<PoiMarker>();
            }
        }

        private static void RepairElectricalSystem(Scene scene, List<GameObject> objects)
        {
            var generator = objects.Find(item => item.GetComponent<MansionPowerController>() != null)?.GetComponent<MansionPowerController>();
            if (generator == null)
            {
                var generatorObject = objects.Find(item => item.name.IndexOf("generador", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                           item.name.IndexOf("generator", StringComparison.OrdinalIgnoreCase) >= 0);
                if (generatorObject != null) generator = generatorObject.AddComponent<MansionPowerController>();
            }

            var switches = new List<SwitchInteractable>();
            var boxes = new List<FuseBoxInteractable>();
            foreach (var instance in objects)
            {
                var roomSwitch = instance.GetComponent<SwitchInteractable>();
                if (roomSwitch != null)
                {
                    if (roomSwitch.ControlledLight == null)
                    {
                        var room = FindRoomTransform(instance.transform);
                        var lightTransform = room != null ? FindNamedChild(room, "LuzHabitacion") : null;
                        if (lightTransform != null) roomSwitch.SetControlledLight(lightTransform.GetComponent<Light>());
                    }
                    roomSwitch.EnsureRealtimeLight();
                    if (generator != null)
                    {
                        roomSwitch.SetPowerController(generator);
                        generator.RegisterSwitch(roomSwitch);
                    }
                    switches.Add(roomSwitch);
                }

                var box = instance.GetComponent<FuseBoxInteractable>();
                if (box == null && (instance.name.IndexOf("CajaElectrica", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    instance.name.IndexOf("CajaFusible", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    instance.name.IndexOf("FuseBox", StringComparison.OrdinalIgnoreCase) >= 0))
                    box = instance.AddComponent<FuseBoxInteractable>();
                if (box == null) continue;

                box.SetGenerator(generator);
                RepairFuseBoxPosition(instance);
                boxes.Add(box);
            }

            var boxSummary = boxes.Count == 0 ? "ninguna" : string.Join("; ", boxes.ConvertAll(item =>
            {
                item.GetDiagnosticState(out var colliderValid);
                return $"{item.name}(collider={(colliderValid ? "OK" : "FALTA")}, generador={(generator != null ? generator.name : "FALTA")})";
            }));
            Debug.Log($"[DIAG] electricidad: cajas={boxSummary}; generador={(generator != null ? generator.name + ", activado=" + generator.IsPowered : "FALTA")}; " +
                      $"interruptores enlazados={switches.Count}; escena={scene.name}.");
        }

        private static void RepairFuseBoxPosition(GameObject fuseBox)
        {
            var room = FindRoomTransform(fuseBox.transform);
            var box = fuseBox.GetComponent<BoxCollider>();
            if (room == null || room.name != "G-SER" || box == null) return;

            var floorBounds = default(Bounds);
            var foundFloor = false;
            foreach (var floorCollider in room.GetComponentsInChildren<Collider>(true))
            {
                if (!floorCollider.enabled || floorCollider.gameObject.name != "Floor") continue;
                if (!foundFloor) { floorBounds = floorCollider.bounds; foundFloor = true; }
                else floorBounds.Encapsulate(floorCollider.bounds);
            }
            if (!foundFloor) return;

            var position = fuseBox.transform.position;
            var distances = new[]
            {
                Mathf.Abs(position.x - floorBounds.min.x), Mathf.Abs(floorBounds.max.x - position.x),
                Mathf.Abs(position.z - floorBounds.min.z), Mathf.Abs(floorBounds.max.z - position.z)
            };
            var side = 0;
            for (var index = 1; index < distances.Length; index++)
                if (distances[index] < distances[side]) side = index;
            if (distances[side] > 0.4f) return;

            var depthHalf = Mathf.Abs(box.size.z * fuseBox.transform.lossyScale.z) * 0.5f;
            var widthHalf = Mathf.Abs(box.size.x * fuseBox.transform.lossyScale.x) * 0.5f;
            var yaw = 0f;
            switch (side)
            {
                case 0:
                    position.x = floorBounds.min.x + SharedWallHalfThickness + FuseBoxWallGap + depthHalf;
                    position.z = Mathf.Clamp(position.z, floorBounds.min.z + widthHalf, floorBounds.max.z - widthHalf);
                    yaw = 90f;
                    break;
                case 1:
                    position.x = floorBounds.max.x - SharedWallHalfThickness - FuseBoxWallGap - depthHalf;
                    position.z = Mathf.Clamp(position.z, floorBounds.min.z + widthHalf, floorBounds.max.z - widthHalf);
                    yaw = 270f;
                    break;
                case 2:
                    position.z = floorBounds.min.z + SharedWallHalfThickness + FuseBoxWallGap + depthHalf;
                    position.x = Mathf.Clamp(position.x, floorBounds.min.x + widthHalf, floorBounds.max.x - widthHalf);
                    yaw = 0f;
                    break;
                default:
                    position.z = floorBounds.max.z - SharedWallHalfThickness - FuseBoxWallGap - depthHalf;
                    position.x = Mathf.Clamp(position.x, floorBounds.min.x + widthHalf, floorBounds.max.x - widthHalf);
                    yaw = 180f;
                    break;
            }

            fuseBox.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Debug.LogWarning($"Caja eléctrica {fuseBox.name}: se separó del muro hacia G-SER para que el raycast la alcance.", fuseBox);
        }

        private static void DiagnoseInteraction(List<GameObject> objects)
        {
            var switches = new List<string>();
            foreach (var instance in objects)
            {
                var roomSwitch = instance.GetComponent<SwitchInteractable>();
                if (roomSwitch == null) continue;
                var controlled = roomSwitch.ControlledLight;
                if (controlled == null)
                {
                    switches.Add($"{instance.name}->NULL");
                    continue;
                }
                switches.Add($"{instance.name}->{HierarchyPath(controlled.transform)}, tipo={controlled.type}, bake={controlled.lightmapBakeType}, enabled={controlled.enabled}");
            }
            var hideoutCount = objects.FindAll(item => item.GetComponent<HideoutInteractable>() != null).Count;
            Debug.Log($"[DIAG] interruptores={switches.Count}: {string.Join("; ", switches)}; escondites listos={hideoutCount}.");
        }

        private static void DiagnosePoi(List<GameObject> objects)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var instance in objects)
            {
                if (!instance.name.StartsWith("POI_", StringComparison.OrdinalIgnoreCase)) continue;
                var type = PoiMarker.TypeLabel(instance.name);
                var room = FindRoomAncestor(instance.transform);
                var key = type + "@" + room;
                counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
            }
            Debug.Log($"[DIAG] POI por tipo y recinto={FormatCounts(counts)}.");
        }

        private static void DiagnoseNavMesh(Scene scene, List<GameObject> objects)
        {
            NavMeshSurface surface = null;
            foreach (var instance in objects)
            {
                var candidate = instance.GetComponent<NavMeshSurface>();
                if (candidate != null && (candidate.name == "Mansion_NavMeshSurface" || surface == null)) surface = candidate;
            }

            var triangulation = NavMesh.CalculateTriangulation();
            var triangles = triangulation.indices.Length / 3;
            var samples = new List<string>();
            foreach (var instance in objects)
            {
                if (!IsRoomName(instance.name) && !instance.name.StartsWith("Escalera_", StringComparison.Ordinal)) continue;
                var samplePosition = FindWalkableProbe(instance);
                var status = NavMesh.SamplePosition(samplePosition, out _, 2f, NavMesh.AllAreas) ? "OK" : "SIN NAVMESH";
                samples.Add(instance.name + "=" + status);
            }

            var assetPath = "runtime: no disponible";
#if UNITY_EDITOR
            if (surface != null && surface.navMeshData != null) assetPath = AssetDatabase.GetAssetPath(surface.navMeshData);
#endif
            Debug.Log($"[DIAG] NavMesh: surface={(surface != null ? surface.name : "FALTA")}, " +
                      $"data={(surface != null && surface.navMeshData != null ? surface.navMeshData.name : "FALTA")}, " +
                      $"asset={assetPath}, triángulos={triangles}; {string.Join("; ", samples)}.");
        }

        private static Vector3 FindWalkableProbe(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                if (collider.enabled && collider.gameObject.name.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new Vector3(collider.bounds.center.x, collider.bounds.max.y + 0.05f, collider.bounds.center.z);

            var bounds = default(Bounds);
            var found = false;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (!found) { bounds = collider.bounds; found = true; }
                else bounds.Encapsulate(collider.bounds);
            }
            if (!found) return instance.transform.position + Vector3.up * 0.1f;
            return new Vector3(bounds.center.x, bounds.min.y + 0.1f, bounds.center.z);
        }

        private static void DiagnoseLighting(Scene scene)
        {
            var directions = new List<string>();
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.gameObject.scene != scene || light.type != LightType.Directional) continue;
                directions.Add($"{light.name}(enabled={light.enabled}, intensidad={light.intensity:0.###}, color={light.color}, sombras={light.shadows})");
            }

            var exposure = new List<string>();
            foreach (var volume in FindObjectsByType<Volume>(FindObjectsInactive.Include))
            {
                if (volume.gameObject.scene != scene || volume.profile == null || !volume.enabled || volume.weight <= 0f) continue;
                if (!volume.profile.TryGet<ColorAdjustments>(out var adjustments) || !adjustments.active) continue;
                if (!adjustments.postExposure.overrideState) continue;
                exposure.Add($"{volume.name}.postExposure={adjustments.postExposure.value:0.##}");
                if (adjustments.postExposure.value > 0f) adjustments.postExposure.value = 0f;
            }

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var lightLimit = pipeline != null ? pipeline.maxAdditionalLightsCount.ToString() : "URP no asignado";
            Debug.Log($"[DIAG] iluminación: ambiente={RenderSettings.ambientMode}/{RenderSettings.ambientLight} x {RenderSettings.ambientIntensity:0.###}, " +
                      $"reflexión={RenderSettings.reflectionIntensity:0.###}, direccionales={string.Join("; ", directions)}, " +
                      $"volúmenes={string.Join("; ", exposure)}, luces adicionales URP/objeto={lightLimit}.");
        }

        private static List<GameObject> GetSceneObjects(Scene scene)
        {
            var objects = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    objects.Add(transform.gameObject);
            return objects;
        }

        private static string FindRoomAncestor(Transform transform)
        {
            for (var parent = transform; parent != null; parent = parent.parent)
                if (IsRoomName(parent.name)) return parent.name;
            return "sin recinto";
        }

        private static Transform FindRoomTransform(Transform transform)
        {
            for (var parent = transform; parent != null; parent = parent.parent)
                if (IsRoomName(parent.name)) return parent;
            return null;
        }

        private static Transform FindNamedChild(Transform parent, string targetName)
        {
            if (parent.name == targetName) return parent;
            for (var index = 0; index < parent.childCount; index++)
            {
                var found = FindNamedChild(parent.GetChild(index), targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static bool IsRoomName(string name) =>
            name.StartsWith("G-", StringComparison.Ordinal) || name.StartsWith("U-", StringComparison.Ordinal) ||
            name.StartsWith("B-", StringComparison.Ordinal) || name.StartsWith("A-", StringComparison.Ordinal);

        private static string HierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }

        private static string FormatCounts(Dictionary<string, int> counts)
        {
            var entries = new List<string>();
            foreach (var item in counts) entries.Add(item.Key + "=" + item.Value);
            return entries.Count == 0 ? "ninguno" : string.Join("; ", entries);
        }
    }
}

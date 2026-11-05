#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UltimaCinta.Data;
using UltimaCinta.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    internal static class MansionDecorationConstants
    {
        public const string PlanPath = "Docs/PlanoMansion.md";
        public const string ScenePath = "Assets/Scenes/Mansion_Generated.unity";
        public const float PlayerFloorOffset = 0.05f;
        public const float CandidateInset = 0.9f;
        public const float ObjectSpacing = 0.85f;
        public const float PlaceholderSize = 0.45f;
        public const float CeilingLightHeight = 2.35f;
        public const float LightRange = 6.0f;
        public const float WarmLightIntensity = 0.8f;
        public const float KelvinWarm = 2800f;
        public const float Half = 0.5f;
        public const float DoorClearance = 0.9f;
        public const float StairClearance = 1.2f;
        public const float FurnitureMaxDimension = 2.4f;
        public const float HideoutMaxDimension = 2.0f;
        public const float HideoutColliderMargin = 0.3f;
        public const float BedLength = 2.0f;
        public const float BedWidth = 1.0f;
        public const float BedHeight = 0.8f;
        public const float WardrobeWidth = 1.0f;
        public const float WardrobeDepth = 0.6f;
        public const float WardrobeHeight = 2.0f;
        public const float SofaLength = 2.0f;
        public const float SofaHeight = 0.9f;
        public const float SofaDepth = 0.9f;
        public const float DiningTableLength = 2.0f;
        public const float DiningTableWidth = 1.0f;
        public const float DiningTableHeight = 0.8f;
        public const float BookshelfWidth = 1.2f;
        public const float BookshelfHeight = 2.2f;
        public const float BookshelfDepth = 0.35f;
        public const float TelevisionWidth = 1.0f;
        public const float TelevisionHeight = 0.75f;
        public const float TelevisionDepth = 0.3f;
        public const float TelevisionBaseHeight = 0.9f;
        public const float CounterHeight = 0.9f;
        public const float KitchenWidth = 1.2f;
        public const float KitchenDepth = 0.65f;
        public const float NightstandWidth = 0.5f;
        public const float NightstandHeight = 0.6f;
        public const float NightstandDepth = 0.4f;
        public const float PhotographWidth = 0.4f;
        public const float PhotographHeight = 0.3f;
        public const float PhotographDepth = 0.04f;
        public const float MirrorWidth = 0.5f;
        public const float MirrorHeight = 0.8f;
        public const float MirrorDepth = 0.05f;
        public const float FridgeWidth = 0.7f;
        public const float FridgeHeight = 1.8f;
        public const float SmallObjectSize = 0.25f;
        public const float KeySize = 0.22f;
        public const float ObjectScaleBoost = 1.3f;
        public const float CeilingLampDiameter = 0.4f;
        public const float CeilingLampLightOffset = 0.2f;
        public const float SwitchFrameOffset = 0.15f;
        public const float WallOffset = 0.05f;
        public const float DoorArcClearance = 0.9f;
        public const float MinimumHallWidth = 0.9f;
        public const float SurfaceItemOffset = 0.03f;
        public const float WallHangingHeight = 1.4f;
        public const float DiningClearance = 0.9f;
        public const float VanLength = 5.5f;
        public const float VanHeight = 2.4f;
        public const float StorageBoxSize = 0.7f;
        public const float BathtubLength = 1.7f;
        public const float BathtubWidth = 0.75f;
        public const float BathtubHeight = 0.6f;
        public const float ToiletHeight = 0.8f;
        public const float ToiletWidth = 0.45f;
        public const float ToiletDepth = 0.7f;
        public const float VanWidth = 2.4f;
        public const float ObjectWallGap = 0.03f;
        public const float WindowHeight = 1.0f;
        public const float SwitchHeight = 1.2f;
        public const float VanGearHeight = 0.55f;
        public const float VanGearSpacing = 0.32f;
        public const float VanGearRearInset = 0.45f;
        public const float VanGearStartZ = 0.45f;
        public const float CribLength = 1.3f;
        public const float CribWidth = 0.75f;
        public const float CribHeight = 1.0f;
        public const float HandLadderLength = 2.0f;
        public const float MannequinHeight = 1.7f;
        public const float WindowWidth = 1.2f;
        public const float WindowHeightTarget = 1.0f;
        public const float WindowDepth = 0.08f;
        public const float SmallCanSize = 0.15f;
        public const float MusicBoxWidth = 0.25f;
        public const float RitualBoardWidth = 0.8f;
    }

    internal enum MansionPlacementKind
    {
        Furniture,
        Wall,
        Surface,
        Corner,
        Center,
        Window,
        Switch,
        RitualFloor,
        Hideout,
        Key
    }

    public static class MansionDecorator
    {
        private static AudioClip sparkClipCache;
        private static bool sparkClipSearched;
        private const string MenuPath = "Tools/UltimaCinta/3. Decorar mansión";
        private const string GeneratedRoot = "Assets/Generated";
        private const string KeyItemsFolder = "Assets/Generated/KeyItems";
        private const float PlacementY = 0.02f;

        private sealed class RoomMetadata
        {
            public string Detail;
            public string Light;
        }

        private sealed class Placement
        {
            public string Asset;
            public string Name;
            public Vector3 Position;
            public float Yaw;
            public MansionPlacementKind Kind;
            public int InstanceIndex;
        }

        private sealed class SupportSurface
        {
            public string Id;
            public string Name;
            public Bounds Bounds;
        }

        private sealed class PlacedObject
        {
            public string Name;
            public Bounds Bounds;
            public string SupportId;
        }

        private static readonly Dictionary<string, string[]> ClueAssets = new Dictionary<string, string[]>
        {
            { "B-COR", new[] { "cajadecables", "Cajas de almacén" } },
            { "B-ESC", new[] { "caldera", "cajademusica" } },
            { "B-RIT", new[] { "ouija", "campana", "vela negra" } },
            { "B-BOD", new[] { "Cajas de almacén", "Estantería con libros" } },
            { "B-CAL", new[] { "caldera" } },
            { "G-REC", new[] { "fotografia", "Espejo de recibidor" } },
            { "G-TV", new[] { "Muñeca", "osito" } },
            { "G-DES", new[] { "lata", "Cajas de almacén" } },
            { "G-EST", new[] { "Máquina de escribir" } },
            { "G-BIB", new[] { "Estantería con libros", "librogrueso" } },
            { "G-BAN", new[] { "Espejo de recibidor", "tolet" } },
            { "G-SER", new[] { "interruptor", "cajadecables" } },
            { "G-AMP", new[] { "Maniquí", "fotografia" } },
            { "G-GAR", new[] { "caballitodemadera", "cajadeherramientas" } },
            { "U-D4", new[] { "cajademusica", "Cajas de almacén" } },
            { "U-D3", new[] { "Reloj infantil", "cama" } },
            { "U-B1", new[] { "bañera", "tolet" } },
            { "U-D5", new[] { "Muñeca", "osito" } },
            { "U-COR", new[] { "fotografia", "Espejo de recibidor" } },
            { "U-S1-HALL", new[] { "fotografia", "Maniquí" } },
            { "U-COR-E", new[] { "fotografia", "Cajas de almacén" } },
            { "U-B2", new[] { "tolet", "bañera" } },
            { "U-COS", new[] { "cajademusica", "Cajas de almacén" } },
            { "U-GAL-E", new[] { "Maniquí", "fotografia" } },
            { "A-OPEN", new[] { "Cajas de almacén", "maletindemetal" } }
        };

        [MenuItem(MenuPath)]
        public static void DecorateMansion()
        {
            MansionGenerator.ResetDoorSwingCache();
            var projectRoot = Directory.GetParent(Application.dataPath);
            var absoluteScenePath = Path.Combine(projectRoot.FullName, MansionDecorationConstants.ScenePath);
            if (!File.Exists(absoluteScenePath))
            {
                Debug.LogError("MansionDecorator: no existe Mansion_Generated.unity; ejecuta primero el menú 2.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var planPath = Path.Combine(projectRoot.FullName, MansionDecorationConstants.PlanPath);
            if (!File.Exists(planPath))
            {
                Debug.LogError("MansionDecorator: no existe Docs/PlanoMansion.md.");
                return;
            }
            var lines = File.ReadAllLines(planPath);
            var errors = new List<string>();
            var spaces = MansionLayoutPlanReader.ReadSpaces(lines, errors);
            var doors = MansionLayoutPlanReader.ReadDoors(lines, errors);
            var stairs = MansionLayoutPlanReader.ReadStairs(lines, errors);
            var hideouts = ReadHideouts(lines, errors);
            if (errors.Count > 0)
            {
                foreach (var error in errors) Debug.LogError("MansionDecorator: " + error);
                return;
            }

            var scene = EditorSceneManager.OpenScene(MansionDecorationConstants.ScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var ceilingLampPaths = FindCeilingLampModels();
            var modelPaths = FindFbxModels();
            var modelsByName = IndexModels(modelPaths);
            var placed = 0;
            var omitted = 0;
            var hideoutCount = 0;
            var keyLocations = new List<string>();
            var hideoutLocations = new List<string>();
            var lampLocations = new List<string>();
            var omittedDetails = new List<string>();
            var roomPlacements = CreateRoomPlacements(spaces);
            var usedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var space in spaces)
                PopulateRoomModels(space.Id, roomPlacements[space.Id], modelsByName, usedModels);
            AddHideouts(hideouts, spaces, roomPlacements, modelsByName, usedModels);
            AddKeys(roomPlacements, modelsByName, usedModels);
            AddRitualItems(roomPlacements, modelsByName, usedModels);
            AddVanGear(modelsByName, usedModels);
            AddUnassignedModels(roomPlacements, modelPaths, modelsByName, usedModels, omittedDetails, ref omitted);
            foreach (var space in spaces)
            {
                var room = FindGameObjectInScene(scene, space.Id);
                if (room == null)
                {
                    OmitObject(space.Id, "decoración", "sin candidatos", omittedDetails);
                    omitted++;
                    continue;
                }
                var container = ResetDecorationContainer(room.transform);
                PlaceRoomObjects(space, room, container, roomPlacements[space.Id], modelPaths, spaces, doors, stairs,
                    ref placed, ref omitted, ref hideoutCount, keyLocations, hideoutLocations, omittedDetails);
                var roomSwitch = container.Find("interruptor");
                if (roomSwitch != null && ceilingLampPaths.Count > 0 && !IsStairOrOpening(space, stairs))
                {
                    var lampPath = ceilingLampPaths[lampLocations.Count % ceilingLampPaths.Count];
                    var lamp = PlaceCeilingLamp(space, container, lampPath);
                    if (lamp != null)
                    {
                        var switchComponent = roomSwitch.GetComponent<SwitchInteractable>();
                        if (switchComponent != null) switchComponent.SetControlledFixture(lamp);
                        var lightTransform = room.transform.Find("LuzHabitacion");
                        if (lightTransform != null)
                        {
                            TryGetRendererBounds(lamp, out var lampBounds);
                            lightTransform.position = new Vector3(lampBounds.center.x,
                                lampBounds.center.y - MansionDecorationConstants.CeilingLampLightOffset, lampBounds.center.z);
                        }
                        lampLocations.Add($"Lámpara {NormalizeModelName(Path.GetFileNameWithoutExtension(lampPath))} en {space.Id}");
                        placed++;
                    }
                }
            }

            ReplaceVan(scene, modelsByName, ref placed, ref omitted, omittedDetails);
            var poiSummary = AnnotateAndSummarizePoi(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MansionDecorationConstants.ScenePath);
            Debug.Log($"Mansión decorada: objetos colocados {placed}; omitidos {omitted} [{ShortOmissionList(omittedDetails)}]; llaves [{string.Join("; ", keyLocations)}]; lámparas [{string.Join("; ", lampLocations)}]; escondites [{string.Join("; ", hideoutLocations)}]; POI [{poiSummary}].");
        }

        private static string ShortOmissionList(List<string> omissions)
        {
            const int maximumShown = 12;
            if (omissions.Count <= maximumShown) return string.Join(", ", omissions);
            var shown = omissions.GetRange(0, maximumShown);
            return string.Join(", ", shown) + $", … +{omissions.Count - maximumShown}";
        }

        private static string AnnotateAndSummarizePoi(Scene scene)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var markerTransform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!markerTransform.name.StartsWith("POI_", StringComparison.OrdinalIgnoreCase)) continue;
                if (markerTransform.GetComponent<PoiMarker>() == null) markerTransform.gameObject.AddComponent<PoiMarker>();
                var room = markerTransform.parent;
                while (room != null && !IsRoomId(room.name)) room = room.parent;
                var key = PoiMarker.TypeLabel(markerTransform.name) + " " + (room != null ? room.name : "sin recinto");
                counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
            }
            var entries = new List<string>();
            foreach (var item in counts) entries.Add(item.Key + "=" + item.Value);
            entries.Sort(StringComparer.OrdinalIgnoreCase);
            return entries.Count == 0 ? "sin POI" : string.Join("; ", entries);
        }

        private static Dictionary<string, RoomMetadata> ReadRoomMetadata(string[] lines)
        {
            var result = new Dictionary<string, RoomMetadata>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Espacios por planta", StringComparison.Ordinal)) { reading = true; continue; }
                if (reading && line.StartsWith("## ", StringComparison.Ordinal)) break;
                if (!reading || !line.StartsWith("|", StringComparison.Ordinal)) continue;
                var cells = line.Split('|');
                if (cells.Length < 8 || !IsRoomId(cells[1].Trim())) continue;
                result[cells[1].Trim()] = new RoomMetadata { Detail = cells[6].Trim(), Light = cells[7].Trim() };
            }
            return result;
        }

        private static List<(string Room, string Type, int Count)> ReadHideouts(string[] lines, List<string> errors)
        {
            var result = new List<(string Room, string Type, int Count)>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Escondites", StringComparison.Ordinal)) { reading = true; continue; }
                if (reading && line.StartsWith("## ", StringComparison.Ordinal)) break;
                if (!reading || !line.StartsWith("|", StringComparison.Ordinal)) continue;
                var cells = line.Split('|');
                if (cells.Length < 5 || cells[1].Trim() == "Habitación" || !int.TryParse(cells[3].Trim(), out var count)) continue;
                result.Add((cells[1].Trim(), cells[2].Trim(), count));
            }
            if (result.Count == 0) errors.Add("No se encontró la tabla de escondites.");
            var total = 0;
            foreach (var row in result) total += row.Count;
            if (total != 9) errors.Add("La tabla debe declarar exactamente nueve escondites.");
            foreach (var row in result)
                if (row.Count < 1 || row.Count > 2) errors.Add($"{row.Room}: cantidad de escondites fuera de 1–2.");
            return result;
        }

        private static bool IsRoomId(string id) => id.StartsWith("B-", StringComparison.Ordinal) || id.StartsWith("G-", StringComparison.Ordinal) ||
                                                    id.StartsWith("U-", StringComparison.Ordinal) || id.StartsWith("A-", StringComparison.Ordinal);

        private static List<string> FindFbxModels()
        {
            var result = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { GeneratedRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var allowedCategory = path.StartsWith("Assets/Generated/Ambientacion/", StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWith("Assets/Generated/Herramientas/", StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWith("Assets/Generated/Ritual/", StringComparison.OrdinalIgnoreCase);
                if (allowedCategory && path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) result.Add(path);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static List<string> FindCeilingLampModels()
        {
            var result = new List<string>();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".fbx", ".glb", ".gltf", ".obj", ".prefab" };
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { MansionGeneratorConstants.MaterialsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
                if (!stem.StartsWith("focotecho", StringComparison.Ordinal) || !allowedExtensions.Contains(Path.GetExtension(path))) continue;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                result.Add(path);
                Debug.Log($"MansionDecorator: focotecho encontrado: {path}");
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static bool IsStairOrOpening(MansionLayoutSpace space, List<MansionLayoutStair> stairs)
        {
            var kind = space.Kind ?? string.Empty;
            if (space.Id.IndexOf("ESC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kind.IndexOf("escalera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                kind.IndexOf("hueco", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var lampRadius = MansionDecorationConstants.CeilingLampDiameter * 0.5f;
            foreach (var stair in stairs)
                if ((stair.LandingA == space.Id || stair.LandingB == space.Id) &&
                    space.Bounds.center.x + lampRadius > stair.Opening.xMin && space.Bounds.center.x - lampRadius < stair.Opening.xMax &&
                    space.Bounds.center.y + lampRadius > stair.Opening.yMin && space.Bounds.center.y - lampRadius < stair.Opening.yMax)
                    return true;
            return false;
        }

        private static GameObject PlaceCeilingLamp(MansionLayoutSpace space, Transform container, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return null;
            var lamp = PrefabUtility.InstantiatePrefab(asset, container.gameObject.scene) as GameObject;
            if (lamp == null) lamp = UnityEngine.Object.Instantiate(asset);
            lamp.name = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
            lamp.transform.SetParent(container, false);
            if (!TryGetRendererBounds(lamp, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(lamp);
                return null;
            }
            ScaleToTarget(lamp.transform, bounds.size, Vector3.one * MansionDecorationConstants.CeilingLampDiameter);
            if (!TryGetRendererBounds(lamp, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(lamp);
                return null;
            }
            var ceilingY = MansionGenerator.FloorY(space.Level) +
                           (space.Level == "Desván" ? MansionGeneratorConstants.AtticHeight : MansionGeneratorConstants.ClearHeight);
            var center = new Vector3(space.Bounds.center.x, ceilingY - bounds.extents.y, space.Bounds.center.y);
            SetBoundsCenter(lamp.transform, bounds, center);
            TryGetRendererBounds(lamp, out bounds);
            lamp.transform.position += Vector3.up * (ceilingY - bounds.max.y);
            return lamp;
        }

        private static Dictionary<string, string> IndexModels(List<string> modelPaths)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in modelPaths)
            {
                var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
                if (!result.ContainsKey(stem)) result.Add(stem, path);
            }
            return result;
        }

        private static string NormalizeModelName(string name)
        {
            var decomposed = name.Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(character)) result.Append(char.ToLowerInvariant(character));
            }
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static Dictionary<string, List<Placement>> CreateRoomPlacements(List<MansionLayoutSpace> spaces)
        {
            var result = new Dictionary<string, List<Placement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var space in spaces) result.Add(space.Id, new List<Placement>());
            return result;
        }

        private static void AddModel(List<Placement> placements, Dictionary<string, string> models,
            HashSet<string> usedModels, string stem, MansionPlacementKind kind, int count = 1, string instanceName = null)
        {
            var normalized = NormalizeModelName(stem);
            if (!models.TryGetValue(normalized, out var path)) return;
            usedModels.Add(path);
            for (var index = 0; index < count; index++)
                placements.Add(new Placement { Asset = path, Name = instanceName ?? normalized, Kind = kind, InstanceIndex = index });
        }

        private static void PopulateRoomModels(string roomId, List<Placement> placements, Dictionary<string, string> models,
            HashSet<string> usedModels)
        {
            AddModel(placements, models, usedModels, "interruptor", MansionPlacementKind.Switch);
            if (roomId == "G-COM")
            {
                AddModel(placements, models, usedModels, "Mesa de comedor con sillas", MansionPlacementKind.Center);
                AddModel(placements, models, usedModels, "platoroto", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "tazarota", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "lampara", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                return;
            }
            if (roomId.StartsWith("U-D", StringComparison.Ordinal) && roomId != "U-D5")
            {
                AddModel(placements, models, usedModels, "cama", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Mesilla de noche", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "lampara", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                AddModel(placements, models, usedModels, "frenteventana", MansionPlacementKind.Window);
                if (roomId == "U-D1") AddModel(placements, models, usedModels, "mesaconlampara", MansionPlacementKind.Furniture);
                if (roomId == "U-D3") AddModel(placements, models, usedModels, "Reloj infantil", MansionPlacementKind.Surface);
                return;
            }
            if (roomId == "U-D5")
            {
                AddModel(placements, models, usedModels, "cunadebebe", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "cama", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Muñeca", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "osito", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "caballitodemadera", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                AddModel(placements, models, usedModels, "frenteventana", MansionPlacementKind.Window);
                return;
            }
            if (roomId == "G-BAN" || roomId == "U-B1" || roomId == "U-B2")
            {
                AddModel(placements, models, usedModels, "tolet", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Espejo de recibidor", MansionPlacementKind.Wall);
                if (roomId != "G-BAN") AddModel(placements, models, usedModels, "bañera", MansionPlacementKind.Furniture);
                return;
            }
            if (roomId == "G-COC")
            {
                AddModel(placements, models, usedModels, "cocinaagas", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "refrigerador", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Estantería con libros", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "tazarota", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "platoroto", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "lata", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "G-DES")
            {
                AddModel(placements, models, usedModels, "Estantería con libros", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "lata", MansionPlacementKind.Surface, 2);
                AddModel(placements, models, usedModels, "platoroto", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "tazarota", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "G-SAL" || roomId == "G-TV")
            {
                AddModel(placements, models, usedModels, "Sofá", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Televisor viejo", MansionPlacementKind.Wall);
                AddModel(placements, models, usedModels, "mesaconlampara", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Radio", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "Mecedora", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "maceta", MansionPlacementKind.Corner);
                if (roomId == "G-TV") AddModel(placements, models, usedModels, "Muñeca", MansionPlacementKind.Surface);
                return;
            }
            if (roomId == "G-EST")
            {
                AddModel(placements, models, usedModels, "mesaconlampara", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Máquina de escribir", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "Libro de escritura", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                return;
            }
            if (roomId == "G-BIB")
            {
                AddModel(placements, models, usedModels, "Estantería con libros", MansionPlacementKind.Furniture, 2);
                AddModel(placements, models, usedModels, "librogrueso", MansionPlacementKind.Surface, 2);
                AddModel(placements, models, usedModels, "Mecedora", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                return;
            }
            if (roomId == "G-GAR")
            {
                AddModel(placements, models, usedModels, "cajadeherramientas", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "escaleras", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner, 2);
                AddModel(placements, models, usedModels, "lata", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "caballitodemadera", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "G-REC" || roomId == "U-COR" || roomId == "U-S1-HALL" || roomId == "U-COR-E" || roomId == "G-SER")
            {
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                if (roomId == "G-REC") AddModel(placements, models, usedModels, "Espejo de recibidor", MansionPlacementKind.Wall);
                if (roomId != "U-COR-E") AddModel(placements, models, usedModels, "maceta", MansionPlacementKind.Corner);
                if (roomId == "G-SER" || roomId == "U-S1-HALL") AddModel(placements, models, usedModels, "Maniquí", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "U-COS" || roomId == "U-GAL-E" || roomId == "G-AMP")
            {
                AddModel(placements, models, usedModels, "Maniquí", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "cajademusica", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "fotografia", MansionPlacementKind.Wall);
                AddModel(placements, models, usedModels, "maceta", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "B-GEN")
            {
                AddModel(placements, models, usedModels, "Generador", MansionPlacementKind.Furniture);
                AddModel(placements, models, usedModels, "cajadeherramientas", MansionPlacementKind.Corner);
                return;
            }
            if (roomId == "B-CAL")
            {
                AddModel(placements, models, usedModels, "caldera", MansionPlacementKind.Furniture);
                return;
            }
            if (roomId == "B-BOD")
            {
                AddModel(placements, models, usedModels, "Estantería con libros", MansionPlacementKind.Furniture, 2);
                AddModel(placements, models, usedModels, "botella", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "lata", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner, 2);
                AddModel(placements, models, usedModels, "maletindemetal", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "Agua bendita", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "ajo", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "amuleto", MansionPlacementKind.Surface);
                AddModel(placements, models, usedModels, "estaca", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "incienso", MansionPlacementKind.Surface);
                return;
            }
            if (roomId == "B-COR" || roomId == "B-ESC")
            {
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "cajadecables", MansionPlacementKind.Corner);
                if (roomId == "B-ESC") AddModel(placements, models, usedModels, "escaleras", MansionPlacementKind.Furniture);
                return;
            }
            if (roomId == "A-OPEN")
            {
                AddModel(placements, models, usedModels, "Cajas de almacén", MansionPlacementKind.Corner, 3);
                AddModel(placements, models, usedModels, "maletindemetal", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "cd1da979c0aa41a2e056ddf80ea043ad", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "Mecedora", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "Maniquí", MansionPlacementKind.Corner);
                AddModel(placements, models, usedModels, "frenteventana", MansionPlacementKind.Window);
            }
        }

        private static void AddRoomClues(MansionLayoutSpace space, List<Placement> placements, List<string> models,
            Dictionary<string, RoomMetadata> metadata)
        {
            if (space.Id == "G-SAL" || space.Id == "G-COM" || space.Id == "G-COC") return;
            if (space.Id == "U-D1" || space.Id == "U-D2") return;
            if (!ClueAssets.TryGetValue(space.Id, out var names))
            {
                placements.Add(new Placement { Name = "Pista_" + space.Id, Position = RoomCenter(space, PlacementY) });
                return;
            }
            var asset = FindModel(models, names);
            var detail = metadata.TryGetValue(space.Id, out var roomMetadata) ? roomMetadata.Detail : space.Id;
            placements.Add(new Placement { Asset = asset, Name = "Pista_" + space.Id + "_" + detail, Position = RoomCenter(space, PlacementY) });
        }

        private static void AddRoomFurniture(MansionLayoutSpace space, List<Placement> placements, List<string> models)
        {
            if (space.Id.StartsWith("U-D", StringComparison.Ordinal))
            {
                placements.Add(new Placement { Asset = FindModel(models, "cama"), Name = "Cama_" + space.Id, Position = RoomOffset(space, -0.8f, -0.8f) });
                placements.Add(new Placement { Asset = FindModel(models, "Mesilla de noche"), Name = "Mesilla_" + space.Id, Position = RoomOffset(space, 0.8f, -0.8f) });
            }
            else if (space.Id == "G-COM")
                placements.Add(new Placement { Asset = FindModel(models, "Mesa de comedor con sillas"), Name = "Mesa_Comedor", Position = RoomCenter(space, PlacementY) });
            else if (space.Id == "G-COC")
            {
                placements.Add(new Placement { Asset = FindModel(models, "cocinaagas"), Name = "Cocina", Position = RoomOffset(space, -0.8f, -0.6f) });
                placements.Add(new Placement { Asset = FindModel(models, "refrigerador"), Name = "Nevera", Position = RoomOffset(space, 0.8f, -0.6f) });
            }
            else if (space.Id == "G-SAL" || space.Id == "G-TV")
            {
                placements.Add(new Placement { Asset = FindModel(models, "Sofá"), Name = "Sofa_" + space.Id, Position = RoomOffset(space, 0.8f, -0.7f) });
                placements.Add(new Placement { Asset = FindModel(models, "Televisor viejo"), Name = "Televisor_" + space.Id, Position = RoomOffset(space, -0.8f, 0.7f) });
            }
            else if (space.Id == "G-BIB")
                placements.Add(new Placement { Asset = FindModel(models, "Estantería con libros"), Name = "Estanteria_Biblioteca", Position = RoomOffset(space, 1.2f, 0f) });
            else if (space.Id == "G-EST")
                placements.Add(new Placement { Asset = FindModel(models, "mesaconlampara"), Name = "Escritorio_Estudio", Position = RoomOffset(space, 0f, 0f) });
            else if (space.Id == "B-RIT")
                placements.Add(new Placement { Asset = FindModel(models, "ouija"), Name = "Tablero_Ritual", Position = RoomCenter(space, PlacementY) });
        }

        private static void AddHideouts(List<(string Room, string Type, int Count)> hideouts, List<MansionLayoutSpace> spaces,
            Dictionary<string, List<Placement>> placements, Dictionary<string, string> models, HashSet<string> usedModels)
        {
            foreach (var entry in hideouts)
            {
                if (!placements.TryGetValue(entry.Room, out var list)) continue;
                var room = spaces.Find(space => space.Id == entry.Room);
                if (room == null) continue;
                for (var index = 0; index < entry.Count; index++)
                {
                    var stem = NormalizeModelName(HideoutModel(entry.Type, index));
                    AddModel(list, models, usedModels, stem, MansionPlacementKind.Hideout, 1,
                        "Escondite_" + entry.Room + "_" + (index + 1));
                }
            }
        }

        private static string HideoutModel(string type, int index)
        {
            var normalized = type.ToLowerInvariant();
            if (normalized.Contains("armario") && normalized.Contains("bajo cama")) return index == 1 ? "cama" : "armario";
            if (normalized.Contains("bajo cama")) return "cama";
            if (normalized.Contains("cama")) return "cama";
            if (normalized.Contains("estanter")) return "Estantería con libros";
            return "armario";
        }

        private static void AddKeys(Dictionary<string, List<Placement>> placements, Dictionary<string, string> models, HashSet<string> usedModels)
        {
            AddModel(placements["G-EST"], models, usedModels, "llave", MansionPlacementKind.Key, 1, "Llave_KEY_UPPER");
            AddModel(placements["U-D1"], models, usedModels, "llave", MansionPlacementKind.Key, 1, "Llave_KEY_BASEMENT");
        }

        private static void AddRitualItems(Dictionary<string, List<Placement>> placements, Dictionary<string, string> models, HashSet<string> usedModels)
        {
            AddModel(placements["B-RIT"], models, usedModels, "ouija", MansionPlacementKind.Center);
            AddModel(placements["B-RIT"], models, usedModels, "vela negra", MansionPlacementKind.RitualFloor, 3);
            AddModel(placements["B-RIT"], models, usedModels, "campana", MansionPlacementKind.Surface);
            AddModel(placements["B-RIT"], models, usedModels, "crusifijo", MansionPlacementKind.Wall);
            AddModel(placements["B-RIT"], models, usedModels, "espejo roto", MansionPlacementKind.Wall);
            AddModel(placements["B-RIT"], models, usedModels, "sal", MansionPlacementKind.Surface);
        }

        private static void AddVanGear(Dictionary<string, string> models, HashSet<string> usedModels)
        {
            var gear = new[]
            {
                "linterna", "Medidor EMF", "Termómetro", "Cinta", "Batería", "Diario", "Libro de escritura",
                "vela blanca", "ajo", "amuleto", "Agua bendita", "estaca", "incienso", "botella de agua"
            };
            foreach (var item in gear)
                if (models.TryGetValue(NormalizeModelName(item), out var path)) usedModels.Add(path);
            if (models.TryGetValue("camcorder", out var camcorder)) usedModels.Add(camcorder);
            if (models.TryGetValue("camioneta", out var van)) usedModels.Add(van);
        }

        private static void AddUnassignedModels(Dictionary<string, List<Placement>> placements, List<string> modelPaths,
            Dictionary<string, string> models, HashSet<string> usedModels, List<string> omittedDetails, ref int omitted)
        {
            foreach (var path in modelPaths)
            {
                if (usedModels.Contains(path)) continue;
                var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
                var room = FallbackRoomFor(stem);
                var kind = FallbackKindFor(stem);
                if (!placements.TryGetValue(room, out var list))
                {
                    OmitObject(room, stem, "sin candidatos", omittedDetails);
                    omitted++;
                    continue;
                }
                AddModel(list, models, usedModels, stem, kind);
            }
        }

        private static string FallbackRoomFor(string stem)
        {
            if (stem.Contains("banera") || stem.Contains("tolet") || stem.Contains("espejo")) return "U-B1";
            if (stem.Contains("cocina") || stem.Contains("refrigerador") || stem.Contains("taza") || stem.Contains("plato")) return "G-COC";
            if (stem.Contains("cama") || stem.Contains("armario") || stem.Contains("muneca") || stem.Contains("osito")) return "U-D1";
            if (stem.Contains("foto") || stem.Contains("maceta") || stem.Contains("interruptor")) return "G-REC";
            if (stem.Contains("libro") || stem.Contains("estanteria") || stem.Contains("maquina")) return "G-BIB";
            if (stem.Contains("ritual") || stem.Contains("vela") || stem.Contains("campana") || stem.Contains("crusifijo") || stem.Contains("sal")) return "B-RIT";
            if (stem.Contains("generador")) return "B-GEN";
            if (stem.Contains("caldera")) return "B-CAL";
            if (stem.Contains("caja") || stem.Contains("cable") || stem.Contains("herramienta") || stem.Contains("maletin")) return "B-BOD";
            if (stem.Contains("sofa") || stem.Contains("radio") || stem.Contains("televisor") || stem.Contains("mecedora")) return "G-SAL";
            return "B-BOD";
        }

        private static MansionPlacementKind FallbackKindFor(string stem)
        {
            if (stem.Contains("foto") || stem.Contains("espejo") || stem.Contains("crusifijo")) return MansionPlacementKind.Wall;
            if (stem.Contains("caja") || stem.Contains("cable") || stem.Contains("maletin") || stem.Contains("herramienta")) return MansionPlacementKind.Corner;
            if (stem.Contains("lata") || stem.Contains("botella") || stem.Contains("radio") || stem.Contains("libro") || stem.Contains("vela")) return MansionPlacementKind.Surface;
            if (stem.Contains("interruptor")) return MansionPlacementKind.Switch;
            if (stem.Contains("frenteventana")) return MansionPlacementKind.Window;
            return MansionPlacementKind.Furniture;
        }

        private static Transform ResetDecorationContainer(Transform room)
        {
            var previous = room.Find("Decoracion");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var container = new GameObject("Decoracion").transform;
            container.SetParent(room, false);
            return container;
        }

        private static void PlaceRoomObjects(MansionLayoutSpace space, GameObject room, Transform container,
            List<Placement> placements, List<string> models, List<MansionLayoutSpace> allSpaces,
            List<MansionLayoutDoor> doors, List<MansionLayoutStair> stairs,
            ref int placed, ref int omitted, ref int hideoutCount, List<string> keyLocations,
            List<string> hideoutLocations, List<string> omittedDetails)
        {
            var occupied = new List<PlacedObject>();
            var surfaces = new List<SupportSurface>();
            placements.Sort((left, right) => PlacementPriority(left).CompareTo(PlacementPriority(right)));
            foreach (var placement in placements)
            {
                if (placement.Name.StartsWith("Escondite_", StringComparison.Ordinal) &&
                    Path.GetFileNameWithoutExtension(placement.Asset ?? string.Empty).IndexOf("cama", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var existingBed = container.Find("cama") ?? container.Find("cunadebebe");
                    if (existingBed != null)
                    {
                        if (!TryGetRendererBounds(existingBed.gameObject, out var bedBounds))
                        {
                            Debug.LogError($"MansionDecorator: escondite {placement.Name} en {space.Id} no tiene punto de ocultación.");
                            continue;
                        }
                        existingBed.name = placement.Name;
                        RemoveChildColliders(existingBed);
                        AddBoundsCollider(existingBed.gameObject, bedBounds, placement, false);
                        ConfigureHideout(existingBed.gameObject, true, "cama", space.Bounds);
                        hideoutLocations.Add($"Escondite {placement.Name} en {space.Id}, tipo cama");
                        hideoutCount++;
                        continue;
                    }
                }
                var asset = string.IsNullOrEmpty(placement.Asset) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(placement.Asset);
                if (asset == null)
                {
                    OmitObject(space.Id, placement.Name, "sin candidatos", omittedDetails);
                    omitted++;
                    continue;
                }
                var instance = PrefabUtility.InstantiatePrefab(asset, room.scene) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(asset);
                instance.name = placement.Name;
                instance.transform.SetParent(container, false);
                var isHanging = placement.Kind == MansionPlacementKind.Wall || placement.Kind == MansionPlacementKind.Window || placement.Kind == MansionPlacementKind.Switch;
                var isFurniture = placement.Kind == MansionPlacementKind.Furniture || placement.Kind == MansionPlacementKind.Hideout;
                var isKey = placement.Kind == MansionPlacementKind.Key;
                var targetSize = TargetSize(placement);
                if (!TryGetRendererBounds(instance, out var sourceBounds))
                {
                    OmitObject(space.Id, placement.Name, "sin candidatos", omittedDetails);
                    UnityEngine.Object.DestroyImmediate(instance);
                    omitted++;
                    continue;
                }
                var prefabScale = instance.transform.localScale;
                Bounds finalBounds = default;
                var fit = false;
                var relaxedDining = false;
                string failureReason = placement.Kind == MansionPlacementKind.Switch ? "sin puerta apta" : "sin candidatos";
                SupportSurface selectedSurface = null;
                var retryFactors = isKey || NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset)) == "camioneta"
                    ? new[] { 1f } : new[] { 1f, 1.15f / MansionDecorationConstants.ObjectScaleBoost, 1f / MansionDecorationConstants.ObjectScaleBoost };
                var usedFactor = 1f;
                for (var attempt = 0; attempt < retryFactors.Length && !fit; attempt++)
                {
                    usedFactor = retryFactors[attempt];
                    instance.transform.localScale = prefabScale;
                    ScaleToTarget(instance.transform, sourceBounds.size, targetSize * usedFactor, placement);
                    instance.transform.localScale *= (placement.Kind == MansionPlacementKind.Key) ? 1.3f : 1.15f;
                    var scaledScale = instance.transform.localScale;
                    var candidates = BuildPlacementCandidates(space, placement, instance, allSpaces, surfaces, doors, stairs);
                    if (candidates.Count == 0) failureReason = placement.Kind == MansionPlacementKind.Switch ? "sin puerta apta" : "sin candidatos";
                    foreach (var candidate in candidates)
                    {
                        instance.transform.localScale = scaledScale;
                        instance.transform.localRotation = Quaternion.Euler(0f, candidate.yaw, 0f);
                        if (!TryGetRendererBounds(instance, out var initialBounds)) continue;
                        var bounds = initialBounds;
                        var desiredCenter = candidate.center;
                        if (placement.Kind == MansionPlacementKind.RitualFloor)
                            desiredCenter.y = MansionGenerator.FloorY(space.Level) + bounds.extents.y + MansionDecorationConstants.SurfaceItemOffset;
                        else if (!isHanging && !isKey && !candidate.onSurface)
                            desiredCenter.y = MansionGenerator.FloorY(space.Level) + bounds.extents.y + MansionDecorationConstants.SurfaceItemOffset;
                        if (candidate.onSurface)
                            desiredCenter.y = candidate.surface.Bounds.max.y + bounds.extents.y + MansionDecorationConstants.SurfaceItemOffset;
                        SetBoundsCenter(instance.transform, bounds, desiredCenter);
                        if (!TryGetRendererBounds(instance, out bounds) || !FitsRoom(bounds, space.Bounds))
                        {
                            failureReason = "fuera del cuarto";
                            continue;
                        }

                        var diningTable = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset)) == "mesadecomedorconsillas";
                        if (diningTable && !HasRoomClearance(bounds, space.Bounds, MansionDecorationConstants.DiningClearance))
                        {
                            if (!HasRoomClearance(bounds, space.Bounds, 0.6f))
                            {
                                failureReason = "fuera del cuarto";
                                continue;
                            }
                            relaxedDining = true;
                        }

                        if (candidate.onSurface)
                        {
                            if (!FitsSurface(bounds, candidate.surface))
                            {
                                failureReason = "fuera del cuarto";
                                continue;
                            }
                            var overlap = FindSurfaceOverlap(bounds, occupied, candidate.surface.Id);
                            if (overlap != null)
                            {
                                failureReason = "solapa con " + overlap;
                                continue;
                            }
                        }
                        else
                        {
                            var overlap = FindOverlap(bounds, occupied);
                            if (overlap != null)
                            {
                                failureReason = "solapa con " + overlap;
                                continue;
                            }
                            var obstruction = ViolatesDoorOrStairClearance(bounds, space, placement.Kind, allSpaces, doors, stairs);
                            if (obstruction != null)
                            {
                                failureReason = obstruction;
                                continue;
                            }
                        }
                        finalBounds = bounds;
                        selectedSurface = candidate.surface;
                        fit = true;
                        break;
                    }
                }
                if (!fit)
                {
                    if (isKey)
                    {
                        var detail = space.Id + ":" + placement.Name;
                        omittedDetails.Add(detail + " (" + failureReason + ")");
                        omitted++;
                        Debug.LogError($"MansionDecorator: no se pudo colocar {placement.Name} en {space.Id}; {failureReason}.");
                    }
                    else
                        OmitObject(space.Id, placement.Name, failureReason, omittedDetails);
                    UnityEngine.Object.DestroyImmediate(instance);
                    continue;
                }

                if (relaxedDining)
                    Debug.LogWarning($"MansionDecorator: Mesa de comedor con sillas en {space.Id} requiere 0.6 m libres; no alcanza 0.9 m.");
                if (!isKey && usedFactor < 0.999f)
                    Debug.LogWarning($"MansionDecorator: {placement.Name} en {space.Id} quedó con factor de escala {usedFactor * MansionDecorationConstants.ObjectScaleBoost:0.##}.");

                RemoveChildColliders(instance.transform);
                var pickup = instance.GetComponent<PickupInteractable>();
                if (isKey)
                {
                    var keyId = placement.Name.Substring("Llave_".Length);
                    pickup = pickup ?? instance.AddComponent<PickupInteractable>();
                    pickup.SetItem(LoadOrCreateInventoryItem(keyId));
                    keyLocations.Add(placement.Name + " en " + space.Id + " sobre " + (selectedSurface != null ? selectedSurface.Name : "superficie alternativa"));
                }
                AddBoundsCollider(instance, finalBounds, placement, pickup != null);
                if (placement.Kind == MansionPlacementKind.Hideout)
                {
                    var hideoutType = HideoutType(placement.Asset);
                    ConfigureHideout(instance, hideoutType == "cama", hideoutType, space.Bounds);
                    hideoutLocations.Add($"Escondite {placement.Name} en {space.Id}, tipo {hideoutType}");
                }
                if (placement.Kind == MansionPlacementKind.Switch)
                    ConfigureRoomSwitch(instance, room);
                occupied.Add(new PlacedObject { Name = placement.Name, Bounds = finalBounds, SupportId = selectedSurface != null ? selectedSurface.Id : null });
                if (isFurniture || placement.Kind == MansionPlacementKind.Center)
                {
                    var surface = new SupportSurface { Id = placement.Name, Name = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset)), Bounds = finalBounds };
                    surfaces.Add(surface);
                }
                if (placement.Name.StartsWith("Escondite_", StringComparison.Ordinal)) hideoutCount++;
                placed++;
            }
        }

        private static void OmitObject(string room, string name, string reason, List<string> omittedDetails)
        {
            omittedDetails.Add(room + ":" + name + " (" + reason + ")");
            Debug.LogWarning($"MansionDecorator: omitido {room}:{name}; {reason}.");
        }

        private static string HideoutType(string assetPath)
        {
            var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(assetPath ?? string.Empty));
            if (stem.Contains("cama")) return "cama";
            if (stem.Contains("armario")) return "armario";
            return "otro";
        }

        private static void ConfigureHideout(GameObject instance, bool underBed, string type, Rect roomBounds)
        {
            var hideout = instance.GetComponent<UltimaCinta.Interaction.HideoutInteractable>() ??
                          instance.AddComponent<UltimaCinta.Interaction.HideoutInteractable>();
            hideout.SetUnderBed(underBed);
            hideout.SetHideoutType(type);
            hideout.SetRoomBounds(new Vector2(roomBounds.xMin, roomBounds.yMin), new Vector2(roomBounds.xMax, roomBounds.yMax));
        }

        private static void ConfigureRoomSwitch(GameObject instance, GameObject room)
        {
            var roomLightTransform = room.transform.Find("LuzHabitacion");
            var roomLight = roomLightTransform == null ? null : roomLightTransform.GetComponent<Light>();
            var toggle = instance.GetComponent<SwitchInteractable>() ?? instance.AddComponent<SwitchInteractable>();
            var serialized = new SerializedObject(toggle);
            serialized.FindProperty("controlledLight").objectReferenceValue = roomLight;
            serialized.FindProperty("controlledObject").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            toggle.SetFailureClip(FindSparkAudioClip());
        }

        private static AudioClip FindSparkAudioClip()
        {
            if (sparkClipSearched) return sparkClipCache;
            sparkClipSearched = true;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
                if (!name.Contains("chispa") && !name.Contains("spark") && !name.Contains("pop")) continue;
                sparkClipCache = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (sparkClipCache != null) break;
            }
            return sparkClipCache;
        }

        private sealed class PlacementCandidate
        {
            public Vector3 center;
            public float yaw;
            public bool onSurface;
            public SupportSurface surface;
        }

        private static int PlacementPriority(Placement placement)
        {
            if (placement.Kind == MansionPlacementKind.Furniture || placement.Kind == MansionPlacementKind.Hideout || placement.Kind == MansionPlacementKind.Center) return 0;
            if (placement.Kind == MansionPlacementKind.Key) return 1;
            if (placement.Kind == MansionPlacementKind.Surface) return 2;
            if (placement.Kind == MansionPlacementKind.RitualFloor) return 2;
            return 1;
        }

        private static List<PlacementCandidate> BuildPlacementCandidates(MansionLayoutSpace space, Placement placement,
            GameObject instance, List<MansionLayoutSpace> allSpaces, List<SupportSurface> surfaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs)
        {
            var result = new List<PlacementCandidate>();
            var floorY = MansionGenerator.FloorY(space.Level);
            if (placement.Kind == MansionPlacementKind.Key)
            {
                var preferred = new List<SupportSurface>();
                foreach (var surface in surfaces)
                    if (surface.Name == "mesaconlampara") preferred.Add(surface);
                AddSurfaceCandidates(result, instance, preferred);
                foreach (var surface in surfaces)
                    if (!preferred.Contains(surface)) AddSurfaceCandidates(result, instance, new List<SupportSurface> { surface });
                return result;
            }
            if (placement.Kind == MansionPlacementKind.Surface)
            {
                AddSurfaceCandidates(result, instance, surfaces);
                return result;
            }
            if (placement.Kind == MansionPlacementKind.Center)
            {
                result.Add(new PlacementCandidate { center = new Vector3(space.Bounds.center.x, floorY, space.Bounds.center.y) });
                return result;
            }
            if (placement.Kind == MansionPlacementKind.RitualFloor)
            {
                var angle = placement.InstanceIndex * 120f * Mathf.Deg2Rad;
                var radius = 1.35f;
                result.Add(new PlacementCandidate
                {
                    center = new Vector3(space.Bounds.center.x + Mathf.Cos(angle) * radius, floorY,
                        space.Bounds.center.y + Mathf.Sin(angle) * radius)
                });
                return result;
            }
            if (placement.Kind == MansionPlacementKind.Corner)
            {
                AddCornerCandidates(result, space, instance, placement, floorY);
                return result;
            }
            if (NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset)) == "mesilladenoche")
            {
                var bed = surfaces.Find(item => item.Name == "cama");
                if (bed != null)
                {
                    AddBedsideCandidates(result, instance, bed.Bounds, floorY);
                    return result;
                }
            }
            if (placement.Kind == MansionPlacementKind.Switch)
            {
                AddSwitchCandidate(result, space, instance, doors, allSpaces, stairs, floorY);
                return result;
            }

            AddWallCandidates(result, space, instance, placement, allSpaces, floorY);
            return result;
        }

        private static void AddBedsideCandidates(List<PlacementCandidate> result, GameObject instance, Bounds bed, float floorY)
        {
            if (!TryGetRendererBounds(instance, out var nightstand)) return;
            var horizontalX = bed.size.x >= bed.size.z;
            var offset = horizontalX
                ? new Vector3(bed.extents.x + nightstand.extents.x + MansionDecorationConstants.ObjectWallGap, 0f, 0f)
                : new Vector3(0f, 0f, bed.extents.z + nightstand.extents.z + MansionDecorationConstants.ObjectWallGap);
            result.Add(new PlacementCandidate { center = new Vector3(bed.center.x + offset.x, floorY, bed.center.z + offset.z) });
            result.Add(new PlacementCandidate { center = new Vector3(bed.center.x - offset.x, floorY, bed.center.z - offset.z) });
        }

        private static void AddSurfaceCandidates(List<PlacementCandidate> result, GameObject instance, List<SupportSurface> surfaces)
        {
            if (!TryGetRendererBounds(instance, out var objectBounds)) return;
            for (var index = surfaces.Count - 1; index >= 0; index--)
            {
                var surface = surfaces[index];
                var minX = surface.Bounds.min.x + objectBounds.extents.x;
                var maxX = surface.Bounds.max.x - objectBounds.extents.x;
                var minZ = surface.Bounds.min.z + objectBounds.extents.z;
                var maxZ = surface.Bounds.max.z - objectBounds.extents.z;
                for (var x = minX; x <= maxX + MansionGeneratorConstants.GridTolerance; x += 0.1f)
                for (var z = minZ; z <= maxZ + MansionGeneratorConstants.GridTolerance; z += 0.1f)
                    result.Add(new PlacementCandidate
                    {
                        center = new Vector3(x, surface.Bounds.max.y, z),
                        onSurface = true,
                        surface = surface
                    });
            }
        }

        private static void AddCornerCandidates(List<PlacementCandidate> result, MansionLayoutSpace space,
            GameObject instance, Placement placement, float floorY)
        {
            var isStorageBox = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset)) == "cajasdealmacen";
            for (var corner = 0; corner < 4; corner++)
            {
                var inward = new Vector3(corner % 2 == 0 ? 1f : -1f, 0f, corner < 2 ? 1f : -1f).normalized;
                var yaw = Quaternion.LookRotation(inward).eulerAngles.y;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                if (!TryGetRendererBounds(instance, out var bounds)) continue;
                var x = corner % 2 == 0 ? space.Bounds.xMin + bounds.extents.x + MansionDecorationConstants.WallOffset :
                    space.Bounds.xMax - bounds.extents.x - MansionDecorationConstants.WallOffset;
                var z = corner < 2 ? space.Bounds.yMin + bounds.extents.z + MansionDecorationConstants.WallOffset :
                    space.Bounds.yMax - bounds.extents.z - MansionDecorationConstants.WallOffset;
                result.Add(new PlacementCandidate { center = new Vector3(x, floorY, z), yaw = yaw });
                if (isStorageBox && placement.InstanceIndex > 0)
                {
                    var direction = corner % 2 == 0 ? 1f : -1f;
                    var spacing = (bounds.extents.x * 2f + MansionDecorationConstants.SurfaceItemOffset) * placement.InstanceIndex;
                    result.Add(new PlacementCandidate { center = new Vector3(x + direction * spacing, floorY, z), yaw = yaw });
                }
            }
            AddWallCandidates(result, space, instance,
                new Placement { Asset = "corner", Kind = MansionPlacementKind.Corner },
                new List<MansionLayoutSpace> { space }, floorY);
        }

        private static void AddSwitchCandidate(List<PlacementCandidate> result, MansionLayoutSpace space, GameObject instance,
            List<MansionLayoutDoor> doors, List<MansionLayoutSpace> spaces, List<MansionLayoutStair> stairs, float floorY)
        {
            var roomDoors = doors.FindAll(item => item.A == space.Id || item.B == space.Id);
            roomDoors.Sort((left, right) => SwitchDoorPriority(left, space, spaces).CompareTo(SwitchDoorPriority(right, space, spaces)));
            var floorYForDoor = floorY;
            var inward = Vector3.zero;
            if (!TryGetRendererBounds(instance, out var sourceBounds)) return;
            foreach (var door in roomDoors)
            {
                if (!MansionGenerator.TryResolveDoorSwing(door, spaces, stairs, out var hingeAtMax, out _, out _)) continue;
                inward = door.Axis == 'X'
                    ? (space.Bounds.center.x >= door.Position ? Vector3.right : Vector3.left)
                    : (space.Bounds.center.y >= door.Position ? Vector3.forward : Vector3.back);
                var yaw = Quaternion.LookRotation(inward).eulerAngles.y;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                if (!TryGetRendererBounds(instance, out var switchBounds)) continue;
                var handleAtMax = !hingeAtMax;
                for (var side = 0; side < 2; side++)
                {
                    var alongAtMax = side == 0 ? handleAtMax : !handleAtMax;
                    var alongExtent = door.Axis == 'X' ? switchBounds.extents.z : switchBounds.extents.x;
                    var along = alongAtMax
                        ? door.Max + MansionDecorationConstants.SwitchFrameOffset + alongExtent
                        : door.Min - MansionDecorationConstants.SwitchFrameOffset - alongExtent;
                    var center = door.Axis == 'X'
                        ? new Vector3(door.Position + inward.x * (switchBounds.extents.x + MansionDecorationConstants.ObjectWallGap),
                            floorYForDoor + MansionDecorationConstants.SwitchHeight, along)
                        : new Vector3(along, floorYForDoor + MansionDecorationConstants.SwitchHeight,
                            door.Position + inward.z * (switchBounds.extents.z + MansionDecorationConstants.ObjectWallGap));
                    var candidateBounds = switchBounds;
                    candidateBounds.center = center;
                    if (!FitsRoom(candidateBounds, space.Bounds)) continue;
                    if (DoorLeafIntersects(candidateBounds, door, spaces, stairs, space.Level)) continue;
                    result.Add(new PlacementCandidate { center = center, yaw = yaw });
                }
            }
        }

        private static int SwitchDoorPriority(MansionLayoutDoor door, MansionLayoutSpace room, List<MansionLayoutSpace> spaces)
        {
            var neighborId = door.A == room.Id ? door.B : door.A;
            var neighbor = spaces.Find(item => item.Id == neighborId);
            if (neighbor == null) return 1;
            var id = NormalizeModelName(neighbor.Id);
            var kind = NormalizeModelName(neighbor.Kind ?? string.Empty);
            return id.Contains("cor") || id.Contains("hall") || id.Contains("ves") || id.Contains("rec") || id.Contains("ser") ||
                   kind.Contains("pasillo") || kind.Contains("vestibulo") || kind.Contains("circulacion") || kind.Contains("recibidor") ? 0 : 1;
        }

        private static void AddWallCandidates(List<PlacementCandidate> result, MansionLayoutSpace space, GameObject instance,
            Placement placement, List<MansionLayoutSpace> allSpaces, float floorY)
        {
            var alongPositions = new[] { 0.25f, 0.5f, 0.75f };
            var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset));
            var edgeOrder = stem == "sofa" ? new[] { 2, 3, 0, 1 } :
                stem == "televisorviejo" ? new[] { 3, 2, 0, 1 } : new[] { 0, 1, 2, 3 };
            foreach (var edgeIndex in edgeOrder)
            for (var index = 0; index < alongPositions.Length; index++)
            {
                var alongFraction = alongPositions[index];
                char axis;
                float wallPosition;
                float along;
                if (edgeIndex == 0)
                {
                    axis = 'X'; wallPosition = space.Bounds.xMin; along = Mathf.Lerp(space.Bounds.yMin, space.Bounds.yMax, alongFraction);
                }
                else if (edgeIndex == 1)
                {
                    axis = 'X'; wallPosition = space.Bounds.xMax; along = Mathf.Lerp(space.Bounds.yMin, space.Bounds.yMax, alongFraction);
                }
                else if (edgeIndex == 2)
                {
                    axis = 'Z'; wallPosition = space.Bounds.yMin; along = Mathf.Lerp(space.Bounds.xMin, space.Bounds.xMax, alongFraction);
                }
                else
                {
                    axis = 'Z'; wallPosition = space.Bounds.yMax; along = Mathf.Lerp(space.Bounds.xMin, space.Bounds.xMax, alongFraction);
                }
                if (placement.Kind == MansionPlacementKind.Window && !IsExteriorEdge(space, allSpaces, axis, wallPosition)) continue;
                AddWallCandidate(result, space, instance, axis, wallPosition, along, floorY,
                    placement.Kind == MansionPlacementKind.Wall || placement.Kind == MansionPlacementKind.Window,
                    placement.Kind == MansionPlacementKind.Window ? MansionDecorationConstants.WindowHeight :
                    placement.Kind == MansionPlacementKind.Wall ? MansionDecorationConstants.WallHangingHeight :
                    placement.Kind == MansionPlacementKind.Switch ? MansionDecorationConstants.SwitchHeight : 0f);
            }
        }

        private static void AddWallCandidate(List<PlacementCandidate> result, MansionLayoutSpace space, GameObject instance,
            char axis, float wallPosition, float along, float floorY, bool wallMounted, float height, float fixedYaw = float.NaN)
        {
            var inward = axis == 'X'
                ? (wallPosition <= space.Bounds.center.x ? Vector3.right : Vector3.left)
                : (wallPosition <= space.Bounds.center.y ? Vector3.forward : Vector3.back);
            var yaw = float.IsNaN(fixedYaw) ? Quaternion.LookRotation(inward).eulerAngles.y : fixedYaw;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (!TryGetRendererBounds(instance, out var bounds)) return;
            var isTelevision = NormalizeModelName(instance.name) == "televisorviejo";
            var verticalCenter = isTelevision
                ? floorY + MansionDecorationConstants.TelevisionBaseHeight + bounds.extents.y
                : floorY + height;
            var center = axis == 'X'
                ? new Vector3(wallPosition + inward.x * (bounds.extents.x + (wallMounted ? MansionDecorationConstants.ObjectWallGap : MansionDecorationConstants.WallOffset)),
                    verticalCenter, along)
                : new Vector3(along, verticalCenter,
                    wallPosition + inward.z * (bounds.extents.z + (wallMounted ? MansionDecorationConstants.ObjectWallGap : MansionDecorationConstants.WallOffset)));
            result.Add(new PlacementCandidate { center = center, yaw = yaw });
        }

        private static bool IsExteriorEdge(MansionLayoutSpace space, List<MansionLayoutSpace> allSpaces, char axis, float position)
        {
            foreach (var other in allSpaces)
            {
                if (other.Id == space.Id || other.Level != space.Level) continue;
                var touching = axis == 'X'
                    ? Mathf.Approximately(other.Bounds.xMin, position) || Mathf.Approximately(other.Bounds.xMax, position)
                    : Mathf.Approximately(other.Bounds.yMin, position) || Mathf.Approximately(other.Bounds.yMax, position);
                if (!touching) continue;
                var overlap = axis == 'X'
                    ? Mathf.Min(space.Bounds.yMax, other.Bounds.yMax) - Mathf.Max(space.Bounds.yMin, other.Bounds.yMin)
                    : Mathf.Min(space.Bounds.xMax, other.Bounds.xMax) - Mathf.Max(space.Bounds.xMin, other.Bounds.xMin);
                if (overlap > MansionGeneratorConstants.GridTolerance) return false;
            }
            return true;
        }

        private static Vector3 TargetSize(Placement placement)
        {
            var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset ?? placement.Name));
            if (placement.Kind == MansionPlacementKind.Key) return Vector3.one * MansionDecorationConstants.KeySize;
            if (stem == "camioneta") return TargetSizeRaw(placement);
            return TargetSizeRaw(placement) * MansionDecorationConstants.ObjectScaleBoost;
        }

        private static Vector3 TargetSizeRaw(Placement placement)
        {
            var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset ?? placement.Name));
            if (placement.Kind == MansionPlacementKind.Key) return Vector3.one * MansionDecorationConstants.KeySize;
            if (stem == "camioneta") return new Vector3(MansionDecorationConstants.VanLength, MansionDecorationConstants.VanHeight, MansionDecorationConstants.VanWidth);
            if (stem == "cama") return new Vector3(MansionDecorationConstants.BedLength, MansionDecorationConstants.BedHeight, MansionDecorationConstants.BedWidth);
            if (stem == "armario") return new Vector3(MansionDecorationConstants.WardrobeWidth, MansionDecorationConstants.WardrobeHeight, MansionDecorationConstants.WardrobeDepth);
            if (stem == "sofa") return new Vector3(MansionDecorationConstants.SofaLength, MansionDecorationConstants.SofaHeight, MansionDecorationConstants.SofaDepth);
            if (stem == "mesadecomedorconsillas") return new Vector3(MansionDecorationConstants.DiningTableLength, MansionDecorationConstants.DiningTableHeight, MansionDecorationConstants.DiningTableWidth);
            if (stem == "estanteriaconlibros") return new Vector3(MansionDecorationConstants.BookshelfWidth, MansionDecorationConstants.BookshelfHeight, MansionDecorationConstants.BookshelfDepth);
            if (stem == "televisorviejo") return new Vector3(MansionDecorationConstants.TelevisionWidth, MansionDecorationConstants.TelevisionHeight, MansionDecorationConstants.TelevisionDepth);
            if (stem == "cocinaagas") return new Vector3(MansionDecorationConstants.KitchenWidth, MansionDecorationConstants.CounterHeight, MansionDecorationConstants.KitchenDepth);
            if (stem == "mesilladenoche") return new Vector3(MansionDecorationConstants.NightstandWidth, MansionDecorationConstants.NightstandHeight, MansionDecorationConstants.NightstandDepth);
            if (stem == "refrigerador") return Vector3.one * MansionDecorationConstants.FridgeWidth + Vector3.up * (MansionDecorationConstants.FridgeHeight - MansionDecorationConstants.FridgeWidth);
            if (stem == "banera") return new Vector3(MansionDecorationConstants.BathtubWidth, MansionDecorationConstants.BathtubHeight, MansionDecorationConstants.BathtubLength);
            if (stem == "tolet") return new Vector3(MansionDecorationConstants.ToiletWidth, MansionDecorationConstants.ToiletHeight, MansionDecorationConstants.ToiletDepth);
            if (stem == "fotografia") return new Vector3(MansionDecorationConstants.PhotographWidth, MansionDecorationConstants.PhotographHeight, MansionDecorationConstants.PhotographDepth);
            if (stem == "espejoderecibidor") return new Vector3(MansionDecorationConstants.MirrorWidth, MansionDecorationConstants.MirrorHeight, MansionDecorationConstants.MirrorDepth);
            if (stem == "espejoroto") return new Vector3(MansionDecorationConstants.MirrorWidth, MansionDecorationConstants.MirrorHeight, MansionDecorationConstants.MirrorDepth);
            if (stem == "frenteventana") return new Vector3(1.2f, 1.0f, 0.08f);
            if (stem == "interruptor") return new Vector3(0.12f, 0.12f, 0.05f);
            if (stem == "mesaconlampara") return new Vector3(1.2f, 0.8f, 0.65f);
            if (stem == "cunadebebe" || stem == "cd1da979c0aa41a2e056ddf80ea043ad") return new Vector3(1.3f, 1.0f, 0.75f);
            if (stem == "mecedora") return new Vector3(0.8f, 1.0f, 0.8f);
            if (stem == "maniqui") return new Vector3(0.5f, 1.7f, 0.5f);
            if (stem == "caldera") return new Vector3(0.9f, 1.4f, 0.8f);
            if (stem == "generador") return new Vector3(1.0f, 1.1f, 0.7f);
            if (stem == "escaleras") return new Vector3(0.55f, 2.0f, 0.15f);
            if (stem == "cajadeherramientas" || stem == "cajadecables" || stem == "maletindemetal") return new Vector3(0.55f, 0.35f, 0.4f);
            if (stem == "cajasdealmacen") return Vector3.one * MansionDecorationConstants.StorageBoxSize;
            if (stem == "lata") return Vector3.one * 0.15f;
            if (stem == "maceta") return new Vector3(0.35f, 0.55f, 0.35f);
            if (stem == "caballitodemadera") return new Vector3(0.7f, 0.8f, 0.3f);
            if (stem == "radio" || stem == "cajademusica") return new Vector3(0.25f, 0.2f, 0.18f);
            return Vector3.one * MansionDecorationConstants.SmallObjectSize;
        }

        private static void ScaleToTarget(Transform root, Vector3 currentSize, Vector3 targetSize, Placement placement = null)
        {
            var stem = placement == null ? NormalizeModelName(root.name) : NormalizeModelName(Path.GetFileNameWithoutExtension(placement.Asset ?? placement.Name));
            var dominantRatio = stem.Contains("cama") || stem == "sofa" || stem.Contains("mesa")
                ? targetSize.x / Mathf.Max(currentSize.x, Mathf.Epsilon)
                : stem == "armario" || stem.Contains("estanteria") || stem.Contains("maquina") || stem == "maniqui" || stem == "televisorviejo"
                    ? targetSize.y / Mathf.Max(currentSize.y, Mathf.Epsilon)
                    : targetSize.x >= targetSize.y && targetSize.x >= targetSize.z
                        ? targetSize.x / Mathf.Max(currentSize.x, Mathf.Epsilon)
                        : targetSize.y >= targetSize.z
                            ? targetSize.y / Mathf.Max(currentSize.y, Mathf.Epsilon)
                            : targetSize.z / Mathf.Max(currentSize.z, Mathf.Epsilon);
            var uniformScale = dominantRatio;
            var maxScaleX = targetSize.x * 1.25f / Mathf.Max(currentSize.x, Mathf.Epsilon);
            var maxScaleY = targetSize.y * 1.25f / Mathf.Max(currentSize.y, Mathf.Epsilon);
            var maxScaleZ = targetSize.z * 1.25f / Mathf.Max(currentSize.z, Mathf.Epsilon);
            uniformScale = Mathf.Min(uniformScale, Mathf.Min(maxScaleX, Mathf.Min(maxScaleY, maxScaleZ)));
            if (float.IsInfinity(uniformScale) || uniformScale <= 0f) return;
            root.localScale *= uniformScale;
        }

        private static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = default;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static void SetBoundsCenter(Transform root, Bounds bounds, Vector3 center)
        {
            root.position += center - bounds.center;
        }

        private static bool FitsRoom(Bounds bounds, Rect room) => bounds.min.x >= room.xMin && bounds.max.x <= room.xMax &&
            bounds.min.z >= room.yMin && bounds.max.z <= room.yMax;

        private static bool HasRoomClearance(Bounds bounds, Rect room, float clearance) =>
            bounds.min.x - room.xMin >= clearance && room.xMax - bounds.max.x >= clearance &&
            bounds.min.z - room.yMin >= clearance && room.yMax - bounds.max.z >= clearance;

        private static string FindOverlap(Bounds bounds, List<PlacedObject> occupied)
        {
            foreach (var other in occupied)
                if (bounds.Intersects(other.Bounds)) return other.Name;
            return null;
        }

        private static bool FitsSurface(Bounds bounds, SupportSurface surface) =>
            bounds.min.x >= surface.Bounds.min.x && bounds.max.x <= surface.Bounds.max.x &&
            bounds.min.z >= surface.Bounds.min.z && bounds.max.z <= surface.Bounds.max.z &&
            bounds.min.y >= surface.Bounds.max.y - MansionDecorationConstants.SurfaceItemOffset;

        private static string FindSurfaceOverlap(Bounds bounds, List<PlacedObject> occupied, string supportId)
        {
            foreach (var other in occupied)
                if (other.SupportId == supportId && bounds.Intersects(other.Bounds)) return other.Name;
            return null;
        }

        private static string ViolatesDoorOrStairClearance(Bounds bounds, MansionLayoutSpace space,
            MansionPlacementKind kind, List<MansionLayoutSpace> allSpaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs)
        {
            var isWallObject = kind == MansionPlacementKind.Wall || kind == MansionPlacementKind.Window || kind == MansionPlacementKind.Switch;
            foreach (var door in doors)
            {
                if (door.A != space.Id && door.B != space.Id) continue;
                var alongMin = door.Axis == 'X' ? bounds.min.z : bounds.min.x;
                var alongMax = door.Axis == 'X' ? bounds.max.z : bounds.max.x;
                var overlapsDoorSpan = alongMax > door.Min && alongMin < door.Max;
                var wallDepthMin = door.Axis == 'X' ? bounds.min.x : bounds.min.z;
                var wallDepthMax = door.Axis == 'X' ? bounds.max.x : bounds.max.z;
                var reachesDoorWall = door.Position >= wallDepthMin - MansionGeneratorConstants.SharedWallHalfThickness &&
                                      door.Position <= wallDepthMax + MansionGeneratorConstants.SharedWallHalfThickness;
                var floorY = MansionGenerator.FloorY(space.Level);
                var overlapsOpeningHeight = bounds.max.y > floorY && bounds.min.y < floorY + MansionGeneratorConstants.DoorHeight;

                if (isWallObject)
                {
                    if (overlapsDoorSpan && reachesDoorWall && overlapsOpeningHeight) return "bloquea puerta " + door.Id;
                    if (bounds.min.y < floorY + MansionGeneratorConstants.DoorHeight &&
                        DoorLeafIntersects(bounds, door, allSpaces, stairs, space.Level)) return "bloquea puerta " + door.Id;
                    continue;
                }

                var direction = door.Axis == 'X'
                    ? (space.Bounds.center.x >= door.Position ? 1f : -1f)
                    : (space.Bounds.center.y >= door.Position ? 1f : -1f);
                var strip = door.Axis == 'X'
                    ? Rect.MinMaxRect(Mathf.Min(door.Position, door.Position + direction * MansionDecorationConstants.DoorClearance), door.Min,
                        Mathf.Max(door.Position, door.Position + direction * MansionDecorationConstants.DoorClearance), door.Max)
                    : Rect.MinMaxRect(door.Min, Mathf.Min(door.Position, door.Position + direction * MansionDecorationConstants.DoorClearance),
                        door.Max, Mathf.Max(door.Position, door.Position + direction * MansionDecorationConstants.DoorClearance));
                if (IntersectsXZ(bounds, strip)) return "bloquea puerta " + door.Id;
                if (DoorSweepIntersects(bounds, door, allSpaces, stairs, space.Id)) return "bloquea puerta " + door.Id;
            }
            foreach (var stair in stairs)
            {
                if (stair.LandingA != space.Id && stair.LandingB != space.Id) continue;
                if (isWallObject) continue;
                if (IntersectsXZ(bounds, stair.Opening)) return "bloquea escalera " + stair.Id;
                var arrival = stair.LandingA == space.Id ? stair.ArrivalA : stair.ArrivalB;
                if (IntersectsXZ(bounds, Expand(arrival, MansionDecorationConstants.StairClearance))) return "bloquea escalera " + stair.Id;
            }
            return null;
        }

        private static bool DoorSweepIntersects(Bounds bounds, MansionLayoutDoor door, List<MansionLayoutSpace> spaces,
            List<MansionLayoutStair> stairs, string roomId)
        {
            if (!MansionGenerator.TryResolveDoorSwing(door, spaces, stairs, out var hingeAtMax, out var swingSign, out var inward) ||
                inward == null || inward.Id != roomId) return false;
            var along = hingeAtMax ? door.Max : door.Min;
            var hinge = door.Axis == 'X' ? new Vector3(door.Position, 0f, along) : new Vector3(along, 0f, door.Position);
            var leaf = door.Axis == 'X'
                ? new Vector3(0f, 0f, hingeAtMax ? -door.Width : door.Width)
                : new Vector3(hingeAtMax ? -door.Width : door.Width, 0f, 0f);
            var previous = hinge;
            for (var angle = 5f; angle <= MansionGeneratorConstants.DoorOpenAngle; angle += 5f)
            {
                var next = hinge + Quaternion.Euler(0f, swingSign * angle, 0f) * leaf;
                var segment = Rect.MinMaxRect(Mathf.Min(previous.x, next.x) - 0.03f, Mathf.Min(previous.z, next.z) - 0.03f,
                    Mathf.Max(previous.x, next.x) + 0.03f, Mathf.Max(previous.z, next.z) + 0.03f);
                if (IntersectsXZ(bounds, segment)) return true;
                previous = next;
            }
            return false;
        }

        private static bool DoorLeafIntersects(Bounds bounds, MansionLayoutDoor door, List<MansionLayoutSpace> spaces,
            List<MansionLayoutStair> stairs, string level)
        {
            if (!MansionGenerator.TryResolveDoorSwing(door, spaces, stairs, out var hingeAtMax, out var swingSign, out _)) return false;
            var floorY = MansionGenerator.FloorY(level);
            if (bounds.max.y <= floorY || bounds.min.y >= floorY + MansionGeneratorConstants.DoorHeight) return false;
            var along = hingeAtMax ? door.Max : door.Min;
            var hinge = door.Axis == 'X' ? new Vector3(door.Position, floorY, along) : new Vector3(along, floorY, door.Position);
            var leaf = door.Axis == 'X'
                ? new Vector3(0f, 0f, hingeAtMax ? -door.Width : door.Width)
                : new Vector3(hingeAtMax ? -door.Width : door.Width, 0f, 0f);
            var tip = hinge + Quaternion.Euler(0f, swingSign * MansionGeneratorConstants.DoorOpenAngle, 0f) * leaf;
            var opened = Rect.MinMaxRect(Mathf.Min(hinge.x, tip.x) - 0.03f, Mathf.Min(hinge.z, tip.z) - 0.03f,
                Mathf.Max(hinge.x, tip.x) + 0.03f, Mathf.Max(hinge.z, tip.z) + 0.03f);
            return IntersectsXZ(bounds, opened);
        }

        private static float DistanceToInterval(float value, float min, float max) => value < min ? min - value : value > max ? value - max : 0f;

        private static bool IntersectsOccupied(Bounds bounds, List<Bounds> occupied)
        {
            foreach (var other in occupied)
                if (bounds.Intersects(other)) return true;
            return false;
        }

        private static bool IntersectsXZ(Bounds bounds, Rect rect) => bounds.max.x > rect.xMin && bounds.min.x < rect.xMax &&
            bounds.max.z > rect.yMin && bounds.min.z < rect.yMax;

        private static void RemoveChildColliders(Transform root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void AddBoundsCollider(GameObject instance, Bounds worldBounds, Placement placement, bool pickup)
        {
            var localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
            {
                var corner = new Vector3(x == 0 ? worldBounds.min.x : worldBounds.max.x,
                    y == 0 ? worldBounds.min.y : worldBounds.max.y, z == 0 ? worldBounds.min.z : worldBounds.max.z);
                var local = instance.transform.InverseTransformPoint(corner);
                localMin = Vector3.Min(localMin, local);
                localMax = Vector3.Max(localMax, local);
            }
            var box = instance.AddComponent<BoxCollider>();
            if (placement.Kind == MansionPlacementKind.Hideout)
            {
                var margin = Vector3.one * MansionDecorationConstants.HideoutColliderMargin;
                localMin -= margin;
                localMax += margin;
                instance.layer = LayerMask.NameToLayer("Default");
            }
            box.center = (localMin + localMax) * MansionDecorationConstants.Half;
            box.size = localMax - localMin;
            box.isTrigger = placement.Kind == MansionPlacementKind.Hideout ||
                            !pickup && Mathf.Max(worldBounds.size.x, Mathf.Max(worldBounds.size.y, worldBounds.size.z)) <= MansionDecorationConstants.SmallObjectSize * 2f;
        }

        private static void ReplaceVan(Scene scene, Dictionary<string, string> models,
            ref int placed, ref int omitted, List<string> omittedDetails)
        {
            var exterior = FindGameObjectInScene(scene, "Exterior_Porche");
            if (exterior == null)
            {
                OmitObject("Exterior", "camioneta", "sin candidatos", omittedDetails);
                omitted++;
                return;
            }
            var existingVan = FindChild(exterior.transform, "camioneta") ?? FindChild(exterior.transform, "Camioneta");
            var oldPlaceholder = FindChild(exterior.transform, "Camioneta_Placeholder");
            if (!models.TryGetValue("camioneta", out var vanPath))
            {
                OmitObject("Exterior", "camioneta", "sin candidatos", omittedDetails);
                omitted++;
                return;
            }
            if (existingVan == null && oldPlaceholder == null)
            {
                OmitObject("Exterior", "camioneta", "sin candidatos", omittedDetails);
                omitted++;
                return;
            }

            var position = oldPlaceholder != null ? oldPlaceholder.transform.position : existingVan.transform.position;
            if (existingVan != null && TryGetRendererBounds(existingVan, out var existingBounds)) position = existingBounds.center;
            var oldDecoration = exterior.transform.Find("Decoracion");
            if (oldDecoration != null) UnityEngine.Object.DestroyImmediate(oldDecoration.gameObject);
            if (oldPlaceholder != null) UnityEngine.Object.DestroyImmediate(oldPlaceholder);
            var container = exterior.transform.Find("Decoracion");
            if (container == null) { container = new GameObject("Decoracion").transform; container.SetParent(exterior.transform, false); }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(vanPath);
            if (asset == null)
            {
                OmitObject("Exterior", "camioneta", "sin candidatos", omittedDetails);
                omitted++;
                return;
            }
            var van = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
            if (van == null) van = UnityEngine.Object.Instantiate(asset);
            van.name = "camioneta";
            van.transform.SetParent(container, false);
            var vanPlacement = new Placement { Name = "camioneta", Asset = vanPath, Kind = MansionPlacementKind.Furniture };
            if (!TryGetRendererBounds(van, out var vanBounds))
            {
                OmitObject("Exterior", "camioneta", "sin candidatos", omittedDetails);
                UnityEngine.Object.DestroyImmediate(van);
                omitted++;
                return;
            }
            ScaleToTarget(van.transform, vanBounds.size, TargetSize(vanPlacement));
            if (!TryGetRendererBounds(van, out vanBounds))
            {
                UnityEngine.Object.DestroyImmediate(van);
                omitted++;
                return;
            }
            SetBoundsCenter(van.transform, vanBounds, new Vector3(position.x,
                MansionGeneratorConstants.ExteriorGroundY + vanBounds.extents.y, position.z));
            TryGetRendererBounds(van, out vanBounds);
            RemoveChildColliders(van.transform);
            placed++;
            PlaceVanEquipment(container, vanBounds, scene, models, ref placed, ref omitted, omittedDetails);
        }

        private static void PlaceVanEquipment(Transform decorationRoot, Bounds vehicleBounds, Scene scene,
            Dictionary<string, string> models, ref int placed, ref int omitted, List<string> omittedDetails)
        {
            var gearRoot = new GameObject("Camioneta_Equipo").transform;
            gearRoot.SetParent(decorationRoot, false);
            var equipment = new List<(string Stem, int Count, bool Pickup)>
            {
                ("linterna", 1, true), ("medidoremf", 1, true), ("termometro", 1, true), ("cinta", 1, true),
                ("bateria", 2, true), ("diario", 1, true), ("librodeescritura", 1, true), ("velablanca", 2, true),
                ("ajo", 1, true), ("amuleto", 1, true), ("aguabendita", 1, true), ("estaca", 1, true),
                ("incienso", 1, true), ("botelladeagua", 1, true), ("camcorder", 1, false)
            };
            var slot = 0;
            var occupied = new List<PlacedObject>();
            foreach (var item in equipment)
            {
                if (!models.TryGetValue(item.Stem, out var path))
                {
                    OmitObject("Exterior", item.Stem, "sin candidatos", omittedDetails);
                    omitted += item.Count;
                    continue;
                }
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    OmitObject("Exterior", item.Stem, "sin candidatos", omittedDetails);
                    omitted += item.Count;
                    continue;
                }
                for (var copy = 0; copy < item.Count; copy++)
                {
                    var instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
                    if (instance == null) instance = UnityEngine.Object.Instantiate(asset);
                    var stem = NormalizeModelName(Path.GetFileNameWithoutExtension(path));
                    instance.name = stem;
                    instance.transform.SetParent(gearRoot, false);
                    var placement = new Placement { Asset = path, Name = stem, Kind = MansionPlacementKind.Surface };
                    if (!TryGetRendererBounds(instance, out var sourceBounds))
                    {
                        OmitObject("Exterior", stem, "sin candidatos", omittedDetails);
                        UnityEngine.Object.DestroyImmediate(instance);
                        omitted++;
                        continue;
                    }
                    var column = slot % 4;
                    var row = slot / 4;
                    var originalScale = instance.transform.localScale;
                    var targetSize = TargetSize(placement);
                    var placedGear = false;
                    string gearFailure = "fuera del cuarto";
                    var gearFactor = 1f;
                    var gearFactors = new[] { 1f, 1.15f / MansionDecorationConstants.ObjectScaleBoost, 1f / MansionDecorationConstants.ObjectScaleBoost };
                    Bounds bounds = default;
                    for (var attempt = 0; attempt < gearFactors.Length && !placedGear; attempt++)
                    {
                        gearFactor = gearFactors[attempt];
                        instance.transform.localScale = originalScale;
                        ScaleToTarget(instance.transform, sourceBounds.size, targetSize * gearFactor, placement);
                        instance.transform.localRotation = Quaternion.identity;
                        if (!TryGetRendererBounds(instance, out bounds))
                        {
                            gearFailure = "sin candidatos";
                            continue;
                        }
                        var target = new Vector3(vehicleBounds.max.x - 0.45f - column * MansionDecorationConstants.VanGearSpacing,
                            vehicleBounds.min.y + MansionDecorationConstants.VanGearHeight + bounds.extents.y,
                            vehicleBounds.center.z - 0.45f + row * MansionDecorationConstants.VanGearSpacing);
                        SetBoundsCenter(instance.transform, bounds, target);
                        if (!TryGetRendererBounds(instance, out bounds) || !ContainsBoundsXZ(vehicleBounds, bounds))
                        {
                            gearFailure = "fuera del cuarto";
                            continue;
                        }
                        var overlap = FindOverlap(bounds, occupied);
                        if (overlap != null)
                        {
                            gearFailure = "solapa con " + overlap;
                            continue;
                        }
                        placedGear = true;
                    }
                    if (!placedGear)
                    {
                        OmitObject("Exterior", stem, gearFailure, omittedDetails);
                        UnityEngine.Object.DestroyImmediate(instance);
                        omitted++;
                        continue;
                    }
                    if (gearFactor < 0.999f)
                        Debug.LogWarning($"MansionDecorator: {stem} en Exterior quedó con factor de escala {gearFactor * MansionDecorationConstants.ObjectScaleBoost:0.##}.");
                    RemoveChildColliders(instance.transform);
                    if (item.Pickup)
                    {
                        var pickup = instance.AddComponent<PickupInteractable>();
                        pickup.SetItem(LoadOrCreateInventoryItem(stem));
                    }
                    AddBoundsCollider(instance, bounds, placement, item.Pickup);
                    occupied.Add(new PlacedObject { Name = stem, Bounds = bounds });
                    placed++;
                    slot++;
                }
            }
        }

        private static bool ContainsBoundsXZ(Bounds outer, Bounds inner) => inner.min.x >= outer.min.x && inner.max.x <= outer.max.x &&
            inner.min.z >= outer.min.z && inner.max.z <= outer.max.z;

        private static GameObject FindGameObjectInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChild(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (var index = 0; index < parent.childCount; index++)
            {
                var found = FindChild(parent.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }

        private static string FindModel(List<string> models, params string[] names)
        {
            foreach (var requested in names)
            {
                foreach (var path in models)
                {
                    var stem = Path.GetFileNameWithoutExtension(path);
                    if (stem.IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0) return path;
                }
            }
            return null;
        }

        private static InventoryItem LoadOrCreateInventoryItem(string itemId)
        {
            if (!AssetDatabase.IsValidFolder(KeyItemsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
                AssetDatabase.CreateFolder("Assets/Generated", "KeyItems");
            }
            var assetName = NormalizeModelName(itemId);
            var path = KeyItemsFolder + "/" + assetName + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            if (item != null) return item;
            item = ScriptableObject.CreateInstance<InventoryItem>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("displayName").stringValue = itemId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        private static Vector3 RoomCenter(MansionLayoutSpace space, float y) =>
            new Vector3(space.Bounds.center.x, Mathf.Approximately(y, 0f) ? PlacementY : y, space.Bounds.center.y);

        private static Vector3 RoomOffset(MansionLayoutSpace space, float x, float z) =>
            new Vector3(Mathf.Clamp(space.Bounds.center.x + x, space.Bounds.xMin + MansionDecorationConstants.CandidateInset, space.Bounds.xMax - MansionDecorationConstants.CandidateInset),
                PlacementY,
                Mathf.Clamp(space.Bounds.center.y + z, space.Bounds.yMin + MansionDecorationConstants.CandidateInset, space.Bounds.yMax - MansionDecorationConstants.CandidateInset));

        private static Rect Expand(Rect rect, float margin) => Rect.MinMaxRect(rect.xMin - margin, rect.yMin - margin, rect.xMax + margin, rect.yMax + margin);
        private static bool Contains(Rect rect, Vector3 point) => point.x >= rect.xMin && point.x <= rect.xMax && point.z >= rect.yMin && point.z <= rect.yMax;
    }
}
#endif



#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UltimaCinta.Data;
using UltimaCinta.Interaction;
using UltimaCinta.Lighting;
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
        public const float TelevisionWidth = 0.9f;
        public const float TelevisionHeight = 0.55f;
        public const float TelevisionDepth = 0.12f;
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
        public const float KeySize = 0.15f;
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
    }

    public static class MansionDecorator
    {
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
            var metadata = ReadRoomMetadata(lines);
            var hideouts = ReadHideouts(lines, errors);
            if (errors.Count > 0)
            {
                foreach (var error in errors) Debug.LogError("MansionDecorator: " + error);
                return;
            }

            var scene = EditorSceneManager.OpenScene(MansionDecorationConstants.ScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var modelPaths = FindFbxModels();
            var roomPlacements = new Dictionary<string, List<Placement>>();
            foreach (var space in spaces)
            {
                var details = new List<Placement>();
                roomPlacements.Add(space.Id, details);
                AddRoomClues(space, details, modelPaths, metadata);
                AddRoomFurniture(space, details, modelPaths);
            }
            AddHideouts(hideouts, spaces, roomPlacements, modelPaths);
            AddKeys(roomPlacements, spaces, modelPaths);
            AddRitualItems(roomPlacements, spaces, modelPaths);

            var placed = 0;
            var placeholders = 0;
            var lights = 0;
            var hideoutCount = 0;
            var keyLocations = new List<string>();
            foreach (var space in spaces)
            {
                var room = FindGameObjectInScene(scene, space.Id);
                if (room == null)
                {
                    Debug.LogWarning($"MansionDecorator: no se encontró el recinto {space.Id}; se omite su decoración.");
                    continue;
                }
                var container = ResetDecorationContainer(room.transform);
                var roomMetadata = metadata.TryGetValue(space.Id, out var value) ? value : new RoomMetadata();
                PlaceRoomObjects(space, room, container, roomPlacements[space.Id], modelPaths, spaces, doors, stairs,
                    ref placed, ref placeholders, ref hideoutCount, keyLocations);
                lights += CreateRoomLight(space, roomMetadata, container);
            }

            ReplaceVan(scene, modelPaths, ref placed, ref placeholders);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MansionDecorationConstants.ScenePath);
            Debug.Log($"Mansión decorada: objetos {placed}, escondites {hideoutCount}, luces {lights}, placeholders {placeholders}, llaves [{string.Join(", ", keyLocations)}].");
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
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWith("Assets/Generated/MansionMaterials/", StringComparison.OrdinalIgnoreCase)) result.Add(path);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
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
            Dictionary<string, List<Placement>> placements, List<string> models)
        {
            foreach (var entry in hideouts)
            {
                if (!placements.TryGetValue(entry.Room, out var list)) continue;
                var room = spaces.Find(space => space.Id == entry.Room);
                if (room == null) continue;
                for (var index = 0; index < entry.Count; index++)
                {
                    var modelName = HideoutModel(entry.Type, index);
                    var asset = FindModel(models, modelName);
                    var offset = entry.Count > 1 ? (index == 0 ? -0.75f : 0.75f) : 0f;
                    list.Add(new Placement
                    {
                        Asset = asset,
                        Name = "Escondite_" + entry.Room + "_" + (index + 1),
                        Position = RoomOffset(room, offset, 1.0f)
                    });
                }
            }
        }

        private static string HideoutModel(string type, int index)
        {
            var normalized = type.ToLowerInvariant();
            if (normalized.Contains("bajo cama")) return "cama";
            if (normalized.Contains("cama") && index == 1) return "cama";
            if (normalized.Contains("estanter")) return "Estantería con libros";
            if (normalized.Contains("cama") && normalized.Contains("armario") && index == 1) return "cama";
            return "armario";
        }

        private static void AddKeys(Dictionary<string, List<Placement>> placements, List<MansionLayoutSpace> spaces, List<string> models)
        {
            var upperRoom = spaces.Find(space => space.Id == "G-EST");
            var basementKeyRoom = spaces.Find(space => space.Id == "U-D1");
            AddAt(placements, "G-EST", FindModel(models, "llave"), "Llave_KEY_UPPER", upperRoom == null ? Vector3.zero : RoomCenter(upperRoom, PlacementY));
            AddAt(placements, "U-D1", FindModel(models, "llave"), "Llave_KEY_BASEMENT", basementKeyRoom == null ? Vector3.zero : RoomCenter(basementKeyRoom, PlacementY));
        }

        private static void AddRitualItems(Dictionary<string, List<Placement>> placements, List<MansionLayoutSpace> spaces, List<string> models)
        {
            var items = new[] { "campana", "crusifijo", "espejo roto", "sal", "vela negra" };
            var room = spaces.Find(space => space.Id == "B-RIT");
            for (var index = 0; index < items.Length; index++)
                AddAt(placements, "B-RIT", FindModel(models, items[index]), "Objeto_Ritual_" + items[index],
                    room == null ? Vector3.zero : RoomOffset(room, (index - 2) * 0.65f, 1.1f));
            var generatorRoom = spaces.Find(space => space.Id == "B-GEN");
            AddAt(placements, "B-GEN", FindModel(models, "Generador"), "Generador_Sotano", generatorRoom == null ? Vector3.zero : RoomCenter(generatorRoom, PlacementY));
        }

        private static void AddAt(Dictionary<string, List<Placement>> placements, string room, string asset, string name, Vector3 offset)
        {
            if (placements.TryGetValue(room, out var list)) list.Add(new Placement { Asset = asset, Name = name, Position = offset });
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
            ref int placed, ref int placeholders, ref int hideoutCount, List<string> keyLocations)
        {
            var occupied = new List<Bounds>();
            var surfaces = new List<Bounds>();
            placements.Sort((left, right) => PlacementPriority(left).CompareTo(PlacementPriority(right)));
            foreach (var placement in placements)
            {
                if (placement.Name.StartsWith("Escondite_", StringComparison.Ordinal) &&
                    Path.GetFileNameWithoutExtension(placement.Asset ?? string.Empty).IndexOf("cama", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var existingBed = container.Find("Cama_" + space.Id);
                    if (existingBed != null)
                    {
                        existingBed.name = placement.Name;
                        hideoutCount++;
                        continue;
                    }
                }
                var asset = string.IsNullOrEmpty(placement.Asset) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(placement.Asset);
                if (asset == null)
                {
                    Debug.LogWarning($"MansionDecorator: se omite {placement.Name} en {space.Id}; no se encontró su FBX.");
                    continue;
                }
                var instance = PrefabUtility.InstantiatePrefab(asset, room.scene) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(asset);
                instance.name = placement.Name;
                instance.transform.SetParent(container, false);
                var isHanging = IsWallHanging(placement);
                var isFurniture = IsLargeFurniture(placement);
                var isKey = placement.Name.StartsWith("Llave_", StringComparison.Ordinal);
                var targetSize = TargetSize(placement);
                if (!TryGetRendererBounds(instance, out var sourceBounds))
                {
                    Debug.LogWarning($"MansionDecorator: se omite {placement.Name} en {space.Id}; el FBX no tiene Renderers.");
                    UnityEngine.Object.DestroyImmediate(instance);
                    continue;
                }
                ScaleToTarget(instance.transform, sourceBounds.size, targetSize);
                var scaledScale = instance.transform.localScale;

                var candidates = BuildPlacementCandidates(space, placement, instance, isFurniture, isHanging, isKey, surfaces, doors);
                Bounds finalBounds = default;
                var fit = false;
                foreach (var candidate in candidates)
                {
                    instance.transform.localScale = scaledScale;
                    instance.transform.localRotation = Quaternion.Euler(0f, candidate.yaw, 0f);
                    if (!TryGetRendererBounds(instance, out var initialBounds)) continue;
                    var bounds = initialBounds;
                    var desiredCenter = candidate.center;
                    if (!isHanging && !isKey && !candidate.onSurface)
                        desiredCenter.y = MansionGenerator.FloorY(space.Level) + bounds.extents.y + MansionDecorationConstants.SurfaceItemOffset;
                    if (candidate.onSurface)
                        desiredCenter.y = candidate.surface.max.y + bounds.extents.y + MansionDecorationConstants.SurfaceItemOffset;
                    SetBoundsCenter(instance.transform, bounds, desiredCenter);
                    if (!TryGetRendererBounds(instance, out bounds) || !FitsRoom(bounds, space.Bounds) ||
                        (placement.Name.IndexOf("Mesa_Comedor", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         !HasRoomClearance(bounds, space.Bounds, MansionDecorationConstants.DiningClearance)) ||
                        IntersectsOccupied(bounds, occupied) || ViolatesDoorOrStairClearance(bounds, space, allSpaces, doors, stairs)) continue;
                    finalBounds = bounds;
                    fit = true;
                    break;
                }
                if (!fit)
                {
                    Debug.LogWarning($"MansionDecorator: se omite {placement.Name} en {space.Id}; no cabe sin bloquear puertas, escaleras ni otros objetos.");
                    UnityEngine.Object.DestroyImmediate(instance);
                    continue;
                }

                RemoveChildColliders(instance.transform);
                var pickup = instance.GetComponent<PickupInteractable>();
                if (isKey)
                {
                    var keyId = placement.Name.Substring("Llave_".Length);
                    pickup = pickup ?? instance.AddComponent<PickupInteractable>();
                    pickup.SetItem(LoadOrCreateKeyItem(keyId));
                    keyLocations.Add(keyId + " en " + space.Id);
                }
                AddBoundsCollider(instance, finalBounds, placement, pickup != null);
                occupied.Add(finalBounds);
                if (isFurniture) surfaces.Add(finalBounds);
                if (placement.Name.StartsWith("Escondite_", StringComparison.Ordinal)) hideoutCount++;
                placed++;
            }
        }

        private sealed class PlacementCandidate
        {
            public Vector3 center;
            public float yaw;
            public bool onSurface;
            public Bounds surface;
        }

        private static int PlacementPriority(Placement placement)
        {
            if (placement.Name.StartsWith("Cama_", StringComparison.Ordinal)) return -1;
            if (placement.Name.StartsWith("Llave_", StringComparison.Ordinal)) return 2;
            if (placement.Name.StartsWith("Escondite_", StringComparison.Ordinal)) return 1;
            if (IsLargeFurniture(placement)) return 0;
            return 1;
        }

        private static List<PlacementCandidate> BuildPlacementCandidates(MansionLayoutSpace space, Placement placement,
            GameObject instance, bool isFurniture, bool isHanging, bool isKey, List<Bounds> surfaces, List<MansionLayoutDoor> doors)
        {
            var result = new List<PlacementCandidate>();
            var floorY = MansionGenerator.FloorY(space.Level);
            if (IsStoredJunk(placement))
            {
                for (var index = 0; index < 4; index++)
                {
                    var column = index % 2;
                    var row = index / 2;
                    var yaw = 45f + index * 90f;
                    instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    if (!TryGetRendererBounds(instance, out var boxBounds)) continue;
                    result.Add(new PlacementCandidate
                    {
                        center = new Vector3(space.Bounds.xMin + boxBounds.extents.x + MansionDecorationConstants.WallOffset + column * (boxBounds.size.x + MansionDecorationConstants.WallOffset),
                            floorY, space.Bounds.yMin + boxBounds.extents.z + MansionDecorationConstants.WallOffset + row * (boxBounds.size.z + MansionDecorationConstants.WallOffset)),
                        yaw = yaw
                    });
                }
                return result;
            }
            if (isFurniture && placement.Name.IndexOf("Mesa_Comedor", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Add(new PlacementCandidate { center = new Vector3(space.Bounds.center.x, floorY, space.Bounds.center.y) });

            if (space.Id == "G-SAL" || space.Id == "G-TV")
            {
                if (placement.Name.StartsWith("Sofa_", StringComparison.Ordinal))
                {
                    instance.transform.localRotation = Quaternion.identity;
                    if (TryGetRendererBounds(instance, out var sofaBounds))
                        result.Add(new PlacementCandidate { center = new Vector3(space.Bounds.center.x, floorY, space.Bounds.yMin + sofaBounds.extents.z + MansionDecorationConstants.WallOffset), yaw = 0f });
                }
                else if (placement.Name.StartsWith("Televisor_", StringComparison.Ordinal))
                {
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    if (TryGetRendererBounds(instance, out var tvBounds))
                        result.Add(new PlacementCandidate { center = new Vector3(space.Bounds.center.x, floorY, space.Bounds.yMax - tvBounds.extents.z - MansionDecorationConstants.WallOffset), yaw = 180f });
                }
            }

            if (isKey)
            {
                if (surfaces.Count > 0)
                {
                    var surface = surfaces[surfaces.Count - 1];
                    foreach (var offset in new[] { Vector2.zero, Vector2.left * 0.2f, Vector2.right * 0.2f, Vector2.up * 0.2f, Vector2.down * 0.2f })
                    {
                        result.Add(new PlacementCandidate
                        {
                            center = new Vector3(surface.center.x + offset.x, surface.max.y, surface.center.z + offset.y),
                            onSurface = true,
                            surface = surface
                        });
                    }
                }
                return result;
            }

            if (!isFurniture && !isHanging)
            {
                for (var index = surfaces.Count - 1; index >= 0; index--)
                {
                    var surface = surfaces[index];
                    var fraction = (index % 3 - 1) * 0.2f;
                    result.Add(new PlacementCandidate
                    {
                        center = new Vector3(surface.center.x + fraction, surface.max.y, surface.center.z),
                        onSurface = true,
                        surface = surface
                    });
                }
            }

            var wallOffsets = new[] { 0.25f, 0.5f, 0.75f };
            for (var wall = 0; wall < 4; wall++)
            for (var offsetIndex = 0; offsetIndex < wallOffsets.Length; offsetIndex++)
            {
                var offset = wallOffsets[offsetIndex];
                Vector3 inward;
                Vector3 target;
                switch (wall)
                {
                    case 0: inward = Vector3.right; target = new Vector3(space.Bounds.xMin, floorY, Mathf.Lerp(space.Bounds.yMin, space.Bounds.yMax, offset)); break;
                    case 1: inward = Vector3.left; target = new Vector3(space.Bounds.xMax, floorY, Mathf.Lerp(space.Bounds.yMin, space.Bounds.yMax, offset)); break;
                    case 2: inward = Vector3.forward; target = new Vector3(Mathf.Lerp(space.Bounds.xMin, space.Bounds.xMax, offset), floorY, space.Bounds.yMin); break;
                    default: inward = Vector3.back; target = new Vector3(Mathf.Lerp(space.Bounds.xMin, space.Bounds.xMax, offset), floorY, space.Bounds.yMax); break;
                }
                var yaw = Quaternion.LookRotation(inward).eulerAngles.y;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                if (!TryGetRendererBounds(instance, out var bounds)) continue;
                var extents = new Vector3(bounds.extents.x, 0f, bounds.extents.z);
                if (isHanging)
                {
                    target.y += MansionDecorationConstants.WallHangingHeight;
                    if (Mathf.Abs(inward.x) > 0f) target.x += inward.x * (extents.x + MansionDecorationConstants.WallOffset);
                    else target.z += inward.z * (extents.z + MansionDecorationConstants.WallOffset);
                }
                else if (Mathf.Abs(inward.x) > 0f) target.x += inward.x * (extents.x + MansionDecorationConstants.WallOffset);
                else target.z += inward.z * (extents.z + MansionDecorationConstants.WallOffset);
                if (isHanging && placement.Name.IndexOf("puerta", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var entry = doors.Find(door => door.A == space.Id && door.B == "EXTERIOR");
                    if (entry != null)
                    {
                        var doorPoint = entry.Axis == 'X'
                            ? new Vector3(entry.Position, target.y, (entry.Min + entry.Max) * MansionDecorationConstants.Half)
                            : new Vector3((entry.Min + entry.Max) * MansionDecorationConstants.Half, target.y, entry.Position);
                        var look = doorPoint - target;
                        if (look.sqrMagnitude > Mathf.Epsilon) yaw = Quaternion.LookRotation(look).eulerAngles.y;
                    }
                }
                result.Add(new PlacementCandidate { center = target, yaw = yaw });
            }

            if (!isFurniture && !isHanging && surfaces.Count == 0)
            {
                var corner = new Vector3(space.Bounds.xMin + MansionDecorationConstants.WallOffset, floorY, space.Bounds.yMin + MansionDecorationConstants.WallOffset);
                result.Add(new PlacementCandidate { center = corner, yaw = 45f });
            }
            return result;
        }

        private static bool IsLargeFurniture(Placement placement)
        {
            var name = (placement.Name + " " + placement.Asset).ToLowerInvariant();
            return name.Contains("cama") || name.Contains("armario") || name.Contains("mesilla") || name.Contains("estanter") ||
                   name.Contains("sofa") || name.Contains("sofá") || name.Contains("nevera") || name.Contains("refrigerador") ||
                   name.Contains("cocina") || name.Contains("televisor") || name.Contains("mesa_comedor") || name.Contains("escritorio") ||
                   name.Contains("generador") || name.Contains("caldera") || name.Contains("bañera") || name.Contains("tolet");
        }

        private static bool IsWallHanging(Placement placement)
        {
            var name = (placement.Name + " " + placement.Asset).ToLowerInvariant();
            return name.Contains("fotografia") || name.Contains("fotografía") || name.Contains("espejo de recibidor");
        }

        private static bool IsStoredJunk(Placement placement)
        {
            var name = (placement.Name + " " + placement.Asset).ToLowerInvariant();
            return name.Contains("caja") || name.Contains("lata") || name.Contains("trasto");
        }

        private static Vector3 TargetSize(Placement placement)
        {
            var name = (placement.Name + " " + placement.Asset).ToLowerInvariant();
            if (name.Contains("llave_")) return Vector3.one * MansionDecorationConstants.KeySize;
            if (name.Contains("camioneta")) return new Vector3(MansionDecorationConstants.VanLength, MansionDecorationConstants.VanHeight, MansionDecorationConstants.VanWidth);
            if (name.Contains("lata")) return Vector3.one * 0.15f;
            if (IsStoredJunk(placement)) return Vector3.one * MansionDecorationConstants.StorageBoxSize;
            if (name.Contains("cama")) return new Vector3(MansionDecorationConstants.BedLength, MansionDecorationConstants.BedHeight, MansionDecorationConstants.BedWidth);
            if (name.Contains("armario")) return new Vector3(MansionDecorationConstants.WardrobeWidth, MansionDecorationConstants.WardrobeHeight, MansionDecorationConstants.WardrobeDepth);
            if (name.Contains("sofa") || name.Contains("sofá")) return new Vector3(MansionDecorationConstants.SofaLength, MansionDecorationConstants.SofaHeight, MansionDecorationConstants.SofaDepth);
            if (name.Contains("mesa_comedor")) return new Vector3(MansionDecorationConstants.DiningTableLength, MansionDecorationConstants.DiningTableHeight, MansionDecorationConstants.DiningTableWidth);
            if (name.Contains("estanter")) return new Vector3(MansionDecorationConstants.BookshelfWidth, MansionDecorationConstants.BookshelfHeight, MansionDecorationConstants.BookshelfDepth);
            if (name.Contains("televisor")) return new Vector3(MansionDecorationConstants.TelevisionWidth, MansionDecorationConstants.TelevisionHeight, MansionDecorationConstants.TelevisionDepth);
            if (name.Contains("cocina")) return new Vector3(MansionDecorationConstants.KitchenWidth, MansionDecorationConstants.CounterHeight, MansionDecorationConstants.KitchenDepth);
            if (name.Contains("mesilla")) return new Vector3(MansionDecorationConstants.NightstandWidth, MansionDecorationConstants.NightstandHeight, MansionDecorationConstants.NightstandDepth);
            if (name.Contains("nevera") || name.Contains("refrigerador")) return new Vector3(MansionDecorationConstants.FridgeWidth, MansionDecorationConstants.FridgeHeight, MansionDecorationConstants.FridgeWidth);
            if (name.Contains("bañera")) return new Vector3(MansionDecorationConstants.BathtubWidth, MansionDecorationConstants.BathtubHeight, MansionDecorationConstants.BathtubLength);
            if (name.Contains("tolet")) return new Vector3(MansionDecorationConstants.ToiletWidth, MansionDecorationConstants.ToiletHeight, MansionDecorationConstants.ToiletDepth);
            if (name.Contains("fotografia") || name.Contains("fotografía")) return new Vector3(MansionDecorationConstants.PhotographWidth, MansionDecorationConstants.PhotographHeight, MansionDecorationConstants.PhotographDepth);
            if (name.Contains("espejo de recibidor")) return new Vector3(MansionDecorationConstants.MirrorWidth, MansionDecorationConstants.MirrorHeight, MansionDecorationConstants.MirrorDepth);
            if (IsLargeFurniture(placement)) return Vector3.one * MansionDecorationConstants.FurnitureMaxDimension;
            return Vector3.one * MansionDecorationConstants.SmallObjectSize;
        }

        private static void ScaleToTarget(Transform root, Vector3 currentSize, Vector3 targetSize)
        {
            var scale = new Vector3(
                currentSize.x > Mathf.Epsilon ? targetSize.x / currentSize.x : 1f,
                currentSize.y > Mathf.Epsilon ? targetSize.y / currentSize.y : 1f,
                currentSize.z > Mathf.Epsilon ? targetSize.z / currentSize.z : 1f);
            root.localScale = Vector3.Scale(root.localScale, scale);
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

        private static bool IntersectsOccupied(Bounds bounds, List<Bounds> occupied)
        {
            foreach (var other in occupied)
                if (bounds.Intersects(other)) return true;
            return false;
        }

        private static bool ViolatesDoorOrStairClearance(Bounds bounds, MansionLayoutSpace space,
            List<MansionLayoutSpace> allSpaces, List<MansionLayoutDoor> doors, List<MansionLayoutStair> stairs)
        {
            foreach (var door in doors)
            {
                if (door.A != space.Id && door.B != space.Id) continue;
                var swing = MansionGenerator.DoorSwingBounds(door, allSpaces, MansionDecorationConstants.DoorArcClearance);
                if (swing.HasValue && IntersectsXZ(bounds, swing.Value)) return true;
                var alongMin = door.Axis == 'X' ? bounds.min.z : bounds.min.x;
                var alongMax = door.Axis == 'X' ? bounds.max.z : bounds.max.x;
                var normalMin = door.Axis == 'X' ? bounds.min.x : bounds.min.z;
                var normalMax = door.Axis == 'X' ? bounds.max.x : bounds.max.z;
                var doorDistance = door.Axis == 'X'
                    ? DistanceToInterval(door.Position, normalMin, normalMax)
                    : DistanceToInterval(door.Position, normalMin, normalMax);
                var overlapsDoorSpan = alongMax >= door.Min && alongMin <= door.Max;
                if (overlapsDoorSpan && doorDistance < MansionDecorationConstants.DoorClearance) return true;
            }
            foreach (var stair in stairs)
            {
                if (stair.LandingA != space.Id && stair.LandingB != space.Id) continue;
                if (IntersectsXZ(bounds, stair.Opening)) return true;
                var arrival = stair.LandingA == space.Id ? stair.ArrivalA : stair.ArrivalB;
                if (IntersectsXZ(bounds, Expand(arrival, MansionDecorationConstants.StairClearance))) return true;
            }
            return false;
        }

        private static float DistanceToInterval(float value, float min, float max) => value < min ? min - value : value > max ? value - max : 0f;

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
            box.center = (localMin + localMax) * MansionDecorationConstants.Half;
            box.size = localMax - localMin;
            box.isTrigger = !pickup && Mathf.Max(worldBounds.size.x, Mathf.Max(worldBounds.size.y, worldBounds.size.z)) <= MansionDecorationConstants.SmallObjectSize * 2f;
        }

        private static int CreateRoomLight(MansionLayoutSpace space, RoomMetadata metadata, Transform container)
        {
            if (metadata == null || metadata.Light == null || metadata.Light.Equals("apagada", StringComparison.OrdinalIgnoreCase)) return 0;
            var root = new GameObject(metadata.Light.Equals("parpadeante", StringComparison.OrdinalIgnoreCase) ? "Luz_Parpadeante" : "Luz_Calida").transform;
            root.SetParent(container, false);
            root.localPosition = RoomCenter(space, MansionDecorationConstants.CeilingLightHeight);
            var light = root.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = MansionDecorationConstants.LightRange;
            light.intensity = MansionDecorationConstants.WarmLightIntensity;
            light.colorTemperature = MansionDecorationConstants.KelvinWarm;
            light.useColorTemperature = true;
            if (metadata.Light.Equals("parpadeante", StringComparison.OrdinalIgnoreCase)) root.gameObject.AddComponent<LightFlicker>();
            return 1;
        }

        private static void ReplaceVan(Scene scene, List<string> models, ref int placed, ref int placeholders)
        {
            var exterior = FindGameObjectInScene(scene, "Exterior_Porche");
            if (exterior == null) return;
            var existingVan = FindChild(exterior.transform, "Camioneta");
            var oldPlaceholder = FindChild(exterior.transform, "Camioneta_Placeholder");
            if (existingVan == null && oldPlaceholder == null) return;
            var position = existingVan != null ? existingVan.transform.position : oldPlaceholder.transform.position;
            var oldDecoration = exterior.transform.Find("Decoracion");
            if (oldDecoration != null) UnityEngine.Object.DestroyImmediate(oldDecoration.gameObject);
            if (oldPlaceholder != null) UnityEngine.Object.DestroyImmediate(oldPlaceholder);
            var asset = FindModel(models, "camioneta");
            var container = exterior.transform.Find("Decoracion");
            if (container == null) { container = new GameObject("Decoracion").transform; container.SetParent(exterior.transform, false); }
            if (asset != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                var van = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (van == null) van = UnityEngine.Object.Instantiate(prefab);
                van.name = "Camioneta";
                van.transform.SetParent(container, false);
                var vanPlacement = new Placement { Name = "Camioneta", Asset = asset };
                if (TryGetRendererBounds(van, out var vanBounds))
                {
                    ScaleToTarget(van.transform, vanBounds.size, TargetSize(vanPlacement));
                    if (TryGetRendererBounds(van, out vanBounds))
                    {
                        SetBoundsCenter(van.transform, vanBounds, new Vector3(position.x,
                            UltimaCinta.Editor.MansionGeneratorConstants.ExteriorGroundY + vanBounds.extents.y, position.z));
                        TryGetRendererBounds(van, out vanBounds);
                        RemoveChildColliders(van.transform);
                        AddBoundsCollider(van, vanBounds, vanPlacement, false);
                        placed++;
                    }
                }
                else Debug.LogWarning("MansionDecorator: se omite la camioneta; el FBX no tiene Renderers.");
            }
            else
            {
                var van = GameObject.CreatePrimitive(PrimitiveType.Cube);
                van.name = "PLACEHOLDER_Camioneta";
                van.transform.SetParent(container, false);
                van.transform.position = position;
                van.transform.localScale = new Vector3(6f, 2.7f, 6f);
                placeholders++;
                Debug.LogWarning("MansionDecorator: falta camioneta.fbx; se conservó un cubo placeholder.");
            }
        }

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

        private static InventoryItem LoadOrCreateKeyItem(string keyId)
        {
            if (!AssetDatabase.IsValidFolder(KeyItemsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
                AssetDatabase.CreateFolder("Assets/Generated", "KeyItems");
            }
            var path = KeyItemsFolder + "/" + keyId + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            if (item != null) return item;
            item = ScriptableObject.CreateInstance<InventoryItem>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("displayName").stringValue = keyId;
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

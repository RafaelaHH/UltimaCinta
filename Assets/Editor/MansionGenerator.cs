#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UltimaCinta.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaCinta.Editor
{
    internal static class MansionGeneratorConstants
    {
        public const string PlanPath = "Docs/PlanoMansion.md";
        public const string ScenePath = "Assets/Scenes/Mansion_Generated.unity";
        public const string MaterialsFolder = "Assets/Generated/MansionMaterials";
        public const float WallThickness = 0.18f;
        public const float SharedWallHalfThickness = 0.09f;
        public const float FloorBuildUp = 0.326f;
        public const float GroundFloorY = 0f;
        public const float BasementY = -3.026f;
        public const float FirstFloorY = 3.026f;
        public const float AtticY = 6.052f;
        public const float ClearHeight = 2.70f;
        public const float AtticHeight = 2.60f;
        public const float DoorHeight = 2.10f;
        public const float DoorLeafThickness = 0.06f;
        public const float DoorFrameWidth = 0.06f;
        public const float DoorFrameDepth = 0.04f;
        public const float CeilingSlabThickness = 0.12f;
        public const float ExteriorGroundY = -0.45f;
        public const float StepRise = 0.15f;
        public const float StepTread = 0.28f;
        public const int StepCount = 3;
        public const float RailingHeight = 1.0f;
        public const float RailingThickness = 0.06f;
        public const float TerrainMargin = 3.0f;
        public const float GridTolerance = 0.001f;
        public const float Half = 0.5f;
        public const float StairRiserHeight = 0.178f;
        public const float StairTreadDepth = 0.28f;
        public const int StairRiserCount = 17;
        public const int StairTreadCount = 16;
        public const float StairWidth = 1.20f;
        public const float StairRailHeight = 0.95f;
        public const float StairRailThickness = 0.06f;
        public const float MinimumHeadroom = 2.10f;
        public const float MinimumStairRun = 3.80f;
        public const float BasementShaftWallThickness = 0.18f;
        public const float DimDirectionalIntensity = 0.12f;
        public const float FallbackCameraHeight = 1.60f;
        public const float PlayerSpawnY = 0.10f;
        public const float StairTreadThickness = 0.04f;
        public const float StairRampThickness = 0.08f;
        public const int BasementExitRiserCount = 17;
        public const float GroundFillThickness = 0.124f;
        public const float PorchWidth = 6f;
        public const float PorchDepth = 3f;
        public const float VanBodyHeight = 2.7f;
        public const float FallbackCharacterHeight = 2.0f;
        public const float FallbackCharacterRadius = 0.3f;
        public const float DirectionalPitch = 35f;
        public const float DirectionalYaw = -30f;
        public const float DoorOpenAngle = 90f;
        public const float RoomLightHeight = 2.35f;
        public const float RoomLightRange = 4.5f;
        public const float RoomLightIntensity = 0.45f;
        public const float PointOfInterestHeight = 0.05f;
    }

    public static class MansionGenerator
    {
        private const string MenuPath = "Tools/UltimaCinta/2. Generar mansión completa";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private static readonly string[] LevelNames = { "Sótano", "Planta baja", "Primer piso", "Desván" };
        private sealed class DoorSwingResolution
        {
            public bool Success;
            public bool HingeAtMax;
            public float SwingSign;
            public string InwardSpaceId;
        }
        private static readonly Dictionary<string, DoorSwingResolution> DoorSwingCache = new Dictionary<string, DoorSwingResolution>(StringComparer.Ordinal);

        private sealed class ExteriorRect
        {
            public string Id;
            public Rect Bounds;
            public string Kind;
        }

        private sealed class WallEdge
        {
            public MansionLayoutSpace Owner;
            public MansionLayoutSpace Other;
            public char Axis;
            public float Position;
            public float Min;
            public float Max;
            public bool Exterior;
        }

        private sealed class Materials
        {
            public Material Wall;
            public Material Floor;
            public Material Ceiling;
            public Material Door;
            public Material Frame;
            public Material Railing;
            public Material Step;
            public Material Ground;
            public Material Placeholder;
        }

        [MenuItem(MenuPath)]
        public static void GenerateGroundFloorAndExterior()
        {
            DoorSwingCache.Clear();
            var projectRoot = Directory.GetParent(Application.dataPath);
            var planPath = Path.Combine(projectRoot.FullName, MansionGeneratorConstants.PlanPath);
            if (!File.Exists(planPath))
            {
                Debug.LogError($"MansionGenerator: no existe {MansionGeneratorConstants.PlanPath}.");
                return;
            }

            var lines = File.ReadAllLines(planPath);
            var readErrors = new List<string>();
            var allSpaces = MansionLayoutPlanReader.ReadSpaces(lines, readErrors);
            var allDoors = MansionLayoutPlanReader.ReadDoors(lines, readErrors);
            var stairs = MansionLayoutPlanReader.ReadStairs(lines, readErrors);
            var gates = MansionLayoutPlanReader.ReadGates(lines, readErrors);
            var exterior = ReadExterior(lines, readErrors);
            var groundSpaces = allSpaces.FindAll(space => space.Id.StartsWith("G-", StringComparison.Ordinal));

            if (readErrors.Count > 0 || !ValidatePlanData(allSpaces, allDoors, stairs, gates, exterior, readErrors))
            {
                foreach (var error in readErrors) Debug.LogError($"MansionGenerator: {error}");
                if (readErrors.Count == 0) Debug.LogError("MansionGenerator: lectura del plano incompleta; generación cancelada.");
                return;
            }

            var materials = CreateMaterials();
            if (materials == null)
            {
                Debug.LogError($"MansionGenerator: no se encontró el shader '{LitShaderName}'.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var spaces = allSpaces;
            var doors = allDoors;
            var levelRoots = CreateLevelRoots();
            var roots = new Dictionary<string, Transform>();
            foreach (var space in spaces)
            {
                var room = new GameObject(space.Id).transform;
                room.SetParent(levelRoots[space.Level], false);
                roots.Add(space.Id, room);
            }

            var wallCount = 0;
            var doorCount = 0;
            var stepCount = 0;
            var doorsSwingingPositive = 0;
            var doorsSwingingNegative = 0;
            var unresolvedDoorCount = 0;
            BuildFloorsAndCeilings(spaces, stairs, roots, materials);
            foreach (var level in LevelNames)
            {
                var levelSpaces = spaces.FindAll(space => space.Level == level);
                var levelDoors = doors.FindAll(door => door.B == "EXTERIOR" && IsLevel(door.A, spaces, level) ||
                    door.B != "EXTERIOR" && IsLevel(door.A, spaces, level) && IsLevel(door.B, spaces, level));
                foreach (var edge in BuildWallEdges(levelSpaces))
                    wallCount += BuildWallEdge(edge, levelDoors, roots, materials);
            }
            foreach (var door in doors)
            {
                if (!roots.TryGetValue(door.A, out var owner)) continue;
                if (TryResolveDoorSwing(door, spaces, stairs, out var hingeAtMax, out var swingSign, out _))
                {
                    BuildDoor(door, owner, materials, FloorY(FindSpace(spaces, door.A).Level), hingeAtMax, swingSign);
                    if (swingSign > 0f) doorsSwingingPositive++; else doorsSwingingNegative++;
                }
                else
                {
                    unresolvedDoorCount++;
                    BuildDoor(door, owner, materials, FloorY(FindSpace(spaces, door.A).Level), false, 1f, false);
                }
                doorCount++;
                if (door.B == "EXTERIOR" && FindSpace(spaces, door.A).Level == "Planta baja")
                    stepCount += BuildSteps(door, owner, spaces.FindAll(space => space.Level == "Planta baja"), materials);
            }

            BuildExterior(exterior, groundSpaces, materials);
            BuildBasementBackfill(spaces, groundSpaces, materials);
            BuildBasementExit(doors, spaces, roots, materials, ref stepCount);
            BuildStairs(stairs, spaces, roots, materials);
            BuildStairGates(gates, stairs, spaces, roots, materials, ref doorCount);
            BuildPointOfInterestMarkers(spaces, roots);
            AddPlayerAndLighting(scene, roots, levelRoots, projectRoot.FullName, spaces);

            var target = MansionGeneratorConstants.ScenePath;
            Directory.CreateDirectory(Path.Combine(projectRoot.FullName, Path.GetDirectoryName(target)));
            if (!EditorSceneManager.SaveScene(scene, target))
            {
                Debug.LogError($"MansionGenerator: no se pudo guardar {MansionGeneratorConstants.ScenePath}.");
                return;
            }

            AssetDatabase.SaveAssets();
            var counts = new List<string>();
            foreach (var level in LevelNames) counts.Add(level + " " + spaces.FindAll(space => space.Level == level).Count);
            Debug.Log($"Mansión generada: recintos por nivel [{string.Join(", ", counts)}], {wallCount} muros, {doorCount} puertas, {stepCount} escalones y {stairs.Count} escaleras. Giro +90°: {doorsSwingingPositive}, giro -90°: {doorsSwingingNegative}, sin resolver: {unresolvedDoorCount}.");
        }

        private static bool ValidateInput(List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs, List<MansionLayoutGate> gates, List<ExteriorRect> exterior, List<string> errors)
        {
            if (spaces.Count == 0) errors.Add("No se encontraron recintos G-*.");
            if (doors.Count == 0) errors.Add("No se encontraron puertas P-G* ni P-POR.");
            if (stairs.Count == 0) errors.Add("No se pudieron leer las escaleras.");
            if (gates.Count == 0) errors.Add("No se pudieron leer las compuertas.");
            if (!exterior.Exists(item => item.Id == "Porche") || !exterior.Exists(item => item.Id == "Camioneta"))
                errors.Add("Faltan los rectángulos de Porche o Camioneta.");

            var ids = new HashSet<string>();
            foreach (var space in spaces)
                if (!ids.Add(space.Id) || space.Level != "Planta baja" || space.Bounds.width <= 0f || space.Bounds.height <= 0f)
                    errors.Add($"Recinto de planta baja inválido o duplicado: {space.Id}.");
            foreach (var door in doors)
            {
                var owner = spaces.Find(space => space.Id == door.A);
                if (owner == null || door.Min >= door.Max || Mathf.Abs((door.Max - door.Min) - door.Width) > MansionGeneratorConstants.GridTolerance)
                    errors.Add($"Puerta inválida o sin espacio A en planta baja: {door.Id}.");
                if (door.B != "EXTERIOR" && !spaces.Exists(space => space.Id == door.B))
                    errors.Add($"Puerta {door.Id}: espacio B no encontrado en planta baja ({door.B}).");
            }
            var wallEdges = BuildWallEdges(spaces);
            foreach (var door in doors)
                if (!wallEdges.Exists(edge => DoorOnEdge(edge, door))) errors.Add($"Puerta {door.Id}: no coincide con un muro de planta baja.");
            foreach (var id in new[] { "P-G0", "P-G15", "P-G17", "P-G19", "P-POR" })
                if (!doors.Exists(door => door.Id == id && door.B == "EXTERIOR")) errors.Add($"Falta la puerta exterior {id}.");

            return errors.Count == 0;
        }

        private static bool ValidatePlanData(List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs, List<MansionLayoutGate> gates, List<ExteriorRect> exterior, List<string> errors)
        {
            if (spaces.Count < 28 || spaces.Count > 32) errors.Add($"El plano declara {spaces.Count} recintos; se esperaban 28–32.");
            if (doors.Count == 0 || stairs.Count != 4 || gates.Count != 4) errors.Add("No se pudieron leer todas las puertas, escaleras o compuertas.");
            foreach (var level in LevelNames)
                if (!spaces.Exists(space => space.Level == level)) errors.Add($"No hay recintos para el nivel {level}.");
            var ids = new HashSet<string>();
            foreach (var space in spaces)
                if (!ids.Add(space.Id) || !Array.Exists(LevelNames, level => level == space.Level) ||
                    space.Bounds.width <= 0f || space.Bounds.height <= 0f || space.Height <= 0f)
                    errors.Add($"Recinto inválido o duplicado: {space.Id}.");
            foreach (var level in LevelNames)
            {
                var levelSpaces = spaces.FindAll(space => space.Level == level);
                var levelDoors = doors.FindAll(door => IsLevel(door.A, spaces, level) &&
                    (door.B == "EXTERIOR" || IsLevel(door.B, spaces, level)));
                foreach (var door in levelDoors)
                    if (!BuildWallEdges(levelSpaces).Exists(edge => DoorOnEdge(edge, door)))
                        errors.Add($"Puerta {door.Id}: no coincide con un muro del nivel {level}.");
            }
            foreach (var door in doors)
                if (!ids.Contains(door.A) || (door.B != "EXTERIOR" && !ids.Contains(door.B)) || door.Min >= door.Max ||
                    Mathf.Abs(door.Max - door.Min - door.Width) > MansionGeneratorConstants.GridTolerance)
                    errors.Add($"Puerta mal formada: {door.Id}.");
            foreach (var id in new[] { "P-G0", "P-G15", "P-G17", "P-G19", "P-POR", "P-B7" })
                if (!doors.Exists(door => door.Id == id && door.B == "EXTERIOR")) errors.Add($"Falta la salida exterior {id}.");
            foreach (var stair in stairs)
            {
                if (stair.Width < MansionGeneratorConstants.StairWidth || stair.Risers != MansionGeneratorConstants.StairRiserCount ||
                    Mathf.Abs(stair.Tread - MansionGeneratorConstants.StairTreadDepth) > MansionGeneratorConstants.GridTolerance ||
                    stair.Headroom < MansionGeneratorConstants.MinimumHeadroom || Mathf.Max(stair.Opening.width, stair.Opening.height) < MansionGeneratorConstants.MinimumStairRun)
                    errors.Add($"Escalera {stair.Id}: hueco, peldaños o altura libre inválidos.");
            }
            if (!exterior.Exists(item => item.Id == "Porche") || !exterior.Exists(item => item.Id == "Camioneta"))
                errors.Add("Faltan el porche o la camioneta del plano.");
            return errors.Count == 0;
        }

        private static Dictionary<string, Transform> CreateLevelRoots()
        {
            var result = new Dictionary<string, Transform>();
            for (var index = 0; index < LevelNames.Length; index++)
            {
                var rootName = index == 0 ? "Nivel_Sotano" : index == 1 ? "Nivel_PlantaBaja" : index == 2 ? "Nivel_PrimerPiso" : "Nivel_Desvan";
                result.Add(LevelNames[index], new GameObject(rootName).transform);
            }
            return result;
        }

        private static bool IsLevel(string roomId, List<MansionLayoutSpace> spaces, string level)
        {
            var space = FindSpace(spaces, roomId);
            return space != null && space.Level == level;
        }

        private static MansionLayoutSpace FindSpace(List<MansionLayoutSpace> spaces, string id) => spaces.Find(space => space.Id == id);

        internal static float FloorY(string level)
        {
            if (level == "Sótano") return MansionGeneratorConstants.BasementY;
            if (level == "Planta baja") return MansionGeneratorConstants.GroundFloorY;
            if (level == "Primer piso") return MansionGeneratorConstants.FirstFloorY;
            return MansionGeneratorConstants.AtticY;
        }

        private static float CeilingHeight(string level) => level == "Desván" ? MansionGeneratorConstants.AtticHeight : MansionGeneratorConstants.ClearHeight;

        private static List<ExteriorRect> ReadExterior(string[] lines, List<string> errors)
        {
            var result = new List<ExteriorRect>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Exterior", StringComparison.Ordinal)) { reading = true; continue; }
                if (reading && line.StartsWith("## ", StringComparison.Ordinal)) break;
                if (!reading || !line.StartsWith("|", StringComparison.Ordinal)) continue;
                var cells = line.Split('|');
                if (cells.Length < 5 || cells[1].Trim() == "Elemento" || cells[1].Trim().StartsWith("---", StringComparison.Ordinal)) continue;
                if (!TryRectangle(cells[2], out var bounds))
                {
                    errors.Add($"Rectángulo exterior mal formado: {cells[1].Trim()}.");
                    continue;
                }
                result.Add(new ExteriorRect { Id = cells[1].Trim(), Bounds = bounds, Kind = cells[3].Trim() });
            }
            return result;
        }

        private static bool TryRectangle(string text, out Rect bounds)
        {
            bounds = default;
            var values = text.Trim().Trim('(', ')').Split(';');
            if (values.Length != 4 || !Number(values[0], out var x0) || !Number(values[1], out var z0) ||
                !Number(values[2], out var x1) || !Number(values[3], out var z1) || x1 <= x0 || z1 <= z0) return false;
            bounds = Rect.MinMaxRect(x0, z0, x1, z1);
            return true;
        }

        private static bool Number(string text, out float value) => float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static Materials CreateMaterials()
        {
            var shader = Shader.Find(LitShaderName);
            if (shader == null) return null;
            EnsureAssetFolder("Assets/Generated");
            EnsureAssetFolder(MansionGeneratorConstants.MaterialsFolder);
            return new Materials
            {
                Wall = LoadOrCreateMaterial(shader, "Mansion_Wall", new Color(0.34f, 0.32f, 0.29f)),
                Floor = LoadOrCreateMaterial(shader, "Mansion_Floor", new Color(0.22f, 0.20f, 0.18f)),
                Ceiling = LoadOrCreateMaterial(shader, "Mansion_Ceiling", new Color(0.38f, 0.37f, 0.35f)),
                Door = LoadOrCreateMaterial(shader, "Mansion_Door", new Color(0.20f, 0.14f, 0.10f)),
                Frame = LoadOrCreateMaterial(shader, "Mansion_Frame", new Color(0.30f, 0.24f, 0.18f)),
                Railing = LoadOrCreateMaterial(shader, "Mansion_Railing", new Color(0.24f, 0.19f, 0.14f)),
                Step = LoadOrCreateMaterial(shader, "Mansion_Step", new Color(0.31f, 0.29f, 0.26f)),
                Ground = LoadOrCreateMaterial(shader, "Mansion_Ground", new Color(0.16f, 0.18f, 0.14f)),
                Placeholder = LoadOrCreateMaterial(shader, "Mansion_VanPlaceholder", new Color(0.12f, 0.14f, 0.16f))
            };
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static Material LoadOrCreateMaterial(Shader shader, string name, Color color)
        {
            var path = MansionGeneratorConstants.MaterialsFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void BuildFloorsAndCeilings(List<MansionLayoutSpace> spaces, List<MansionLayoutStair> stairs,
            Dictionary<string, Transform> roots, Materials materials)
        {
            foreach (var space in spaces)
            {
                var floorOpenings = new List<Rect>();
                var ceilingOpenings = new List<Rect>();
                foreach (var stair in stairs)
                {
                if (stair.LevelB == space.Level && stair.LandingB == space.Id) floorOpenings.Add(stair.Opening);
                if (stair.LevelA == space.Level && stair.LandingA == space.Id) ceilingOpenings.Add(stair.Opening);
                }
            var levelIndex = Array.IndexOf(LevelNames, space.Level);
            if (levelIndex >= 0 && levelIndex + 1 < LevelNames.Length)
                foreach (var above in spaces.FindAll(item => item.Level == LevelNames[levelIndex + 1] && Overlaps(item.Bounds, space.Bounds)))
                    ceilingOpenings.Add(above.Bounds);
            var floorY = FloorY(space.Level);
            var ceilingHeight = CeilingHeight(space.Level);
            BuildSlab(space.Bounds, floorOpenings, floorY - MansionGeneratorConstants.FloorBuildUp,
                    MansionGeneratorConstants.FloorBuildUp, roots[space.Id], materials.Floor, "Floor");
            BuildSlab(space.Bounds, ceilingOpenings, floorY + ceilingHeight,
                    MansionGeneratorConstants.CeilingSlabThickness, roots[space.Id], materials.Ceiling, "Ceiling");
            }
        }

        private static void BuildSlab(Rect bounds, List<Rect> openings, float y, float thickness, Transform parent, Material material, string name)
        {
            var xCuts = new SortedSet<float> { bounds.xMin, bounds.xMax };
            var zCuts = new SortedSet<float> { bounds.yMin, bounds.yMax };
            foreach (var opening in openings)
            {
                if (!Overlaps(bounds, opening)) continue;
                xCuts.Add(Mathf.Clamp(opening.xMin, bounds.xMin, bounds.xMax));
                xCuts.Add(Mathf.Clamp(opening.xMax, bounds.xMin, bounds.xMax));
                zCuts.Add(Mathf.Clamp(opening.yMin, bounds.yMin, bounds.yMax));
                zCuts.Add(Mathf.Clamp(opening.yMax, bounds.yMin, bounds.yMax));
            }
            var xs = new List<float>(xCuts);
            var zs = new List<float>(zCuts);
            for (var xi = 0; xi < xs.Count - 1; xi++)
            for (var zi = 0; zi < zs.Count - 1; zi++)
            {
                var x0 = xs[xi]; var x1 = xs[xi + 1]; var z0 = zs[zi]; var z1 = zs[zi + 1];
                if (x1 - x0 <= MansionGeneratorConstants.GridTolerance || z1 - z0 <= MansionGeneratorConstants.GridTolerance) continue;
                var center = new Vector2((x0 + x1) * MansionGeneratorConstants.Half, (z0 + z1) * MansionGeneratorConstants.Half);
                if (openings.Exists(opening => Contains(opening, center))) continue;
                CreateBox(name, new Vector3(center.x, y + thickness * 0.5f, center.y), new Vector3(x1 - x0, thickness, z1 - z0), parent, material);
            }
        }

        private static List<WallEdge> BuildWallEdges(List<MansionLayoutSpace> spaces)
        {
            var result = new List<WallEdge>();
            foreach (var space in spaces)
            {
                AddEdge(result, spaces, space, 'X', space.Bounds.xMin, space.Bounds.yMin, space.Bounds.yMax);
                AddEdge(result, spaces, space, 'X', space.Bounds.xMax, space.Bounds.yMin, space.Bounds.yMax);
                AddEdge(result, spaces, space, 'Z', space.Bounds.yMin, space.Bounds.xMin, space.Bounds.xMax);
                AddEdge(result, spaces, space, 'Z', space.Bounds.yMax, space.Bounds.xMin, space.Bounds.xMax);
            }
            return result;
        }

        private static void AddEdge(List<WallEdge> edges, List<MansionLayoutSpace> spaces, MansionLayoutSpace space,
            char axis, float position, float min, float max)
        {
            var covered = new List<Vector2>();
            foreach (var other in spaces)
            {
                if (other == space) continue;
                var touches = axis == 'X'
                    ? Near(other.Bounds.xMin, position) || Near(other.Bounds.xMax, position)
                    : Near(other.Bounds.yMin, position) || Near(other.Bounds.yMax, position);
                if (!touches) continue;
                var sharedMin = axis == 'X' ? Mathf.Max(min, other.Bounds.yMin) : Mathf.Max(min, other.Bounds.xMin);
                var sharedMax = axis == 'X' ? Mathf.Min(max, other.Bounds.yMax) : Mathf.Min(max, other.Bounds.xMax);
                if (sharedMax - sharedMin <= MansionGeneratorConstants.GridTolerance) continue;
                covered.Add(new Vector2(sharedMin, sharedMax));
                if (string.CompareOrdinal(space.Id, other.Id) <= 0)
                    edges.Add(new WallEdge { Owner = space, Other = other, Axis = axis, Position = position, Min = sharedMin, Max = sharedMax });
            }
            covered.Sort((left, right) => left.x.CompareTo(right.x));
            var cursor = min;
            foreach (var span in covered)
            {
                if (span.x > cursor + MansionGeneratorConstants.GridTolerance)
                    edges.Add(new WallEdge { Owner = space, Axis = axis, Position = position, Min = cursor, Max = span.x, Exterior = true });
                cursor = Mathf.Max(cursor, span.y);
            }
            if (max > cursor + MansionGeneratorConstants.GridTolerance)
                edges.Add(new WallEdge { Owner = space, Axis = axis, Position = position, Min = cursor, Max = max, Exterior = true });
        }

        private static int BuildWallEdge(WallEdge edge, List<MansionLayoutDoor> doors, Dictionary<string, Transform> roots, Materials materials)
        {
            var matching = doors.FindAll(door => DoorOnEdge(edge, door));
            matching.Sort((left, right) => left.Min.CompareTo(right.Min));
            var cursor = edge.Min;
            var count = 0;
            foreach (var door in matching)
            {
                BuildWallSection(edge, cursor, door.Min, roots[edge.Owner.Id], materials.Wall,
                    FloorY(edge.Owner.Level), CeilingHeight(edge.Owner.Level));
                BuildHeader(edge, door, roots[edge.Owner.Id], materials.Wall,
                    FloorY(edge.Owner.Level), CeilingHeight(edge.Owner.Level));
                cursor = Mathf.Max(cursor, door.Max);
                count++;
            }
            BuildWallSection(edge, cursor, edge.Max, roots[edge.Owner.Id], materials.Wall,
                FloorY(edge.Owner.Level), CeilingHeight(edge.Owner.Level));
            return count + 1;
        }

        private static bool DoorOnEdge(WallEdge edge, MansionLayoutDoor door)
        {
            if (door.Axis != edge.Axis || !Near(door.Position, edge.Position) || door.Min < edge.Min || door.Max > edge.Max) return false;
            if (edge.Other == null) return door.B == "EXTERIOR";
            return (door.A == edge.Owner.Id && door.B == edge.Other.Id) || (door.B == edge.Owner.Id && door.A == edge.Other.Id);
        }

        private static void BuildWallSection(WallEdge edge, float min, float max, Transform parent, Material material, float floorY, float height)
        {
            if (max - min <= MansionGeneratorConstants.GridTolerance) return;
            var center = (min + max) * 0.5f;
            var thickness = MansionGeneratorConstants.WallThickness;
            var y = floorY + height * MansionGeneratorConstants.Half;
            Vector3 position;
            Vector3 size;
            if (edge.Axis == 'X')
            {
                var x = edge.Position;
                if (edge.Exterior) x += Near(edge.Position, edge.Owner.Bounds.xMin) ? -MansionGeneratorConstants.SharedWallHalfThickness : MansionGeneratorConstants.SharedWallHalfThickness;
                position = new Vector3(x, y, center);
                size = new Vector3(thickness, height, max - min);
            }
            else
            {
                var z = edge.Position;
                if (edge.Exterior) z += Near(edge.Position, edge.Owner.Bounds.yMin) ? -MansionGeneratorConstants.SharedWallHalfThickness : MansionGeneratorConstants.SharedWallHalfThickness;
                position = new Vector3(center, y, z);
                size = new Vector3(max - min, height, thickness);
            }
            CreateBox("Wall", position, size, parent, material);
        }

        private static void BuildHeader(WallEdge edge, MansionLayoutDoor door, Transform parent, Material material, float floorY, float height)
        {
            height -= MansionGeneratorConstants.DoorHeight;
            var center = (door.Min + door.Max) * MansionGeneratorConstants.Half;
            var thickness = MansionGeneratorConstants.WallThickness;
            Vector3 position;
            Vector3 size;
            if (edge.Axis == 'X')
            {
                var x = edge.Position;
                if (edge.Exterior) x += Near(edge.Position, edge.Owner.Bounds.xMin) ? -MansionGeneratorConstants.SharedWallHalfThickness : MansionGeneratorConstants.SharedWallHalfThickness;
                position = new Vector3(x, floorY + MansionGeneratorConstants.DoorHeight + height * MansionGeneratorConstants.Half, center);
                size = new Vector3(thickness, height, door.Width);
            }
            else
            {
                var z = edge.Position;
                if (edge.Exterior) z += Near(edge.Position, edge.Owner.Bounds.yMin) ? -MansionGeneratorConstants.SharedWallHalfThickness : MansionGeneratorConstants.SharedWallHalfThickness;
                position = new Vector3(center, floorY + MansionGeneratorConstants.DoorHeight + height * MansionGeneratorConstants.Half, z);
                size = new Vector3(door.Width, height, thickness);
            }
            CreateBox("DoorLintel", position, size, parent, material);
        }

        internal static bool TryResolveDoorSwing(MansionLayoutDoor door, List<MansionLayoutSpace> spaces,
            out bool hingeAtMax, out float swingSign, out MansionLayoutSpace inwardSpace)
        {
            return TryResolveDoorSwing(door, spaces, null, out hingeAtMax, out swingSign, out inwardSpace);
        }

        internal static bool TryResolveDoorSwing(MansionLayoutDoor door, List<MansionLayoutSpace> spaces,
            List<MansionLayoutStair> stairs, out bool hingeAtMax, out float swingSign, out MansionLayoutSpace inwardSpace)
        {
            if (!DoorSwingCache.TryGetValue(door.Id, out var cached))
            {
                cached = new DoorSwingResolution { SwingSign = 1f };
                cached.Success = TryResolveDoorSwingUncached(door, spaces, stairs, out cached.HingeAtMax,
                    out cached.SwingSign, out var resolvedSpace);
                cached.InwardSpaceId = resolvedSpace != null ? resolvedSpace.Id : null;
                DoorSwingCache[door.Id] = cached;
            }
            hingeAtMax = cached.HingeAtMax;
            swingSign = cached.SwingSign;
            inwardSpace = cached.InwardSpaceId == null ? null : FindSpace(spaces, cached.InwardSpaceId);
            return cached.Success;
        }

        internal static void ResetDoorSwingCache() => DoorSwingCache.Clear();

        private static bool TryResolveDoorSwingUncached(MansionLayoutDoor door, List<MansionLayoutSpace> spaces,
            List<MansionLayoutStair> stairs, out bool hingeAtMax, out float swingSign, out MansionLayoutSpace inwardSpace)
        {
            hingeAtMax = false;
            swingSign = 1f;
            inwardSpace = null;
            var roomA = FindSpace(spaces, door.A);
            var roomB = door.B == "EXTERIOR" ? null : FindSpace(spaces, door.B);
            if (roomA == null || (door.B != "EXTERIOR" && roomB == null))
            {
                Debug.LogWarning($"MansionGenerator: no se pudo resolver el barrido de la puerta {door.Id}; falta un recinto conectado.");
                return false;
            }
            if (roomB == null) inwardSpace = roomA;
            else
            {
                var roomAIsRoom = string.Equals(roomA.Kind, "habitación", StringComparison.OrdinalIgnoreCase);
                var roomBIsRoom = string.Equals(roomB.Kind, "habitación", StringComparison.OrdinalIgnoreCase);
                if (roomAIsRoom && roomBIsRoom)
                    inwardSpace = roomA.Bounds.width * roomA.Bounds.height <= roomB.Bounds.width * roomB.Bounds.height ? roomA : roomB;
                else if (roomAIsRoom) inwardSpace = roomA;
                else if (roomBIsRoom) inwardSpace = roomB;
                else
                    inwardSpace = roomA.Bounds.width * roomA.Bounds.height <= roomB.Bounds.width * roomB.Bounds.height ? roomA : roomB;
            }
            var inward = inwardSpace;

            var bestInwardDot = float.NegativeInfinity;
            var bestInwardHingeAtMax = false;
            var bestInwardSign = 1f;
            for (var stairPass = 0; stairPass < 2; stairPass++)
            {
                var bestFitDot = float.NegativeInfinity;
                var fitHingeAtMax = false;
                var fitSign = 1f;
                for (var hingeMax = 0; hingeMax <= 1; hingeMax++)
                {
                    var hingeAlong = hingeMax == 1 ? door.Max : door.Min;
                    var hinge = WallPoint(door.Axis, door.Position, hingeAlong, 0f);
                    var interior = inward.Bounds.center;
                    var target = new Vector3(interior.x - hinge.x, 0f, interior.y - hinge.z);
                    if (target.sqrMagnitude <= MansionGeneratorConstants.GridTolerance) continue;
                    target.Normalize();
                    var closedLeaf = door.Axis == 'X'
                        ? new Vector3(0f, 0f, hingeMax == 1 ? -door.Width : door.Width)
                        : new Vector3(hingeMax == 1 ? -door.Width : door.Width, 0f, 0f);
                    foreach (var sign in new[] { -1f, 1f })
                    {
                        var openedLeaf = Quaternion.Euler(0f, sign * MansionGeneratorConstants.DoorOpenAngle, 0f) * closedLeaf;
                        var dot = Vector3.Dot(openedLeaf.normalized, target);
                        if (dot > bestInwardDot)
                        {
                            bestInwardDot = dot;
                            bestInwardHingeAtMax = hingeMax == 1;
                            bestInwardSign = sign;
                        }

                        var sweep = CalculateDoorSwingBounds(door, hingeMax == 1, sign);
                        if (sweep.xMin < inward.Bounds.xMin - MansionGeneratorConstants.GridTolerance ||
                            sweep.xMax > inward.Bounds.xMax + MansionGeneratorConstants.GridTolerance ||
                            sweep.yMin < inward.Bounds.yMin - MansionGeneratorConstants.GridTolerance ||
                            sweep.yMax > inward.Bounds.yMax + MansionGeneratorConstants.GridTolerance) continue;
                        if (stairPass == 0 && stairs != null && stairs.Exists(stair =>
                            (stair.LandingA == inward.Id || stair.LandingB == inward.Id) && Overlaps(sweep, stair.Opening))) continue;
                        if (dot <= bestFitDot) continue;
                        bestFitDot = dot;
                        fitHingeAtMax = hingeMax == 1;
                        fitSign = sign;
                    }
                }
                if (bestFitDot > 0f)
                {
                    hingeAtMax = fitHingeAtMax;
                    swingSign = fitSign;
                    return true;
                }
            }

            if (bestInwardDot > 0f)
            {
                hingeAtMax = bestInwardHingeAtMax;
                swingSign = bestInwardSign;
                return true;
            }

            Debug.LogWarning($"MansionGenerator: no se pudo resolver hacia adentro el barrido de la puerta {door.Id}.");
            return false;
        }

        internal static Rect? DoorSwingBounds(MansionLayoutDoor door, List<MansionLayoutSpace> spaces, float clearance)
        {
            if (!TryResolveDoorSwing(door, spaces, out var hingeAtMax, out var swingSign, out _)) return null;
            var bounds = CalculateDoorSwingBounds(door, hingeAtMax, swingSign);
            bounds.xMin -= clearance;
            bounds.xMax += clearance;
            bounds.yMin -= clearance;
            bounds.yMax += clearance;
            return bounds;
        }

        private static Rect CalculateDoorSwingBounds(MansionLayoutDoor door, bool hingeAtMax, float swingSign)
        {
            var hingeAlong = hingeAtMax ? door.Max : door.Min;
            var hinge = WallPoint(door.Axis, door.Position, hingeAlong, 0f);
            var closedLeaf = door.Axis == 'X'
                ? new Vector3(0f, 0f, hingeAtMax ? -door.Width : door.Width)
                : new Vector3(hingeAtMax ? -door.Width : door.Width, 0f, 0f);
            var minX = hinge.x;
            var maxX = hinge.x;
            var minZ = hinge.z;
            var maxZ = hinge.z;
            for (var degrees = 0; degrees <= MansionGeneratorConstants.DoorOpenAngle; degrees += 5)
            {
                var tip = hinge + Quaternion.Euler(0f, swingSign * degrees, 0f) * closedLeaf;
                minX = Mathf.Min(minX, tip.x); maxX = Mathf.Max(maxX, tip.x);
                minZ = Mathf.Min(minZ, tip.z); maxZ = Mathf.Max(maxZ, tip.z);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private static void BuildDoor(MansionLayoutDoor door, Transform parent, Materials materials, float floorY,
            bool hingeAtMax, float swingSign, bool canInteract = true)
        {
            var root = new GameObject(door.Id + " [" + door.KeyId + "]").transform;
            root.SetParent(parent, false);
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root, false);
            var edge = hingeAtMax ? door.Max : door.Min;
            hinge.position = WallPoint(door.Axis, door.Position, edge, floorY);
            var leaf = CreateBox("DoorLeaf", Vector3.zero,
                door.Axis == 'X'
                    ? new Vector3(MansionGeneratorConstants.DoorLeafThickness, MansionGeneratorConstants.DoorHeight, door.Width)
                    : new Vector3(door.Width, MansionGeneratorConstants.DoorHeight, MansionGeneratorConstants.DoorLeafThickness),
                hinge, materials.Door).transform;
            leaf.localPosition = door.Axis == 'X'
                ? new Vector3(0f, MansionGeneratorConstants.DoorHeight * 0.5f, hingeAtMax ? -door.Width * 0.5f : door.Width * 0.5f)
                : new Vector3(hingeAtMax ? -door.Width * 0.5f : door.Width * 0.5f, MansionGeneratorConstants.DoorHeight * 0.5f, 0f);

            BuildDoorFrame(door, parent, materials.Frame, floorY);
            if (!canInteract) return;
            var interactable = root.gameObject.AddComponent<DoorInteractable>();
            var serialized = new SerializedObject(interactable);
            serialized.FindProperty("hinge").objectReferenceValue = hinge;
            var config = AssetDatabase.LoadAssetAtPath<UltimaCinta.Data.GameConfig>("Assets/Scripts/Data/GameConfig.asset");
            serialized.FindProperty("gameConfig").objectReferenceValue = config;
            serialized.FindProperty("keyId").stringValue = door.KeyId;
            serialized.FindProperty("swingSign").floatValue = swingSign;
            serialized.FindProperty("openAngle").floatValue = MansionGeneratorConstants.DoorOpenAngle;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildDoorFrame(MansionLayoutDoor door, Transform parent, Material material, float floorY)
        {
            var jambHeight = MansionGeneratorConstants.DoorHeight;
            var center = (door.Min + door.Max) * MansionGeneratorConstants.Half;
            var depth = MansionGeneratorConstants.WallThickness + MansionGeneratorConstants.DoorFrameDepth;
            foreach (var end in new[] { door.Min, door.Max })
            {
                var point = WallPoint(door.Axis, door.Position, end, floorY + jambHeight * MansionGeneratorConstants.Half);
                var size = door.Axis == 'X' ? new Vector3(depth, jambHeight, MansionGeneratorConstants.DoorFrameWidth) :
                    new Vector3(MansionGeneratorConstants.DoorFrameWidth, jambHeight, depth);
                CreateBox("DoorJamb", point, size, parent, material);
            }
            var headerCenter = WallPoint(door.Axis, door.Position, center, floorY + MansionGeneratorConstants.DoorHeight + MansionGeneratorConstants.DoorFrameWidth * MansionGeneratorConstants.Half);
            var headerSize = door.Axis == 'X'
                ? new Vector3(depth, MansionGeneratorConstants.DoorFrameWidth, door.Width)
                : new Vector3(door.Width, MansionGeneratorConstants.DoorFrameWidth, depth);
            CreateBox("DoorFrameHeader", headerCenter, headerSize, parent, material);
        }

        private static Vector3 WallPoint(char axis, float position, float along, float y) =>
            axis == 'X' ? new Vector3(position, y, along) : new Vector3(along, y, position);

        private static int BuildSteps(MansionLayoutDoor door, Transform parent, List<MansionLayoutSpace> spaces, Materials materials)
        {
            var room = spaces.Find(space => space.Id == door.A);
            if (room == null) return 0;
            var exteriorSign = ExteriorDirection(door, room);
            var center = (door.Min + door.Max) * MansionGeneratorConstants.Half;
            for (var index = 0; index < MansionGeneratorConstants.StepCount; index++)
            {
                var stepHeight = MansionGeneratorConstants.StepRise * (MansionGeneratorConstants.StepCount - index);
                var depth = MansionGeneratorConstants.StepTread;
                var offset = (index + MansionGeneratorConstants.Half) * MansionGeneratorConstants.StepTread;
                var position = door.Axis == 'X'
                    ? new Vector3(door.Position + exteriorSign * offset, MansionGeneratorConstants.ExteriorGroundY + stepHeight * MansionGeneratorConstants.Half, center)
                    : new Vector3(center, MansionGeneratorConstants.ExteriorGroundY + stepHeight * MansionGeneratorConstants.Half, door.Position + exteriorSign * offset);
                var size = door.Axis == 'X' ? new Vector3(depth, stepHeight, door.Width) : new Vector3(door.Width, stepHeight, depth);
                CreateBox("ExteriorStep_" + (index + 1), position, size, parent, materials.Step);
            }
            return MansionGeneratorConstants.StepCount;
        }

        private static float ExteriorDirection(MansionLayoutDoor door, MansionLayoutSpace room)
        {
            if (door.Axis == 'X') return Near(door.Position, room.Bounds.xMin) ? -1f : 1f;
            return Near(door.Position, room.Bounds.yMin) ? -1f : 1f;
        }

        private static void BuildExterior(List<ExteriorRect> exterior, List<MansionLayoutSpace> spaces, Materials materials)
        {
            var bounds = exterior.Find(item => item.Id == "Porche").Bounds;
            var porchRoot = new GameObject("Exterior_Porche").transform;
            BuildSlab(bounds, new List<Rect>(), MansionGeneratorConstants.ExteriorGroundY - MansionGeneratorConstants.FloorBuildUp, MansionGeneratorConstants.FloorBuildUp,
                porchRoot, materials.Step, "PorchFloor");
            foreach (var item in exterior)
            {
                if (item.Id == "Porche") continue;
                if (item.Id == "Camioneta")
                {
                    var center = new Vector3(item.Bounds.center.x, MansionGeneratorConstants.ExteriorGroundY + MansionGeneratorConstants.ClearHeight * MansionGeneratorConstants.Half, item.Bounds.center.y);
                    CreateBox("Camioneta_Placeholder", center, new Vector3(item.Bounds.width, MansionGeneratorConstants.ClearHeight, item.Bounds.height), porchRoot, materials.Placeholder);
                }
            }
            var minX = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;
            foreach (var space in spaces)
            {
                minX = Mathf.Min(minX, space.Bounds.xMin); minZ = Mathf.Min(minZ, space.Bounds.yMin);
                maxX = Mathf.Max(maxX, space.Bounds.xMax); maxZ = Mathf.Max(maxZ, space.Bounds.yMax);
            }
            foreach (var item in exterior)
            {
                minX = Mathf.Min(minX, item.Bounds.xMin); minZ = Mathf.Min(minZ, item.Bounds.yMin);
                maxX = Mathf.Max(maxX, item.Bounds.xMax); maxZ = Mathf.Max(maxZ, item.Bounds.yMax);
            }
            var footprint = Rect.MinMaxRect(minX - MansionGeneratorConstants.TerrainMargin, minZ - MansionGeneratorConstants.TerrainMargin,
                maxX + MansionGeneratorConstants.TerrainMargin, maxZ + MansionGeneratorConstants.TerrainMargin);
            var terrainOpenings = new List<Rect> { bounds };
            foreach (var space in spaces) terrainOpenings.Add(space.Bounds);
            BuildSlab(footprint, terrainOpenings, MansionGeneratorConstants.ExteriorGroundY - MansionGeneratorConstants.FloorBuildUp,
                MansionGeneratorConstants.FloorBuildUp, porchRoot, materials.Ground, "ExteriorGround");
        }

        private static void BuildStairs(List<MansionLayoutStair> stairs, List<MansionLayoutSpace> spaces,
            Dictionary<string, Transform> roots, Materials materials)
        {
            foreach (var stair in stairs)
            {
                var parent = roots[stair.LandingA];
                var stairRoot = new GameObject("Escalera_" + stair.Id).transform;
                stairRoot.SetParent(parent, false);
                var lowerY = FloorY(stair.LevelA);
                var upperY = FloorY(stair.LevelB);
                var risesPositive = stair.Direction.IndexOf("sur", StringComparison.OrdinalIgnoreCase) < 0 &&
                                    stair.Direction.IndexOf("oeste", StringComparison.OrdinalIgnoreCase) < 0;
                var alongZ = stair.Opening.height >= stair.Opening.width;
                var direction = risesPositive ? 1f : -1f;
                var startAlong = alongZ ? (risesPositive ? stair.Opening.yMin : stair.Opening.yMax) :
                    (risesPositive ? stair.Opening.xMin : stair.Opening.xMax);
                var endAlong = alongZ ? (risesPositive ? stair.Opening.yMax : stair.Opening.yMin) :
                    (risesPositive ? stair.Opening.xMax : stair.Opening.xMin);
                var run = Mathf.Abs(endAlong - startAlong);
                var rise = (upperY - lowerY) / MansionGeneratorConstants.StairRiserCount;
                var centerAcross = alongZ ? stair.Opening.center.x : stair.Opening.center.y;
                var rampCenter = alongZ
                    ? new Vector3(centerAcross, (lowerY + upperY) * MansionGeneratorConstants.Half, stair.Opening.center.y)
                    : new Vector3(stair.Opening.center.x, (lowerY + upperY) * MansionGeneratorConstants.Half, centerAcross);
                var ramp = CreateBox("Rampa_Colisionable", rampCenter,
                    alongZ ? new Vector3(MansionGeneratorConstants.StairWidth, MansionGeneratorConstants.StairRampThickness, run) :
                        new Vector3(run, MansionGeneratorConstants.StairRampThickness, MansionGeneratorConstants.StairWidth),
                    stairRoot, materials.Step);
                var slopeAngle = Mathf.Atan2(upperY - lowerY, run) * Mathf.Rad2Deg;
                ramp.transform.rotation = alongZ
                    ? Quaternion.Euler(risesPositive ? -slopeAngle : slopeAngle, 0f, 0f)
                    : Quaternion.Euler(0f, 0f, risesPositive ? slopeAngle : -slopeAngle);

                for (var index = 0; index < MansionGeneratorConstants.StairTreadCount; index++)
                {
                    var along = startAlong + direction * (index + MansionGeneratorConstants.Half) * MansionGeneratorConstants.StairTreadDepth;
                    var treadTop = lowerY + rise * (index + 1);
                    var position = alongZ ? new Vector3(centerAcross, treadTop - MansionGeneratorConstants.StairTreadThickness * MansionGeneratorConstants.Half, along) :
                        new Vector3(along, treadTop - MansionGeneratorConstants.StairTreadThickness * MansionGeneratorConstants.Half, centerAcross);
                    var tread = CreateBox("Huella_" + (index + 1), position,
                        alongZ ? new Vector3(MansionGeneratorConstants.StairWidth, MansionGeneratorConstants.StairTreadThickness, MansionGeneratorConstants.StairTreadDepth) :
                            new Vector3(MansionGeneratorConstants.StairTreadDepth, MansionGeneratorConstants.StairTreadThickness, MansionGeneratorConstants.StairWidth),
                        stairRoot, materials.Railing);
                    UnityEngine.Object.DestroyImmediate(tread.GetComponent<BoxCollider>());
                }
                for (var index = 0; index < MansionGeneratorConstants.StairRiserCount; index++)
                {
                    var along = startAlong + direction * index * MansionGeneratorConstants.StairTreadDepth;
                    var riserCenterY = lowerY + rise * (index + MansionGeneratorConstants.Half);
                    var position = alongZ ? new Vector3(centerAcross, riserCenterY, along) : new Vector3(along, riserCenterY, centerAcross);
                    var size = alongZ
                        ? new Vector3(MansionGeneratorConstants.StairWidth, rise, MansionGeneratorConstants.StairTreadThickness)
                        : new Vector3(MansionGeneratorConstants.StairTreadThickness, rise, MansionGeneratorConstants.StairWidth);
                    var riser = CreateBox("Contrahuella_" + (index + 1), position, size, stairRoot, materials.Railing);
                    UnityEngine.Object.DestroyImmediate(riser.GetComponent<BoxCollider>());
                }
                BuildStairSideRails(stair.Opening, alongZ, risesPositive, lowerY, upperY, stairRoot, materials.Railing);
            }
        }

        private static void BuildStairSideRails(Rect opening, bool alongZ, bool risesPositive, float lowerY, float upperY,
            Transform parent, Material material)
        {
            var lowAlong = alongZ ? (risesPositive ? opening.yMin : opening.yMax) : (risesPositive ? opening.xMin : opening.xMax);
            var highAlong = alongZ ? (risesPositive ? opening.yMax : opening.yMin) : (risesPositive ? opening.xMax : opening.xMin);
            var acrossMin = alongZ ? opening.xMin : opening.yMin;
            var acrossMax = alongZ ? opening.xMax : opening.yMax;
            var length = Mathf.Sqrt((highAlong - lowAlong) * (highAlong - lowAlong) + (upperY - lowerY) * (upperY - lowerY));
            foreach (var side in new[] { acrossMin, acrossMax })
            {
                var low = alongZ ? new Vector3(side, lowerY + MansionGeneratorConstants.StairRailHeight, lowAlong) :
                    new Vector3(lowAlong, lowerY + MansionGeneratorConstants.StairRailHeight, side);
                var high = alongZ ? new Vector3(side, upperY + MansionGeneratorConstants.StairRailHeight, highAlong) :
                    new Vector3(highAlong, upperY + MansionGeneratorConstants.StairRailHeight, side);
                var beam = CreateBox("Baranda_Escalera", (low + high) * MansionGeneratorConstants.Half,
                    new Vector3(MansionGeneratorConstants.StairRailThickness, MansionGeneratorConstants.StairRailThickness, length), parent, material);
                beam.transform.rotation = Quaternion.FromToRotation(Vector3.forward, (high - low).normalized);
            }
        }

        private static void BuildStairGates(List<MansionLayoutGate> gates, List<MansionLayoutStair> stairs,
            List<MansionLayoutSpace> spaces, Dictionary<string, Transform> roots, Materials materials, ref int doorCount)
        {
            foreach (var gate in gates)
            {
                var stair = stairs.Find(item => item.Id == gate.StairId);
                if (stair == null || !roots.TryGetValue(stair.LandingB, out var parent)) continue;
                var alongZ = stair.Opening.height >= stair.Opening.width;
                var positive = stair.Direction.IndexOf("sur", StringComparison.OrdinalIgnoreCase) < 0 &&
                               stair.Direction.IndexOf("oeste", StringComparison.OrdinalIgnoreCase) < 0;
                var axis = alongZ ? 'Z' : 'X';
                var position = alongZ ? (positive ? stair.Opening.yMax : stair.Opening.yMin) :
                    (positive ? stair.Opening.xMax : stair.Opening.xMin);
                var lo = alongZ ? stair.Opening.xMin : stair.Opening.yMin;
                var hi = alongZ ? stair.Opening.xMax : stair.Opening.yMax;
                var door = new MansionLayoutDoor
                {
                    Id = gate.Id, A = stair.LandingB, B = stair.LandingA, Axis = axis,
                    Position = position, Min = lo, Max = hi, Width = hi - lo, KeyId = gate.KeyId
                };
                if (TryResolveDoorSwing(door, spaces, stairs, out var hingeAtMax, out var swingSign, out _))
                    BuildDoor(door, parent, materials, FloorY(stair.LevelB), hingeAtMax, swingSign);
                else
                {
                    BuildDoor(door, parent, materials, FloorY(stair.LevelB), false, 1f, false);
                }
                doorCount++;
            }
        }

        private static void BuildBasementBackfill(List<MansionLayoutSpace> spaces, List<MansionLayoutSpace> groundSpaces, Materials materials)
        {
            var parent = new GameObject("Exterior_Terraplen_Sotano").transform;
            foreach (var basement in spaces.FindAll(space => space.Level == "Sótano"))
                BuildSlab(basement.Bounds, groundSpaces.ConvertAll(space => space.Bounds), MansionGeneratorConstants.ExteriorGroundY,
                    MansionGeneratorConstants.GroundFillThickness, parent, materials.Ground, "Terraplen");
        }

        private static void BuildBasementExit(List<MansionLayoutDoor> doors, List<MansionLayoutSpace> spaces,
            Dictionary<string, Transform> roots, Materials materials, ref int stepCount)
        {
            var exit = doors.Find(door => door.Id == "P-B7");
            var room = exit == null ? null : FindSpace(spaces, exit.A);
            if (exit == null || room == null) return;
            var parent = new GameObject("Exterior_Escalera_Sotano").transform;
            var center = (exit.Min + exit.Max) * MansionGeneratorConstants.Half;
            var sign = exit.Axis == 'X' ? (Near(exit.Position, room.Bounds.xMin) ? -1f : 1f) :
                (Near(exit.Position, room.Bounds.yMin) ? -1f : 1f);
            var run = MansionGeneratorConstants.StairTreadDepth * MansionGeneratorConstants.StairTreadCount;
            var lowerY = FloorY(room.Level);
            var rise = (MansionGeneratorConstants.ExteriorGroundY - lowerY) / MansionGeneratorConstants.BasementExitRiserCount;
            var alongZ = exit.Axis == 'Z';
            for (var index = 0; index < MansionGeneratorConstants.BasementExitRiserCount; index++)
            {
                var top = lowerY + rise * (index + 1);
                var along = exit.Position + sign * (index + MansionGeneratorConstants.Half) * MansionGeneratorConstants.StairTreadDepth;
                var pos = alongZ ? new Vector3(center, top - MansionGeneratorConstants.StairTreadThickness * MansionGeneratorConstants.Half, along) :
                    new Vector3(along, top - MansionGeneratorConstants.StairTreadThickness * MansionGeneratorConstants.Half, center);
                var size = alongZ ? new Vector3(MansionGeneratorConstants.StairWidth, MansionGeneratorConstants.StairTreadThickness, MansionGeneratorConstants.StairTreadDepth) :
                    new Vector3(MansionGeneratorConstants.StairTreadDepth, MansionGeneratorConstants.StairTreadThickness, MansionGeneratorConstants.StairWidth);
                CreateBox("Escalon_P-B7_" + (index + 1), pos, size, parent, materials.Step);
            }
            var shaftHeight = MansionGeneratorConstants.ExteriorGroundY - lowerY;
            var shaftCenter = alongZ ? new Vector3(center, lowerY + shaftHeight * MansionGeneratorConstants.Half, exit.Position + sign * run * MansionGeneratorConstants.Half) :
                new Vector3(exit.Position + sign * run * MansionGeneratorConstants.Half, lowerY + shaftHeight * MansionGeneratorConstants.Half, center);
            foreach (var side in new[] { -1f, 1f })
            {
                var wallPosition = shaftCenter;
                if (alongZ) wallPosition.x += side * (MansionGeneratorConstants.StairWidth * MansionGeneratorConstants.Half + MansionGeneratorConstants.BasementShaftWallThickness * MansionGeneratorConstants.Half);
                else wallPosition.z += side * (MansionGeneratorConstants.StairWidth * MansionGeneratorConstants.Half + MansionGeneratorConstants.BasementShaftWallThickness * MansionGeneratorConstants.Half);
                var wallSize = alongZ ? new Vector3(MansionGeneratorConstants.BasementShaftWallThickness, shaftHeight, run) :
                    new Vector3(run, shaftHeight, MansionGeneratorConstants.BasementShaftWallThickness);
                CreateBox("PozoProtegido_P-B7", wallPosition, wallSize, parent, materials.Wall);
            }
            var ramp = CreateBox("RampaColisionable_P-B7", shaftCenter,
                alongZ ? new Vector3(MansionGeneratorConstants.StairWidth, MansionGeneratorConstants.StairRampThickness, run) :
                    new Vector3(run, MansionGeneratorConstants.StairRampThickness, MansionGeneratorConstants.StairWidth), parent, materials.Step);
            var angle = Mathf.Atan2(shaftHeight, run) * Mathf.Rad2Deg;
            ramp.transform.rotation = alongZ ? Quaternion.Euler(sign > 0f ? -angle : angle, 0f, 0f) :
                Quaternion.Euler(0f, 0f, sign > 0f ? angle : -angle);
            stepCount += MansionGeneratorConstants.BasementExitRiserCount;
        }

        private static void AddPlayerAndLighting(Scene scene, Dictionary<string, Transform> roots,
            Dictionary<string, Transform> levelRoots, string projectRoot, List<MansionLayoutSpace> spaces)
        {
            var lightObject = new GameObject("Directional Light");
            var directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = MansionGeneratorConstants.DimDirectionalIntensity;
            lightObject.transform.rotation = Quaternion.Euler(MansionGeneratorConstants.DirectionalPitch, MansionGeneratorConstants.DirectionalYaw, 0f);

            foreach (var space in spaces)
            {
                if (!roots.TryGetValue(space.Id, out var roomRoot)) continue;
                var roomLightObject = new GameObject("LuzHabitacion");
                roomLightObject.transform.SetParent(roomRoot, false);
                roomLightObject.transform.position = new Vector3(space.Bounds.center.x,
                    FloorY(space.Level) + MansionGeneratorConstants.RoomLightHeight, space.Bounds.center.y);
                var roomLight = roomLightObject.AddComponent<Light>();
                roomLight.type = LightType.Point;
                roomLight.range = MansionGeneratorConstants.RoomLightRange;
                roomLight.intensity = MansionGeneratorConstants.RoomLightIntensity;
                roomLight.color = new Color(1f, 0.82f, 0.64f);
                roomLightObject.AddComponent<UltimaCinta.Lighting.LightFlicker>();
            }

            var prefabPath = string.Empty;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                { prefabPath = path; break; }
            }
            GameObject player = null;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (player != null && roots.TryGetValue("G-REC", out var room)) player.transform.SetParent(room, false);
            }
            if (player == null)
            {
                player = new GameObject("Player");
                if (roots.TryGetValue("G-REC", out var room)) player.transform.SetParent(room, false);
                var controller = player.AddComponent<CharacterController>();
                controller.height = MansionGeneratorConstants.FallbackCharacterHeight;
                controller.radius = MansionGeneratorConstants.FallbackCharacterRadius;
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, MansionGeneratorConstants.FallbackCameraHeight, 0f);
                cameraObject.AddComponent<Camera>();
                Debug.LogWarning("MansionGenerator: no hay prefab de Player; se creó Player con CharacterController y cámara básica.");
            }
            player.name = "Player";
            player.transform.localPosition = new Vector3(0f, MansionGeneratorConstants.PlayerSpawnY, 0f);
            var mainCamera = player.GetComponentInChildren<Camera>(true);
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, MansionGeneratorConstants.FallbackCameraHeight, 0f);
                mainCamera = cameraObject.AddComponent<Camera>();
            }
            mainCamera.tag = "MainCamera";
        }

        private static void BuildPointOfInterestMarkers(List<MansionLayoutSpace> spaces, Dictionary<string, Transform> roots)
        {
            foreach (var space in spaces)
            {
                if (!roots.TryGetValue(space.Id, out var roomRoot)) continue;
                var position = new Vector3(space.Bounds.center.x,
                    FloorY(space.Level) + MansionGeneratorConstants.PointOfInterestHeight, space.Bounds.center.y);
                CreatePointOfInterest("POI_SalaFavorita_Candidata_" + space.Id, position, roomRoot);
                CreatePointOfInterest("POI_ObjetoRitual_" + space.Id, position, roomRoot);
                CreatePointOfInterest("POI_Evidencia_" + space.Id, position, roomRoot);
                if (space.Id == "B-RIT") CreatePointOfInterest("POI_Ritual_Centro", position, roomRoot);
            }
        }

        private static void CreatePointOfInterest(string markerName, Vector3 position, Transform parent)
        {
            var marker = new GameObject(markerName).transform;
            marker.SetParent(parent, false);
            marker.position = position;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Transform parent, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            var renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return box;
        }

        private static bool Overlaps(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > MansionGeneratorConstants.GridTolerance &&
            Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > MansionGeneratorConstants.GridTolerance;

        private static bool Contains(Rect rect, Vector2 point) => point.x > rect.xMin && point.x < rect.xMax && point.y > rect.yMin && point.y < rect.yMax;
        private static bool Near(float a, float b) => Mathf.Abs(a - b) <= MansionGeneratorConstants.GridTolerance;
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UltimaCinta.Editor
{
    public static class MansionLayoutPlanValidator
    {
        private const float Tolerance = 0.001f;

        [MenuItem("Tools/UltimaCinta/Validar plano de mansión")]
        public static void ValidatePlan()
        {
            var root = Directory.GetParent(Application.dataPath);
            var path = Path.Combine(root.FullName, "Docs/PlanoMansion.md");
            if (!File.Exists(path))
            {
                Debug.LogError("MansionLayoutPlanValidator: no existe Docs/PlanoMansion.md.");
                return;
            }

            var lines = File.ReadAllLines(path);
            var errors = new List<string>();
            var checks = new List<string>();
            var spaces = MansionLayoutPlanReader.ReadSpaces(lines, errors);
            var doors = MansionLayoutPlanReader.ReadDoors(lines, errors);
            var stairs = MansionLayoutPlanReader.ReadStairs(lines, errors);
            var gates = MansionLayoutPlanReader.ReadGates(lines, errors);
            var report = new StringBuilder();

            report.AppendLine("MansionLayoutPlanValidator");
            report.AppendLine("Fuente: Docs/PlanoMansion.md");
            report.AppendLine($"Leídos: {spaces.Count} rectángulos, {doors.Count} puertas, {stairs.Count} escaleras y {gates.Count} compuertas.");
            if (errors.Count == 0)
            {
                CheckOverlaps(spaces, errors, checks);
                CheckRoomCountAndDimensions(spaces, errors, checks);
                CheckFootprints(lines, spaces, errors, checks);
                CheckOverheadClearance(spaces, errors, checks);
                CheckDoors(spaces, doors, stairs, errors, checks);
                CheckStairs(spaces, stairs, errors, checks);
                CheckDoorsOutsideStairOpenings(spaces, doors, stairs, errors, checks);
                CheckVerticalSupport(spaces, errors, checks);
                CheckGates(gates, errors, checks);
                CheckReachability(spaces, doors, stairs, gates, errors, checks);
                CheckHideoutsAndExterior(lines, spaces, doors, errors, checks);
            }

            foreach (var check in checks) report.AppendLine(check);
            foreach (var error in errors) report.AppendLine("[ERROR] " + error);
            report.AppendLine(errors.Count == 0 ? "RESULTADO: OK" : $"RESULTADO: ERROR ({errors.Count})");
            if (errors.Count == 0) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        private static void CheckRoomCountAndDimensions(List<MansionLayoutSpace> spaces, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            if (spaces.Count < 28 || spaces.Count > 32) errors.Add($"Hay {spaces.Count} recintos; se requieren 28–32.");
            foreach (var s in spaces)
            {
                var shortSide = Mathf.Min(s.Bounds.width, s.Bounds.height);
                if (s.Kind == "habitación" && (shortSide < 4f - Tolerance || Mathf.Max(s.Bounds.width, s.Bounds.height) < 5f - Tolerance))
                    errors.Add($"{s.Id}: habitación ordinaria menor de 4 × 5 m.");
                if (s.Kind == "pasillo")
                {
                    var expected = s.Id == "G-SER" ? 1.2f : -1f;
                    if (expected > 0 ? Mathf.Abs(shortSide - expected) > Tolerance : shortSide < 1.5f - Tolerance || shortSide > 1.8f + Tolerance)
                        errors.Add($"{s.Id}: ancho de pasillo fuera del requisito.");
                }
            }
            if (errors.Count == initial) checks.Add("[OK] Cantidad y dimensiones mínimas de recintos/pasillos.");
        }

        private static void CheckFootprints(string[] lines, List<MansionLayoutSpace> spaces, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            var footprints = new Dictionary<string, Rect>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Huellas de planta", System.StringComparison.Ordinal)) { reading = true; continue; }
                if (reading && line.StartsWith("## ", System.StringComparison.Ordinal)) break;
                if (!reading) continue;
                var cells = line.Split('|');
                if (cells.Length < 4 || cells[1].Trim() == "Nivel") continue;
                var values = cells[2].Trim().Trim('(', ')').Split(';');
                if (values.Length == 4 && float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x0) &&
                    float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z0) &&
                    float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x1) &&
                    float.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z1))
                    footprints[cells[1].Trim()] = Rect.MinMaxRect(x0, z0, x1, z1);
            }
            foreach (var s in spaces)
                if (!footprints.TryGetValue(s.Level, out var fp) || !Contains(fp, s.Bounds)) errors.Add($"{s.Id}: fuera de huella {s.Level}.");
            foreach (var s in spaces.FindAll(item => item.Level == "Sótano"))
                if (footprints.TryGetValue("Planta baja", out var fpBasement) && !Contains(fpBasement, s.Bounds)) errors.Add($"{s.Id}: sótano fuera de huella de planta baja.");
            foreach (var s in spaces.FindAll(item => item.Level == "Primer piso"))
                if (footprints.TryGetValue("Planta baja", out var fpFirst) && !Contains(fpFirst, s.Bounds)) errors.Add($"{s.Id}: primer piso fuera de huella de planta baja.");
            foreach (var s in spaces.FindAll(item => item.Level == "Desván"))
                if (footprints.TryGetValue("Primer piso", out var fpAttic) && !Contains(fpAttic, s.Bounds)) errors.Add($"{s.Id}: desván fuera de huella del primer piso.");
            if (errors.Count == initial) checks.Add("[OK] Recintos dentro de las huellas declaradas.");
        }

        private static void CheckHideoutsAndExterior(string[] lines, List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors,
            List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            var counts = new Dictionary<string, int>(); var total = 0; var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Escondites", System.StringComparison.Ordinal)) { reading = true; continue; }
                if (reading && line.StartsWith("## ", System.StringComparison.Ordinal)) break;
                if (!reading || !line.StartsWith("|", System.StringComparison.Ordinal)) continue;
                var c = line.Split('|');
                if (c.Length < 5 || c[1].Trim() == "Habitación" || !int.TryParse(c[3].Trim(), out var n)) continue;
                var room = c[1].Trim(); total += n; counts[room] = counts.ContainsKey(room) ? counts[room] + n : n;
                if (n < 1 || n > 2) errors.Add($"{room}: cantidad individual de escondites fuera de 1–2.");
                if (FindSpace(spaces, room) == null) errors.Add($"Escondite en recinto inexistente: {room}.");
            }
            var tooMany = false; foreach (var count in counts.Values) if (count > 2) tooMany = true;
            if (total != 9 || tooMany) errors.Add("Escondites: se requieren 9 en total y máximo 2 por recinto.");
            if (!counts.ContainsKey("G-COC") || !counts.ContainsKey("G-REC") || !spaces.Exists(s => s.Level == "Sótano" && counts.ContainsKey(s.Id)))
                errors.Add("Escondites: falta cocina, recibidor o sótano.");
            var p15 = doors.Find(d => d.Id == "P-G15");
            if (p15 == null || p15.Axis != 'X' || Mathf.Abs(p15.Position - 24f) > Tolerance || p15.B != "EXTERIOR") errors.Add("P-G15 debe estar al exterior en x=24.");
            var planText = string.Join("\n", lines);
            if (planText.IndexOf("P-G15", System.StringComparison.Ordinal) < 0 || planText.IndexOf("3 × 0.15 m", System.StringComparison.Ordinal) < 0)
                errors.Add("P-G15 debe declarar escalones de 3 × 0.15 m.");
            if (!doors.Exists(d => d.B == "EXTERIOR" && FindSpace(spaces, d.A)?.Level == "Sótano")) errors.Add("Falta segunda salida del sótano.");
            var exterior = new Dictionary<string, Rect>(); var inExterior = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Exterior", System.StringComparison.Ordinal)) { inExterior = true; continue; }
                if (inExterior && line.StartsWith("## ", System.StringComparison.Ordinal)) break;
                if (!inExterior || !line.StartsWith("|", System.StringComparison.Ordinal)) continue;
                var c = line.Split('|'); if (c.Length < 5 || c[1].Trim() == "Elemento") continue;
                var v = c[2].Trim().Trim('(', ')').Split(';');
                if (v.Length == 4 && float.TryParse(v[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x0) &&
                    float.TryParse(v[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z0) &&
                    float.TryParse(v[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x1) &&
                    float.TryParse(v[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z1)) exterior[c[1].Trim()] = Rect.MinMaxRect(x0, z0, x1, z1);
            }
            if (!exterior.TryGetValue("Porche", out var porch) || !exterior.TryGetValue("Camioneta", out var van) ||
                Mathf.Abs(porch.width - 6f) > Tolerance || Mathf.Abs(porch.height - 3f) > Tolerance || Overlaps(porch, van))
                errors.Add("Porche/camioneta: falta el porche 6 × 3 m o los rectángulos se solapan.");
            if (errors.Count == initial) checks.Add("[OK] Escondites, P-G15 y segunda salida del sótano.");
        }

        private static void CheckOverlaps(List<MansionLayoutSpace> spaces, List<string> errors, List<string> checks)
        {
            var initialErrorCount = errors.Count;
            for (var i = 0; i < spaces.Count; i++)
            for (var j = i + 1; j < spaces.Count; j++)
            {
                var a = spaces[i];
                var b = spaces[j];
                if (a.Level != b.Level) continue;
                var overlapX = Mathf.Min(a.Bounds.xMax, b.Bounds.xMax) - Mathf.Max(a.Bounds.xMin, b.Bounds.xMin);
                var overlapZ = Mathf.Min(a.Bounds.yMax, b.Bounds.yMax) - Mathf.Max(a.Bounds.yMin, b.Bounds.yMin);
                if (overlapX > Tolerance && overlapZ > Tolerance)
                    errors.Add($"Solape en {a.Level}: {a.Id} / {b.Id}.");
            }
            if (errors.Count == initialErrorCount) checks.Add("[OK] Sin solapes por planta.");
        }

        private static void CheckDoors(List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors, List<MansionLayoutStair> stairs,
            List<string> errors, List<string> checks)
        {
            var initialErrorCount = errors.Count;
            var connected = new HashSet<string>();
            foreach (var door in doors)
            {
                var a = FindSpace(spaces, door.A);
                var b = door.B == "EXTERIOR" ? null : FindSpace(spaces, door.B);
                var widthMatches = Mathf.Abs((door.Max - door.Min) - door.Width) <= Tolerance;
                if (a == null || (door.B != "EXTERIOR" && b == null) || door.Min >= door.Max ||
                    !widthMatches || !OnSharedWall(a, b, door))
                    errors.Add($"{door.Id}: hueco fuera de muro compartido/perímetro o tramo distinto al ancho.");
                if (a != null) connected.Add(a.Id);
                if (b != null) connected.Add(b.Id);
            }
            foreach (var stair in stairs)
            {
                if (FindSpace(spaces, stair.LandingA) != null) connected.Add(stair.LandingA);
                if (FindSpace(spaces, stair.LandingB) != null) connected.Add(stair.LandingB);
            }
            foreach (var space in spaces)
                if (!connected.Contains(space.Id)) errors.Add($"{space.Id}: no tiene puerta ni escalera documentada.");
            if (errors.Count == initialErrorCount)
                checks.Add("[OK] Todas las puertas están en muros compartidos; las exteriores, en el perímetro.");
        }

        private static void CheckOverheadClearance(List<MansionLayoutSpace> spaces, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            var levels = new[] { "Sótano", "Planta baja", "Primer piso", "Desván" };
            foreach (var lower in spaces)
            {
                if (lower.Height <= 2.70f) continue;
                var levelIndex = System.Array.IndexOf(levels, lower.Level);
                if (levelIndex < 0 || levelIndex + 1 >= levels.Length) continue;
                foreach (var above in spaces.FindAll(space => space.Level == levels[levelIndex + 1]))
                    if (Overlaps(lower.Bounds, above.Bounds))
                        errors.Add($"Altura de {lower.Id} ({lower.Height:0.00} m) invade el espacio superior {above.Id}.");
            }
            if (errors.Count == initial) checks.Add("[OK] Ningún recinto de más de 2.70 m tiene otro recinto encima.");
        }

        private static bool OnSharedWall(MansionLayoutSpace a, MansionLayoutSpace b, MansionLayoutDoor door)
        {
            if (door.Axis == 'X')
            {
                var low = a.Bounds.yMin;
                var high = a.Bounds.yMax;
                if (b != null)
                {
                    var shared = Near(a.Bounds.xMax, door.Position) && Near(b.Bounds.xMin, door.Position) ||
                                 Near(b.Bounds.xMax, door.Position) && Near(a.Bounds.xMin, door.Position);
                    if (!shared) return false;
                    low = Mathf.Max(a.Bounds.yMin, b.Bounds.yMin);
                    high = Mathf.Min(a.Bounds.yMax, b.Bounds.yMax);
                }
                else if (!Near(a.Bounds.xMin, door.Position) && !Near(a.Bounds.xMax, door.Position)) return false;
                return door.Min - 0.45f >= low - Tolerance && door.Max + 0.45f <= high + Tolerance;
            }

            if (door.Axis == 'Z')
            {
                var low = a.Bounds.xMin;
                var high = a.Bounds.xMax;
                if (b != null)
                {
                    var shared = Near(a.Bounds.yMax, door.Position) && Near(b.Bounds.yMin, door.Position) ||
                                 Near(b.Bounds.yMax, door.Position) && Near(a.Bounds.yMin, door.Position);
                    if (!shared) return false;
                    low = Mathf.Max(a.Bounds.xMin, b.Bounds.xMin);
                    high = Mathf.Min(a.Bounds.xMax, b.Bounds.xMax);
                }
                else if (!Near(a.Bounds.yMin, door.Position) && !Near(a.Bounds.yMax, door.Position)) return false;
                return door.Min - 0.45f >= low - Tolerance && door.Max + 0.45f <= high + Tolerance;
            }
            return false;
        }

        private static void CheckStairs(List<MansionLayoutSpace> spaces, List<MansionLayoutStair> stairs,
            List<string> errors, List<string> checks)
        {
            var initialErrorCount = errors.Count;
            var expectedIds = new[] { "S1", "S2", "S3", "S4" };
            foreach (var id in expectedIds)
                if (stairs.Find(stair => stair.Id == id) == null)
                    errors.Add($"Falta la escalera {id}.");
            if (stairs.Count != expectedIds.Length)
                errors.Add($"Se esperaban {expectedIds.Length} escaleras y se leyeron {stairs.Count}.");
            foreach (var stair in stairs)
            {
                if ((stair.CenterA - stair.CenterB).sqrMagnitude > Tolerance * Tolerance)
                    errors.Add($"{stair.Id}: los centros no están alineados entre niveles.");
                var floorA = FindSpaceAt(spaces, stair.LevelA, stair.CenterA);
                var floorB = FindSpaceAt(spaces, stair.LevelB, stair.CenterB);
                if (floorA == null || floorB == null || !Contains(floorA.Bounds, stair.Opening) || !Contains(floorB.Bounds, stair.Opening))
                    errors.Add($"{stair.Id}: el centro no cae dentro de un espacio en ambos niveles.");
                if (stair.Width < 1.2f || stair.Risers != 17 || Mathf.Abs(stair.Tread - 0.28f) > Tolerance ||
                    stair.Headroom < 2.1f || !Contains(stair.Opening, stair.CenterA) ||
                    Mathf.Max(stair.Opening.width, stair.Opening.height) < 3.8f - Tolerance ||
                    Mathf.Min(stair.Opening.width, stair.Opening.height) < stair.Width - Tolerance)
                    errors.Add($"{stair.Id}: hueco, ancho, peldaños o altura de cabeza no cumplen el plano.");
                var hostA = FindSpace(spaces, stair.LandingA);
                var hostB = FindSpace(spaces, stair.LandingB);
                if (hostA == null || hostB == null || !Contains(hostA.Bounds, stair.ArrivalA) || !Contains(hostB.Bounds, stair.ArrivalB))
                    errors.Add($"{stair.Id}: rectángulo de llegada fuera del espacio anfitrión.");
                else if (!Contains(hostA.Bounds, Expand(stair.ArrivalA, 1.2f)) || !Contains(hostB.Bounds, Expand(stair.ArrivalB, 1.2f)))
                    errors.Add($"{stair.Id}: menos de 1.2 m libres alrededor de llegada.");
                if (stair.Direction.IndexOf("U", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (hostA == null || Mathf.Min(hostA.Bounds.width, hostA.Bounds.height) < 2.4f - Tolerance))
                    errors.Add($"{stair.Id}: recinto de escalera en U menor de 2.4 m de ancho.");
                if (stair.Id == "S1" && (hostB == null ||
                    Mathf.Max(stair.Opening.xMin - hostB.Bounds.xMin, hostB.Bounds.xMax - stair.Opening.xMax) < 1.2f - Tolerance))
                    errors.Add("S1: el pasillo deja menos de 1.2 m libres junto a la baranda.");
            }
            for (var i = 0; i < stairs.Count; i++)
            for (var j = i + 1; j < stairs.Count; j++)
            {
                var a = stairs[i]; var b = stairs[j];
                var sharesFloor = a.LevelA == b.LevelA || a.LevelA == b.LevelB || a.LevelB == b.LevelA || a.LevelB == b.LevelB;
                var overlapX = Mathf.Min(a.Opening.xMax, b.Opening.xMax) - Mathf.Max(a.Opening.xMin, b.Opening.xMin);
                var overlapZ = Mathf.Min(a.Opening.yMax, b.Opening.yMax) - Mathf.Max(a.Opening.yMin, b.Opening.yMin);
                if (sharesFloor && overlapX > Tolerance && overlapZ > Tolerance)
                    errors.Add($"Huecos de escalera solapados: {a.Id} / {b.Id}.");
            }
            if (errors.Count == initialErrorCount) checks.Add("[OK] S1/S2/S3/S4 alineadas y dentro de sus dos niveles.");
        }

        private static void CheckDoorsOutsideStairOpenings(List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            foreach (var door in doors)
            {
                var host = FindSpace(spaces, door.A);
                if (host == null) continue;
                foreach (var stair in stairs)
                {
                    if (host.Level != stair.LevelA && host.Level != stair.LevelB) continue;
                    var lineHitsOpening = door.Axis == 'X'
                        ? door.Position >= stair.Opening.xMin - Tolerance && door.Position <= stair.Opening.xMax + Tolerance &&
                          door.Max > stair.Opening.yMin + Tolerance && door.Min < stair.Opening.yMax - Tolerance
                        : door.Axis == 'Z' && door.Position >= stair.Opening.yMin - Tolerance && door.Position <= stair.Opening.yMax + Tolerance &&
                          door.Max > stair.Opening.xMin + Tolerance && door.Min < stair.Opening.xMax - Tolerance;
                    var clearance = stair.Id == "S1" || stair.Id == "S3" ? 1.2f : 0.9f;
                    var distance = LineRectDistance(door, stair.Opening);
                    if (lineHitsOpening || distance < clearance - Tolerance)
                        errors.Add($"{door.Id}: queda dentro o demasiado cerca del hueco de {stair.Id} (mínimo {clearance:0.0} m).");
                }
            }
            if (errors.Count == initial) checks.Add("[OK] Ninguna puerta cae dentro de un hueco de escalera.");
        }

        private static void CheckVerticalSupport(List<MansionLayoutSpace> spaces, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            var levels = new[] { "Sótano", "Planta baja", "Primer piso", "Desván" };
            for (var i = 2; i < levels.Length; i++)
            foreach (var upper in spaces.FindAll(space => space.Level == levels[i]))
            {
                var area = upper.Bounds.width * upper.Bounds.height;
                var supported = 0f;
                foreach (var lower in spaces.FindAll(space => space.Level == levels[i - 1]))
                {
                    var dx = Mathf.Max(0f, Mathf.Min(upper.Bounds.xMax, lower.Bounds.xMax) - Mathf.Max(upper.Bounds.xMin, lower.Bounds.xMin));
                    var dz = Mathf.Max(0f, Mathf.Min(upper.Bounds.yMax, lower.Bounds.yMax) - Mathf.Max(upper.Bounds.yMin, lower.Bounds.yMin));
                    supported += dx * dz;
                }
                var ratio = area <= Tolerance ? 0f : supported / area;
                var percent = (ratio * 100f).ToString("0.00", CultureInfo.InvariantCulture) + "%";
                if (ratio < 0.9f - Tolerance) errors.Add($"Apoyo vertical insuficiente: {upper.Id} = {percent} (<90%).");
                else checks.Add($"[OK] Apoyo vertical {upper.Id}: {percent}.");
            }
            if (errors.Count == initial) checks.Add("[OK] Todos los apoyos verticales cumplen el mínimo del 90%.");
        }

        private static void CheckGates(List<MansionLayoutGate> gates, List<string> errors, List<string> checks)
        {
            var initial = errors.Count;
            var required = new Dictionary<string, string[]>
            {
                { "S1", new[] { "P-S1-TOP", "KEY_UPPER" } },
                { "S2", new[] { "P-S2-TOP", "KEY_UPPER" } },
                { "S3", new[] { "P-S3-TOP", "KEY_BASEMENT" } }
            };
            foreach (var entry in required)
            {
                var gate = gates.Find(item => item.StairId == entry.Key);
                if (gate == null || gate.Id != entry.Value[0] || gate.KeyId != entry.Value[1])
                    errors.Add($"{entry.Value[0]}: falta compuerta de {entry.Key} con llave {entry.Value[1]}.");
            }
            if (gates.Find(item => item.StairId == "S4" && item.KeyId == "NONE") == null)
                errors.Add("S4 debe tener compuerta interior sin llave (NONE).");
            if (errors.Count == initial) checks.Add("[OK] Llaves de primer piso y sótano asignadas a sus escaleras.");
        }

        private static void CheckReachability(List<MansionLayoutSpace> spaces, List<MansionLayoutDoor> doors,
            List<MansionLayoutStair> stairs, List<MansionLayoutGate> gates, List<string> errors, List<string> checks)
        {
            var entrance = doors.Find(door => door.Id == "P-G0" && door.B == "EXTERIOR");
            if (entrance == null)
            {
                errors.Add("No se encontró P-G0 como puerta principal desde EXTERIOR.");
                return;
            }

            var links = new Dictionary<string, List<string>>();
            foreach (var space in spaces) links[space.Id] = new List<string>();
            CheckScenario("sin llaves", "", spaces, doors, links, stairs, gates, entrance.A, errors, checks,
                new[] { "Sótano" });
            CheckScenario("KEY_UPPER", "KEY_UPPER", spaces, doors, links, stairs, gates, entrance.A, errors, checks,
                new[] { "Sótano" });
            CheckScenario("KEY_BASEMENT", "KEY_BASEMENT", spaces, doors, links, stairs, gates, entrance.A, errors, checks,
                new[] { "Primer piso", "Desván" });
        }

        private static void CheckScenario(string label, string key, List<MansionLayoutSpace> spaces,
            List<MansionLayoutDoor> doors, Dictionary<string, List<string>> baseLinks, List<MansionLayoutStair> stairs, List<MansionLayoutGate> gates,
            string entrance, List<string> errors, List<string> checks, string[] forbiddenLevels)
        {
            var links = new Dictionary<string, List<string>>();
            foreach (var item in baseLinks) links[item.Key] = new List<string>(item.Value);
            foreach (var door in doors)
            {
                if (door.B == "EXTERIOR" || door.KeyId != "NONE" && door.KeyId != key) continue;
                if (links.ContainsKey(door.A) && links.ContainsKey(door.B)) Link(links, door.A, door.B);
            }
            foreach (var stair in stairs)
            {
                var gate = gates.Find(item => item.StairId == stair.Id);
                if (gate != null && gate.KeyId != "NONE" && gate.KeyId != key) continue;
                var a = FindSpace(spaces, stair.LandingA);
                var b = FindSpace(spaces, stair.LandingB);
                if (a != null && b != null) Link(links, a.Id, b.Id);
            }
            var reached = new HashSet<string> { entrance };
            var queue = new Queue<string>(); queue.Enqueue(entrance);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in links[current]) if (reached.Add(next)) queue.Enqueue(next);
            }
            foreach (var level in forbiddenLevels)
                if (spaces.Exists(space => space.Level == level && reached.Contains(space.Id)))
                    errors.Add($"Recorrido {label}: llegó indebidamente a {level}.");
            foreach (var level in new[] { "Planta baja", "Primer piso", "Desván", "Sótano" })
            {
                var shouldReach = level == "Planta baja" || key == "KEY_UPPER" && (level == "Primer piso" || level == "Desván") ||
                                  key == "KEY_BASEMENT" && level == "Sótano";
                foreach (var space in spaces.FindAll(item => item.Level == level))
                    if (reached.Contains(space.Id) != shouldReach)
                        errors.Add($"Recorrido {label}: alcanzabilidad incorrecta para {space.Id} ({level}).");
            }
            if (!errors.Exists(error => error.Contains("Recorrido " + label))) checks.Add($"[OK] Recorrido {label}: niveles permitidos y bloqueados correctos.");
        }

        private static bool Contains(Rect rect, Vector2 point) => point.x >= rect.xMin - Tolerance && point.x <= rect.xMax + Tolerance &&
            point.y >= rect.yMin - Tolerance && point.y <= rect.yMax + Tolerance;

        private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - Tolerance && inner.xMax <= outer.xMax + Tolerance &&
            inner.yMin >= outer.yMin - Tolerance && inner.yMax <= outer.yMax + Tolerance;

        private static Rect Expand(Rect rect, float amount) => Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);

        private static float LineRectDistance(MansionLayoutDoor door, Rect rect)
        {
            var dx = door.Axis == 'X' ? Mathf.Max(rect.xMin - door.Position, door.Position - rect.xMax, 0f) :
                Mathf.Max(rect.xMin - door.Max, door.Min - rect.xMax, 0f);
            var dz = door.Axis == 'X' ? Mathf.Max(rect.yMin - door.Max, door.Min - rect.yMax, 0f) :
                Mathf.Max(rect.yMin - door.Position, door.Position - rect.yMax, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool Overlaps(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > Tolerance &&
            Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > Tolerance;

        private static void Link(Dictionary<string, List<string>> links, string a, string b)
        {
            links[a].Add(b);
            links[b].Add(a);
        }

        private static MansionLayoutSpace FindSpace(List<MansionLayoutSpace> spaces, string id)
        {
            return spaces.Find(space => space.Id == id);
        }

        private static MansionLayoutSpace FindSpaceAt(List<MansionLayoutSpace> spaces, string level, Vector2 point)
        {
            return spaces.Find(space => space.Level == level &&
                point.x >= space.Bounds.xMin - Tolerance && point.x <= space.Bounds.xMax + Tolerance &&
                point.y >= space.Bounds.yMin - Tolerance && point.y <= space.Bounds.yMax + Tolerance);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) <= Tolerance;
    }
}
#endif

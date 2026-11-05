#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UltimaCinta.Editor
{
    internal sealed class MansionLayoutSpace
    {
        public string Id;
        public string Level;
        public Rect Bounds;
        public float Height;
        public string Kind;
    }

    internal sealed class MansionLayoutDoor
    {
        public string Id;
        public string A;
        public string B;
        public char Axis;
        public float Position;
        public float Min;
        public float Max;
        public float Width;
        public string KeyId;
    }

    internal sealed class MansionLayoutStair
    {
        public string Id;
        public string LevelA;
        public string LevelB;
        public Vector2 CenterA;
        public Vector2 CenterB;
        public Rect Opening;
        public string Direction;
        public string LandingA;
        public string LandingB;
        public string KeyId;
        public float Width;
        public int Risers;
        public float Tread;
        public float Headroom;
        public Rect ArrivalA;
        public Rect ArrivalB;
    }

    internal sealed class MansionLayoutGate
    {
        public string Id;
        public string StairId;
        public string KeyId;
        public string LevelA;
        public string LevelB;
        public string LandingB;
    }

    internal static class MansionLayoutPlanReader
    {
        public static List<MansionLayoutSpace> ReadSpaces(string[] lines, List<string> errors)
        {
            var result = new List<MansionLayoutSpace>();
            var reading = false;
            var level = string.Empty;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Espacios por planta", StringComparison.Ordinal))
                {
                    reading = true;
                    continue;
                }
                if (reading && line.StartsWith("## Puertas", StringComparison.Ordinal)) break;
                if (!reading) continue;
                if (line.StartsWith("### Sótano", StringComparison.Ordinal)) level = "Sótano";
                else if (line.StartsWith("### Planta baja", StringComparison.Ordinal)) level = "Planta baja";
                else if (line.StartsWith("### Primer piso", StringComparison.Ordinal)) level = "Primer piso";
                else if (line.StartsWith("### Desván", StringComparison.Ordinal)) level = "Desván";

                var cells = Cells(line);
                if (cells == null || cells.Length < 5 || !IsSpaceId(cells[0])) continue;
                if (string.IsNullOrEmpty(level) || !TryRectangle(cells[2], out var bounds) || !Number(cells[3], out var height))
                {
                    errors.Add($"Espacio mal formado: {cells[0]}.");
                    continue;
                }
                result.Add(new MansionLayoutSpace { Id = cells[0], Level = level, Bounds = bounds, Height = height, Kind = cells[4] });
            }
            if (result.Count == 0) errors.Add("No se encontraron rectángulos de espacios.");
            return result;
        }

        public static List<MansionLayoutDoor> ReadDoors(string[] lines, List<string> errors)
        {
            var result = new List<MansionLayoutDoor>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Puertas", StringComparison.Ordinal)) reading = true;
                else if (reading && line.StartsWith("## Escaleras", StringComparison.Ordinal)) break;
                if (!reading) continue;
                var cells = Cells(line);
                if (cells == null || cells.Length < 8 || !cells[0].StartsWith("P-", StringComparison.Ordinal)) continue;
                var segment = cells[5].Trim('(', ')').Split(';');
                if (segment.Length != 2 || !Number(cells[4], out var position) ||
                    !Number(segment[0], out var min) || !Number(segment[1], out var max) || !Number(cells[6], out var width))
                {
                    errors.Add($"Puerta mal formada: {cells[0]}.");
                    continue;
                }
                result.Add(new MansionLayoutDoor
                {
                    Id = cells[0], A = cells[1], B = cells[2],
                    Axis = cells[3].Length == 0 ? '?' : char.ToUpperInvariant(cells[3][0]),
                    Position = position, Min = min, Max = max, Width = width, KeyId = cells.Length > 7 ? cells[7] : "NONE"
                });
            }
            if (result.Count == 0) errors.Add("No se encontraron puertas.");
            return result;
        }

        public static List<MansionLayoutStair> ReadStairs(string[] lines, List<string> errors)
        {
            var result = new List<MansionLayoutStair>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Escaleras, compuertas", StringComparison.Ordinal)) reading = true;
                if (reading && line.StartsWith("## Exterior", StringComparison.Ordinal)) break;
                if (!reading) continue;
                var cells = Cells(line);
                if (cells == null || cells.Length < 15 || cells[0].Length != 2 || cells[0][0] != 'S') continue;
                if (!TryPoint(cells[3], out var centerA) || !TryPoint(cells[4], out var centerB) ||
                    !TryRectangle(cells[5], out var opening) || !Number(cells[7], out var width) ||
                    !int.TryParse(cells[8], out var risers) || !Number(cells[9], out var tread) ||
                    !Number(cells[12], out var headroom) || !TryRectangle(cells[13], out var arrivalA) ||
                    !TryRectangle(cells[14], out var arrivalB))
                {
                    errors.Add($"Escalera mal formada: {cells[0]}.");
                    continue;
                }
                result.Add(new MansionLayoutStair
                {
                    Id = cells[0], LevelA = cells[1], LevelB = cells[2], CenterA = centerA, CenterB = centerB,
                    Opening = opening, Direction = cells[6], Width = width, Risers = risers, Tread = tread, Headroom = headroom,
                    LandingA = cells[10], LandingB = cells[11], ArrivalA = arrivalA, ArrivalB = arrivalB
                });
            }
            if (result.Count == 0) errors.Add("No se encontraron escaleras.");
            return result;
        }

        public static List<MansionLayoutGate> ReadGates(string[] lines, List<string> errors)
        {
            var result = new List<MansionLayoutGate>();
            var reading = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("## Compuertas de escalera", StringComparison.Ordinal))
                {
                    reading = true;
                    continue;
                }
                if (reading && line.StartsWith("## ", StringComparison.Ordinal)) break;
                if (!reading) continue;
                var cells = Cells(line);
                if (cells == null || cells.Length < 3 || cells[0] == "ID" || cells[0].StartsWith("---", StringComparison.Ordinal)) continue;
                result.Add(new MansionLayoutGate
                {
                    Id = cells[0], StairId = cells[1], KeyId = cells[2]
                });
            }
            if (result.Count == 0) errors.Add("No se encontraron compuertas de escalera.");
            return result;
        }

        private static string[] Cells(string line)
        {
            if (!line.StartsWith("|", StringComparison.Ordinal)) return null;
            var cells = line.Split('|');
            var result = new string[cells.Length - 2];
            for (var i = 1; i < cells.Length - 1; i++) result[i - 1] = cells[i].Trim();
            return result;
        }

        private static bool TryRectangle(string text, out Rect bounds)
        {
            bounds = default;
            var values = text.Trim('(', ')').Split(';');
            if (values.Length != 4 || !Number(values[0], out var x0) || !Number(values[1], out var z0) ||
                !Number(values[2], out var x1) || !Number(values[3], out var z1) || x1 <= x0 || z1 <= z0)
                return false;
            bounds = Rect.MinMaxRect(x0, z0, x1, z1);
            return true;
        }

        private static bool TryPoint(string text, out Vector2 point)
        {
            point = default;
            var values = text.Trim('(', ')').Split(';');
            if (values.Length != 2 || !Number(values[0], out var x) || !Number(values[1], out var z)) return false;
            point = new Vector2(x, z);
            return true;
        }

        private static bool Number(string text, out float value)
        {
            return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsSpaceId(string value)
        {
            return value.StartsWith("B-", StringComparison.Ordinal) || value.StartsWith("G-", StringComparison.Ordinal) ||
                   value.StartsWith("U-", StringComparison.Ordinal) || value.StartsWith("A-", StringComparison.Ordinal);
        }
    }
}
#endif

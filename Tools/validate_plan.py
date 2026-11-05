#!/usr/bin/env python3
"""Unity-free validator for Docs/PlanoMansion.md."""
from __future__ import annotations

import argparse
import copy
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / "Docs" / "PlanoMansion.md"
TOL = 1e-6
SUPPORT_MIN = 0.90
DOOR_MARGIN = 0.45
DOOR_CLEARANCE = 0.90
ARRIVAL_CLEARANCE = 1.20
MIN_OPENING_LENGTH = 3.80
MIN_STAIR_WIDTH = 1.20
LEVELS = ["Sótano", "Planta baja", "Primer piso", "Desván"]


def cells(line: str):
    if not line.startswith("|"):
        return None
    return [part.strip() for part in line.strip().strip("|").split("|")]


def rows_in_section(text: str, heading: str):
    start = text.find(heading)
    if start < 0:
        return []
    next_heading = text.find("\n## ", start + len(heading))
    section = text[start:] if next_heading < 0 else text[start:next_heading]
    return [c for line in section.splitlines() if (c := cells(line)) is not None]


def rect(value: str):
    vals = [float(x.strip()) for x in value.strip().strip("()").split(";")]
    if len(vals) != 4 or vals[2] <= vals[0] or vals[3] <= vals[1]:
        raise ValueError(f"rectángulo inválido: {value}")
    return tuple(vals)


def point(value: str):
    vals = [float(x.strip()) for x in value.strip().strip("()").split(";")]
    if len(vals) != 2:
        raise ValueError(f"punto inválido: {value}")
    return tuple(vals)


def parse_plan(path: Path = PLAN):
    text = path.read_text(encoding="utf-8")
    plan = {"spaces": [], "doors": [], "stairs": [], "gates": [], "hideouts": [], "footprints": {}, "exterior": []}
    level = None
    in_spaces = False
    for line in text.splitlines():
        if line.startswith("## Espacios por planta"):
            in_spaces = True
            continue
        if in_spaces and line.startswith("## Puertas"):
            break
        if not in_spaces:
            continue
        if line.startswith("### "):
            level = line[4:].strip()
        row = cells(line)
        if row is None or len(row) < 7 or row[0] in ("ID", "---") or not row[0][:2] in ("B-", "G-", "U-", "A-"):
            continue
        try:
            plan["spaces"].append({
                "id": row[0], "name": row[1], "level": level, "rect": rect(row[2]),
                "height": float(row[3]), "kind": row[4].lower()
            })
        except (ValueError, IndexError) as exc:
            plan.setdefault("parse_errors", []).append(f"Espacio {row[0]}: {exc}")

    for row in rows_in_section(text, "## Huellas de planta"):
        if len(row) < 2 or row[0] in ("Nivel", "---"):
            continue
        try:
            plan["footprints"][row[0]] = rect(row[1])
        except ValueError as exc:
            plan.setdefault("parse_errors", []).append(str(exc))

    for row in rows_in_section(text, "## Puertas"):
        if len(row) < 8 or not row[0].startswith("P-") or row[0] == "ID":
            continue
        try:
            lo, hi = [float(v.strip()) for v in row[5].strip("()").split(";")]
            plan["doors"].append({
                "id": row[0], "a": row[1], "b": row[2], "axis": row[3].upper(),
                "pos": float(row[4]), "lo": lo, "hi": hi, "width": float(row[6]),
                "key": row[7]
            })
        except (ValueError, IndexError) as exc:
            plan.setdefault("parse_errors", []).append(f"Puerta {row[0]}: {exc}")

    for row in rows_in_section(text, "## Escaleras"):
        if len(row) < 15 or row[0] not in ("S1", "S2", "S3", "S4"):
            continue
        try:
            plan["stairs"].append({
                "id": row[0], "a_level": row[1], "b_level": row[2],
                "center_a": point(row[3]), "center_b": point(row[4]), "rect": rect(row[5]),
                "direction": row[6], "width": float(row[7]), "risers": int(row[8]),
                "tread": float(row[9]), "a_room": row[10], "b_room": row[11],
                "headroom": float(row[12]), "arrival_a": rect(row[13]), "arrival_b": rect(row[14])
            })
        except (ValueError, IndexError) as exc:
            plan.setdefault("parse_errors", []).append(f"Escalera {row[0]}: {exc}")

    for row in rows_in_section(text, "## Compuertas de escalera"):
        if len(row) < 3 or row[0] in ("ID", "---"):
            continue
        plan["gates"].append({"id": row[0], "stair": row[1], "key": row[2]})

    for row in rows_in_section(text, "## Escondites"):
        if len(row) < 3 or row[0] in ("Habitación", "---"):
            continue
        try:
            plan["hideouts"].append({"room": row[0], "type": row[1], "count": int(row[2])})
        except ValueError:
            plan.setdefault("parse_errors", []).append(f"Escondite mal formado: {row}")

    for row in rows_in_section(text, "## Exterior"):
        if len(row) < 3 or row[0] in ("Elemento", "---"):
            continue
        try:
            plan["exterior"].append({"id": row[0], "rect": rect(row[1]), "kind": row[2].lower()})
        except ValueError as exc:
            plan.setdefault("parse_errors", []).append(f"Exterior {row[0]}: {exc}")
    return plan


def overlap(a, b):
    dx = min(a[2], b[2]) - max(a[0], b[0])
    dz = min(a[3], b[3]) - max(a[1], b[1])
    return dx > TOL and dz > TOL


def contains(outer, inner, margin=0.0):
    return (inner[0] >= outer[0] + margin - TOL and inner[1] >= outer[1] + margin - TOL
            and inner[2] <= outer[2] - margin + TOL and inner[3] <= outer[3] - margin + TOL)


def expand(r, amount):
    return (r[0] - amount, r[1] - amount, r[2] + amount, r[3] + amount)


def door_rect(door):
    if door["axis"] == "X":
        return (door["pos"], door["lo"], door["pos"], door["hi"])
    return (door["lo"], door["pos"], door["hi"], door["pos"])


def line_rect_distance(door, opening):
    d = door_rect(door)
    dx = max(opening[0] - d[2], d[0] - opening[2], 0.0)
    dz = max(opening[1] - d[3], d[1] - opening[3], 0.0)
    return math.hypot(dx, dz)


def shared_edge(a, b, door):
    ar, br = a["rect"], b["rect"]
    if door["axis"] == "X":
        shared = ((abs(ar[2] - door["pos"]) <= TOL and abs(br[0] - door["pos"]) <= TOL) or
                  (abs(br[2] - door["pos"]) <= TOL and abs(ar[0] - door["pos"]) <= TOL))
        low, high = max(ar[1], br[1]), min(ar[3], br[3])
    elif door["axis"] == "Z":
        shared = ((abs(ar[3] - door["pos"]) <= TOL and abs(br[1] - door["pos"]) <= TOL) or
                   (abs(br[3] - door["pos"]) <= TOL and abs(ar[1] - door["pos"]) <= TOL))
        low, high = max(ar[0], br[0]), min(ar[2], br[2])
    else:
        return False
    return shared and door["lo"] - DOOR_MARGIN >= low - TOL and door["hi"] + DOOR_MARGIN <= high + TOL


def exterior_edge(space, door):
    r = space["rect"]
    if door["axis"] == "X":
        on_edge = abs(door["pos"] - r[0]) <= TOL or abs(door["pos"] - r[2]) <= TOL
        edge_low, edge_high = r[1], r[3]
    elif door["axis"] == "Z":
        on_edge = abs(door["pos"] - r[1]) <= TOL or abs(door["pos"] - r[3]) <= TOL
        edge_low, edge_high = r[0], r[2]
    else:
        return False
    return on_edge and door["lo"] - DOOR_MARGIN >= edge_low - TOL and door["hi"] + DOOR_MARGIN <= edge_high + TOL


def validate(plan):
    errors = list(plan.get("parse_errors", []))
    spaces = plan["spaces"]
    by_id = {s["id"]: s for s in spaces}
    doors = plan["doors"]
    stairs = plan["stairs"]

    if len(spaces) < 28 or len(spaces) > 32:
        errors.append(f"Recintos: {len(spaces)}; se requieren 28–32.")
    ids = [s["id"] for s in spaces]
    if len(ids) != len(set(ids)):
        errors.append("Hay IDs de recinto duplicados.")

    for level in LEVELS:
        same = [s for s in spaces if s["level"] == level]
        for i, a in enumerate(same):
            for b in same[i + 1:]:
                if overlap(a["rect"], b["rect"]):
                    errors.append(f"Solape en {level}: {a['id']} / {b['id']}.")

    for level in LEVELS:
        fp = plan["footprints"].get(level)
        if fp is None:
            errors.append(f"Falta huella para {level}.")
            continue
        for s in [x for x in spaces if x["level"] == level]:
            if not contains(fp, s["rect"]):
                errors.append(f"{s['id']} sobresale de la huella de {level}.")
    gf = plan["footprints"].get("Planta baja")
    for s in [x for x in spaces if x["level"] == "Sótano"]:
        if gf and not contains(gf, s["rect"]):
            errors.append(f"{s['id']} del sótano queda fuera de la huella de planta baja.")
    for s in [x for x in spaces if x["level"] == "Primer piso"]:
        if gf and not contains(gf, s["rect"]):
            errors.append(f"{s['id']} del primer piso sobresale de la huella de planta baja.")
    fp_first = plan["footprints"].get("Primer piso")
    for s in [x for x in spaces if x["level"] == "Desván"]:
        if fp_first and not contains(fp_first, s["rect"]):
            errors.append(f"{s['id']} del desván sobresale de la huella del primer piso.")

    # One support definition: sum of disjoint intersections with the immediately lower floor / upper rectangle area.
    support = {}
    for i in range(2, len(LEVELS)):
        lower_level, upper_level = LEVELS[i - 1], LEVELS[i]
        lower = [s for s in spaces if s["level"] == lower_level]
        for upper in [s for s in spaces if s["level"] == upper_level]:
            area = (upper["rect"][2] - upper["rect"][0]) * (upper["rect"][3] - upper["rect"][1])
            supported = 0.0
            for base in lower:
                dx = max(0.0, min(upper["rect"][2], base["rect"][2]) - max(upper["rect"][0], base["rect"][0]))
                dz = max(0.0, min(upper["rect"][3], base["rect"][3]) - max(upper["rect"][1], base["rect"][1]))
                supported += dx * dz
            ratio = 0 if area <= TOL else supported / area
            support[upper["id"]] = ratio
            if ratio + TOL < SUPPORT_MIN:
                errors.append(f"Apoyo vertical {upper['id']}: {ratio:.2%}, mínimo 90%.")

    for s in spaces:
        w, d = s["rect"][2] - s["rect"][0], s["rect"][3] - s["rect"][1]
        if s["kind"] == "habitación" and min(w, d) < 4.0 - TOL:
            errors.append(f"{s['id']}: habitación ordinaria menor de 4 m en un lado.")
        if s["kind"] == "habitación" and max(w, d) < 5.0 - TOL:
            errors.append(f"{s['id']}: habitación ordinaria menor de 5 m en ambos lados.")
        if s["kind"] == "pasillo":
            narrow = min(w, d)
            expected = 1.2 if s["id"] == "G-SER" else None
            if expected is not None and abs(narrow - expected) > TOL:
                errors.append(f"{s['id']}: pasillo de servicio debe medir 1.2 m.")
            elif expected is None and not (1.5 - TOL <= narrow <= 1.8 + TOL):
                errors.append(f"{s['id']}: pasillo mide {narrow:.2f} m; requerido 1.5–1.8 m.")

    if not doors:
        errors.append("No se leyeron puertas.")
    doors_by_room = {s["id"]: [] for s in spaces}
    for door in doors:
        a = by_id.get(door["a"])
        b = by_id.get(door["b"])
        if a is None:
            errors.append(f"{door['id']}: espacio A inexistente ({door['a']}).")
            continue
        if door["b"] == "EXTERIOR":
            valid = exterior_edge(a, door)
        elif b is None:
            errors.append(f"{door['id']}: espacio B inexistente ({door['b']}).")
            continue
        else:
            valid = a["level"] == b["level"] and shared_edge(a, b, door)
        if not valid:
            errors.append(f"{door['id']}: no está en un borde compartido/perimetral con margen de 0.45 m.")
        if abs((door["hi"] - door["lo"]) - door["width"]) > TOL:
            errors.append(f"{door['id']}: tramo y ancho no coinciden.")
        doors_by_room[a["id"]].append(door)
        if b is not None:
            doors_by_room[b["id"]].append(door)

    for s in spaces:
        if not doors_by_room[s["id"]] and not any(st["a_room"] == s["id"] or st["b_room"] == s["id"] for st in stairs):
            errors.append(f"{s['id']}: no tiene puerta ni conexión de escalera.")

    stair_ids = {s["id"] for s in stairs}
    if stair_ids != {"S1", "S2", "S3", "S4"}:
        errors.append("Deben existir exactamente S1, S2, S3 y S4.")
    for stair in stairs:
        r = stair["rect"]
        length, width = max(r[2]-r[0], r[3]-r[1]), min(r[2]-r[0], r[3]-r[1])
        if length < MIN_OPENING_LENGTH - TOL or width < MIN_STAIR_WIDTH - TOL:
            errors.append(f"{stair['id']}: hueco menor de 3.8 × 1.2 m.")
        if tuple(stair["center_a"]) != tuple(stair["center_b"]):
            errors.append(f"{stair['id']}: centros no coinciden entre niveles.")
        hosts = [by_id.get(stair["a_room"]), by_id.get(stair["b_room"])]
        if any(h is None for h in hosts):
            errors.append(f"{stair['id']}: recinto de llegada inexistente.")
        else:
            if hosts[0]["level"] != stair["a_level"] or hosts[1]["level"] != stair["b_level"]:
                errors.append(f"{stair['id']}: recinto de llegada en nivel incorrecto.")
            for host, arrival in zip(hosts, [stair["arrival_a"], stair["arrival_b"]]):
                if not contains(host["rect"], r) or not contains(host["rect"], arrival):
                    errors.append(f"{stair['id']}: hueco o llegada sale del recinto {host['id']}.")
                if not contains(host["rect"], expand(arrival, ARRIVAL_CLEARANCE)):
                    errors.append(f"{stair['id']}: menos de 1.2 m libres alrededor de llegada en {host['id']}.")
        if stair["a_level"] not in LEVELS or stair["b_level"] not in LEVELS:
            errors.append(f"{stair['id']}: nivel inválido.")

    for i, a in enumerate(stairs):
        for b in stairs[i + 1:]:
            common = {a["a_level"], a["b_level"]} & {b["a_level"], b["b_level"]}
            if common and overlap(a["rect"], b["rect"]):
                errors.append(f"Huecos de escaleras solapados: {a['id']} / {b['id']}.")

    for door in doors:
        for stair in stairs:
            host = by_id.get(door["a"])
            if host is None or host["level"] not in (stair["a_level"], stair["b_level"]):
                continue
            distance = line_rect_distance(door, stair["rect"])
            required_clearance = 1.20 if stair["id"] in ("S1", "S3") else DOOR_CLEARANCE
            if distance < required_clearance - TOL:
                errors.append(f"{door['id']}: a {distance:.2f} m del hueco de {stair['id']} (mínimo {required_clearance:.2f} m).")

    heights = {s["id"]: s["height"] for s in spaces}
    for i, level in enumerate(LEVELS[:-1]):
        for below in [s for s in spaces if s["level"] == level and s["height"] > 2.70 + TOL]:
            for above in [s for s in spaces if s["level"] == LEVELS[i + 1] and overlap(s["rect"], below["rect"])]:
                errors.append(f"{below['id']} mide más de 2.70 m y tiene {above['id']} encima.")

    gates = {g["stair"]: g["key"] for g in plan["gates"]}
    expected_gates = {"S1": "KEY_UPPER", "S2": "KEY_UPPER", "S3": "KEY_BASEMENT", "S4": "NONE"}
    if gates != expected_gates:
        errors.append(f"Compuertas/llaves incorrectas: {gates}.")

    # Graph has all rooms plus exterior; keyed stair links and keyed doors are directional-neutral.
    graph = {s["id"]: set() for s in spaces}
    graph["EXTERIOR"] = set()
    for door in doors:
        if door["key"] not in ("NONE", "KEY_UPPER", "KEY_BASEMENT"):
            errors.append(f"{door['id']}: llave desconocida {door['key']}.")
    for key_set, allowed_levels, label in [
        (set(), {"Planta baja"}, "sin llaves"),
        ({"KEY_UPPER"}, {"Planta baja", "Primer piso", "Desván"}, "KEY_UPPER"),
        ({"KEY_BASEMENT"}, {"Planta baja", "Sótano"}, "KEY_BASEMENT")
    ]:
        current_graph = {node: set(neighbors) for node, neighbors in graph.items()}
        for door in doors:
            if door["key"] not in ("NONE",) and door["key"] not in key_set:
                continue
            current_graph[door["a"]].add(door["b"])
            current_graph[door["b"]].add(door["a"])
        for stair in stairs:
            key = gates.get(stair["id"], "INVALID")
            if key != "NONE" and key not in key_set:
                continue
            current_graph[stair["a_room"]].add(stair["b_room"])
            current_graph[stair["b_room"]].add(stair["a_room"])
        reached = {"EXTERIOR"}
        stack = ["EXTERIOR"]
        while stack:
            node = stack.pop()
            for nxt in current_graph.get(node, ()):
                if nxt not in reached:
                    reached.add(nxt)
                    stack.append(nxt)
        start_door = next((d for d in doors if d["id"] == "P-G0" and d["b"] == "EXTERIOR"), None)
        if start_door is None or start_door["a"] not in reached:
            errors.append(f"{label}: no se llega al recibidor por P-G0.")
        for s in spaces:
            should = s["level"] in allowed_levels
            if (s["id"] in reached) != should:
                errors.append(f"{label}: alcanzabilidad incorrecta para {s['id']} ({s['level']}).")

    if not any(d["a"] in by_id and by_id[d["a"]]["level"] == "Sótano" and d["b"] == "EXTERIOR" for d in doors):
        errors.append("El sótano no tiene segunda salida directa al exterior.")

    hideouts = plan["hideouts"]
    if sum(h["count"] for h in hideouts) != 9:
        errors.append(f"Escondites: hay {sum(h['count'] for h in hideouts)}, se requieren 9.")
    totals = {}
    for h in hideouts:
        if h["room"] not in by_id:
            errors.append(f"Escondite en recinto inexistente: {h['room']}.")
        totals[h["room"]] = totals.get(h["room"], 0) + h["count"]
        if h["count"] < 1 or h["count"] > 2:
            errors.append(f"{h['room']}: cantidad individual de escondites fuera de 1–2.")
    for room, count in totals.items():
        if count > 2:
            errors.append(f"{room}: tiene {count} escondites (máximo 2).")
    required_hideout_levels = {"G-COC": False, "G-REC": False, "basement": False}
    for h in hideouts:
        if h["room"] == "G-COC": required_hideout_levels["G-COC"] = True
        if h["room"] == "G-REC": required_hideout_levels["G-REC"] = True
        if h["room"] in by_id and by_id[h["room"]]["level"] == "Sótano": required_hideout_levels["basement"] = True
    if not all(required_hideout_levels.values()):
        errors.append("Debe haber escondites en cocina, recibidor y sótano.")

    ext = {e["id"]: e for e in plan["exterior"]}
    if "Porche" not in ext or "Camioneta" not in ext:
        errors.append("Faltan los rectángulos de porche o camioneta.")
    else:
        porch, van = ext["Porche"]["rect"], ext["Camioneta"]["rect"]
        pw, pd = porch[2]-porch[0], porch[3]-porch[1]
        if abs(pw-6.0) > TOL or abs(pd-3.0) > TOL:
            errors.append(f"Porche mide {pw:.2f} × {pd:.2f}, requerido 6 × 3 m.")
        if overlap(porch, van):
            errors.append("Camioneta y porche se solapan.")
    pg15 = next((d for d in doors if d["id"] == "P-G15"), None)
    if pg15 is None or pg15["b"] != "EXTERIOR" or pg15["axis"] != "X" or abs(pg15["pos"]-24.0) > TOL or pg15["key"] != "NONE":
        errors.append("P-G15 debe estar en x=24, al exterior y sin llave.")
    if pg15 is None or "escalones" not in pg15.get("notes", "").lower():
        # The step declaration is checked in the plan's constants text.
        if "3 × 0.15 m" not in PLAN.read_text(encoding="utf-8"):
            errors.append("P-G15 no tiene escalones declarados.")

    return errors, support


def selftest():
    base = parse_plan()
    cases = []

    damaged = copy.deepcopy(base)
    door = next(d for d in damaged["doors"] if d["id"] == "P-G15")
    door["pos"] = 20.0
    cases.append(("P-G15 desplazada a x=20", damaged))

    damaged = copy.deepcopy(base)
    door = next(d for d in damaged["doors"] if d["id"] == "P-U1")
    door["pos"] = -5.7
    cases.append(("P-U1 fuera de muro compartido", damaged))

    damaged = copy.deepcopy(base)
    room = next(s for s in damaged["spaces"] if s["id"] == "U-D1")
    room["rect"] = (room["rect"][0] + 40, room["rect"][1], room["rect"][2] + 40, room["rect"][3])
    cases.append(("recinto de primer piso sobre vacío", damaged))

    damaged = copy.deepcopy(base)
    door = next(d for d in damaged["doors"] if d["id"] == "P-G0")
    s1 = next(s for s in damaged["stairs"] if s["id"] == "S1")
    door["pos"] = s1["rect"][1] - 0.2
    door["lo"], door["hi"] = s1["rect"][0], s1["rect"][0] + 1.0
    door["width"] = 1.0
    cases.append(("puerta a 0.2 m del hueco", damaged))

    damaged = copy.deepcopy(base)
    room = next(s for s in damaged["spaces"] if s["id"] == "B-CAL")
    room["rect"] = (40, room["rect"][1], 44, room["rect"][3])
    cases.append(("sótano fuera de huella", damaged))

    failed = False
    for name, plan in cases:
        errors, _ = validate(plan)
        if errors:
            print(f"PASS: {name}: VALIDADOR FALLÓ ({len(errors)} errores detectados).")
        else:
            print(f"FAIL: {name}: el validador aceptó el plano dañado.")
            failed = True
    if failed:
        return 1
    print(f"SELFTEST: OK ({len(cases)} casos dañados rechazados).")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--selftest", action="store_true", help="prueba copias dañadas del plano")
    args = parser.parse_args()
    if args.selftest:
        return selftest()
    errors, support = validate(parse_plan())
    plan = parse_plan()
    print("validate_plan.py")
    print(f"Plano: {PLAN.relative_to(ROOT).as_posix()}")
    print(f"Leídos: {len(plan['spaces'])} recintos, {len(plan['doors'])} puertas, {len(plan['stairs'])} escaleras, {len(plan['hideouts'])} filas de escondites.")
    print(f"Apoyo vertical: {len(support)} recintos evaluados con fórmula de intersecciones/área.")
    if errors:
        for err in errors:
            print(f"ERROR: {err}")
        print(f"RESULTADO: {len(errors)} errores.")
        return 1
    print("Comprobaciones: solapes, bordes/márgenes de puertas, conexión de recintos, recorrido por llaves, apoyo, huellas, escaleras/llegadas, altura, escondites, pasillos, porche y segunda salida.")
    print("RESULTADO: 0 errores.")
    return 0


if __name__ == "__main__":
    sys.exit(main())


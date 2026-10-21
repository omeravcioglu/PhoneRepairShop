"""
ENV_Workbench - the Milestone 1 repair corner (desk, repair mat, lamp, wall, cozy props).

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_env_workbench.py

Outputs:
    ArtSource/Blender/Environment/ENV_Workbench.blend
    Assets/_Project/Art/Models/Environment/ENV_Workbench.fbx
    ArtSource/Blender/Previews/ENV_Workbench.png

Design space: X = right, Y = away from the player, Z = up (metres). Unity gets (x, z, y).
Desk top surface is at 0.75 m, repair mat top at 0.754 m, mat centre at (0, -0.05).
"""

import math
import os
import sys

from mathutils import Euler, Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402

ROOT_NAME = "ENV_Workbench"
DESK_TOP = 0.75
MAT_CENTER = (0.0, -0.05)
MAT_SIZE = (0.50, 0.34)
MAT_TOP = DESK_TOP + 0.004


def make_materials():
    m = P.make_material
    return {
        "Floor": m("M_Env_Floor", "#B98B66", 0.7),
        "Wall": m("M_Env_Wall", "#F2E6D6", 0.9),
        "WoodDark": m("M_Env_WoodDark", "#8C5E43", 0.6),
        "Wood": m("M_Env_Wood", "#D4A577", 0.55),
        "Mat": m("M_Env_Mat", "#8ECDBD", 0.85),
        "MatLines": m("M_Env_MatLines", "#E8F7F1", 0.85),
        "Tray": m("M_Env_Tray", "#9CC9E8", 0.45),
        "Lamp": m("M_Env_Lamp", "#F7D58B", 0.4),
        "Metal": m("M_Env_Metal", "#C7CDD6", 0.3, 1.0),
        "Bulb": m("M_Env_Bulb", "#FFF2D6", 0.3, emission_strength=4.0),
        "Pegboard": m("M_Env_Pegboard", "#E8D2B0", 0.75),
        "PegHole": m("M_Env_PegHole", "#5B4636", 0.9),
        "Mug": m("M_Env_Mug", "#FFF4E8", 0.35),
        "MugStripe": m("M_Env_MugStripe", "#F4A6B8", 0.35),
        "Coffee": m("M_Env_Coffee", "#6B4A3A", 0.15),
        "Pot": m("M_Env_Pot", "#D98E6A", 0.8),
        "Soil": m("M_Env_Soil", "#5A4636", 0.95),
        "Plant": m("M_Env_Plant", "#7FBF8E", 0.6),
        "PlantLight": m("M_Env_PlantLight", "#A8DDB0", 0.6),
    }


def build_room(col, root, M):
    f = P.MeshBuilder()
    f.slab(P.rounded_rect(3.0, 3.0, 0.0, 1, center=(0.0, 0.2)), -0.02, 0.0)
    f.build("Floor", col, [M["Floor"]], parent=root)

    w = P.MeshBuilder()
    w.box((0.0, 0.45, 1.3), (3.2, 0.06, 2.6), mat=0)
    w.box((0.0, 0.412, 0.05), (3.2, 0.016, 0.1), mat=1)  # skirting board
    w.build("BackWall", col, [M["Wall"], M["WoodDark"]], parent=root, bevel=(0.003, 2))


def build_desk(col, root, M):
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(1.40, 0.70, 0.03, 8), DESK_TOP - 0.04, DESK_TOP, mat=0)
    leg_h = DESK_TOP - 0.04
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box((sx * 0.64, sy * 0.29, leg_h * 0.5), (0.05, 0.05, leg_h), mat=1)
    b.box((0.0, -0.29, leg_h - 0.04), (1.23, 0.03, 0.08), mat=1)   # aprons
    b.box((0.0, 0.29, leg_h - 0.04), (1.23, 0.03, 0.08), mat=1)
    b.box((-0.64, 0.0, leg_h - 0.04), (0.03, 0.53, 0.08), mat=1)
    b.box((0.64, 0.0, leg_h - 0.04), (0.03, 0.53, 0.08), mat=1)
    b.build("Desk", col, [M["Wood"], M["WoodDark"]], parent=root, bevel=(0.004, 3))


def build_repair_mat(col, root, M):
    cx, cy = MAT_CENTER
    sx, sy = MAT_SIZE
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(sx, sy, 0.018, 8, center=(cx, cy)), DESK_TOP, MAT_TOP, mat=0)
    z = MAT_TOP + 0.0002
    lw = 0.0018
    # border
    inset = 0.012
    x0, x1 = cx - sx * 0.5 + inset, cx + sx * 0.5 - inset
    y0, y1 = cy - sy * 0.5 + inset, cy + sy * 0.5 - inset
    for p0, p1 in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))):
        b.ribbon(p0, p1, lw, z, mat=1)
    # grid
    step = 0.04
    gx = x0 + step
    while gx < x1 - 0.005:
        b.ribbon((gx, y0 + 0.02), (gx, y1), lw * 0.5, z, mat=1)
        gx += step
    gy = y0 + 0.02 + step
    while gy < y1 - 0.005:
        b.ribbon((x0, gy), (x1, gy), lw * 0.5, z, mat=1)
        gy += step
    # ruler ticks along the front edge (1 cm, long every 5 cm)
    t = 0
    tx = x0 + 0.01
    while tx < x1 - 0.005:
        length = 0.012 if t % 5 == 0 else 0.006
        b.ribbon((tx, y0), (tx, y0 + length), lw * 0.6, z, mat=1)
        tx += 0.01
        t += 1
    b.build("RepairMat", col, [M["Mat"], M["MatLines"]], parent=root, bevel=(0.0015, 2))


def build_screw_tray(col, root, M):
    """Six-compartment tray ON the mat, right of the phone, so screws visibly land in it.
    Empties 'ScrewTray_Slot_0..5' mark the compartment floors (inner column first); Unity's
    PartsArea uses them as landing spots."""
    c = (0.165, 0.055)
    w, d, r = 0.13, 0.09, 0.012
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(w, d, r, 8), 0.0, 0.003)
    b.slab(P.rounded_rect(w, d, r, 8), 0.0, 0.016, holes=[P.rounded_rect(w - 0.008, d - 0.008, r - 0.004, 8)])
    b.box((0.0, 0.0, 0.007), (w - 0.006, 0.003, 0.014))
    for x in (-w / 6.0, w / 6.0):
        b.box((x, 0.0, 0.007), (0.003, d - 0.006, 0.014))
    tray = b.build("ScrewTray", col, [M["Tray"]], location_design=(c[0], c[1], MAT_TOP), parent=root,
                   bevel=(0.001, 2))

    columns = (-w / 3.0, 0.0, w / 3.0)
    rows = (-d / 4.0, d / 4.0)
    for k, (i, j) in enumerate([(0, 0), (0, 1), (1, 0), (1, 1), (2, 0), (2, 1)]):
        P.make_empty(f"ScrewTray_Slot_{k}", col, (columns[i], rows[j], 0.003), parent=tray)


def build_lamp(col, root, M):
    base = Vector((-0.46, 0.18, DESK_TOP))
    elbow = Vector((-0.44, 0.27, 1.10))
    head = Vector((-0.27, 0.06, 1.06))
    target = Vector((MAT_CENTER[0], MAT_CENTER[1], MAT_TOP))
    aim = (target - head).normalized()
    shade_len = 0.075
    wide = head + aim * shade_len

    b = P.MeshBuilder()
    rel = lambda v: tuple(v - base)  # noqa: E731
    b.cylinder((0.0, 0.0), 0.065, 0.0, 0.018, segments=40, mat=0)
    b.ellipsoid(rel(base + Vector((0, 0, 0.022))), (0.013, 0.013, 0.013), mat=1)
    b.cylinder_between(rel(base + Vector((0, 0, 0.02))), rel(elbow), 0.008, mat=0)
    b.ellipsoid(rel(elbow), (0.013, 0.013, 0.013), mat=1)
    b.cylinder_between(rel(elbow), rel(head), 0.008, mat=0)
    b.ellipsoid(rel(head), (0.016, 0.016, 0.016), mat=1)
    b.cylinder_between(rel(head), rel(wide), 0.024, segments=40, mat=0, radius_end=0.056)
    lamp = b.build("DeskLamp", col, [M["Lamp"], M["Metal"]], location_design=tuple(base), parent=root,
                   bevel=(0.002, 2))

    bulb_center = wide + aim * 0.0015
    bb = P.MeshBuilder()
    bb.cylinder_between(tuple(-aim * 0.001), tuple(aim * 0.001), 0.05, segments=40, mat=0)
    bb.build("DeskLamp_Bulb", col, [M["Bulb"]], location_design=tuple(bulb_center - base), parent=lamp)


def build_pegboard(col, root, M):
    c = Vector((0.0, 0.405, 1.12))
    w, h, t = 1.0, 0.5, 0.012
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.0), (w, t, h), mat=0)
    cols, rows = 20, 10
    for i in range(cols):
        for j in range(rows):
            x = -w * 0.5 + (i + 0.5) * w / cols
            z = -h * 0.5 + (j + 0.5) * h / rows
            b.cylinder_between((x, -t * 0.5 - 0.0004, z), (x, -t * 0.5 + 0.0006, z), 0.0035, segments=10, mat=1)
    b.build("Pegboard", col, [M["Pegboard"], M["PegHole"]], location_design=tuple(c), parent=root,
            bevel=(0.002, 2))


def build_mug(col, root, M):
    c = (0.50, 0.14)
    b = P.MeshBuilder()
    b.slab(P.circle(0.04, 40), 0.0, 0.10, holes=[P.circle(0.036, 40)], mat=0)
    b.cylinder((0.0, 0.0), 0.037, 0.0, 0.006, segments=40, mat=0)
    b.slab(P.circle(0.0405, 40), 0.055, 0.068, holes=[P.circle(0.0395, 40)], mat=1)
    b.cylinder((0.0, 0.0), 0.0362, 0.075, 0.08, segments=40, mat=2)
    pts = []
    for k in range(7):
        a = math.radians(-70 + k * (140 / 6))
        pts.append((0.04 + 0.022 * math.cos(a), 0.0, 0.05 + 0.028 * math.sin(a)))
    for p0, p1 in zip(pts, pts[1:]):
        b.cylinder_between(p0, p1, 0.006, segments=12, mat=0)
    b.build("Mug", col, [M["Mug"], M["MugStripe"], M["Coffee"]], location_design=(c[0], c[1], DESK_TOP),
            parent=root, bevel=(0.0012, 2))


def build_plant(col, root, M):
    c = (0.60, 0.25)
    b = P.MeshBuilder()
    b.cylinder((0.0, 0.0), 0.035, 0.0, 0.07, segments=36, mat=0, radius_top=0.046)
    b.slab(P.circle(0.05, 36), 0.062, 0.074, holes=[P.circle(0.041, 36)], mat=0)   # rim
    b.cylinder((0.0, 0.0), 0.042, 0.06, 0.066, segments=36, mat=1)                 # soil
    rng = P.seeded_random(3)
    for ring, (count, dist, tilt, size) in enumerate(((8, 0.026, 55, 1.0), (6, 0.014, 35, 0.8))):
        for k in range(count):
            yaw = 2 * math.pi * k / count + ring * 0.4
            rot = Euler((math.radians(tilt), 0.0, yaw - math.pi / 2), "XYZ").to_quaternion()
            pos = (math.cos(yaw) * dist, math.sin(yaw) * dist, 0.075 + ring * 0.006)
            s = size * rng.uniform(0.9, 1.1)
            b.ellipsoid(pos, (0.011 * s, 0.022 * s, 0.007 * s), mat=2 if ring == 0 else 3, u_segments=16,
                        v_segments=8, rotation=rot)
    b.ellipsoid((0.0, 0.0, 0.085), (0.008, 0.008, 0.012), mat=3, u_segments=12, v_segments=8)
    b.build("Plant", col, [M["Pot"], M["Soil"], M["Plant"], M["PlantLight"]], location_design=(c[0], c[1], DESK_TOP),
            parent=root, bevel=(0.0015, 2))


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)

    P.reset_scene()
    col = P.get_collection(ROOT_NAME)
    preview_col = P.get_collection("Preview")
    M = make_materials()
    root = P.make_empty(ROOT_NAME, col)

    build_room(col, root, M)
    build_desk(col, root, M)
    build_repair_mat(col, root, M)
    build_screw_tray(col, root, M)
    build_lamp(col, root, M)
    build_pegboard(col, root, M)
    build_mug(col, root, M)
    build_plant(col, root, M)

    fbx_path = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Environment", f"{ROOT_NAME}.fbx")
    P.export_fbx_for_unity(root, fbx_path)
    print(f"PRS_EXPORTED {fbx_path}")

    if "--no-preview" not in args:
        P.setup_preview_render()
        cam = P.preview_camera(preview_col, lens=32.0)
        out = os.path.join(project_root, "ArtSource", "Blender", "Previews", f"{ROOT_NAME}.png")
        print("PRS_PREVIEW " + P.render_from(cam, (0.0, -1.05, 1.35), (0.0, 0.05, 0.85), out))

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Environment", f"{ROOT_NAME}.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")


if __name__ == "__main__":
    main()

"""
ENV_PF_RepairBench - the Phone Fixer repair workstation (art direction v1, "Pocket Diorama").

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_pf_repair_bench.py
    (append "-- --no-preview" to skip the quick preview render)

Outputs:
    ArtSource/Blender/Environment/ENV_PF_RepairBench.blend
    Assets/_Project/Art/Models/Environment/PF/ENV_PF_RepairBench.fbx
    Assets/_Project/Art/Materials/PhoneFixer/pf_palette.json      (palette for Unity)

Layout (metres, origin = bench centre on the floor, +Y = towards the wall, top surface at 0.80):
    navy repair mat with grid, ruler and a printed 8-slot screw map (ScrewTray_Slot_0..7 empties),
    parts bento tray left (PartSpot_Big_*), tool caddy right (Caddy_Slot_*), kusuri-dansu parts
    drawers back-left, cream CRT diagnostic monitor back-right, articulated desk lamp (DeskLamp_Bulb
    marks the light), drawer cabinet under the right side, pegboard on the wall behind.
Interactive / animatable pieces are separate objects with pivots where they move.
"""

import math
import os
import sys

from mathutils import Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
import pf_bpy as PF  # noqa: E402

ROOT_NAME = "ENV_PF_RepairBench"
TOP = 0.80                    # work surface height
MAT_CENTER = (0.0, -0.03)     # where the phone lives
MAT_W, MAT_D, MAT_T = 0.62, 0.46, 0.005


def build_bench(col, root):
    M = PF.M
    b = P.MeshBuilder()
    PF.rounded_slab(b, 0.0, 0.0, 1.90, 0.80, 0.04, TOP - 0.05, TOP)
    b.box((0.0, 0.385, TOP + 0.022), (1.90, 0.03, 0.044))                       # back lip
    top = b.build("Bench_Top", col, [M("M_PF_WoodLight")], parent=root, bevel=(0.01, 3))

    f = P.MeshBuilder()
    for x, y in ((-0.88, -0.33), (-0.88, 0.33)):
        f.box((x, y, (TOP - 0.05) * 0.5), (0.065, 0.065, TOP - 0.05))           # left legs
    f.box((-0.27, -0.37, 0.70), (1.26, 0.025, 0.09))                            # front apron
    f.box((-0.27, 0.0, 0.155), (1.26, 0.66, 0.03))                              # lower shelf
    f.build("Bench_Frame", col, [M("M_PF_Wood")], parent=root, bevel=(0.008, 3))

    c = P.MeshBuilder()
    c.box((0.64, 0.0, 0.395), (0.56, 0.72, 0.71))                                # cabinet body
    c.box((0.64, 0.02, 0.02), (0.52, 0.66, 0.04), mat=1)                        # toe kick
    c.build("Bench_Cabinet", col, [M("M_PF_Wood"), M("M_PF_WoodDark")], parent=root, bevel=(0.01, 3))

    for i, (z0, z1) in enumerate(((0.52, 0.72), (0.30, 0.50), (0.07, 0.28))):
        cz = (z0 + z1) * 0.5
        d = P.MeshBuilder()
        d.box((0.0, -0.01, 0.0), (0.50, 0.02, z1 - z0))                         # drawer front
        PF.torus(d, (0.0, -0.03, 0.025), 0.022, 0.0045, mat=1, axis="Y", major_segments=20, minor_segments=8)
        d.box((0.0, -0.022, -0.035), (0.08, 0.004, 0.032), mat=1)               # label frame
        d.box((0.0, -0.025, -0.035), (0.07, 0.003, 0.024), mat=2)               # paper label
        d.build(f"Bench_Drawer_{i + 1:02d}", col, [M("M_PF_WoodLight"), M("M_PF_Brass"), M("M_PF_Paper")],
                location_design=(0.64, -0.36, cz), parent=root, bevel=(0.006, 2))

    # a cardboard parts box and a toolbox on the lower shelf
    s = P.MeshBuilder()
    s.box((-0.62, 0.02, 0.17 + 0.11), (0.30, 0.34, 0.22))
    s.box((-0.62, -0.152, 0.17 + 0.17), (0.30, 0.008, 0.05), mat=1)             # tape
    s.box((-0.18, 0.02, 0.17 + 0.075), (0.34, 0.22, 0.15), mat=2)               # toolbox
    s.box((-0.18, 0.02, 0.17 + 0.165), (0.12, 0.03, 0.03), mat=3)               # handle
    s.build("Bench_ShelfClutter", col, [M("M_PF_Cardboard"), M("M_PF_Butter"), M("M_PF_DustyBlue"), M("M_PF_Navy")],
            parent=root, bevel=(0.01, 2))
    return top


def build_mat(col, root):
    M = PF.M
    cx, cy = MAT_CENTER
    b = P.MeshBuilder()
    PF.rounded_slab(b, 0.0, 0.0, MAT_W, MAT_D, 0.022, 0.0, MAT_T)
    mat_obj = b.build("RepairMat", col, [M("M_PF_NavyMat")], location_design=(cx, cy, TOP), parent=root,
                      bevel=(0.0015, 2))

    g = P.MeshBuilder()
    z0, z1 = MAT_T, MAT_T + 0.0004
    half_w, half_d = MAT_W * 0.5 - 0.02, MAT_D * 0.5 - 0.02
    x = -0.28
    while x <= 0.2801:
        g.box((x, 0.0, (z0 + z1) * 0.5), (0.0012, half_d * 2.0, z1 - z0))
        x += 0.04
    y = -0.20
    while y <= 0.2001:
        g.box((0.0, y, (z0 + z1) * 0.5), (half_w * 2.0, 0.0012, z1 - z0))
        y += 0.04
    g.build("RepairMat_Grid", col, [M("M_PF_MatLines")], parent=mat_obj, smooth=False)

    pr = P.MeshBuilder()
    zp = MAT_T + 0.0006
    # ruler along the front edge
    for i in range(57):
        xx = -0.28 + i * 0.01
        long_tick = i % 5 == 0
        h = 0.012 if long_tick else 0.006
        pr.box((xx, -MAT_D * 0.5 + 0.012 + h * 0.5, zp), (0.0012, h, 0.0006))
    # screw map rings (2 rows x 4), back-left
    slots = []
    for row, sy in enumerate((0.175, 0.145)):
        for colm in range(4):
            sx = -0.265 + colm * 0.03
            PF.torus(pr, (sx, sy, zp), 0.0105, 0.0011, major_segments=24, minor_segments=6)
            slots.append((sx, sy))
    # brand crest ring + phone glyph, front-right
    PF.torus(pr, (0.255, -0.195, zp), 0.016, 0.0016, major_segments=28, minor_segments=6)
    pr.box((0.255, -0.195, zp), (0.009, 0.016, 0.0008))
    pr.build("RepairMat_Print", col, [M("M_PF_MatPrint")], parent=mat_obj, smooth=False)

    label = PF.text_object("FIXER MAT", 0.014, 0.0006, col, "RepairMat_Label", M("M_PF_MatPrint"), align_x="RIGHT",
                           offset=0.0004, parent=mat_obj, location=(0.23, -0.197, zp))
    for i, (sx, sy) in enumerate(slots):
        PF.text_object(str(i + 1), 0.0075, 0.0004, col, f"RepairMat_SlotNum_{i + 1}", M("M_PF_MatPrint"),
                       parent=mat_obj, location=(sx, sy + 0.0155, zp))
        PF.empty(f"ScrewTray_Slot_{i}", col, (sx, sy, MAT_T + 0.0008), parent=mat_obj, size=0.01)

    PF.empty("PhoneSpot", col, (0.0, 0.0, MAT_T), parent=mat_obj, size=0.04)
    for name, (x, y) in {
        "PartSpot_Medium_1": (0.205, 0.075), "PartSpot_Medium_2": (0.205, -0.075),
        "PartSpot_Medium_3": (-0.205, 0.06), "PartSpot_Medium_4": (-0.205, -0.09),
        "PartSpot_Small_1": (-0.12, -0.185), "PartSpot_Small_2": (0.0, -0.185), "PartSpot_Small_3": (0.12, -0.185),
    }.items():
        PF.empty(name, col, (x, y, MAT_T), parent=mat_obj, size=0.02)
    return mat_obj, label


def build_bento(col, root):
    M = PF.M
    w, d, h, wall = 0.34, 0.32, 0.035, 0.009
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.005), (w, d, 0.01))                                       # floor
    for sx in (-1, 1):
        b.box((sx * (w - wall) * 0.5, 0.0, h * 0.5), (wall, d, h))
    for sy in (-1, 1):
        b.box((0.0, sy * (d - wall) * 0.5, h * 0.5), (w, wall, h))
    b.box((0.0, 0.0, h * 0.42), (w, wall * 0.8, h * 0.84))                      # dividers
    b.box((0.0, 0.0, h * 0.42), (wall * 0.8, d, h * 0.84))
    tray = b.build("PartsBento", col, [M("M_PF_Wood")], location_design=(-0.53, -0.07, TOP), parent=root,
                   bevel=(0.003, 2))
    for i, (x, y) in enumerate(((-0.084, 0.078), (0.084, 0.078), (-0.084, -0.078), (0.084, -0.078))):
        PF.empty(f"PartSpot_Big_{i + 1}", col, (x, y, 0.0101), parent=tray, size=0.03)
    return tray


def build_caddy(col, root):
    M = PF.M
    w, d, h, wall = 0.24, 0.20, 0.10, 0.012
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.006), (w, d, 0.012))
    for sx in (-1, 1):
        b.box((sx * (w - wall) * 0.5, 0.0, h * 0.5), (wall, d, h))
    for sy in (-1, 1):
        b.box((0.0, sy * (d - wall) * 0.5, h * 0.5), (w, wall, h))
    for x in (-w / 6.0, w / 6.0):
        b.box((x, 0.0, h * 0.4), (wall * 0.7, d, h * 0.8))
    b.box((0.0, 0.0, h * 0.4), (w, wall * 0.7, h * 0.8))
    b.box((0.0, -d * 0.5 - 0.002, h * 0.55), (0.10, 0.004, 0.03), mat=1)        # name plate
    caddy = b.build("ToolCaddy", col, [M("M_PF_WoodWalnut"), M("M_PF_Paper")], location_design=(0.49, 0.03, TOP),
                    parent=root, bevel=(0.004, 2))
    names = ["Screwdriver", "Spudger", "Tweezers", "SuctionCup", "Brush", "Tester"]
    i = 0
    for y in (d * 0.25, -d * 0.25):
        for x in (-w / 3.0, 0.0, w / 3.0):
            PF.empty(f"Caddy_Slot_{names[i]}", col, (x, y, 0.012), parent=caddy, size=0.02)
            i += 1
    return caddy


def build_dansu(col, root):
    """Kusuri-dansu: an apothecary chest of small parts drawers, each its own object."""
    M = PF.M
    w, d, h = 0.36, 0.26, 0.38
    origin = (-0.74, 0.23, TOP)
    b = P.MeshBuilder()
    b.box((0.0, 0.0, h * 0.5), (w, d, h))
    b.box((0.0, 0.0, h + 0.008), (w + 0.02, d + 0.02, 0.016), mat=1)            # top board
    body = b.build("PartsDrawers", col, [M("M_PF_WoodWalnut"), M("M_PF_WoodDark")], location_design=origin,
                   parent=root, bevel=(0.008, 3))
    cell_w, cell_h, gap = 0.105, 0.105, 0.0125
    for r in range(3):
        for c in range(3):
            x = (c - 1) * (cell_w + gap)
            z = 0.035 + r * (cell_h + gap) + cell_h * 0.5
            dr = P.MeshBuilder()
            dr.box((0.0, -0.009, 0.0), (cell_w, 0.018, cell_h))
            PF.torus(dr, (0.0, -0.022, -0.012), 0.012, 0.0028, mat=1, axis="Y", major_segments=16, minor_segments=6)
            dr.box((0.0, -0.0185, 0.026), (0.06, 0.002, 0.022), mat=2)
            dr.build(f"PartsDrawer_{'ABC'[r]}{c + 1}", col, [M("M_PF_WoodLight"), M("M_PF_Brass"), M("M_PF_Paper")],
                     location_design=(x, -d * 0.5 + 0.004, z), parent=body, bevel=(0.004, 2))
    # succulent on top
    s = P.MeshBuilder()
    s.cylinder((0.08, 0.02), 0.04, h + 0.016, h + 0.086, segments=20, radius_top=0.046)
    s.cylinder((0.08, 0.02), 0.04, h + 0.08, h + 0.084, segments=20, mat=1)
    for i in range(7):
        a = 2.0 * math.pi * i / 7.0
        s.ellipsoid((0.08 + 0.022 * math.cos(a), 0.02 + 0.022 * math.sin(a), h + 0.098), (0.016, 0.016, 0.026),
                    mat=2, u_segments=10, v_segments=6)
    s.ellipsoid((0.08, 0.02, h + 0.112), (0.014, 0.014, 0.03), mat=3, u_segments=10, v_segments=6)
    s.build("PartsDrawers_Plant", col, [M("M_PF_Terracotta"), M("M_PF_Soil"), M("M_PF_Leaf"), M("M_PF_LeafLight")],
            parent=body)
    return body


def build_monitor(col, root):
    """Chunky cream CRT diagnostic monitor. The screen faces the player (-Y)."""
    M = PF.M
    origin = (0.57, 0.25, TOP)
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.03), (0.16, 0.14, 0.06), mat=1)                          # stand
    b.box((0.0, 0.0, 0.205), (0.36, 0.22, 0.29))                                 # front body
    b.box((0.0, 0.10, 0.20), (0.27, 0.12, 0.22))                                 # tapered back
    b.box((-0.11, -0.112, 0.08), (0.05, 0.006, 0.012), mat=2)                    # badge
    b.cylinder((0.13, -0.11), 0.009, 0.072, 0.088, segments=12, mat=3)           # power knob (lies along Z)
    for i in range(5):
        b.box((-0.08 + i * 0.04, 0.03, 0.352), (0.024, 0.11, 0.004), mat=1)      # vents
    monitor = b.build("DiagnosticMonitor", col,
                      [M("M_PF_CreamPlastic"), M("M_PF_Kraft"), M("M_PF_Brass"), M("M_PF_Butter")],
                      location_design=origin, parent=root, bevel=(0.012, 3))

    sc = P.MeshBuilder()
    sc.slab(P.rounded_rect(0.28, 0.20, 0.025, 8), -0.004, 0.0, axis="Y", offset=(0.0, 0.0, 0.0))
    screen = sc.build("Monitor_Screen", col, [M("M_PF_ScreenOff")], location_design=(0.0, -0.111, 0.215),
                      parent=monitor, bevel=(0.002, 2))
    tx = P.MeshBuilder()
    rows = [(-0.10, 0.07, 0.09, 0), (-0.10, 0.04, 0.15, 0), (-0.10, 0.01, 0.06, 0),
            (-0.10, -0.025, 0.12, 1), (-0.10, -0.06, 0.08, 0)]
    for x0, z, length, m in rows:
        tx.box((x0 + length * 0.5, -0.0045, z), (length, 0.001, 0.012), mat=m)
    tx.box((0.07, -0.0045, -0.06), (0.06, 0.001, 0.03), mat=0)                   # battery glyph
    tx.build("Monitor_ScreenText", col, [M("M_PF_ScreenText"), M("M_PF_ScreenWarn")], parent=screen, smooth=False)
    return monitor


def build_lamp(col, root):
    """Articulated lamp: every segment is its own object with its pivot on the joint."""
    M = PF.M
    base_pos = Vector((-0.42, 0.25, TOP))
    elbow = Vector((-0.40, 0.31, 1.27))
    head_joint = Vector((-0.13, 0.10, 1.24))
    aim = Vector((MAT_CENTER[0], MAT_CENTER[1], TOP)) - head_joint
    aim.normalize()

    b = P.MeshBuilder()
    b.cylinder((0.0, 0.0), 0.075, 0.0, 0.026, segments=32)
    b.ellipsoid((0.0, 0.0, 0.026), (0.03, 0.03, 0.02), mat=1, u_segments=16, v_segments=8)
    base = b.build("DeskLamp_Base", col, [M("M_PF_Navy"), M("M_PF_Brass")], location_design=base_pos, parent=root,
                   bevel=(0.006, 3))

    lo = P.MeshBuilder()
    PF.capsule(lo, (0.0, 0.0, 0.03), tuple(elbow - base_pos), 0.011)
    lo.ellipsoid(tuple(elbow - base_pos), (0.02, 0.02, 0.02), mat=1, u_segments=16, v_segments=8)
    lower = lo.build("DeskLamp_ArmLower", col, [M("M_PF_Navy"), M("M_PF_Brass")], location_design=(0, 0, 0),
                     parent=base)

    up = P.MeshBuilder()
    PF.capsule(up, (0.0, 0.0, 0.0), tuple(head_joint - elbow), 0.01)
    up.ellipsoid(tuple(head_joint - elbow), (0.018, 0.018, 0.018), mat=1, u_segments=16, v_segments=8)
    upper = up.build("DeskLamp_ArmUpper", col, [M("M_PF_Navy"), M("M_PF_Brass")],
                     location_design=tuple(elbow - base_pos), parent=lower)

    # shade: frustum along +Z, then rotated so +Z points along the aim
    rot = aim.to_track_quat("Z", "Y").to_matrix()
    hd = P.MeshBuilder()
    PF.shell_frustum(hd, 0.0, 0.10, 0.036, 0.08, 0.004, mat=0, inner_mat=2, segments=32)
    hd.frustum(-0.012, 0.004, 0.02, 0.036, segments=24, mat=1)
    head = hd.build("DeskLamp_Head", col, [M("M_PF_Butter"), M("M_PF_Navy"), M("M_PF_PaperCream")],
                    location_design=tuple(head_joint - elbow), parent=upper,
                    rotation_design=rot.to_quaternion(), bevel=(0.004, 2))
    bl = P.MeshBuilder()
    bl.ellipsoid((0.0, 0.0, 0.0), (0.028, 0.028, 0.028), u_segments=20, v_segments=10)
    PF_bulb = bl.build("DeskLamp_Bulb", col, [M("M_PF_LampBulb")], location_design=(0.0, 0.0, 0.07), parent=head)
    return base, PF_bulb


def build_teacup(col, root):
    M = PF.M
    b = P.MeshBuilder()
    b.cylinder((0.0, 0.0), 0.05, 0.0, 0.006, segments=28, mat=2)                 # cork coaster
    b.cylinder((0.0, 0.0), 0.033, 0.006, 0.086, segments=28, radius_top=0.037)   # yunomi
    b.cylinder((0.0, 0.0), 0.031, 0.07, 0.0868, segments=28, mat=1)              # tea
    return b.build("TeaCup", col, [M("M_PF_Sage"), M("M_PF_WoodDark"), M("M_PF_Cardboard")],
                   location_design=(0.33, 0.27, TOP), parent=root, bevel=(0.003, 2))


def build_desk_notes(col, root):
    M = PF.M
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.0005), (0.075, 0.075, 0.001))
    b.box((0.012, 0.02, 0.0018), (0.075, 0.075, 0.001), mat=1)
    b.box((0.0, -0.006, 0.0023), (0.05, 0.004, 0.0004), mat=2)
    b.box((0.0, -0.018, 0.0023), (0.04, 0.004, 0.0004), mat=2)
    notes = b.build("DeskNotes", col, [M("M_PF_Butter"), M("M_PF_Blush"), M("M_PF_Navy")],
                    location_design=(-0.27, -0.33, TOP), parent=root, smooth=False)
    notes.rotation_euler = (0.0, 0.0, math.radians(-8.0))
    pen = P.MeshBuilder()
    PF.capsule(pen, (-0.07, 0.0, 0.0045), (0.07, 0.0, 0.0045), 0.0045, mat=0, segments=8)
    pen.frustum(0.0, 0.012, 0.0045, 0.0005, segments=8, mat=1, center=(0.0, 0.0))
    p = pen.build("DeskPencil", col, [M("M_PF_Butter"), M("M_PF_Navy")], location_design=(-0.17, -0.33, TOP),
                  parent=root)
    p.rotation_euler = (0.0, 0.0, math.radians(14.0))
    return notes


def build_pegboard(col, root):
    """Wall board behind the bench with hanging tools, notes and a little parts shelf."""
    M = PF.M
    origin = (0.0, 0.445, 1.02)
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.40), (1.72, 0.014, 0.80))
    board = b.build("Pegboard", col, [M("M_PF_Kraft")], location_design=origin, parent=root, bevel=(0.006, 2))

    holes = P.MeshBuilder()
    for ix in range(27):
        for iz in range(13):
            PF.flat_disc_y(holes, -0.78 + ix * 0.06, -0.0074, 0.05 + iz * 0.06, 0.0055)
    holes.build("Pegboard_Holes", col, [M("M_PF_WoodDark")], parent=board, smooth=False)

    t = P.MeshBuilder()
    # three hanging spare screwdrivers
    for i, (x, m) in enumerate(((-0.62, 1), (-0.56, 2), (-0.50, 3))):
        t.cylinder_between((x, -0.03, 0.62), (x, -0.03, 0.70), 0.013, segments=16, mat=m)
        t.cylinder_between((x, -0.03, 0.54), (x, -0.03, 0.62), 0.003, segments=8, mat=0)
    # hooks
    for x in (-0.62, -0.56, -0.50, -0.30, 0.05, 0.30):
        t.cylinder_between((x, -0.007, 0.72), (x, -0.035, 0.72), 0.0025, segments=8, mat=0)
    # cable coil
    PF.torus(t, (-0.30, -0.03, 0.62), 0.07, 0.009, mat=2, axis="Y", major_segments=28, minor_segments=8)
    # clipboard with a repair note
    t.box((0.05, -0.016, 0.58), (0.16, 0.008, 0.22), mat=4)
    t.box((0.05, -0.021, 0.57), (0.135, 0.003, 0.18), mat=5)
    t.box((0.05, -0.024, 0.675), (0.05, 0.006, 0.02), mat=0)
    for k in range(5):
        t.box((0.04, -0.0235, 0.62 - k * 0.028), (0.09 if k % 2 == 0 else 0.07, 0.001, 0.006), mat=6)
    # pliers-like tweezers silhouette
    t.cylinder_between((0.29, -0.02, 0.72), (0.27, -0.02, 0.58), 0.004, segments=8, mat=0)
    t.cylinder_between((0.31, -0.02, 0.72), (0.33, -0.02, 0.58), 0.004, segments=8, mat=0)
    # magnifier hanging on its hook
    PF.torus(t, (0.30, -0.03, 0.47), 0.045, 0.006, mat=7, axis="Y", major_segments=28, minor_segments=8)
    t.cylinder_between((0.30, -0.03, 0.42), (0.30, -0.03, 0.30), 0.008, segments=10, mat=8)
    # sticky notes
    t.box((0.50, -0.009, 0.66), (0.075, 0.002, 0.075), mat=3)
    t.box((0.58, -0.009, 0.62), (0.075, 0.002, 0.075), mat=9)
    t.box((0.53, -0.012, 0.55), (0.075, 0.002, 0.075), mat=3)
    # little shelf with parts boxes
    t.box((0.62, -0.06, 0.26), (0.30, 0.10, 0.016), mat=8)
    for k, m in enumerate((10, 2, 10)):
        t.box((0.52 + k * 0.10, -0.06, 0.30), (0.08, 0.08, 0.07), mat=m)
    t.build("Pegboard_Items", col,
            [M("M_PF_Steel"), M("M_PF_Butter"), M("M_PF_DustyBlue"), M("M_PF_Sage"), M("M_PF_WoodDark"),
             M("M_PF_Paper"), M("M_PF_SteelDark"), M("M_PF_Brass"), M("M_PF_Wood"), M("M_PF_Blush"),
             M("M_PF_Cardboard")],
            parent=board, bevel=(0.002, 2))

    PF.text_object("Mrs. T - Thu", 0.012, 0.0005, col, "Pegboard_NoteText", M("M_PF_Navy"), parent=board,
                   location=(0.50, -0.0105, 0.665), rotation=(math.radians(90.0), 0.0, 0.0))
    return board


def summarize(root):
    import bpy
    deps = bpy.context.evaluated_depsgraph_get()
    rows = []
    for o in [root] + P.descendants(root):
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(deps)
        rows.append(f"{o.name:<26} verts={len(ev.data.vertices):>6}")
    return rows


def build(col):
    root = P.make_empty(ROOT_NAME, col)
    build_bench(col, root)
    build_mat(col, root)
    build_bento(col, root)
    build_caddy(col, root)
    build_dansu(col, root)
    build_monitor(col, root)
    build_lamp(col, root)
    build_teacup(col, root)
    build_desk_notes(col, root)
    build_pegboard(col, root)
    return root


def quick_preview(project_root, preview_col):
    import bpy
    out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "PF", f"{ROOT_NAME}_quick.png")
    PF.setup_beauty_render(resolution=(1600, 900), samples=32)
    PF.golden_hour_rig(preview_col, focus=(0.0, 0.0, TOP), scale=1.0, sun_from=(-0.6, -1.0, 0.7), sun_energy=3.2,
                       fill_energy=40.0)
    PF.add_light(preview_col, "PF_LampSpot", "SPOT", (-0.12, 0.08, 1.20), (0.0, -0.03, TOP), "#FFE2B0", 25.0,
                 spot_deg=70.0, shadow_soft=0.03)
    floor = PF.ground_plane(preview_col, "PreviewFloor", 8.0, 0.0, "M_PF_FloorOak")
    wall = P.MeshBuilder()
    wall.box((0.0, 0.47, 1.4), (4.0, 0.02, 2.8))
    wall.build("PreviewWall", preview_col, [PF.M("M_PF_Plaster")], smooth=False)
    cam = PF.camera(preview_col, lens=40.0)
    PF.render_still(cam, (0.0, -1.05, 1.62), (0.0, 0.05, 0.86), out)
    return out


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)
    P.reset_scene()
    print("PF_PALETTE " + PF.write_palette_json(project_root))
    col = P.get_collection(ROOT_NAME)
    preview_col = P.get_collection("Preview")
    root = build(col)

    fbx_path = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Environment", "PF", f"{ROOT_NAME}.fbx")
    P.export_fbx_for_unity(root, fbx_path)
    print(f"PRS_EXPORTED {fbx_path}")
    print(f"PF_TRIS {PF.count_tris(root)}")

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Environment", f"{ROOT_NAME}.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    if "--no-preview" not in args:
        print("PRS_PREVIEW " + quick_preview(project_root, preview_col))
    # previews live in their own collection; keep them out of the saved asset file
    for o in list(preview_col.objects):
        bpy.data.objects.remove(o)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")
    for row in summarize(root):
        print("PRS_PART " + row)


if __name__ == "__main__":
    main()

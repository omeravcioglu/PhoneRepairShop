"""
PHN_MochiOne - the first playable phone (Milestone 1).

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_phone_mochi_one.py
    (append "-- --no-preview" to skip the preview renders)

Outputs:
    ArtSource/Blender/Phones/PHN_MochiOne.blend                  editable source (modifiers kept live)
    Assets/_Project/Art/Models/Phones/MochiOne/PHN_MochiOne.fbx  Unity import
    ArtSource/Blender/Previews/PHN_MochiOne_*.png                 quick visual check

Gameplay-driven modelling rules:
    * Every clickable / replaceable part is its own object, parented directly to the root.
      Object names are SLOT IDS that Unity's PhoneModelDefinition maps to PartDefinitions.
    * Origins sit where interactions pivot: screws at the head base with local +Z pointing out
      (the unscrew direction), connectors at their hinge (flip-up axis), everything else centred.
    * Decorative sub-meshes (labels, logo, crack overlay, display) are CHILDREN of their part.
    * One phone = one root empty at the geometric centre, so rotation in Unity feels natural.
"""

import math
import os
import sys

from mathutils import Matrix

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
from prs_bpy import mm  # noqa: E402

ROOT_NAME = "PHN_MochiOne"

# ---- Dimensions in millimetres. Design space: X = right, Y = toward top edge, Z = out of screen.
W, L, R = 74.0, 152.0, 11.0          # outer footprint and corner radius (chubby on purpose)
BAND = 2.2                           # frame wall thickness
Z_BAND = (-5.4, 5.4)                 # frame spans the full 10.8 mm thickness
IN_W, IN_L, IN_R = W - 2 * BAND, L - 2 * BAND, R - BAND
GAP = 0.3                            # clearance around inset parts (reads as a seam line)
Z_COVER = (-5.2, -4.0)               # back cover (outer face at -5.2)
Z_MID = (1.6, 2.4)                   # midplate between internals and screen
Z_SCREEN = (2.4, 5.2)                # display assembly (glass top at 5.2)
CAM_X, CAM_Y = 17.0, 56.0            # rear camera: top-left when looking at the back

# Numbered as seen from the BACK, starting top-left, clockwise.
SCREWS = [
    ("Screw_01", (28.6, 67.6)),
    ("Screw_02", (-28.6, 67.6)),
    ("Screw_03", (-28.6, -67.6)),
    ("Screw_04", (28.6, -67.6)),
]


def make_materials():
    m = P.make_material
    return {
        "Frame": m("M_Phone_Frame", "#F3E9DA", 0.45),
        "Midplate": m("M_Phone_Midplate", "#B9C0C9", 0.35, 0.6),
        "Accent": m("M_Phone_Accent", "#EF8FA8", 0.45),
        "BackCover": m("M_Phone_BackCover", "#F4A7BE", 0.4),
        "Logo": m("M_Phone_Logo", "#FFF9F2", 0.5),
        "Blush": m("M_Phone_Blush", "#F27E9B", 0.5),
        "Ink": m("M_Phone_Ink", "#2B2433", 0.6),
        "Glass": m("M_Phone_Glass", "#14172A", 0.08),
        "Display": m("M_Phone_Display", "#232B4D", 0.15),
        "Crack": m("M_Phone_Crack", "#EAF6FF", 0.3),
        "Screw": m("M_Phone_Screw", "#C7CDD6", 0.3, 1.0),
        "ScrewRecess": m("M_Phone_ScrewRecess", "#3A3F4A", 0.6, 0.5),
        "Battery": m("M_Phone_Battery", "#4B5A78", 0.55),
        "BatteryLabel": m("M_Phone_BatteryLabel", "#FFD36E", 0.6),
        "PullTab": m("M_Phone_PullTab", "#F25C54", 0.6),
        "PCB": m("M_Phone_PCB", "#2E8C7E", 0.45),
        "Chip": m("M_Phone_Chip", "#2A2D3A", 0.4),
        "Metal": m("M_Phone_Metal", "#AEB6C1", 0.3, 1.0),
        "Gold": m("M_Phone_Gold", "#E7B95B", 0.3, 1.0),
        "Flex": m("M_Phone_Flex", "#E9A23B", 0.5),
        "CameraBody": m("M_Phone_CameraBody", "#30343F", 0.5),
        "Lens": m("M_Phone_Lens", "#0D1630", 0.05),
        "LensInner": m("M_Phone_LensInner", "#5B7BD6", 0.05),
        "Speaker": m("M_Phone_Speaker", "#4A505C", 0.6),
    }


# --------------------------------------------------------------------------------------
# Parts
# --------------------------------------------------------------------------------------

def build_frame(col, cutters, root, M):
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(W), mm(L), mm(R), 10), mm(Z_BAND[0]), mm(Z_BAND[1]),
           holes=[P.rounded_rect(mm(IN_W), mm(IN_L), mm(IN_R), 10)])
    frame = b.build("Frame", col, [M["Frame"]], parent=root, bevel=(mm(0.6), 3))

    # Slot in the bottom wall for the charging port (boolean keeps the frame mesh simple).
    cb = P.MeshBuilder()
    cb.slab(P.rounded_rect(mm(10.2), mm(4.0), mm(1.8), 6), mm(-78.0), mm(-72.5), axis="Y")
    cutter = cb.build("CUT_PortSlot", cutters, [], smooth=False)
    cutter.display_type = "WIRE"
    cutter.hide_render = True
    P.add_boolean(frame, cutter, "PortSlot")

    # Midplate + screw bosses (what the back-cover screws bite into).
    mb = P.MeshBuilder()
    mb.slab(P.rounded_rect(mm(IN_W + 0.6), mm(IN_L + 0.6), mm(IN_R + 0.3), 10), mm(Z_MID[0]), mm(Z_MID[1]))
    for _, (x, y) in SCREWS:
        mb.cylinder(mm(x, y), mm(2.0), mm(Z_COVER[1]), mm(Z_MID[0] + 0.05), segments=20)
        mb.flat_polygon(P.circle(mm(0.9), 16, mm(x, y)), mm(Z_COVER[1] - 0.03), mat=1, facing_up=False)
    mb.build("Frame_Midplate", col, [M["Midplate"], M["Ink"]], parent=frame, bevel=(mm(0.25), 2))

    # Side buttons: power on the right, volume on the left.
    bb = P.MeshBuilder()
    bb.box(mm(37.0, 29.0, 0.0), mm(1.6, 14.0, 2.6))
    bb.box(mm(-37.0, 37.0, 0.0), mm(1.6, 22.0, 2.6))
    bb.build("Frame_Buttons", col, [M["Accent"]], parent=frame, bevel=(mm(0.5), 3))
    return frame


def build_screen(col, root, M):
    zc = (Z_SCREEN[0] + Z_SCREEN[1]) * 0.5
    sw, sl, sr = IN_W - GAP, IN_L - GAP, IN_R - GAP * 0.5
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(sw), mm(sl), mm(sr), 10), mm(Z_SCREEN[0] - zc), mm(Z_SCREEN[1] - zc))
    screen = b.build("Screen", col, [M["Glass"]], location_design=mm(0, 0, zc), parent=root,
                     bevel=(mm(0.5), 3))

    dw, dl = sw - 3.2, sl - 3.2
    d = P.MeshBuilder()
    d.flat_polygon(P.rounded_rect(mm(dw), mm(dl), mm(sr - 1.6), 10), mm(5.25 - zc))
    d.build("Screen_Display", col, [M["Display"]], parent=screen, smooth=False)

    fc = P.MeshBuilder()
    fc.cylinder(mm(0.0, sl * 0.5 - 4.2), mm(1.5), mm(5.26 - zc), mm(5.30 - zc), segments=20)
    fc.build("Screen_FrontCam", col, [M["Ink"]], parent=screen)

    build_crack_overlay(col, screen, M, zc, dw, dl)
    build_display_connector(col, screen, (0.0, 0.0, zc), M)
    return screen


def build_crack_overlay(col, screen, M, zc, dw, dl):
    """Procedural spider-web crack. Lives on a child object so Unity can toggle it with
    the screen's condition (cracked / repaired)."""
    rng = P.seeded_random(11)
    b = P.MeshBuilder()
    z = mm(5.32 - zc)
    ix, iy = 13.0, 30.0
    lim_x, lim_y = dw * 0.5 - 1.2, dl * 0.5 - 1.2

    def inside(x, y):
        return abs(x) < lim_x and abs(y) < lim_y

    def crack_path(x, y, ang, max_len, width, branch_chance):
        travelled = 0.0
        while travelled < max_len:
            step = rng.uniform(2.5, 6.5)
            ang += rng.uniform(-0.32, 0.32)
            nx, ny = x + math.cos(ang) * step, y + math.sin(ang) * step
            if not inside(nx, ny):
                break
            b.ribbon(mm(x, y), mm(nx, ny), mm(width), z)
            if rng.random() < branch_chance:
                crack_path(nx, ny, ang + rng.choice((-1, 1)) * rng.uniform(0.5, 0.9),
                           rng.uniform(4.0, 12.0), max(0.2, width * 0.7), 0.0)
            x, y = nx, ny
            travelled += step
            width = max(0.22, width * 0.88)

    b.flat_polygon(P.circle(mm(1.0), 7, mm(ix, iy)), z)
    rays = 9
    angles = [2 * math.pi * i / rays + rng.uniform(-0.22, 0.22) for i in range(rays)]
    for ang in angles:
        crack_path(ix, iy, ang, rng.uniform(16.0, 58.0), 0.5, 0.22)

    for radius in (5.0, 11.5):
        pts = []
        for ang in angles:
            r = radius * rng.uniform(0.85, 1.15)
            pts.append((ix + math.cos(ang) * r, iy + math.sin(ang) * r))
        for i in range(rays):
            p0, p1 = pts[i], pts[(i + 1) % rays]
            if rng.random() < 0.75 and inside(*p0) and inside(*p1):
                mid = ((p0[0] + p1[0]) * 0.5, (p0[1] + p1[1]) * 0.5)
                ox, oy = mid[0] - ix, mid[1] - iy
                k = 1.0 + rng.uniform(0.05, 0.18)
                mid = (ix + ox * k, iy + oy * k)
                b.ribbon(mm(*p0), mm(*mid), mm(0.28), z)
                b.ribbon(mm(*mid), mm(*p1), mm(0.28), z)

    b.build("Screen_Crack", col, [M["Crack"]], parent=screen, smooth=False)


def build_back_cover(col, root, M):
    zc = (Z_COVER[0] + Z_COVER[1]) * 0.5
    cw, cl, cr = IN_W - GAP, IN_L - GAP, IN_R - GAP * 0.5
    holes = [P.circle(mm(7.0), 40, mm(CAM_X, CAM_Y))]
    holes += [P.circle(mm(1.25), 16, mm(x, y)) for _, (x, y) in SCREWS]
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(cw), mm(cl), mm(cr), 10), mm(Z_COVER[0] - zc), mm(Z_COVER[1] - zc), holes=holes)
    cover = b.build("BackCover", col, [M["BackCover"]], location_design=mm(0, 0, zc), parent=root,
                    bevel=(mm(0.45), 3))
    build_logo(col, cover, M, zc)
    return cover


def build_logo(col, cover, M, zc):
    """The Mochi Mobile mascot: a squishy mochi with a face. Embossed on the back."""
    cx, cy, rx, ry, h = 0.0, 18.0, 6.0, 5.0, 0.75

    def dome_z(x, y):
        u = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
        return Z_COVER[0] - h * math.sqrt(max(0.0, 1.0 - u))

    b = P.MeshBuilder()
    b.ellipsoid(mm(cx, cy, Z_COVER[0] - zc), mm(rx, ry, h), mat=0, u_segments=32, v_segments=12)
    for sx in (-1.0, 1.0):
        ex, ey = sx * 1.9, cy + 0.9
        z_out = dome_z(ex, ey) - 0.06
        b.cylinder(mm(ex, ey), mm(0.55), mm(z_out - zc), mm(z_out + 0.3 - zc), segments=16, mat=1)
        bx, by = sx * 3.6, cy - 0.7
        z_out = dome_z(bx, by) - 0.04
        b.cylinder(mm(bx, by), mm(0.95), mm(z_out - zc), mm(z_out + 0.3 - zc), segments=16, mat=2)
    smile = [(cx + 1.3 * math.cos(math.radians(a)), cy - 0.4 + 1.3 * math.sin(math.radians(a)))
             for a in range(205, 336, 26)]
    for p0, p1 in zip(smile, smile[1:]):
        z_out = min(dome_z(*p0), dome_z(*p1)) - 0.05
        b.ribbon(mm(*p0), mm(*p1), mm(0.42), mm(z_out - zc), mat=1, facing_up=False)
    b.build("BackCover_Logo", col, [M["Logo"], M["Ink"], M["Blush"]], parent=cover)


def build_screws(col, root, M):
    out_rotation = Matrix.Rotation(math.pi, 3, "X")  # local +Z points out of the back
    screws = []
    for name, (x, y) in SCREWS:
        b = P.MeshBuilder()
        b.cylinder((0.0, 0.0), mm(2.2), 0.0, mm(0.7), segments=28)          # pan head
        b.cylinder((0.0, 0.0), mm(0.95), mm(-4.2), mm(0.02), segments=16)   # shaft
        b.box(mm(0.0, 0.0, 0.68), mm(2.7, 0.55, 0.12), mat=1)              # cross recess
        b.box(mm(0.0, 0.0, 0.68), mm(0.55, 2.7, 0.12), mat=1)
        screws.append(b.build(name, col, [M["Screw"], M["ScrewRecess"]],
                              location_design=mm(x, y, Z_COVER[0]), parent=root,
                              rotation_design=out_rotation, bevel=(mm(0.32), 3)))
    return screws


def build_battery(col, root, M):
    z0, z1 = -2.9, 1.6
    c = (0.0, -20.0, (z0 + z1) * 0.5)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(52), mm(70), mm(4), 8), mm(z0 - c[2]), mm(z1 - c[2]))
    battery = b.build("Battery", col, [M["Battery"]], location_design=mm(*c), parent=root, bevel=(mm(0.6), 3))

    lb = P.MeshBuilder()
    lb.slab(P.rounded_rect(mm(44), mm(48), mm(2.5), 6, center=mm(0.0, 3.0)), mm(-3.0 - c[2]), mm(-2.86 - c[2]))
    lb.slab(P.rounded_rect(mm(44), mm(5), mm(1.0), 4, center=mm(0.0, 21.0)), mm(-3.04 - c[2]), mm(-2.95 - c[2]), mat=1)
    lb.build("Battery_Label", col, [M["BatteryLabel"], M["Ink"]], parent=battery)

    tb = P.MeshBuilder()
    tb.slab(P.rounded_rect(mm(10), mm(12), mm(2.0), 6, center=mm(0.0, -39.0)), mm(-3.0 - c[2]), mm(-2.9 - c[2]))
    tb.build("Battery_PullTab", col, [M["PullTab"]], parent=battery)

    build_battery_connector(col, battery, c, M)
    return battery


def build_battery_connector(col, battery, battery_center, M):
    """The battery's own flex cable: a CHILD of the battery, so it travels with it when the battery
    comes out. Origin on the hinge line; flips up around its local X axis to unplug."""
    hinge = (0.0, 15.0, -0.5)
    hx, hy, hz = hinge
    b = P.MeshBuilder()
    b.box(mm(0.0, 18.0 - hy, -0.545 - hz), mm(7.0, 9.0, 0.15), mat=0)   # flex ribbon
    b.box(mm(0.0, 21.5 - hy, 0.1 - hz), mm(9.0, 4.0, 1.0), mat=1)       # plug
    local = tuple(h - c for h, c in zip(hinge, battery_center))
    return b.build("BatteryConnector", col, [M["Flex"], M["Ink"]], location_design=mm(*local),
                   parent=battery, bevel=(mm(0.12), 2))


def build_display_connector(col, screen, screen_center, M):
    """The screen's flex cable: a CHILD of the screen. Origin on the hinge line at the board edge;
    flips up around its local Y axis (Unity local Z) to unplug."""
    hinge = (29.5, 28.5, -0.55)
    hx, hy, hz = hinge
    b = P.MeshBuilder()
    b.box(mm(20.5 - hx, 0.0, 0.1 - hz), mm(7.0, 5.0, 1.0), mat=1)       # plug
    b.box(mm(24.05 - hx, 0.0, -0.545 - hz), mm(11.1, 4.0, 0.15), mat=0)  # flex ribbon
    b.box(mm(29.575 - hx, 0.0, 0.73 - hz), mm(0.15, 4.0, 2.7), mat=0)   # bend down to the screen
    local = tuple(h - c for h, c in zip(hinge, screen_center))
    return b.build("DisplayConnector", col, [M["Flex"], M["Ink"]], location_design=mm(*local),
                   parent=screen, bevel=(mm(0.12), 2))


def build_logicboard(col, root, M):
    c = (0.0, 45.0, 1.1)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(52), mm(52), mm(2.5), 6), mm(0.6 - c[2]), mm(1.6 - c[2]), mat=0)

    def comp(x, y, sx, sy, h, mat):
        b.box(mm(x - c[0], y - c[1], 0.6 - h * 0.5 - c[2]), mm(sx, sy, h), mat=mat)

    comp(-12.0, 50.0, 12.0, 12.0, 1.0, 1)   # SoC
    comp(-12.0, 33.0, 10.0, 7.0, 0.8, 1)    # RAM
    comp(6.0, 36.0, 6.0, 6.0, 0.7, 1)       # PMIC
    comp(1.0, 62.0, 10.0, 12.0, 1.3, 2)     # shield can
    for i in range(4):
        comp(-22.0, 22.0 + i * 4.0, 1.6, 1.6, 0.12, 3)      # test pads
    for x in (8.0, 10.5, 13.0):
        comp(x, 24.0, 1.4, 0.7, 0.5, 1)                      # capacitors
    for x in (-1.5, 1.5):
        comp(x, 41.0, 1.0, 0.6, 0.5, 1)
    b.cylinder(mm(-20.0 - c[0], 60.0 - c[1]), mm(2.2), mm(0.52 - c[2]), mm(0.6 - c[2]), segments=20, mat=4)
    return b.build("Logicboard", col, [M["PCB"], M["Chip"], M["Metal"], M["Gold"], M["BatteryLabel"]],
                   location_design=mm(*c), parent=root, bevel=(mm(0.15), 2))


def build_charging_port(col, root, M):
    c = (0.0, -70.0, 0.0)
    b = P.MeshBuilder()
    b.box((0.0, 0.0, 0.0), mm(14.0, 7.0, 3.4), mat=0)
    b.slab(P.rounded_rect(mm(9.6), mm(3.4), mm(1.6), 6), mm(-76.05 - c[1]), mm(-73.4 - c[1]), axis="Y", mat=0)
    b.slab(P.rounded_rect(mm(8.0), mm(2.2), mm(1.0), 6), mm(-76.12 - c[1]), mm(-75.5 - c[1]), axis="Y", mat=1)
    return b.build("ChargingPort", col, [M["Metal"], M["Ink"]], location_design=mm(*c), parent=root,
                   bevel=(mm(0.2), 2))


def build_speaker(col, root, M):
    c = (-17.0, -62.0, -0.3)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(16), mm(8), mm(1.2), 6), mm(-1.8 - c[2]), mm(1.2 - c[2]), mat=0)
    b.slab(P.rounded_rect(mm(12), mm(4.6), mm(0.9), 6), mm(-1.88 - c[2]), mm(-1.76 - c[2]), mat=1)
    return b.build("Speaker", col, [M["Speaker"], M["Ink"]], location_design=mm(*c), parent=root,
                   bevel=(mm(0.2), 2))


def build_rear_camera(col, root, M):
    c = (CAM_X, CAM_Y, 0.2)
    z = lambda v: mm(v - c[2])  # noqa: E731
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(14), mm(14), mm(2.5), 6), z(-0.2), z(0.6), mat=0)          # base plate
    b.cylinder((0.0, 0.0), mm(5.4), z(-4.9), z(-0.2), segments=32, mat=0)               # barrel
    b.slab(P.circle(mm(6.8), 40), z(-5.32), z(-4.0), holes=[P.circle(mm(5.0), 40)], mat=1)  # bezel ring
    b.cylinder((0.0, 0.0), mm(5.0), z(-5.05), z(-4.95), segments=40, mat=2)             # lens glass
    b.cylinder((0.0, 0.0), mm(2.4), z(-5.09), z(-5.03), segments=32, mat=3)             # inner element
    return b.build("RearCamera", col, [M["CameraBody"], M["Metal"], M["Lens"], M["LensInner"]],
                   location_design=mm(*c), parent=root, bevel=(mm(0.15), 2))


# --------------------------------------------------------------------------------------
# Previews
# --------------------------------------------------------------------------------------

EXPLODE_Z = {  # design-space Z offsets (mm) for the exploded preview (connectors ride on their parents)
    "Screen": 18.0, "Logicboard": -6.0, "Battery": -12.0,
    "RearCamera": -12.0, "Speaker": -12.0, "ChargingPort": -12.0,
    "BackCover": -28.0, "Screw_01": -38.0, "Screw_02": -38.0, "Screw_03": -38.0, "Screw_04": -38.0,
}


def render_previews(project_root, root, preview_col):
    import bpy
    out = os.path.join(project_root, "ArtSource", "Blender", "Previews")
    P.setup_preview_render()
    cam = P.preview_camera(preview_col, lens=60.0)
    files = []
    files.append(P.render_from(cam, (0.11, -0.22, 0.30), (0, 0, 0), os.path.join(out, "PHN_MochiOne_front.png")))
    files.append(P.render_from(cam, (-0.11, -0.22, -0.30), (0, 0, 0), os.path.join(out, "PHN_MochiOne_back.png")))

    hidden = [bpy.data.objects[n] for n in ("BackCover",) + tuple(s for s, _ in SCREWS)]
    hidden_all = []
    for o in hidden:
        hidden_all += [o] + P.descendants(o)
    for o in hidden_all:
        o.hide_render = True
    files.append(P.render_from(cam, (-0.10, -0.20, -0.30), (0, 0, 0), os.path.join(out, "PHN_MochiOne_open.png")))
    for o in hidden_all:
        o.hide_render = False

    original = {}
    for name, dz in EXPLODE_Z.items():
        o = bpy.data.objects[name]
        original[name] = o.location.copy()
        o.location.z += mm(dz)
    files.append(P.render_from(cam, (-0.30, -0.36, -0.20), (0, 0, mm(-8.0)), os.path.join(out, "PHN_MochiOne_exploded.png")))
    for name, loc in original.items():
        bpy.data.objects[name].location = loc
    return files


def summarize(root):
    import bpy
    deps = bpy.context.evaluated_depsgraph_get()
    rows = []
    for o in [root] + P.descendants(root):
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(deps)
        dims = [round(d * 1000.0, 2) for d in ev.dimensions]
        rows.append(f"{o.name:<20} verts={len(ev.data.vertices):>5}  dims(mm)={dims}")
    return rows


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)

    P.reset_scene()
    col = P.get_collection(ROOT_NAME)
    cutters = P.get_collection("Cutters")
    preview_col = P.get_collection("Preview")
    M = make_materials()

    root = P.make_empty(ROOT_NAME, col)
    build_frame(col, cutters, root, M)
    build_screen(col, root, M)
    build_back_cover(col, root, M)
    build_screws(col, root, M)
    build_battery(col, root, M)          # + BatteryConnector (child)
    build_logicboard(col, root, M)
    build_charging_port(col, root, M)
    build_speaker(col, root, M)
    build_rear_camera(col, root, M)

    fbx_path = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Phones", "MochiOne", f"{ROOT_NAME}.fbx")
    P.export_fbx_for_unity(root, fbx_path)
    print(f"PRS_EXPORTED {fbx_path}")

    if "--no-preview" not in args:
        for f in render_previews(project_root, root, preview_col):
            print(f"PRS_PREVIEW {f}")

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Phones", f"{ROOT_NAME}.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")
    for row in summarize(root):
        print("PRS_PART " + row)


if __name__ == "__main__":
    main()

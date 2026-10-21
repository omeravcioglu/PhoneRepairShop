"""
PHN_MochiDango - Phone Fixer's hero phone (art direction v1).

Mochi makes soft, chunky, repair-friendly phones named after wagashi. The Dango's signature is three
rear lenses on a brass "skewer" in hanami-dango colours (pink, cream, matcha) on a cream island.

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_phone_mochi_dango.py
    (append "-- --no-preview" to skip the renders)

Outputs:
    ArtSource/Blender/Phones/PHN_MochiDango.blend
    Assets/_Project/Art/Models/Phones/MochiDango/PHN_MochiDango.fbx
    ArtSource/Blender/Previews/PF/PHN_MochiDango_*.png

Same contract as PHN_MochiOne (see ArtSource/Blender/README.md):
    * every clickable / replaceable part is its own object, parented to the root; object names are
      slot ids (Frame, Screen, BackCover, Screw_01..04, Battery, BatteryConnector, DisplayConnector,
      Logicboard, ChargingPort, RearCamera, Speaker)
    * screws: origin at the head base, local +Z out of the back; connectors: origin on the hinge
    * decorations and DAMAGE STATES are children of their part, named <Part>_<State>, so Unity can
      toggle them per fault: Screen_Crack, Battery_Bulge, Battery_Warning, ChargingPort_Lint,
      Logicboard_Corrosion, DisplayConnector_Tear, RearCamera_Scratch
Size: 76 x 156 x 11 mm. Design space: X = right, Y = toward the top edge, Z = out of the screen.
"""

import math
import os
import sys

from mathutils import Matrix

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
import pf_bpy as PF  # noqa: E402
from prs_bpy import mm  # noqa: E402

ROOT_NAME = "PHN_MochiDango"

W, L, R = 76.0, 156.0, 12.0
BAND = 2.2
Z_BAND = (-5.5, 5.5)
IN_W, IN_L, IN_R = W - 2 * BAND, L - 2 * BAND, R - BAND
GAP = 0.3
Z_COVER = (-5.3, -4.1)
Z_MID = (1.6, 2.4)
Z_SCREEN = (2.4, 5.3)
ISLAND = (17.0, 46.0, 20.0, 54.0, 10.0)        # cx, cy, w, h, r (on the back, top-left seen from the back)
LENS_Y = (62.0, 46.0, 30.0)                      # pink, cream, matcha
LENS_RING_MATS = ("M_PF_RingPink", "M_PF_RingCream", "M_PF_RingMatcha")

SCREWS = [
    ("Screw_01", (30.4, 70.4)),
    ("Screw_02", (-30.4, 70.4)),
    ("Screw_03", (-30.4, -70.4)),
    ("Screw_04", (30.4, -70.4)),
]


def M(name):
    return PF.M(name)


# --------------------------------------------------------------------------------------
# Frame
# --------------------------------------------------------------------------------------

def build_frame(col, cutters, root):
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(W), mm(L), mm(R), 12), mm(Z_BAND[0]), mm(Z_BAND[1]),
           holes=[P.rounded_rect(mm(IN_W), mm(IN_L), mm(IN_R), 12)])
    frame = b.build("Frame", col, [M("M_PF_PhoneFrame")], parent=root, bevel=(mm(0.9), 3))

    cb = P.MeshBuilder()
    cb.slab(P.rounded_rect(mm(10.6), mm(4.2), mm(1.9), 6), mm(-80.0), mm(-74.5), axis="Y")
    cutter = cb.build("CUT_PortSlot", cutters, [], smooth=False)
    cutter.display_type = "WIRE"
    cutter.hide_render = True
    P.add_boolean(frame, cutter, "PortSlot")

    mb = P.MeshBuilder()
    mb.slab(P.rounded_rect(mm(IN_W + 0.6), mm(IN_L + 0.6), mm(IN_R + 0.3), 12), mm(Z_MID[0]), mm(Z_MID[1]))
    for _, (x, y) in SCREWS:
        mb.cylinder(mm(x, y), mm(2.2), mm(Z_COVER[1]), mm(Z_MID[0] + 0.05), segments=20)
        mb.flat_polygon(P.circle(mm(1.0), 16, mm(x, y)), mm(Z_COVER[1] - 0.03), mat=1, facing_up=False)
    mb.build("Frame_Midplate", col, [M("M_PF_Steel"), M("M_PF_Charcoal")], parent=frame, bevel=(mm(0.25), 2))

    bb = P.MeshBuilder()
    bb.box(mm(38.2, 30.0, 0.0), mm(1.8, 15.0, 3.0), mat=0)                     # power
    bb.box(mm(-38.2, 38.0, 0.0), mm(1.8, 24.0, 3.0), mat=0)                    # volume
    bb.box(mm(-38.2, 10.0, 0.0), mm(1.8, 9.0, 3.0), mat=1)                     # matcha "mochi key"
    bb.build("Frame_Buttons", col, [M("M_PF_PhoneFrame"), M("M_PF_RingMatcha")], parent=frame, bevel=(mm(0.6), 3))

    st = P.MeshBuilder()
    st.box(mm(38.25, -14.0, -1.0), mm(0.5, 16.0, 2.4), mat=0)                  # SIM tray face
    st.cylinder(mm(38.6, -21.0), mm(0.45), mm(-1.4), mm(-0.6), segments=8, mat=1)
    st.build("Frame_SimTray", col, [M("M_PF_Steel"), M("M_PF_Charcoal")], parent=frame, bevel=(mm(0.15), 2))

    fx = P.MeshBuilder()
    fx.box(mm(31.6, -21.0, -0.4), mm(4.6, 74.0, 0.18), mat=0)                  # main flex along the side
    fx.box(mm(31.6, 16.8, -0.1), mm(5.6, 4.0, 0.8), mat=1)                    # its plug onto the board
    fx.cylinder(mm(24.0, -64.0), mm(4.0), mm(-2.6), mm(0.6), segments=20, mat=2)    # vibration motor
    fx.build("Frame_Internals", col, [M("M_PF_Kapton"), M("M_PF_Chip"), M("M_PF_SteelDark")], parent=frame,
             bevel=(mm(0.15), 2))
    return frame


# --------------------------------------------------------------------------------------
# Screen (+ display connector)
# --------------------------------------------------------------------------------------

def build_screen(col, root):
    zc = (Z_SCREEN[0] + Z_SCREEN[1]) * 0.5
    sw, sl, sr = IN_W - GAP, IN_L - GAP, IN_R - GAP * 0.5
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(sw), mm(sl), mm(sr), 12), mm(Z_SCREEN[0] - zc), mm(Z_SCREEN[1] - zc))
    screen = b.build("Screen", col, [M("M_PF_PhoneGlass")], location_design=mm(0, 0, zc), parent=root,
                     bevel=(mm(0.6), 3))

    dw, dl = sw - 3.4, sl - 3.4
    d = P.MeshBuilder()
    d.flat_polygon(P.rounded_rect(mm(dw), mm(dl), mm(sr - 1.7), 12), mm(5.35 - zc))
    d.build("Screen_Display", col, [M("M_PF_PhoneDisplay")], parent=screen, smooth=False)

    fc = P.MeshBuilder()
    fc.cylinder(mm(0.0, sl * 0.5 - 4.4), mm(1.6), mm(5.36 - zc), mm(5.40 - zc), segments=20)
    fc.build("Screen_FrontCam", col, [M("M_PF_LensDark")], parent=screen)

    build_crack_overlay(col, screen, zc, dw, dl)
    build_display_connector(col, screen, (0.0, 0.0, zc))
    return screen


def build_crack_overlay(col, screen, zc, dw, dl):
    rng = P.seeded_random(23)
    b = P.MeshBuilder()
    z = mm(5.42 - zc)
    ix, iy = -14.0, 28.0
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
                           rng.uniform(4.0, 12.0), max(0.22, width * 0.7), 0.0)
            x, y = nx, ny
            travelled += step
            width = max(0.25, width * 0.88)

    b.flat_polygon(P.circle(mm(1.2), 7, mm(ix, iy)), z)
    rays = 10
    angles = [2 * math.pi * i / rays + rng.uniform(-0.22, 0.22) for i in range(rays)]
    for ang in angles:
        crack_path(ix, iy, ang, rng.uniform(18.0, 62.0), 0.55, 0.22)
    for radius in (5.5, 12.0):
        pts = []
        for ang in angles:
            r = radius * rng.uniform(0.85, 1.15)
            pts.append((ix + math.cos(ang) * r, iy + math.sin(ang) * r))
        for i in range(rays):
            p0, p1 = pts[i], pts[(i + 1) % rays]
            if rng.random() < 0.75 and inside(*p0) and inside(*p1):
                b.ribbon(mm(*p0), mm(*p1), mm(0.3), z)
    b.build("Screen_Crack", col, [M("M_PF_PhoneCrack")], parent=screen, smooth=False)


def build_display_connector(col, screen, screen_center):
    hinge = (33.0, 26.0, -0.55)
    hx, hy, hz = hinge
    b = P.MeshBuilder()
    b.box(mm(29.0 - hx, 0.0, 0.1 - hz), mm(7.0, 5.2, 1.0), mat=1)             # plug (on the board socket)
    b.box(mm(30.6 - hx, 0.0, -0.545 - hz), mm(5.2, 4.2, 0.16), mat=0)         # ribbon
    b.box(mm(33.1 - hx, 0.0, 0.73 - hz), mm(0.16, 4.2, 2.7), mat=0)           # bend down to the screen
    local = tuple(h - c for h, c in zip(hinge, screen_center))
    conn = b.build("DisplayConnector", col, [M("M_PF_Kapton"), M("M_PF_Chip")], location_design=mm(*local),
                   parent=screen, bevel=(mm(0.12), 2))
    t = P.MeshBuilder()                                                        # damage: torn flex, bent pins
    t.box(mm(31.4 - hx, 1.0, -0.43 - hz), mm(2.2, 2.2, 0.06), mat=0)
    for k in range(4):
        t.box(mm(26.6 - hx + k * 0.9, -2.9, 0.2 - hz + k * 0.12), mm(0.25, 1.4, 0.25), mat=1)
    t.build("DisplayConnector_Tear", col, [M("M_PF_WoodDark"), M("M_PF_PCBGold")], parent=conn, smooth=False)
    return conn


# --------------------------------------------------------------------------------------
# Back cover (+ camera island, logo)
# --------------------------------------------------------------------------------------

def build_back_cover(col, root):
    zc = (Z_COVER[0] + Z_COVER[1]) * 0.5
    cw, cl, cr = IN_W - GAP, IN_L - GAP, IN_R - GAP * 0.5
    icx, icy, iw, ih, ir = ISLAND
    holes = [P.circle(mm(6.4), 40, mm(icx, y)) for y in LENS_Y]
    holes += [P.circle(mm(1.35), 16, mm(x, y)) for _, (x, y) in SCREWS]
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(cw), mm(cl), mm(cr), 12), mm(Z_COVER[0] - zc), mm(Z_COVER[1] - zc), holes=holes)
    cover = b.build("BackCover", col, [M("M_PF_PhoneSakura")], location_design=mm(0, 0, zc), parent=root,
                    bevel=(mm(0.5), 3))

    isl = P.MeshBuilder()
    isl.slab(P.rounded_rect(mm(iw), mm(ih), mm(ir), 10, center=mm(icx, icy)), mm(Z_COVER[0] - 0.9 - zc),
             mm(Z_COVER[0] + 0.05 - zc), holes=[P.circle(mm(6.4), 40, mm(icx, y)) for y in LENS_Y])
    isl.box(mm(icx, icy, Z_COVER[0] - 0.95 - zc), mm(1.2, ih - 10.0, 0.2), mat=1)          # brass skewer
    isl.cylinder(mm(icx - 13.5, LENS_Y[0] + 2.0), mm(2.6), mm(Z_COVER[0] - 0.5 - zc), mm(Z_COVER[0] + 0.05 - zc),
                 segments=20, mat=2)                                                          # flash
    isl.build("BackCover_CameraIsland", col, [M("M_PF_RingCream"), M("M_PF_Brass"), M("M_PF_WindowGlow")],
              parent=cover, bevel=(mm(0.35), 3))
    build_logo(col, cover, zc)
    return cover


def build_logo(col, cover, zc):
    """Embossed mochi mascot + wordmark."""
    cx, cy, rx, ry, h = 0.0, -6.0, 9.0, 6.5, 0.8
    b = P.MeshBuilder()
    b.ellipsoid(mm(cx, cy, Z_COVER[0] - zc), mm(rx, ry, h), mat=0, u_segments=32, v_segments=10)
    for sx in (-1.0, 1.0):
        z_out = Z_COVER[0] - h * 0.86
        b.cylinder(mm(cx + sx * 2.6, cy + 1.0), mm(0.6), mm(z_out - 0.3 - zc), mm(z_out + 0.1 - zc), segments=12,
                   mat=1)
    b.build("BackCover_Logo", col, [M("M_PF_PhoneLogo"), M("M_PF_Mouth")], parent=cover)
    PF.text_object("mochi", mm(8.5), mm(0.3), col, "BackCover_Wordmark", M("M_PF_PhoneLogo"), offset=mm(0.25),
                   parent=cover, location=mm(0.0, -21.0, Z_COVER[0] - 0.1 - zc),
                   rotation=(math.radians(180.0), 0.0, math.radians(180.0)))


# --------------------------------------------------------------------------------------
# Screws
# --------------------------------------------------------------------------------------

def build_screws(col, root):
    out_rotation = Matrix.Rotation(math.pi, 3, "X")
    for name, (x, y) in SCREWS:
        b = P.MeshBuilder()
        b.cylinder((0.0, 0.0), mm(2.3), 0.0, mm(0.8), segments=28)
        b.ellipsoid(mm(0.0, 0.0, 0.8), mm(2.3, 2.3, 0.5), u_segments=28, v_segments=6)
        b.cylinder((0.0, 0.0), mm(1.0), mm(-4.2), mm(0.02), segments=16)
        b.box(mm(0.0, 0.0, 1.18), mm(2.8, 0.6, 0.3), mat=1)
        b.box(mm(0.0, 0.0, 1.18), mm(0.6, 2.8, 0.3), mat=1)
        b.build(name, col, [M("M_PF_Steel"), M("M_PF_Charcoal")], location_design=mm(x, y, Z_COVER[0]),
                parent=root, rotation_design=out_rotation, bevel=(mm(0.3), 3))


# --------------------------------------------------------------------------------------
# Battery (+ connector, damage)
# --------------------------------------------------------------------------------------

def build_battery(col, root):
    z0, z1 = -3.0, 1.6
    c = (0.0, -22.0, (z0 + z1) * 0.5)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(54), mm(72), mm(4.5), 8), mm(z0 - c[2]), mm(z1 - c[2]))
    battery = b.build("Battery", col, [M("M_PF_BatteryWrap")], location_design=mm(*c), parent=root,
                      bevel=(mm(0.7), 3))

    lb = P.MeshBuilder()
    lb.slab(P.rounded_rect(mm(54.2), mm(11.0), mm(1.0), 4, center=mm(0.0, 14.0)), mm(-3.06 - c[2]),
            mm(-2.95 - c[2]))
    lb.build("Battery_Label", col, [M("M_PF_Navy")], parent=battery)
    PF.text_object("MOCHI  4100 mAh", mm(4.2), mm(0.08), col, "Battery_LabelText", M("M_PF_Paper"),
                   parent=battery, location=mm(0.0, 14.0, -3.1 - c[2]),
                   rotation=(math.radians(180.0), 0.0, math.radians(180.0)))
    mascot = P.MeshBuilder()
    mascot.ellipsoid(mm(0.0, -12.0, -3.02 - c[2]), mm(6.0, 4.2, 0.25), u_segments=24, v_segments=6)
    mascot.build("Battery_Mascot", col, [M("M_PF_Paper")], parent=battery)

    tb = P.MeshBuilder()
    for x in (-15.0, 15.0):
        tb.slab(P.rounded_rect(mm(10), mm(10), mm(2.0), 6, center=mm(x, -30.0)), mm(-3.06 - c[2]), mm(-2.95 - c[2]))
    tb.build("Battery_PullTab", col, [M("M_PF_Butter")], parent=battery)

    bu = P.MeshBuilder()                                                         # damage: swelling
    bu.ellipsoid(mm(0.0, -9.0, -3.0 - c[2]), mm(18.0, 20.0, 1.05), u_segments=40, v_segments=12)
    bu.build("Battery_Bulge", col, [M("M_PF_BatteryWrap")], parent=battery)
    wn = P.MeshBuilder()
    wn.slab(P.rounded_rect(mm(9.0), mm(9.0), mm(1.5), 6, center=mm(18.5, -2.0)), mm(-3.1 - c[2]), mm(-3.02 - c[2]))
    wn.box(mm(18.5, -1.2, -3.13 - c[2]), mm(1.1, 4.0, 0.06), mat=1)
    wn.box(mm(18.5, -4.6, -3.13 - c[2]), mm(1.1, 1.1, 0.06), mat=1)
    wn.build("Battery_Warning", col, [M("M_PF_Damage"), M("M_PF_Navy")], parent=battery, smooth=False)

    build_battery_connector(col, battery, c)
    return battery


def build_battery_connector(col, battery, battery_center):
    hinge = (0.0, 13.5, -0.5)
    hx, hy, hz = hinge
    b = P.MeshBuilder()
    b.box(mm(0.0, 17.0 - hy, -0.545 - hz), mm(7.0, 9.0, 0.16), mat=0)
    b.box(mm(0.0, 20.5 - hy, 0.1 - hz), mm(9.0, 4.0, 1.0), mat=1)
    local = tuple(h - c for h, c in zip(hinge, battery_center))
    return b.build("BatteryConnector", col, [M("M_PF_Kapton"), M("M_PF_Chip")], location_design=mm(*local),
                   parent=battery, bevel=(mm(0.12), 2))


# --------------------------------------------------------------------------------------
# Logic board (+ shield, corrosion)
# --------------------------------------------------------------------------------------

def build_logicboard(col, root):
    c = (0.0, 47.0, 1.1)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(66), mm(56), mm(3.0), 6), mm(0.6 - c[2]), mm(1.6 - c[2]), mat=0)

    def comp(x, y, sx, sy, h, mat):
        b.box(mm(x - c[0], y - c[1], 0.6 - h * 0.5 - c[2]), mm(sx, sy, h), mat=mat)

    comp(-16.0, 36.0, 10.0, 7.0, 0.8, 1)           # RAM
    comp(-2.0, 30.0, 6.0, 6.0, 0.7, 1)             # PMIC
    comp(-22.0, 64.0, 7.0, 5.0, 0.6, 1)
    for i in range(5):
        comp(-28.0, 24.0 + i * 3.4, 1.6, 1.6, 0.12, 3)
    for x in (2.0, 4.5, 7.0, 9.5):
        comp(x, 22.5, 1.4, 0.7, 0.5, 1)
    for y in (58.0, 61.0):
        comp(8.0, y, 0.7, 1.4, 0.5, 1)
    comp(0.0, 21.0, 10.0, 4.6, 0.9, 1)             # battery socket
    comp(29.0, 26.0, 7.4, 5.6, 0.9, 1)             # display socket
    b.cylinder(mm(-26.0 - c[0], 70.0 - c[1]), mm(2.4), mm(0.52 - c[2]), mm(0.6 - c[2]), segments=20, mat=4)
    for k in range(6):                                                         # gold traces
        y = 26.0 + k * 4.0
        b.box(mm(-8.0 - c[0], y - c[1], 0.585 - c[2]), mm(14.0, 0.35, 0.03), mat=3)
    board = b.build("Logicboard", col, [M("M_PF_PCB"), M("M_PF_Chip"), M("M_PF_Steel"), M("M_PF_PCBGold"),
                                        M("M_PF_Butter")], location_design=mm(*c), parent=root, bevel=(mm(0.15), 2))

    sh = P.MeshBuilder()
    sh.box(mm(-6.0 - c[0], 52.0 - c[1], 0.6 - 0.7 - c[2]), mm(20.0, 18.0, 1.4))
    sh.box(mm(-6.0 - c[0], 52.0 - c[1], 0.6 - 1.42 - c[2]), mm(16.0, 3.0, 0.05), mat=1)
    sh.build("Logicboard_Shield", col, [M("M_PF_Steel"), M("M_PF_SteelDark")], parent=board, bevel=(mm(0.25), 2))

    rng = P.seeded_random(7)
    co = P.MeshBuilder()                                                         # damage: corrosion bloom
    for _ in range(14):
        x, y = rng.uniform(-24.0, 4.0), rng.uniform(24.0, 44.0)
        s = rng.uniform(1.2, 3.2)
        co.ellipsoid(mm(x - c[0], y - c[1], 0.45 - c[2]), mm(s, s * rng.uniform(0.7, 1.3), 0.35), mat=0,
                     u_segments=10, v_segments=5)
    for _ in range(6):
        x, y = rng.uniform(-22.0, 2.0), rng.uniform(26.0, 42.0)
        co.ellipsoid(mm(x - c[0], y - c[1], 0.38 - c[2]), mm(0.9, 0.9, 0.3), mat=1, u_segments=8, v_segments=4)
    co.build("Logicboard_Corrosion", col, [M("M_PF_Corrosion"), M("M_PF_Rust")], parent=board)
    return board


# --------------------------------------------------------------------------------------
# Port, speaker, cameras
# --------------------------------------------------------------------------------------

def build_charging_port(col, root):
    c = (0.0, -71.5, 0.0)
    b = P.MeshBuilder()
    b.box(mm(0.0, 1.5, -0.6), mm(18.0, 9.0, 1.0), mat=2)                       # little daughterboard
    b.box((0.0, 0.0, 0.0), mm(13.0, 7.0, 3.4), mat=0)
    b.slab(P.rounded_rect(mm(9.8), mm(3.5), mm(1.6), 6), mm(-77.9 - c[1]), mm(-75.0 - c[1]), axis="Y", mat=0)
    b.slab(P.rounded_rect(mm(8.2), mm(2.3), mm(1.0), 6), mm(-77.98 - c[1]), mm(-77.3 - c[1]), axis="Y", mat=1)
    port = b.build("ChargingPort", col, [M("M_PF_Steel"), M("M_PF_Chip"), M("M_PF_PCB")], location_design=mm(*c),
                   parent=root, bevel=(mm(0.2), 2))
    rng = P.seeded_random(3)
    li = P.MeshBuilder()                                                         # damage: lint plug
    for _ in range(9):
        x = rng.uniform(-3.2, 3.2)
        li.ellipsoid(mm(x, -77.4 - c[1] + rng.uniform(-0.3, 0.6), rng.uniform(-0.5, 0.5)),
                     mm(rng.uniform(0.9, 1.6), rng.uniform(0.7, 1.2), rng.uniform(0.5, 0.8)), u_segments=10, v_segments=5)
    li.build("ChargingPort_Lint", col, [M("M_PF_Lint")], parent=port)
    return port


def build_speaker(col, root):
    c = (-17.0, -64.0, -0.3)
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(18), mm(9), mm(1.4), 6), mm(-1.8 - c[2]), mm(1.2 - c[2]), mat=0)
    for i in range(6):
        b.cylinder(mm(-5.0 + i * 2.0, 0.0), mm(0.55), mm(-1.9 - c[2]), mm(-1.78 - c[2]), segments=8, mat=1)
    return b.build("Speaker", col, [M("M_PF_Charcoal"), M("M_PF_Chip")], location_design=mm(*c), parent=root,
                   bevel=(mm(0.2), 2))


def build_rear_camera(col, root):
    icx = ISLAND[0]
    c = (icx, ISLAND[1], 0.2)
    z = lambda v: mm(v - c[2])  # noqa: E731
    b = P.MeshBuilder()
    b.slab(P.rounded_rect(mm(15), mm(54), mm(3.0), 6), z(-0.6), z(0.6), mat=0)                  # bracket
    for i, y in enumerate(LENS_Y):
        ly = y - c[1]
        b.cylinder(mm(0.0, ly), mm(5.6), z(-5.0), z(-0.6), segments=32, mat=0)                  # barrel
        b.slab(P.circle(mm(6.3), 40, mm(0.0, ly)), z(-6.4), z(-5.0), holes=[P.circle(mm(4.6), 40, mm(0.0, ly))],
               mat=2 + i)                                                                      # coloured ring
        b.cylinder(mm(0.0, ly), mm(4.7), z(-6.1), z(-6.0), segments=40, mat=1)                 # glass
        b.cylinder(mm(-1.2, ly + 1.2), mm(1.8), z(-6.16), z(-6.08), segments=24, mat=5)         # inner element
    cam = b.build("RearCamera", col,
                  [M("M_PF_CameraBody") if "M_PF_CameraBody" in PF.PALETTE else M("M_PF_Charcoal"), M("M_PF_LensDark"),
                   M(LENS_RING_MATS[0]), M(LENS_RING_MATS[1]), M(LENS_RING_MATS[2]), M("M_PF_LensBlue")],
                  location_design=mm(*c), parent=root, bevel=(mm(0.15), 2))
    sc = P.MeshBuilder()                                                         # damage: scratched middle lens
    ly = LENS_Y[1] - c[1]
    sc.ribbon(mm(-3.6, ly - 2.2), mm(3.4, ly + 2.6), mm(0.22), z(-6.2))
    sc.ribbon(mm(-2.6, ly + 3.2), mm(2.8, ly - 0.4), mm(0.18), z(-6.2))
    sc.ribbon(mm(-1.0, ly - 3.6), mm(3.0, ly - 1.4), mm(0.16), z(-6.2))
    sc.build("RearCamera_Scratch", col, [M("M_PF_PhoneCrack")], parent=cam, smooth=False)
    return cam


# --------------------------------------------------------------------------------------
# Previews
# --------------------------------------------------------------------------------------

DAMAGE = ("Screen_Crack", "Battery_Bulge", "Battery_Warning", "ChargingPort_Lint", "Logicboard_Corrosion",
          "DisplayConnector_Tear", "RearCamera_Scratch")
EXPLODE_Z = {
    "Screen": 22.0, "Logicboard": -7.0, "Battery": -14.0, "RearCamera": -12.0, "Speaker": -14.0,
    "ChargingPort": -14.0, "BackCover": -32.0,
    "Screw_01": -44.0, "Screw_02": -44.0, "Screw_03": -44.0, "Screw_04": -44.0,
}


def set_damage(visible):
    import bpy
    for n in DAMAGE:
        o = bpy.data.objects.get(n)
        if o is not None:
            o.hide_render = not visible


def render_previews(project_root, preview_col):
    import bpy
    out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "PF")
    PF.setup_beauty_render(resolution=(1400, 1050), samples=48, world_hex="#E9E2D6", world_strength=0.85)
    PF.golden_hour_rig(preview_col, focus=(0.0, 0.0, 0.0), scale=0.25, sun_from=(-0.4, -0.8, 0.9), sun_energy=2.4,
                       fill_energy=26.0)
    PF.ground_plane(preview_col, "PreviewMat", 3.0, mm(-30.0), "M_PF_NavyMat")
    cam = PF.camera(preview_col, lens=60.0)
    root = bpy.data.objects[ROOT_NAME]
    files = []
    set_damage(False)
    files.append(PF.render_still(cam, (0.10, -0.20, 0.30), (0, 0, 0), os.path.join(out, "PHN_MochiDango_front.png")))

    # back views: flip the phone over its long axis (like Space in game) instead of moving the camera under the mat
    root.rotation_euler = (0.0, math.pi, 0.0)
    files.append(PF.render_still(cam, (0.10, -0.20, 0.30), (0, 0, 0), os.path.join(out, "PHN_MochiDango_back.png")))

    hidden = [bpy.data.objects[n] for n in ("BackCover",) + tuple(s for s, _ in SCREWS)]
    hidden_all = []
    for o in hidden:
        hidden_all += [o] + P.descendants(o)
    for o in hidden_all:
        o.hide_render = True
    set_damage(True)
    bpy.data.objects["Screen_Crack"].hide_render = True
    files.append(PF.render_still(cam, (0.09, -0.18, 0.28), (0, 0, 0), os.path.join(out, "PHN_MochiDango_open_damaged.png")))
    set_damage(False)
    files.append(PF.render_still(cam, (0.09, -0.18, 0.28), (0, 0, 0), os.path.join(out, "PHN_MochiDango_open.png")))
    for o in hidden_all:
        o.hide_render = False

    original = {}
    for name, dz in EXPLODE_Z.items():
        o = bpy.data.objects[name]
        original[name] = o.location.copy()
        o.location.z += mm(dz)
    files.append(PF.render_still(cam, (0.30, -0.38, 0.24), (0, 0, mm(12.0)),
                                 os.path.join(out, "PHN_MochiDango_exploded.png")))
    for name, loc in original.items():
        bpy.data.objects[name].location = loc
    root.rotation_euler = (0.0, 0.0, 0.0)
    set_damage(True)
    return files


def build(col, cutters):
    root = P.make_empty(ROOT_NAME, col)
    build_frame(col, cutters, root)
    build_screen(col, root)
    build_back_cover(col, root)
    build_screws(col, root)
    build_battery(col, root)
    build_logicboard(col, root)
    build_charging_port(col, root)
    build_speaker(col, root)
    build_rear_camera(col, root)
    return root


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)
    P.reset_scene()
    PF.write_palette_json(project_root)
    col = P.get_collection(ROOT_NAME)
    cutters = P.get_collection("Cutters")
    preview_col = P.get_collection("Preview")
    root = build(col, cutters)

    fbx_path = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Phones", "MochiDango", f"{ROOT_NAME}.fbx")
    P.export_fbx_for_unity(root, fbx_path)
    print(f"PRS_EXPORTED {fbx_path}")
    print(f"PF_TRIS {PF.count_tris(root)}")

    if "--no-preview" not in args:
        for f in render_previews(project_root, preview_col):
            print(f"PRS_PREVIEW {f}")
        for o in list(preview_col.objects):
            bpy.data.objects.remove(o)

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Phones", f"{ROOT_NAME}.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")
    deps = bpy.context.evaluated_depsgraph_get()
    for o in [root] + P.descendants(root):
        if o.type == "MESH":
            ev = o.evaluated_get(deps)
            dims = [round(d * 1000.0, 2) for d in ev.dimensions]
            print(f"PRS_PART {o.name:<26} verts={len(ev.data.vertices):>5} dims(mm)={dims}")


if __name__ == "__main__":
    main()

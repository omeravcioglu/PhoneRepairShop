"""
ENV_PF_Shop - the starting Phone Fixer shop as a cut-away diorama (Tier 0, "hole in the wall").

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_pf_shop.py
    (append "-- --no-preview" to skip the quick preview render)

Outputs:
    ArtSource/Blender/Environment/ENV_PF_Shop.blend
    Assets/_Project/Art/Models/Environment/PF/ENV_PF_Shop.fbx

Layout (metres; origin = front-left inner corner of the room on the floor; +Y = towards the back):
    room 6.0 x 4.5, walls 2.6 high. The left wall (X=0) is the storefront (window + sliding door,
    noren outside), the back wall (Y=4.5) holds the shelf, posters, plaque and the workbench spot.
    Front (Y=0) and right (X=6) are cut away; ShadowProxy_* objects close the box for SHADOWS ONLY
    (Unity: ShadowCastingMode.ShadowsOnly), so sunlight only enters through the windows.
    A strip of sidewalk with a vending machine sits on the plinth outside the storefront.
    The workbench is a separate asset (ENV_PF_RepairBench) placed at the Spot_Bench empty.
Empties: Spot_* (bench, customers, door), Light_* (pendant bulbs) for the Unity scene builder.
"""

import math
import os
import sys

from mathutils import Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
import pf_bpy as PF  # noqa: E402

ROOT_NAME = "ENV_PF_Shop"
RW, RD, WH, WT = 6.0, 4.5, 2.6, 0.18        # room width, depth, wall height, wall thickness
WINDOW = (0.4, 2.7, 0.8, 2.2)              # y0, y1, z0, z1 on the left wall
DOOR = (3.0, 4.0, 2.15)                    # y0, y1, height
BENCH_SPOT = (4.15, 4.04)
COUNTER = (2.35, 2.85, 1.15, 3.35, 0.92)   # x0, x1, y0, y1, body height (top slab adds 6 cm)


def M(name):
    return PF.M(name)


def bx(b, x0, x1, y0, y1, z0, z1, mat=0):
    """Axis-aligned box from min/max corners."""
    b.box(((x0 + x1) * 0.5, (y0 + y1) * 0.5, (z0 + z1) * 0.5), (x1 - x0, y1 - y0, z1 - z0), mat=mat)


# --------------------------------------------------------------------------------------
# Shell: plinth, floor, walls, windows, door
# --------------------------------------------------------------------------------------

def build_plinth_and_floor(col, root):
    b = P.MeshBuilder()
    bx(b, -1.6, RW + 0.3, -0.3, RD + 0.45, -0.35, -0.02)
    bx(b, -1.6, RW + 0.3, -0.3, -0.27, -0.35, -0.30, mat=1)                    # dark base band, front
    bx(b, RW + 0.27, RW + 0.3, -0.3, RD + 0.45, -0.35, -0.30, mat=1)           # dark base band, right
    bx(b, 2.2, 3.6, -0.303, -0.3, -0.27, -0.10, mat=2)                          # brass nameplate
    b.build("Shop_Plinth", col, [M("M_PF_Wood"), M("M_PF_WoodDark"), M("M_PF_Brass")], parent=root,
            bevel=(0.012, 3))
    PF.text_object("PHONE FIXER  -  TIER 0", 0.075, 0.004, col, "Shop_PlinthText", M("M_PF_WoodDark"), parent=root,
                   location=(2.9, -0.3045, -0.185), rotation=(math.radians(90.0), 0.0, 0.0), offset=0.002)

    f = P.MeshBuilder()
    bx(f, 0.0, RW, 0.0, RD, -0.02, 0.0)
    f.build("Shop_Floor", col, [M("M_PF_FloorOak")], parent=root, smooth=False)
    s = P.MeshBuilder()
    y = 0.25
    row = 0
    while y < RD - 0.01:
        bx(s, 0.0, RW, y - 0.004, y + 0.004, 0.0, 0.0015)
        y += 0.25
    rng = P.seeded_random(5)
    y = 0.0
    while y < RD - 0.01:
        x = rng.uniform(0.2, 1.4)
        while x < RW - 0.1:
            bx(s, x - 0.004, x + 0.004, y + 0.004, y + 0.246, 0.0, 0.0015)
            x += rng.uniform(1.2, 2.0)
        y += 0.25
        row += 1
    s.build("Shop_FloorSeams", col, [M("M_PF_FloorSeam")], parent=root, smooth=False)

    rug = P.MeshBuilder()
    PF.rounded_slab(rug, 1.475, 2.3, 1.25, 2.0, 0.08, 0.0, 0.01, mat=0)
    PF.rounded_slab(rug, 1.475, 2.3, 1.07, 1.82, 0.06, 0.01, 0.012, mat=1)
    PF.rounded_slab(rug, 1.475, 2.3, 1.01, 1.76, 0.05, 0.012, 0.013, mat=0)
    for yy in (1.75, 2.85):
        bx(rug, 1.05, 1.9, yy - 0.04, yy + 0.04, 0.012, 0.0135, mat=2)
    rug.build("Shop_Rug", col, [M("M_PF_DustyBlue"), M("M_PF_PaperCream"), M("M_PF_Navy")], parent=root,
              smooth=False)


def build_walls(col, root):
    wy0, wy1, wz0, wz1 = WINDOW
    dy0, dy1, dz = DOOR
    b = P.MeshBuilder()
    x0, x1 = -WT, 0.0
    bx(b, x0, x1, 0.0, wy0, 0.0, WH)                                              # left wall, front end
    bx(b, x0, x1, wy0, wy1, 0.0, wz0)                                             # under the window
    bx(b, x0, x1, wy0, wy1, wz1, WH)                                              # over the window
    bx(b, x0, x1, wy1, dy0, 0.0, WH)                                              # between window and door
    bx(b, x0, x1, dy0, dy1, dz, WH)                                               # over the door
    bx(b, x0, x1, dy1, RD + WT, 0.0, WH)                                          # corner segment
    bx(b, x0, RW, RD, RD + WT, 0.0, WH)                                           # back wall
    walls = b.build("Shop_Walls", col, [M("M_PF_Plaster")], parent=root, bevel=(0.004, 2))

    caps = P.MeshBuilder()
    bx(caps, x0 - 0.004, x1 + 0.004, -0.004, RD + WT + 0.004, WH, WH + 0.025)
    bx(caps, x0 - 0.004, RW + 0.004, RD - 0.004, RD + WT + 0.004, WH, WH + 0.025)
    caps.build("Shop_WallCaps", col, [M("M_PF_PaperCream")], parent=root, bevel=(0.006, 2))

    w = P.MeshBuilder()                                                           # wainscot + rails
    bx(w, 0.0, 0.02, 0.0, dy0, 0.0, 0.76, mat=0)
    bx(w, 0.0, 0.02, dy1, RD, 0.0, 0.76, mat=0)
    bx(w, 0.0, RW, RD - 0.02, RD, 0.0, 0.85, mat=0)
    bx(w, 0.0, 0.035, 0.0, wy0 - 0.02, 0.76, 0.8, mat=1)                          # (the window sill is the rail here)
    bx(w, 0.0, 0.035, wy1 + 0.02, dy0, 0.76, 0.8, mat=1)
    bx(w, 0.0, 0.035, dy1, RD, 0.76, 0.8, mat=1)
    bx(w, 0.0, RW, RD - 0.035, RD, 0.84, 0.88, mat=1)
    for k in range(int(RW / 0.3)):                                                # board seams on the back
        xx = 0.3 * (k + 1)
        bx(w, xx - 0.003, xx + 0.003, RD - 0.0215, RD - 0.02, 0.0, 0.84, mat=1)
    w.build("Shop_Wainscot", col, [M("M_PF_WoodLight"), M("M_PF_Wood")], parent=root, bevel=(0.003, 2))

    fr = P.MeshBuilder()                                                          # window frame
    t = 0.05
    bx(fr, -WT - 0.01, 0.06, wy0 - 0.02, wy1 + 0.02, wz0 - 0.04, wz0, mat=0)      # sill (deep, inside)
    bx(fr, -WT, 0.0, wy0, wy1, wz1 - t, wz1, mat=0)
    bx(fr, -WT, 0.0, wy0, wy0 + t, wz0, wz1, mat=0)
    bx(fr, -WT, 0.0, wy1 - t, wy1, wz0, wz1, mat=0)
    mid = (wy0 + wy1) * 0.5
    bx(fr, -WT * 0.6, -WT * 0.2, mid - 0.025, mid + 0.025, wz0, wz1, mat=0)       # mullion
    bx(fr, -WT * 0.6, -WT * 0.2, wy0, wy1, 1.85, 1.89, mat=0)                     # transom bar
    fr.build("Shop_WindowFrame", col, [M("M_PF_WoodDark")], parent=root, bevel=(0.006, 2))
    gl = P.MeshBuilder()
    bx(gl, -WT * 0.45, -WT * 0.4, wy0 + t, wy1 - t, wz0, wz1 - t)
    gl.build("Shop_WindowGlass", col, [M("M_PF_Glass")], parent=root, smooth=False)

    df = P.MeshBuilder()                                                          # door frame
    bx(df, -WT - 0.01, 0.01, dy0 - 0.05, dy0, 0.0, dz + 0.05, mat=0)
    bx(df, -WT - 0.01, 0.01, dy1, dy1 + 0.05, 0.0, dz + 0.05, mat=0)
    bx(df, -WT - 0.01, 0.01, dy0 - 0.05, dy1 + 0.05, dz, dz + 0.06, mat=0)
    bx(df, -WT - 0.01, 0.01, dy0, dy1, -0.005, 0.012, mat=1)                      # brass threshold
    df.build("Shop_DoorFrame", col, [M("M_PF_WoodDark"), M("M_PF_Brass")], parent=root, bevel=(0.006, 2))
    half = (dy1 - dy0) * 0.5
    for i, (ya, xo) in enumerate(((dy0, -0.13), (dy0 + half, -0.07))):
        d = P.MeshBuilder()
        bx(d, -0.025, 0.025, 0.0, half + 0.02, 0.0, 0.06, mat=0)                  # bottom rail
        bx(d, -0.025, 0.025, 0.0, half + 0.02, dz - 0.06, dz - 0.005, mat=0)      # top rail
        bx(d, -0.025, 0.025, 0.0, 0.05, 0.0, dz - 0.005, mat=0)
        bx(d, -0.025, 0.025, half - 0.03, half + 0.02, 0.0, dz - 0.005, mat=0)
        bx(d, -0.008, 0.008, 0.05, half - 0.03, 0.06, dz - 0.06, mat=1)           # glass
        bx(d, 0.025, 0.045, half * 0.5 - 0.06, half * 0.5 + 0.06, 0.95, 1.05, mat=2)  # pull
        d.build(f"Shop_DoorPanel_{i + 1}", col, [M("M_PF_WoodDark"), M("M_PF_Glass"), M("M_PF_Brass")],
                location_design=(xo, ya, 0.0), parent=root, bevel=(0.004, 2))
    return walls


def build_noren(col, root):
    dy0, dy1, dz = DOOR
    b = P.MeshBuilder()
    b.cylinder_between((-0.28, dy0 - 0.08, 2.12), (-0.28, dy1 + 0.08, 2.12), 0.014, segments=10, mat=1)
    strips = 3
    span = (dy1 - dy0 + 0.1) / strips
    for k in range(strips):
        ya = dy0 - 0.05 + k * span + 0.012
        bx(b, -0.30, -0.29, ya, ya + span - 0.024, 1.48, 2.11, mat=0)
    nr = b.build("Shop_Noren", col, [M("M_PF_NavyFabric"), M("M_PF_Wood")], parent=root)
    c = P.MeshBuilder()
    cy, cz = (dy0 + dy1) * 0.5, 1.80
    for face_x, ax in ((-0.302, "X"), (-0.288, "X")):
        PF.torus(c, (face_x, cy, cz), 0.12, 0.012, mat=0, axis=ax, major_segments=40, minor_segments=6)
        bx(c, face_x - 0.003, face_x + 0.003, cy - 0.045, cy + 0.045, cz - 0.075, cz + 0.075, mat=0)
    c.build("Shop_NorenCrest", col, [M("M_PF_PaperCream")], parent=nr, smooth=False)
    return nr


# --------------------------------------------------------------------------------------
# Back wall dressing
# --------------------------------------------------------------------------------------

def build_back_wall_dressing(col, root):
    yb = RD - 0.004
    p = P.MeshBuilder()                                                           # Mochi poster
    bx(p, 2.0, 2.66, yb - 0.006, yb, 1.05, 1.95, mat=0)
    p.ellipsoid((2.33, yb - 0.01, 1.66), (0.16, 0.012, 0.09), mat=1, u_segments=24, v_segments=8)
    for sx in (-0.05, 0.05):
        p.ellipsoid((2.33 + sx, yb - 0.022, 1.68), (0.012, 0.004, 0.012), mat=3, u_segments=10, v_segments=5)
    p.slab(P.rounded_rect(0.1, 0.18, 0.02, 8), yb - 0.016, yb - 0.006, axis="Y", mat=2, offset=(2.33, 0.0, 1.38))
    for k in range(3):                                                            # the three dango lenses
        p.ellipsoid((2.31, yb - 0.017, 1.43 - k * 0.035), (0.012, 0.003, 0.012), mat=3, u_segments=10, v_segments=5)
    p.build("Shop_PosterMochi", col, [M("M_PF_DustyBlue"), M("M_PF_Paper"), M("M_PF_PhoneSakura"), M("M_PF_Navy")],
            parent=root, smooth=False)
    PF.text_object("MOCHI DANGO", 0.075, 0.003, col, "Shop_PosterMochiText", M("M_PF_Paper"), parent=root,
                   location=(2.33, yb - 0.008, 1.15), rotation=(math.radians(90.0), 0.0, 0.0), offset=0.002)

    h = P.MeshBuilder()                                                           # handwritten poster
    bx(h, 2.74, 3.14, yb - 0.004, yb, 1.18, 1.72, mat=0)
    bx(h, 2.72, 2.8, yb - 0.006, yb - 0.004, 1.68, 1.74, mat=1)                     # tape
    bx(h, 3.08, 3.16, yb - 0.006, yb - 0.004, 1.68, 1.74, mat=1)
    h.build("Shop_PosterFix", col, [M("M_PF_Butter"), M("M_PF_PaperCream")], parent=root, smooth=False)
    for k, line in enumerate(("WE FIX", "ALMOST", "ANYTHING")):
        PF.text_object(line, 0.07, 0.002, col, f"Shop_PosterFixText_{k + 1}", M("M_PF_Navy"), parent=root,
                       location=(2.94, yb - 0.006, 1.6 - k * 0.12), rotation=(math.radians(90.0), 0.0, 0.0),
                       offset=0.003)

    ck = P.MeshBuilder()                                                          # wall clock
    ck.slab(P.circle(0.17, 40, (0.0, 0.0)), -0.03, 0.0, axis="Y", mat=0)
    ck.slab(P.circle(0.145, 40, (0.0, 0.0)), -0.034, -0.03, axis="Y", mat=1)
    ck.box((0.0, -0.038, 0.05), (0.014, 0.004, 0.1), mat=2)
    ck.box((0.035, -0.04, -0.012), (0.075, 0.004, 0.012), mat=2)
    for k in range(12):
        a = 2 * math.pi * k / 12
        ck.box((0.12 * math.cos(a), -0.036, 0.12 * math.sin(a)), (0.012, 0.004, 0.012), mat=2)
    ck.build("Shop_Clock", col, [M("M_PF_Navy"), M("M_PF_Paper"), M("M_PF_Navy")],
             location_design=(2.42, yb, 2.27), parent=root, bevel=(0.004, 2))

    pl = P.MeshBuilder()                                                          # shop plaque
    bx(pl, -0.78, 0.78, -0.05, 0.0, -0.17, 0.17, mat=0)
    for (a0, a1, b0, b1) in ((-0.74, 0.74, 0.12, 0.135), (-0.74, 0.74, -0.135, -0.12), (-0.755, -0.74, -0.135, 0.135),
                             (0.74, 0.755, -0.135, 0.135)):
        bx(pl, a0, a1, -0.056, -0.05, b0, b1, mat=1)
    plaque = pl.build("Shop_Plaque", col, [M("M_PF_WoodDark"), M("M_PF_Brass")],
                      location_design=(BENCH_SPOT[0], yb, 2.16), parent=root, bevel=(0.01, 3))
    PF.text_object("PHONE FIXER", 0.17, 0.012, col, "Shop_PlaqueText", M("M_PF_PaperCream"), parent=plaque,
                   location=(0.0, -0.06, -0.01), rotation=(math.radians(90.0), 0.0, 0.0), offset=0.006)

    cal = P.MeshBuilder()                                                         # calendar (left wall corner)
    bx(cal, 0.0, 0.006, 4.08, 4.42, 1.05, 1.55, mat=0)
    bx(cal, 0.006, 0.008, 4.08, 4.42, 1.47, 1.55, mat=1)
    for r in range(4):
        for cc in range(5):
            bx(cal, 0.006, 0.008, 4.11 + cc * 0.06, 4.14 + cc * 0.06, 1.09 + r * 0.08, 1.12 + r * 0.08, mat=2)
    PF.torus(cal, (0.009, 4.29, 1.25), 0.025, 0.003, mat=1, axis="X", major_segments=20, minor_segments=5)
    cal.build("Shop_Calendar", col, [M("M_PF_Paper"), M("M_PF_Navy"), M("M_PF_Kraft")], parent=root, smooth=False)

    sw = P.MeshBuilder()                                                          # switch + outlet
    bx(sw, 5.4, 5.48, yb - 0.012, yb, 1.18, 1.3, mat=0)
    bx(sw, 5.425, 5.455, yb - 0.02, yb - 0.012, 1.22, 1.26, mat=1)
    bx(sw, 5.6, 5.68, yb - 0.012, yb, 0.3, 0.4, mat=0)
    sw.build("Shop_Switches", col, [M("M_PF_Paper"), M("M_PF_Kraft")], parent=root, bevel=(0.003, 2))


def build_shelf(col, root):
    x0, x1, y0, y1, h = 0.25, 1.85, RD - 0.42, RD - 0.02, 2.0
    b = P.MeshBuilder()
    bx(b, x0, x0 + 0.04, y0, y1, 0.0, h, mat=0)
    bx(b, x1 - 0.04, x1, y0, y1, 0.0, h, mat=0)
    bx(b, x0, x1, y1 - 0.015, y1, 0.0, h, mat=1)
    for z in (0.0, 0.48, 0.96, 1.44, 1.94):
        bx(b, x0, x1, y0, y1, z, z + 0.04, mat=0)
    shelf = b.build("Shop_Shelf", col, [M("M_PF_Wood"), M("M_PF_WoodWalnut")], parent=root, bevel=(0.008, 3))

    s = P.MeshBuilder()
    rng = P.seeded_random(19)
    colors = (2, 3, 4, 5, 6)
    # level 1: cardboard bins and boxes
    xx = x0 + 0.07
    for wdt, ht, m in ((0.3, 0.3, 0), (0.22, 0.22, 7), (0.34, 0.36, 0), (0.24, 0.2, 3)):
        bx(s, xx, xx + wdt, y0 + 0.04, y1 - 0.04, 0.04, 0.04 + ht, mat=m)
        xx += wdt + 0.05
    # level 2: phone boxes in pastels + cable coils
    xx = x0 + 0.08
    for k in range(6):
        bx(s, xx, xx + 0.085, y0 + 0.1, y0 + 0.26, 0.52, 0.72, mat=colors[k % len(colors)])
        xx += 0.1
    for k, m in enumerate((8, 2, 4)):
        PF.torus(s, (x0 + 0.78 + k * 0.2, y0 + 0.2, 0.6), 0.07, 0.016, mat=m, axis="Y", major_segments=24,
                 minor_segments=8)
    # level 3: retired flip phones, a jar of screws, the waving robot
    for k in range(5):
        bx(s, x0 + 0.1 + k * 0.1, x0 + 0.17 + k * 0.1, y0 + 0.15, y0 + 0.19, 1.0, 1.16 + 0.02 * (k % 2),
           mat=(8, 5, 4, 8, 3)[k])
    s.cylinder((x0 + 0.78, y0 + 0.2), 0.065, 1.0, 1.17, segments=24, mat=9)
    s.cylinder((x0 + 0.78, y0 + 0.2), 0.068, 1.17, 1.2, segments=24, mat=10)
    bx(s, x0 + 1.05, x0 + 1.17, y0 + 0.14, y0 + 0.26, 1.0, 1.15, mat=11)            # robot body
    bx(s, x0 + 1.07, x0 + 1.15, y0 + 0.16, y0 + 0.24, 1.15, 1.24, mat=11)           # head
    s.cylinder_between((x0 + 1.17, y0 + 0.2, 1.12), (x0 + 1.23, y0 + 0.2, 1.24), 0.014, segments=8, mat=11)
    for ex in (x0 + 1.09, x0 + 1.13):
        s.ellipsoid((ex, y0 + 0.155, 1.2), (0.008, 0.004, 0.008), mat=8, u_segments=8, v_segments=4)
    # level 4: books and a trailing pothos
    xx = x0 + 0.08
    for k in range(7):
        ht = rng.uniform(0.22, 0.32)
        bx(s, xx, xx + 0.05, y0 + 0.08, y1 - 0.08, 1.48, 1.48 + ht, mat=colors[k % len(colors)])
        xx += 0.055
    s.cylinder((x0 + 1.1, y0 + 0.2), 0.08, 1.48, 1.62, segments=20, mat=4)
    for k in range(9):
        s.ellipsoid((x0 + 1.04 - 0.02 * k, y0 + 0.06, 1.56 - 0.06 * k), (0.035, 0.012, 0.03),
                    mat=12 if k % 2 else 13, u_segments=8, v_segments=5)
    for k in range(5):
        s.ellipsoid((x0 + 1.1 + 0.05 * math.cos(k), y0 + 0.2 + 0.05 * math.sin(k), 1.66), (0.045, 0.045, 0.03),
                    mat=12 if k % 2 else 13, u_segments=8, v_segments=5)
    # price tags
    for k in range(6):
        bx(s, x0 + 0.15 + k * 0.25, x0 + 0.21 + k * 0.25, y0 - 0.002, y0, 0.485 + 0.48 * (k % 3), 0.515 + 0.48 * (k % 3),
           mat=4)
    s.build("Shop_ShelfStock", col,
            [M("M_PF_Cardboard"), M("M_PF_Kraft"), M("M_PF_Paper"), M("M_PF_Sage"), M("M_PF_PaperCream"),
             M("M_PF_DustyBlue"), M("M_PF_Butter"), M("M_PF_Blush"), M("M_PF_Navy"), M("M_PF_Glass"),
             M("M_PF_Brass"), M("M_PF_SteelDark"), M("M_PF_Leaf"), M("M_PF_LeafLight")],
            parent=shelf, bevel=(0.004, 2))
    return shelf


# --------------------------------------------------------------------------------------
# Counter + register + PC + ticket line
# --------------------------------------------------------------------------------------

def build_counter(col, root):
    x0, x1, y0, y1, hb = COUNTER
    b = P.MeshBuilder()
    # an open-backed frame: the shopkeeper (and the camera) see into the cubbies on the +X side
    bx(b, x0, x0 + 0.04, y0, y1, 0.0, hb, mat=0)                                   # customer-side wall
    bx(b, x0 + 0.04, x0 + 0.05, y0 + 0.04, y1 - 0.04, 0.06, hb, mat=3)             # dark cubby backs
    bx(b, x0, x1, y0, y0 + 0.04, 0.0, hb, mat=0)                                   # end panels
    bx(b, x0, x1, y1 - 0.04, y1, 0.0, hb, mat=0)
    bx(b, x0, x1, y0, y1, 0.0, 0.06, mat=0)                                        # bottom
    bx(b, x0, x1, y0, y1, 0.44, 0.47, mat=0)                                       # middle shelf
    for yy in (y0 + 0.73, y0 + 1.46):                                              # cubby dividers
        bx(b, x0, x1, yy - 0.015, yy + 0.015, 0.47, hb, mat=0)
    bx(b, x0 - 0.05, x1 + 0.05, y0 - 0.05, y1 + 0.05, hb, hb + 0.06, mat=1)        # top slab
    bx(b, x0 - 0.012, x0, y0 + 0.04, y1 - 0.04, 0.06, hb - 0.06, mat=2)            # customer-side panel
    for k in range(9):                                                             # slats
        yy = y0 + 0.12 + k * 0.24
        bx(b, x0 - 0.02, x0 - 0.012, yy - 0.02, yy + 0.02, 0.08, hb - 0.08, mat=0)
    counter = b.build("Shop_Counter", col, [M("M_PF_Wood"), M("M_PF_WoodLight"), M("M_PF_WoodWalnut"),
                                            M("M_PF_WoodDark")], parent=root, bevel=(0.008, 3))

    st = P.MeshBuilder()                                                           # what lives in the cubbies
    for k in range(3):
        PF.torus(st, (x1 - 0.03, y0 + 0.2 + k * 0.17, 0.58), 0.05, 0.02, mat=0, axis="X", major_segments=20,
                 minor_segments=8)
    bx(st, x1 - 0.2, x1 - 0.02, y0 + 0.86, y0 + 1.1, 0.5, 0.78, mat=1)              # paper bag
    for k, m in enumerate((2, 3, 4)):
        bx(st, x1 - 0.24, x1 - 0.02, y0 + 1.55 + k * 0.07, y0 + 1.6 + k * 0.07, 0.5, 0.8, mat=m)
    bx(st, x1 - 0.3, x1 - 0.02, y0 + 0.2, y0 + 0.75, 0.08, 0.3, mat=2)              # toolbox
    bx(st, x1 - 0.3, x1 - 0.02, y0 + 1.0, y0 + 1.6, 0.08, 0.26, mat=1)              # spare phone crate
    st.build("Shop_CounterStock", col, [M("M_PF_Paper"), M("M_PF_Cardboard"), M("M_PF_DustyBlue"),
                                        M("M_PF_Butter"), M("M_PF_Sage")], parent=counter, bevel=(0.004, 2))

    top = hb + 0.06
    r = P.MeshBuilder()                                                            # register
    bx(r, -0.2, 0.2, -0.24, 0.24, 0.0, 0.14, mat=0)
    bx(r, -0.15, 0.17, -0.2, 0.2, 0.14, 0.2, mat=0)
    for i in range(3):
        for j in range(3):
            bx(r, 0.02 + i * 0.045, 0.055 + i * 0.045, -0.12 + j * 0.06, -0.08 + j * 0.06, 0.2, 0.215, mat=1)
    bx(r, -0.17, -0.06, -0.12, 0.12, 0.2, 0.32, mat=2)                              # display box
    bx(r, -0.06, -0.058, -0.1, 0.1, 0.23, 0.3, mat=3)                               # display glow (+X face)
    bx(r, 0.2, 0.205, -0.18, 0.18, 0.03, 0.1, mat=4)                                # cash drawer front
    register = r.build("Counter_Register", col, [M("M_PF_DustyBlue"), M("M_PF_Paper"), M("M_PF_Navy"),
                                                 M("M_PF_ScreenText"), M("M_PF_SteelDark")],
                       location_design=((x0 + x1) * 0.5, 2.25, top), parent=root, bevel=(0.012, 3))

    pc = P.MeshBuilder()                                                           # old beige PC
    bx(pc, -0.2, 0.18, -0.22, 0.22, 0.0, 0.36, mat=0)
    bx(pc, -0.27, -0.2, -0.16, 0.16, 0.05, 0.3, mat=0)                              # tube back
    bx(pc, 0.18, 0.186, -0.16, 0.16, 0.07, 0.315, mat=1)                            # screen on the +X face
    pcm = pc.build("Counter_PC", col, [M("M_PF_CreamPlastic"), M("M_PF_ScreenOff")],
                   location_design=((x0 + x1) * 0.5, 1.52, top), parent=root, bevel=(0.012, 3))
    sx = P.MeshBuilder()
    for k, (z, length) in enumerate(((0.28, 0.2), (0.24, 0.26), (0.2, 0.14), (0.16, 0.22), (0.12, 0.18))):
        bx(sx, 0.189, 0.191, -0.13, -0.13 + length, z - 0.012, z, mat=0)
    sx.build("Counter_PCScreen", col, [M("M_PF_ScreenIdle")], parent=pcm, smooth=False)
    kb = P.MeshBuilder()
    bx(kb, -0.1, 0.1, -0.2, 0.2, 0.0, 0.03)
    for i in range(3):
        for j in range(8):
            bx(kb, -0.08 + i * 0.055, -0.04 + i * 0.055, -0.18 + j * 0.045, -0.145 + j * 0.045, 0.03, 0.038, mat=1)
    kb.build("Counter_Keyboard", col, [M("M_PF_CreamPlastic"), M("M_PF_Kraft")],
             location_design=((x0 + x1) * 0.5 + 0.02, 1.98, top), parent=root, bevel=(0.006, 2))

    bell = P.MeshBuilder()
    bell.cylinder((0.0, 0.0), 0.06, 0.0, 0.02, segments=24, mat=0)
    bell.ellipsoid((0.0, 0.0, 0.02), (0.05, 0.05, 0.05), mat=1, u_segments=24, v_segments=8)
    bell.cylinder((0.0, 0.0), 0.01, 0.06, 0.09, segments=10, mat=1)
    bell.build("Counter_Bell", col, [M("M_PF_WoodDark"), M("M_PF_Brass")],
               location_design=((x0 + x1) * 0.5 + 0.08, 3.12, top), parent=root)

    tl = P.MeshBuilder()                                                           # ticket line
    px = (x0 + x1) * 0.5
    for yy in (y0 + 0.02, y1 - 0.02):
        tl.cylinder((px, yy), 0.013, top, 1.98, segments=10, mat=0)
        tl.ellipsoid((px, yy, 1.98), (0.02, 0.02, 0.02), mat=0, u_segments=10, v_segments=5)
    pts = []
    n = 8
    for k in range(n + 1):
        t = k / n
        pts.append((px, (y0 + 0.02) + t * (y1 - y0 - 0.04), 1.95 - 0.12 * 4 * t * (1 - t)))
    for a, c in zip(pts, pts[1:]):
        tl.cylinder_between(a, c, 0.003, segments=6, mat=1)
    for k, (t, m) in enumerate(((0.2, 3), (0.42, 4), (0.64, 5), (0.84, 3))):
        yy = (y0 + 0.02) + t * (y1 - y0 - 0.04)
        zz = 1.95 - 0.12 * 4 * t * (1 - t)
        bx(tl, px - 0.003, px + 0.003, yy - 0.06, yy + 0.06, zz - 0.2, zz - 0.02, mat=2)       # ticket
        bx(tl, px - 0.012, px + 0.012, yy - 0.012, yy + 0.012, zz - 0.04, zz + 0.02, mat=0)    # clothespin
        bx(tl, px + 0.003, px + 0.0045, yy + 0.02, yy + 0.045, zz - 0.18, zz - 0.155, mat=m)  # status dot
        for line in range(3):
            bx(tl, px + 0.003, px + 0.004, yy - 0.045, yy + 0.02 - 0.015 * line, zz - 0.07 - line * 0.03,
               zz - 0.062 - line * 0.03, mat=6)
    tl.build("Counter_TicketLine", col, [M("M_PF_WoodLight"), M("M_PF_Kraft"), M("M_PF_Paper"), M("M_PF_Damage"),
                                         M("M_PF_Sage"), M("M_PF_Butter"), M("M_PF_SteelDark")],
             parent=root, smooth=False)
    return counter


# --------------------------------------------------------------------------------------
# Waiting area, plants, boxes, upgrade slot, lamps
# --------------------------------------------------------------------------------------

def build_waiting(col, root):
    a = P.MeshBuilder()                                                            # vinyl chair, chrome legs
    PF.rounded_slab(a, 0.0, 0.0, 0.46, 0.46, 0.06, 0.42, 0.49, mat=0)
    PF.rounded_slab(a, 0.0, 0.0, 0.42, 0.42, 0.05, 0.49, 0.5, mat=0)
    bx(a, -0.23, -0.17, -0.2, 0.2, 0.5, 0.92, mat=0)                               # backrest (towards the wall)
    for (lx, ly) in ((-0.18, -0.18), (0.18, -0.18), (-0.18, 0.18), (0.18, 0.18)):
        a.cylinder((lx, ly), 0.013, 0.0, 0.42, segments=10, mat=1)
    a.build("Shop_ChairVinyl", col, [M("M_PF_Mustard"), M("M_PF_Chrome")], location_design=(0.53, 1.67, 0.0),
            parent=root, bevel=(0.02, 3))

    s = P.MeshBuilder()                                                            # wooden stool
    s.cylinder((0.0, 0.0), 0.2, 0.43, 0.47, segments=28)
    for k in range(4):
        ang = math.pi * 0.25 + k * math.pi * 0.5
        s.cylinder_between((0.13 * math.cos(ang), 0.13 * math.sin(ang), 0.43),
                           (0.17 * math.cos(ang), 0.17 * math.sin(ang), 0.0), 0.018, segments=8)
    s.build("Shop_Stool", col, [M("M_PF_WoodLight")], location_design=(0.53, 0.95, 0.0), parent=root,
            bevel=(0.006, 2))

    m = P.MeshBuilder()                                                            # monstera
    m.frustum(0.0, 0.34, 0.15, 0.19, segments=24, mat=0)
    m.cylinder((0.0, 0.0), 0.175, 0.3, 0.32, segments=24, mat=1)
    rng = P.seeded_random(13)
    for k in range(7):
        ang = 2 * math.pi * k / 7 + rng.uniform(-0.2, 0.2)
        reach = rng.uniform(0.18, 0.3)
        height = rng.uniform(0.62, 0.98)
        tip = (reach * math.cos(ang), reach * math.sin(ang), height)
        m.cylinder_between((0.0, 0.0, 0.3), tip, 0.008, segments=6, mat=2)
        m.ellipsoid(tip, (0.16, 0.11, 0.02), mat=3 if k % 2 else 4, u_segments=12, v_segments=6,
                    rotation=Vector((math.cos(ang), math.sin(ang), 0.35)).to_track_quat("X", "Z"))
    m.build("Shop_Monstera", col, [M("M_PF_Terracotta"), M("M_PF_Soil"), M("M_PF_Leaf"), M("M_PF_Leaf"),
                                   M("M_PF_LeafLight")], location_design=(0.36, 0.32, 0.0), parent=root)

    w = P.MeshBuilder()                                                            # window-sill succulents
    for (yy, m_) in ((1.2, 0), (2.25, 1)):
        w.cylinder((0.0, yy), 0.05, 0.0, 0.08, segments=16, mat=m_)
        for k in range(6):
            ang = 2 * math.pi * k / 6
            w.ellipsoid((0.025 * math.cos(ang), yy + 0.025 * math.sin(ang), 0.1), (0.02, 0.02, 0.03), mat=2,
                        u_segments=8, v_segments=4)
    w.build("Shop_SillPlants", col, [M("M_PF_Terracotta"), M("M_PF_Blush"), M("M_PF_LeafLight")],
            location_design=(0.03, 0.0, WINDOW[2]), parent=root)

    cat = P.MeshBuilder()                                                          # Denchi, the shop cat
    cat.ellipsoid((0.0, 0.0, 0.08), (0.17, 0.11, 0.085), mat=0, u_segments=24, v_segments=10)
    cat.ellipsoid((0.05, 0.03, 0.12), (0.07, 0.06, 0.04), mat=1, u_segments=12, v_segments=6)
    cat.ellipsoid((-0.08, -0.03, 0.1), (0.05, 0.05, 0.03), mat=2, u_segments=12, v_segments=6)
    cat.ellipsoid((0.15, 0.0, 0.13), (0.075, 0.075, 0.068), mat=0, u_segments=20, v_segments=10)
    for sy in (-1, 1):
        cat.frustum(0.17, 0.235, 0.028, 0.004, segments=8, mat=0, center=(0.16, sy * 0.042))   # ears
    ears = cat.build("Deco_ShopCat", col, [M("M_PF_Paper"), M("M_PF_HairBlack"), M("M_PF_Ginger")],
                     location_design=(0.53, 1.67, 0.5), parent=root)
    tail = P.MeshBuilder()
    PF.torus(tail, (-0.12, 0.05, 0.03), 0.08, 0.018, mat=0, axis="Z", major_segments=16, minor_segments=8, arc_deg=150.0)
    tail.build("Deco_ShopCat_Tail", col, [M("M_PF_HairBlack")], parent=ears)
    face = P.MeshBuilder()
    for sy in (-1, 1):
        face.cylinder_between((0.218, sy * 0.025, 0.14), (0.226, sy * 0.025, 0.14), 0.006, segments=8)
    face.build("Deco_ShopCat_Face", col, [M("M_PF_Eye")], parent=ears)


def build_boxes_and_slot(col, root):
    b = P.MeshBuilder()
    bx(b, 4.62, 5.18, 1.42, 1.98, 0.0, 0.42, mat=0)
    bx(b, 4.62, 5.18, 1.68, 1.72, 0.42, 0.421, mat=1)
    bx(b, 4.7, 5.1, 1.5, 1.88, 0.42, 0.72, mat=0)
    bx(b, 4.7, 5.1, 1.67, 1.71, 0.72, 0.721, mat=1)
    bx(b, 4.75, 4.98, 1.418, 1.42, 0.12, 0.26, mat=2)                              # label
    b.build("Shop_Boxes", col, [M("M_PF_Cardboard"), M("M_PF_Butter"), M("M_PF_Paper")], parent=root,
            bevel=(0.008, 2))

    t = P.MeshBuilder()                                                            # upgrade slot floor tape
    x0, x1, y0, y1 = 3.35, 4.25, 0.45, 1.55
    dash, gap, w, z0, z1 = 0.12, 0.07, 0.04, 0.0005, 0.003

    def dashed(a0, a1, fixed, along_x):
        p = a0
        while p < a1 - 0.01:
            q = min(p + dash, a1)
            if along_x:
                bx(t, p, q, fixed - w * 0.5, fixed + w * 0.5, z0, z1)
            else:
                bx(t, fixed - w * 0.5, fixed + w * 0.5, p, q, z0, z1)
            p = q + gap

    dashed(x0, x1, y0, True)
    dashed(x0, x1, y1, True)
    dashed(y0, y1, x0, False)
    dashed(y0, y1, x1, False)
    cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
    bx(t, cx - 0.16, cx + 0.16, cy - 0.025, cy + 0.025, z0, z1)
    bx(t, cx - 0.025, cx + 0.025, cy - 0.16, cy + 0.16, z0, z1)
    t.build("Shop_UpgradeSlot", col, [M("M_PF_PickMe")], parent=root, smooth=False)


def build_lamps(col, root):
    for name, (x, y) in (("Counter", (2.6, 2.3)), ("Waiting", (1.05, 1.35))):
        b = P.MeshBuilder()
        b.cylinder((0.0, 0.0), 0.005, 0.16, WH - 2.0 + 0.02, segments=8, mat=1)    # cord up to the cut ceiling
        PF.shell_frustum(b, 0.0, 0.16, 0.13, 0.05, 0.006, mat=0, inner_mat=3, segments=32)
        b.cylinder((0.0, 0.0), 0.03, 0.16, 0.18, segments=16, mat=1)
        lamp = b.build(f"Shop_Pendant_{name}", col, [M("M_PF_Butter"), M("M_PF_Navy"), M("M_PF_Brass"),
                                                     M("M_PF_PaperCream")],
                       location_design=(x, y, 2.0), parent=root, bevel=(0.004, 2))
        bulb = P.MeshBuilder()
        bulb.ellipsoid((0.0, 0.0, 0.0), (0.04, 0.04, 0.045), u_segments=16, v_segments=8)
        bulb.build(f"Shop_Pendant_{name}_Bulb", col, [M("M_PF_LampBulb")], location_design=(0.0, 0.0, 0.06),
                   parent=lamp)
        PF.empty(f"Light_Pendant_{name}", col, (x, y, 1.98), parent=root)


# --------------------------------------------------------------------------------------
# Outside: sidewalk strip with a vending machine, bike, plants and the OPEN sign
# --------------------------------------------------------------------------------------

def build_exterior(col, root):
    s = P.MeshBuilder()
    bx(s, -1.6, -WT, -0.3, RD + 0.45, -0.02, 0.025)
    sw = s.build("Ext_Sidewalk", col, [M("M_PF_Pavement")], parent=root, bevel=(0.006, 2))
    seams = P.MeshBuilder()
    y = 0.1
    while y < RD + 0.4:
        bx(seams, -1.6, -WT, y - 0.004, y + 0.004, 0.025, 0.0262)
        y += 0.4
    for xx in (-1.2, -0.8, -0.45):
        bx(seams, xx - 0.004, xx + 0.004, -0.3, RD + 0.45, 0.025, 0.0262)
    seams.build("Ext_SidewalkSeams", col, [M("M_PF_PavementSeam")], parent=root, smooth=False)

    v = P.MeshBuilder()                                                            # vending machine, faces the front
    bx(v, -0.36, 0.36, -0.32, 0.32, 0.0, 1.82, mat=0)
    bx(v, -0.3, 0.3, -0.325, -0.32, 0.72, 1.7, mat=1)                              # glowing display window
    for r in range(3):
        for c in range(4):
            bx(v, -0.25 + c * 0.13, -0.17 + c * 0.13, -0.33, -0.325, 0.8 + r * 0.3, 0.98 + r * 0.3,
               mat=(2, 3, 4, 5)[(r + c) % 4])
    bx(v, -0.3, 0.3, -0.33, -0.32, 0.14, 0.34, mat=6)                              # pickup slot
    bx(v, 0.14, 0.26, -0.33, -0.32, 0.46, 0.62, mat=6)                             # coin panel
    v.build("Ext_VendingMachine", col, [M("M_PF_Paper"), M("M_PF_WindowGlow"), M("M_PF_DustyBlue"),
                                        M("M_PF_Blush"), M("M_PF_Butter"), M("M_PF_Sage"), M("M_PF_Charcoal")],
            location_design=(-0.92, 0.55, 0.025), parent=root, bevel=(0.02, 3))

    bike = P.MeshBuilder()                                                         # bicycle leaning on the wall
    for yy in (-0.42, 0.42):
        PF.torus(bike, (0.0, yy, 0.33), 0.3, 0.022, mat=0, axis="X", major_segments=32, minor_segments=8)
    bike.cylinder_between((0.0, -0.42, 0.33), (0.0, 0.0, 0.62), 0.016, segments=8, mat=1)
    bike.cylinder_between((0.0, 0.0, 0.62), (0.0, 0.42, 0.33), 0.016, segments=8, mat=1)
    bike.cylinder_between((0.0, -0.42, 0.33), (0.0, 0.1, 0.33), 0.016, segments=8, mat=1)
    bike.cylinder_between((0.0, 0.1, 0.33), (0.0, 0.0, 0.62), 0.016, segments=8, mat=1)
    bike.cylinder_between((0.0, 0.42, 0.33), (0.0, 0.38, 0.82), 0.016, segments=8, mat=1)
    bike.cylinder_between((0.0, 0.3, 0.84), (0.0, 0.48, 0.84), 0.014, segments=8, mat=0)
    PF.rounded_slab(bike, 0.0, -0.02, 0.1, 0.2, 0.04, 0.68, 0.72, mat=0)
    bike.build("Ext_Bicycle", col, [M("M_PF_Charcoal"), M("M_PF_Sage")], location_design=(-0.32, 1.7, 0.025),
               parent=root)

    pots = P.MeshBuilder()
    for (px, py, r) in ((-0.45, 2.82, 0.16), (-0.45, 4.2, 0.13)):
        pots.frustum(0.0, 0.32, r * 0.85, r, segments=20, mat=0, center=(px, py))
        for k in range(5):
            ang = 2 * math.pi * k / 5
            pots.ellipsoid((px + r * 0.6 * math.cos(ang), py + r * 0.6 * math.sin(ang), 0.42), (r * 0.6, r * 0.6, r * 0.5),
                           mat=1 if k % 2 else 2, u_segments=10, v_segments=6)
    pots.build("Ext_Plants", col, [M("M_PF_Terracotta"), M("M_PF_Leaf"), M("M_PF_LeafLight")],
               location_design=(0.0, 0.0, 0.025), parent=root)

    sign = P.MeshBuilder()                                                         # A-frame OPEN sign
    for sy in (-1, 1):
        sign.box((0.0, sy * 0.1, 0.38), (0.42, 0.03, 0.76), mat=0)
    sign.build("Ext_OpenSign", col, [M("M_PF_Paper")], location_design=(-1.15, -0.05, 0.025), parent=root,
               bevel=(0.01, 2))
    PF.text_object("OPEN", 0.12, 0.006, col, "Ext_OpenSignText", M("M_PF_Navy"), parent=root,
                   location=(-1.15, -0.168, 0.5), rotation=(math.radians(90.0), 0.0, 0.0), offset=0.004)
    return sw


def build_shadow_proxies(col, root):
    """Shadow-only box closers (Unity: ShadowCastingMode.ShadowsOnly, Blender: hidden from camera)."""
    proxies = []
    for name, (a, b_) in {
        "ShadowProxy_Ceiling": ((0.0, RW, 0.0, RD, WH - 0.06, WH - 0.01), None),
        "ShadowProxy_Front": ((0.0, RW, -0.06, 0.0, 0.0, WH), None),
        "ShadowProxy_Right": ((RW, RW + 0.06, 0.0, RD + WT, 0.0, WH), None),
    }.items():
        b = P.MeshBuilder()
        bx(b, *a)
        o = b.build(name, col, [M("M_PF_Plaster")], parent=root, smooth=False)
        for attr in ("visible_camera", "visible_glossy", "visible_diffuse"):
            try:
                setattr(o, attr, False)
            except AttributeError:
                pass
        proxies.append(o)
    return proxies


def build_spots(col, root):
    spots = {
        "Spot_Bench": (BENCH_SPOT[0], BENCH_SPOT[1], 0.0),
        "Spot_Customer_Counter": (1.82, 2.3, 0.0),
        "Spot_Waiting_Stool": (0.53, 0.95, 0.47),
        "Spot_Waiting_Chair": (0.53, 1.67, 0.5),
        "Spot_Door_Outside": (-0.9, 3.5, 0.025),
        "Spot_Door_Inside": (0.55, 3.5, 0.0),
        "Spot_Queue_1": (1.5, 3.05, 0.0),
        "Spot_ShopFocus": (2.6, 2.4, 0.9),
    }
    for name, loc in spots.items():
        PF.empty(name, col, loc, parent=root, size=0.2)


def build(col):
    root = P.make_empty(ROOT_NAME, col)
    build_plinth_and_floor(col, root)
    build_walls(col, root)
    build_noren(col, root)
    build_back_wall_dressing(col, root)
    build_shelf(col, root)
    build_counter(col, root)
    build_waiting(col, root)
    build_boxes_and_slot(col, root)
    build_lamps(col, root)
    build_exterior(col, root)
    build_shadow_proxies(col, root)
    build_spots(col, root)
    return root


def quick_preview(project_root, preview_col):
    import bpy
    out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "PF", f"{ROOT_NAME}_quick.png")
    PF.setup_beauty_render(resolution=(1600, 900), samples=32, world_hex="#F1DFC6", world_strength=0.9)
    PF.add_light(preview_col, "PF_Sun", "SUN", (-6.0, 1.0, 4.0), (1.5, 2.2, 0.0), "#FFC98B", 4.0, shadow_soft=0.06)
    for name, loc in (("Counter", (2.6, 2.3, 1.95)), ("Waiting", (1.05, 1.35, 1.95))):
        PF.add_light(preview_col, f"PF_Pendant_{name}", "POINT", loc, (loc[0], loc[1], 0.0), "#FFD9A1", 60.0,
                     shadow_soft=0.05)
    PF.add_light(preview_col, "PF_Fill", "AREA", (9.0, -5.0, 6.0), (2.6, 2.4, 0.5), "#9CC3C9", 900.0, size=6.0)
    cam = PF.camera(preview_col, lens=50.0)
    PF.render_still(cam, (13.2, -10.6, 10.4), (2.35, 2.25, 0.55), out, lens=58.0)
    return out


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)
    P.reset_scene()
    PF.write_palette_json(project_root)
    col = P.get_collection(ROOT_NAME)
    preview_col = P.get_collection("Preview")
    root = build(col)

    fbx_path = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Environment", "PF", f"{ROOT_NAME}.fbx")
    P.export_fbx_for_unity(root, fbx_path)
    print(f"PRS_EXPORTED {fbx_path}")
    print(f"PF_TRIS {PF.count_tris(root)}")
    if "--no-preview" not in args:
        print("PRS_PREVIEW " + quick_preview(project_root, preview_col))
        for o in list(preview_col.objects):
            bpy.data.objects.remove(o)
    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Environment", f"{ROOT_NAME}.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")


if __name__ == "__main__":
    main()

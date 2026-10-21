"""
CHR_PF_* - Phone Fixer customers: one rig, many neighbours (art direction v1).

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_pf_customers.py
    (append "-- --no-preview" to skip renders)

Outputs:
    ArtSource/Blender/Characters/CHR_PF_Customers.blend          all variants, editable
    Assets/_Project/Art/Models/Characters/PF/CHR_PF_Base.fbx      mannequin + ALL animation clips
    Assets/_Project/Art/Models/Characters/PF/CHR_PF_<Name>.fbx    Fumi, Ren, Sasaki, Mio, Kanemochi (mesh + rig)
    Assets/_Project/Art/UI/Portraits/PF_Portrait_<Name>.png       2D dialogue portraits
    ArtSource/Blender/Previews/PF/CHR_PF_Lineup.png

Rig contract (identical in every file, so the base's clips drive every variant in Unity):
    objects "Armature" (bones below) + "Body" (one skinned mesh, rigid weights, one bone per part)
    Root > Hips > Spine > Chest > Neck > Head > Eye.L/R, Brow.L/R, Mouth
    Chest > UpperArm.L/R > LowerArm > Hand ; Hips > UpperLeg.L/R > LowerLeg > Foot
    Characters face +Y (Unity +Z, transform.forward); their right hand is +X.
    Local axes: limbs/torso bend forward with +X rotation; arms raise sideways with +Z (left) / -Z (right);
    face bones point forward: scale Z = squash vertically (blink), rotate Y = tilt brows, Mouth rotated 180 about Y
    = frown (never key negative or zero scale: the FBX SDK turns it into NaN rotations).
    All variants are built at the base height (1.38 m); Unity scales the prefab root per character, so
    shared clips with root/hip motion never make anyone float.
Clips: Idle, Walk, Talk, Celebrate, Disappointed, Confused, Impatient (30 fps).
"""

import math
import os
import sys

import bpy
from mathutils import Euler, Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
import pf_bpy as PF  # noqa: E402

FPS = 30
HEAD_C = Vector((0.0, 0.0, 1.135))
HEAD_R = (0.28, 0.24, 0.245)

BONES = [
    # name, head, tail, parent, roll_axis
    ("Root", (0.0, 0.0, 0.0), (0.0, 0.0, 0.12), None, (0, 1, 0)),
    ("Hips", (0.0, 0.0, 0.36), (0.0, 0.0, 0.46), "Root", (0, 1, 0)),
    ("Spine", (0.0, 0.0, 0.46), (0.0, 0.0, 0.66), "Hips", (0, 1, 0)),
    ("Chest", (0.0, 0.0, 0.66), (0.0, 0.0, 0.88), "Spine", (0, 1, 0)),
    ("Neck", (0.0, 0.0, 0.88), (0.0, 0.0, 0.93), "Chest", (0, 1, 0)),
    ("Head", (0.0, 0.0, 0.93), (0.0, 0.0, 1.38), "Neck", (0, 1, 0)),
    ("Eye.L", (-0.093, 0.20, 1.152), (-0.093, 0.27, 1.152), "Head", (0, 0, 1)),
    ("Eye.R", (0.093, 0.20, 1.152), (0.093, 0.27, 1.152), "Head", (0, 0, 1)),
    ("Brow.L", (-0.095, 0.18, 1.255), (-0.095, 0.25, 1.255), "Head", (0, 0, 1)),
    ("Brow.R", (0.095, 0.18, 1.255), (0.095, 0.25, 1.255), "Head", (0, 0, 1)),
    ("Mouth", (0.0, 0.20, 1.05), (0.0, 0.27, 1.05), "Head", (0, 0, 1)),
]
for side, sx in (("L", -1.0), ("R", 1.0)):
    BONES += [
        (f"UpperArm.{side}", (sx * 0.19, 0.0, 0.82), (sx * 0.226, 0.0, 0.645), "Chest", (0, 1, 0)),
        (f"LowerArm.{side}", (sx * 0.226, 0.0, 0.645), (sx * 0.248, 0.0, 0.50), f"UpperArm.{side}", (0, 1, 0)),
        (f"Hand.{side}", (sx * 0.248, 0.0, 0.50), (sx * 0.255, 0.0, 0.42), f"LowerArm.{side}", (0, 1, 0)),
        (f"UpperLeg.{side}", (sx * 0.09, 0.0, 0.36), (sx * 0.09, 0.0, 0.21), "Hips", (0, 1, 0)),
        (f"LowerLeg.{side}", (sx * 0.09, 0.0, 0.21), (sx * 0.09, 0.0, 0.07), f"UpperLeg.{side}", (0, 1, 0)),
        (f"Foot.{side}", (sx * 0.09, 0.0, 0.07), (sx * 0.09, 0.12, 0.03), f"LowerLeg.{side}", (0, 0, 1)),
    ]


def M(name):
    return PF.M(name)


def head_y(x, z_rel, inset=0.0):
    """Front surface of the head ellipsoid (y) at x and height z relative to the head centre."""
    u = 1.0 - (x / HEAD_R[0]) ** 2 - (z_rel / HEAD_R[2]) ** 2
    return HEAD_R[1] * math.sqrt(max(0.0, u)) - inset


# --------------------------------------------------------------------------------------
# Rig + mesh assembly
# --------------------------------------------------------------------------------------

def make_armature(name, collection):
    data = bpy.data.armatures.new(name + "_Data")
    arm = bpy.data.objects.new(name, data)
    collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    for o in bpy.context.view_layer.objects:
        o.select_set(o == arm)
    bpy.ops.object.mode_set(mode="EDIT")
    edit = {}
    for bname, head, tail, parent, roll_axis in BONES:
        eb = data.edit_bones.new(bname)
        eb.head = Vector(head)
        eb.tail = Vector(tail)
        eb.align_roll(Vector(roll_axis))
        if parent:
            eb.parent = edit[parent]
        eb.use_connect = False
        eb.use_deform = True
        edit[bname] = eb
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
    data.display_type = "STICK"
    return arm


class Parts:
    """Collects (builder, bone, materials) and joins them into one rigidly skinned mesh."""

    def __init__(self):
        self.items = []

    def add(self, bone, *mat_names):
        b = P.MeshBuilder()
        self.items.append((b, bone, [M(n) for n in mat_names]))
        return b

    def assemble(self, name, collection, armature):
        objs = []
        for i, (b, bone, mats) in enumerate(self.items):
            if not b.bm.faces:
                b.bm.free()
                continue
            o = b.build(f"{name}_p{i}", collection, mats, sharp_angle_deg=60.0)
            vg = o.vertex_groups.new(name=bone)
            vg.add(list(range(len(o.data.vertices))), 1.0, "REPLACE")
            objs.append(o)
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.join()
        body = bpy.context.view_layer.objects.active
        body.name = name
        body.data.name = name + "_Mesh"
        body.parent = armature
        mod = body.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        return body


# --------------------------------------------------------------------------------------
# Shared body
# --------------------------------------------------------------------------------------

def body_base(parts, skin, top, bottom, shoes, legs=None, arms=None, torso_scale=1.0, short_legs_skin=False):
    """The gumdrop body every customer shares. legs/arms default to the bottom/top materials."""
    legs = legs or bottom
    arms = arms or top
    s = torso_scale
    t = parts.add("Chest", top)
    t.frustum(0.40, 0.84, 0.205 * s, 0.16 * s, segments=28, scale_xy=(1.0, 0.78))
    t.ellipsoid((0.0, 0.0, 0.40), (0.205 * s, 0.16 * s, 0.06), u_segments=28, v_segments=10)
    t.ellipsoid((0.0, 0.0, 0.84), (0.16 * s, 0.125 * s, 0.085), u_segments=28, v_segments=10)
    p = parts.add("Hips", bottom)
    p.frustum(0.30, 0.42, 0.185, 0.19, segments=24, scale_xy=(1.0, 0.8))
    p.ellipsoid((0.0, 0.0, 0.30), (0.185, 0.148, 0.05), u_segments=24, v_segments=8)
    for side, sx in (("L", -1.0), ("R", 1.0)):
        th = parts.add(f"UpperLeg.{side}", skin if short_legs_skin else legs)
        PF.capsule(th, (sx * 0.09, 0.0, 0.34), (sx * 0.09, 0.0, 0.215), 0.058, segments=14)
        sh = parts.add(f"LowerLeg.{side}", legs)
        PF.capsule(sh, (sx * 0.09, 0.0, 0.205), (sx * 0.09, 0.0, 0.085), 0.052, segments=14)
        ft = parts.add(f"Foot.{side}", shoes)
        ft.ellipsoid((sx * 0.09, 0.035, 0.045), (0.07, 0.105, 0.05), u_segments=16, v_segments=8)
        ua = parts.add(f"UpperArm.{side}", arms)
        PF.capsule(ua, (sx * 0.19, 0.0, 0.80), (sx * 0.226, 0.0, 0.655), 0.048, segments=14)
        la = parts.add(f"LowerArm.{side}", arms)
        PF.capsule(la, (sx * 0.23, 0.0, 0.64), (sx * 0.248, 0.0, 0.515), 0.044, segments=14)
        hd = parts.add(f"Hand.{side}", skin)
        hd.ellipsoid((sx * 0.252, 0.01, 0.468), (0.052, 0.045, 0.058), u_segments=16, v_segments=8)


def head_base(parts, skin, brow="M_PF_Eye", mouth="M_PF_Mouth", sparkle=False, brow_tilt=0.0, eye_scale=1.0):
    h = parts.add("Head", skin, "M_PF_Cheek")
    h.ellipsoid(tuple(HEAD_C), HEAD_R, u_segments=36, v_segments=18)
    for sx in (-1.0, 1.0):
        h.ellipsoid((sx * 0.275, -0.01, HEAD_C.z), (0.045, 0.03, 0.055), u_segments=12, v_segments=6)
        h.ellipsoid((sx * 0.17, head_y(0.17, -0.075, 0.004), HEAD_C.z - 0.075), (0.042, 0.008, 0.022), mat=1,
                    u_segments=14, v_segments=6)
    for side, sx in (("L", -1.0), ("R", 1.0)):
        e = parts.add(f"Eye.{side}", "M_PF_Eye", "M_PF_EyeShine")
        ex, ez = sx * 0.093, HEAD_C.z + 0.017
        ey = head_y(0.093, 0.017, 0.004)
        e.ellipsoid((ex, ey, ez), (0.033 * eye_scale, 0.014, 0.05 * eye_scale), u_segments=16, v_segments=10)
        e.ellipsoid((ex + 0.013, ey + 0.012, ez + 0.022), (0.011, 0.006, 0.011), mat=1, u_segments=8, v_segments=4)
        if sparkle:
            e.ellipsoid((ex - 0.01, ey + 0.012, ez - 0.02), (0.006, 0.004, 0.006), mat=1, u_segments=8, v_segments=4)
        b = parts.add(f"Brow.{side}", brow)
        by = head_y(0.095, 0.12, 0.002)
        inner, outer = sx * 0.065, sx * 0.125
        tilt = brow_tilt                                 # >0: inner ends up (worried)
        PF.capsule(b, (inner, by, 1.255 + tilt), (outer, by - 0.012, 1.255 - tilt * 0.4), 0.011, segments=10)
    m = parts.add("Mouth", mouth)
    PF.torus(m, (0.0, head_y(0.0, -0.085, 0.003), 1.05), 0.032, 0.0065, axis="Y", major_segments=14,
             minor_segments=6, arc_deg=120.0, start_deg=210.0)


# --------------------------------------------------------------------------------------
# Variants
# --------------------------------------------------------------------------------------

def variant_base(parts):
    body_base(parts, "M_PF_Kraft", "M_PF_PaperCream", "M_PF_PaperCream", "M_PF_Cardboard")
    head_base(parts, "M_PF_Kraft")


def variant_fumi(parts):
    skin = "M_PF_SkinPeach"
    body_base(parts, skin, "M_PF_Sage", "M_PF_DustyBlue", "M_PF_Charcoal", legs="M_PF_PaperCream")
    head_base(parts, skin, brow="M_PF_SteelDark")
    c = parts.add("Chest", "M_PF_Paper")                                   # blouse V and buttons
    c.ellipsoid((0.0, 0.118, 0.82), (0.06, 0.03, 0.07), u_segments=12, v_segments=6)
    for z in (0.7, 0.6):
        c.ellipsoid((0.0, 0.155, z), (0.014, 0.008, 0.014), u_segments=8, v_segments=4)
    sk = parts.add("Hips", "M_PF_DustyBlue")                               # long skirt
    sk.frustum(0.12, 0.42, 0.26, 0.19, segments=28, scale_xy=(1.0, 0.85))
    hair = parts.add("Head", "M_PF_HairSilver", "M_PF_Brass")
    hair.ellipsoid((0.0, -0.04, 1.2), (0.29, 0.235, 0.19), u_segments=32, v_segments=14)
    for sx in (-1.0, 1.0):
        hair.ellipsoid((sx * 0.25, -0.03, 1.12), (0.07, 0.13, 0.14), u_segments=14, v_segments=8)
    hair.ellipsoid((0.0, -0.06, 1.43), (0.11, 0.1, 0.09), u_segments=18, v_segments=8)
    hair.cylinder_between((-0.12, -0.02, 1.44), (0.12, -0.1, 1.4), 0.008, segments=8, mat=1)
    g = parts.add("Head", "M_PF_Navy")                                     # round glasses
    for sx in (-1.0, 1.0):
        PF.torus(g, (sx * 0.093, head_y(0.093, 0.017, -0.012), HEAD_C.z + 0.017), 0.058, 0.007, axis="Y",
                 major_segments=24, minor_segments=6)
        g.cylinder_between((sx * 0.151, head_y(0.15, 0.017, -0.005), HEAD_C.z + 0.02),
                           (sx * 0.27, 0.0, HEAD_C.z + 0.03), 0.005, segments=6)
    g.cylinder_between((-0.035, head_y(0.03, 0.02, -0.014), HEAD_C.z + 0.02),
                       (0.035, head_y(0.03, 0.02, -0.014), HEAD_C.z + 0.02), 0.005, segments=6)
    bag = parts.add("LowerArm.L", "M_PF_Plum")                             # quilted handbag on the forearm
    bag.box((-0.3, 0.04, 0.53), (0.07, 0.18, 0.15))
    PF.torus(bag, (-0.27, 0.04, 0.61), 0.06, 0.008, axis="X", major_segments=16, minor_segments=6, arc_deg=180.0)


def variant_ren(parts):
    skin = "M_PF_SkinTan"
    body_base(parts, skin, "M_PF_Wisteria", "M_PF_ClothDenim", "M_PF_White")
    head_base(parts, skin)
    hood = parts.add("Chest", "M_PF_Wisteria", "M_PF_WisteriaDark", "M_PF_Paper")
    hood.ellipsoid((0.0, -0.15, 0.93), (0.2, 0.08, 0.1), u_segments=18, v_segments=8)
    hood.ellipsoid((0.0, 0.15, 0.56), (0.12, 0.02, 0.06), mat=1, u_segments=14, v_segments=6)   # pocket
    for sx in (-1.0, 1.0):
        PF.capsule(hood, (sx * 0.04, 0.155, 0.86), (sx * 0.05, 0.17, 0.72), 0.007, mat=2, segments=8)
    hp = parts.add("Neck", "M_PF_Butter", "M_PF_Brass")                    # headphones around the neck
    PF.torus(hp, (0.0, 0.0, 0.9), 0.2, 0.02, axis="Z", major_segments=24, minor_segments=8, arc_deg=220.0,
             start_deg=160.0)
    for sx in (-1.0, 1.0):
        hp.ellipsoid((sx * 0.19, 0.08, 0.88), (0.05, 0.045, 0.06), u_segments=14, v_segments=8)
    hair = parts.add("Head", "M_PF_HairBlack")
    hair.ellipsoid((0.0, -0.035, 1.21), (0.3, 0.25, 0.2), u_segments=32, v_segments=14)
    rng = P.seeded_random(41)
    for k in range(11):
        ang = math.radians(-160 + k * 32)
        base = Vector((0.24 * math.cos(ang) * 0.9, -0.035 + 0.2 * math.sin(ang), 1.3 + rng.uniform(-0.03, 0.05)))
        tip = base + Vector((math.cos(ang) * 0.08, math.sin(ang) * 0.06, rng.uniform(0.05, 0.11)))
        hair.cylinder_between(tuple(base), tuple(tip), 0.06, segments=10, radius_end=0.005)
    for k, x in enumerate((-0.12, -0.04, 0.05, 0.13)):                     # bangs
        top = (x, 0.17, 1.34)
        tip = (x + 0.02, 0.215, 1.27 + 0.01 * (k % 2))
        hair.cylinder_between(top, tip, 0.045, segments=10, radius_end=0.006)
    pts = [(0.0, -0.02, 1.4), (0.03, 0.02, 1.47), (0.0, 0.08, 1.5), (-0.03, 0.1, 1.46)]
    for a, c in zip(pts, pts[1:]):
        PF.capsule(hair, a, c, 0.012, segments=8)                           # ahoge
    lids = parts.add("Head", skin, "M_PF_HairBlack", "M_PF_SkinWarm")      # sleepy lids, eye bags
    for sx in (-1.0, 1.0):
        ex = sx * 0.093
        lids.ellipsoid((ex, head_y(0.093, 0.035, -0.002), HEAD_C.z + 0.045), (0.042, 0.013, 0.034),
                       u_segments=14, v_segments=8)
        lids.cylinder_between((ex - 0.04, head_y(0.12, 0.012, -0.012), HEAD_C.z + 0.015),
                              (ex + 0.04, head_y(0.07, 0.012, -0.012), HEAD_C.z + 0.015), 0.006, segments=8, mat=1)
        lids.cylinder_between((ex - 0.025, head_y(0.11, -0.04, -0.004), HEAD_C.z - 0.04),
                              (ex + 0.025, head_y(0.08, -0.04, -0.004), HEAD_C.z - 0.04), 0.005, segments=6, mat=2)


def variant_sasaki(parts):
    skin = "M_PF_SkinWarm"
    body_base(parts, skin, "M_PF_ClothSuit", "M_PF_ClothSuit", "M_PF_WoodDark")
    head_base(parts, skin, brow_tilt=0.016)
    c = parts.add("Chest", "M_PF_Paper", "M_PF_Mustard", "M_PF_Navy")
    c.ellipsoid((0.0, 0.122, 0.81), (0.07, 0.03, 0.075), u_segments=12, v_segments=6)
    pts = [(0.0, 0.158, 0.85), (0.006, 0.168, 0.76), (0.02, 0.175, 0.66), (0.028, 0.176, 0.62)]
    for a, b_ in zip(pts, pts[1:]):
        PF.capsule(c, a, b_, 0.022, mat=1, segments=10)                    # loose mustard tie
    for sx in (-1.0, 1.0):
        c.cylinder_between((sx * 0.06, 0.142, 0.86), (sx * 0.1, 0.16, 0.66), 0.012, segments=8, mat=2)  # lapels
    hair = parts.add("Head", "M_PF_HairEspresso")
    hair.ellipsoid((0.0, -0.03, 1.215), (0.295, 0.25, 0.19), u_segments=32, v_segments=14)
    PF.capsule(hair, (-0.17, 0.1, 1.33), (0.12, 0.13, 1.36), 0.035, segments=10)
    pts = [(0.03, 0.15, 1.34), (0.06, 0.2, 1.42), (0.1, 0.22, 1.46), (0.13, 0.2, 1.43)]
    for a, b_ in zip(pts, pts[1:]):
        PF.capsule(hair, a, b_, 0.011, segments=8)                          # the one loose strand
    case = parts.add("Hand.R", "M_PF_WoodWalnut", "M_PF_Brass")
    case.box((0.3, 0.02, 0.36), (0.08, 0.3, 0.22))
    PF.torus(case, (0.3, 0.02, 0.48), 0.045, 0.009, mat=1, axis="X", major_segments=16, minor_segments=6, arc_deg=180.0)
    drop = parts.add("Head", "M_PF_ScreenIdle")
    drop.ellipsoid((0.25, 0.12, 1.31), (0.022, 0.012, 0.03), u_segments=12, v_segments=6)
    drop.cylinder_between((0.25, 0.12, 1.33), (0.25, 0.12, 1.36), 0.01, segments=8, radius_end=0.001)


def variant_mio(parts):
    skin = "M_PF_SkinLight"
    body_base(parts, skin, "M_PF_Butter", "M_PF_ClothSkirt", "M_PF_White", legs="M_PF_Paper",
              short_legs_skin=True)
    head_base(parts, skin, mouth="M_PF_Mouth", sparkle=True, eye_scale=1.12)
    jk = parts.add("Chest", "M_PF_Mustard", "M_PF_Navy")
    PF.torus(jk, (0.0, 0.0, 0.88), 0.14, 0.03, axis="Z", major_segments=24, minor_segments=8)       # collar
    jk.cylinder_between((0.0, 0.16, 0.86), (0.0, 0.2, 0.44), 0.006, segments=6, mat=1)             # zip
    sk = parts.add("Hips", "M_PF_ClothSkirt")
    sk.frustum(0.24, 0.42, 0.245, 0.19, segments=24, scale_xy=(1.0, 0.85))
    bp = parts.add("Chest", "M_PF_DustyBlue", "M_PF_Navy", "M_PF_Blush", "M_PF_Sage", "M_PF_Wisteria", "M_PF_Butter")
    PF.rounded_slab(bp, 0.0, -0.215, 0.3, 0.14, 0.05, 0.48, 0.84)
    for sx in (-1.0, 1.0):
        bp.cylinder_between((sx * 0.1, -0.13, 0.84), (sx * 0.12, 0.13, 0.66), 0.018, segments=8, mat=1)
    for k, m in enumerate((2, 3, 4, 5)):
        bp.cylinder_between((0.16, -0.2 - 0.02 * k, 0.6), (0.17, -0.2 - 0.02 * k, 0.53 - 0.02 * k), 0.003,
                            segments=6, mat=1)
        bp.ellipsoid((0.17, -0.2 - 0.02 * k, 0.51 - 0.02 * k), (0.022, 0.022, 0.022), mat=m, u_segments=10, v_segments=5)
    hair = parts.add("Head", "M_PF_HairBrown", "M_PF_Blush")
    hair.ellipsoid((0.0, -0.025, 1.2), (0.3, 0.255, 0.21), u_segments=32, v_segments=14)
    for sx in (-1.0, 1.0):
        hair.ellipsoid((sx * 0.25, -0.01, 1.06), (0.085, 0.17, 0.18), u_segments=16, v_segments=10)
    hair.ellipsoid((0.0, 0.17, 1.31), (0.22, 0.07, 0.05), u_segments=20, v_segments=8)               # bangs
    hair.box((0.16, 0.19, 1.31), (0.07, 0.02, 0.03), mat=1)                                          # clip
    wave = parts.add("UpperArm.R", "M_PF_Butter")                          # (arm pose comes from the clips)
    wave.ellipsoid((0.2, 0.0, 0.79), (0.055, 0.055, 0.04), u_segments=12, v_segments=6)


def variant_kanemochi(parts):
    skin = "M_PF_SkinDeep"
    body_base(parts, skin, "M_PF_Fur", "M_PF_Fur", "M_PF_Brass", legs="M_PF_HairEspresso", torso_scale=1.22)
    head_base(parts, skin, mouth="M_PF_Plum")
    fur = parts.add("Chest", "M_PF_Fur", "M_PF_Paper")
    for k in range(9):
        ang = 2 * math.pi * k / 9
        fur.ellipsoid((0.17 * math.cos(ang), 0.14 * math.sin(ang), 0.9), (0.07, 0.07, 0.06), mat=k % 2,
                      u_segments=12, v_segments=6)
    hair = parts.add("Head", "M_PF_HairPlum", "M_PF_HairSilver")
    hair.ellipsoid((0.0, -0.035, 1.21), (0.3, 0.25, 0.2), u_segments=32, v_segments=14)
    hair.ellipsoid((0.0, -0.05, 1.45), (0.2, 0.18, 0.2), u_segments=24, v_segments=12)
    hair.ellipsoid((0.0, -0.06, 1.62), (0.13, 0.12, 0.11), u_segments=20, v_segments=10)
    pts = [(0.03, 0.13, 1.33), (0.03, 0.12, 1.47), (0.02, 0.07, 1.6), (0.0, 0.0, 1.71)]
    for a, b_ in zip(pts, pts[1:]):
        PF.capsule(hair, a, b_, 0.022, mat=1, segments=10)                  # silver streak
    sg = parts.add("Head", "M_PF_Navy", "M_PF_Brass")                      # big round sunglasses
    for sx in (-1.0, 1.0):
        cx, cy, cz = sx * 0.1, head_y(0.1, 0.02, -0.022), HEAD_C.z + 0.02
        sg.ellipsoid((cx, cy, cz), (0.075, 0.012, 0.064), u_segments=20, v_segments=10)
        PF.torus(sg, (cx, cy + 0.004, cz), 0.072, 0.008, mat=1, axis="Y", major_segments=24, minor_segments=6)
        sg.cylinder_between((sx * 0.172, cy - 0.01, cz), (sx * 0.27, 0.0, cz + 0.01), 0.006, segments=6, mat=1)
    sg.cylinder_between((-0.03, head_y(0.03, 0.03, -0.03), HEAD_C.z + 0.03),
                        (0.03, head_y(0.03, 0.03, -0.03), HEAD_C.z + 0.03), 0.007, segments=6, mat=1)
    ear = parts.add("Head", "M_PF_Brass")
    for sx in (-1.0, 1.0):
        ear.ellipsoid((sx * 0.285, 0.0, 1.045), (0.025, 0.025, 0.025), u_segments=10, v_segments=6)
    dog = parts.add("LowerArm.L", "M_PF_White", "M_PF_Eye", "M_PF_Blush")  # Bijou
    dog.ellipsoid((-0.32, 0.08, 0.6), (0.09, 0.08, 0.07), u_segments=16, v_segments=8)
    dog.ellipsoid((-0.32, 0.15, 0.68), (0.065, 0.06, 0.06), u_segments=16, v_segments=8)
    for sx in (-1.0, 1.0):
        dog.ellipsoid((-0.32 + sx * 0.045, 0.14, 0.74), (0.022, 0.015, 0.035), u_segments=8, v_segments=4)
        dog.ellipsoid((-0.32 + sx * 0.022, 0.205, 0.695), (0.008, 0.005, 0.01), mat=1, u_segments=8, v_segments=4)
    dog.ellipsoid((-0.32, 0.212, 0.672), (0.012, 0.008, 0.009), mat=1, u_segments=8, v_segments=4)
    dog.ellipsoid((-0.32, 0.205, 0.652), (0.01, 0.006, 0.014), mat=2, u_segments=8, v_segments=4)


VARIANTS = {
    "Base": variant_base,
    "Fumi": variant_fumi,
    "Ren": variant_ren,
    "Sasaki": variant_sasaki,
    "Mio": variant_mio,
    "Kanemochi": variant_kanemochi,
}
# Unity applies these as prefab root scale (see module docstring)
HEIGHT_SCALE = {"Base": 1.0, "Fumi": 0.94, "Ren": 1.0, "Sasaki": 1.04, "Mio": 0.9, "Kanemochi": 1.0}


# --------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------

def key(arm, bone, frame, rot=None, loc=None, scale=None):
    pb = arm.pose.bones[bone]
    if rot is not None:
        pb.rotation_euler = Euler([math.radians(a) for a in rot], "XYZ")
        pb.keyframe_insert("rotation_euler", frame=frame)
    if loc is not None:
        pb.location = Vector(loc)
        pb.keyframe_insert("location", frame=frame)
    if scale is not None:
        pb.scale = Vector(scale)
        pb.keyframe_insert("scale", frame=frame)


def reset_pose(arm):
    for pb in arm.pose.bones:
        pb.location = (0.0, 0.0, 0.0)
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)


def new_action(arm, name):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    reset_pose(arm)
    return act


def set_range(act, start, end):
    try:
        act.use_frame_range = True
        act.frame_start = start
        act.frame_end = end
    except AttributeError:
        pass


def anim_idle(arm):
    act = new_action(arm, "Idle")
    for f, bob, sway, arm_x in ((1, 0.0, -3.0, 3.0), (31, 0.012, 3.0, -3.0), (61, 0.0, -3.0, 3.0)):
        key(arm, "Hips", f, loc=(0.0, bob, 0.0))
        key(arm, "Chest", f, rot=(2.0 if bob else 0.0, 0.0, 0.0))
        key(arm, "Head", f, rot=(0.0, 0.0, sway))
        key(arm, "UpperArm.L", f, rot=(arm_x, 0.0, 4.0))
        key(arm, "UpperArm.R", f, rot=(-arm_x, 0.0, -4.0))
    for side in ("L", "R"):
        for f, s in ((1, 1.0), (40, 1.0), (42, 0.1), (44, 1.0), (61, 1.0)):
            key(arm, f"Eye.{side}", f, scale=(1.0, 1.0, s))
    set_range(act, 1, 61)
    return act


def anim_walk(arm):
    act = new_action(arm, "Walk")
    for f in (1, 13, 25):
        phase = 1.0 if f in (1, 25) else -1.0
        key(arm, "UpperLeg.L", f, rot=(25.0 * phase, 0.0, 0.0))
        key(arm, "UpperLeg.R", f, rot=(-25.0 * phase, 0.0, 0.0))
        key(arm, "UpperArm.L", f, rot=(-22.0 * phase, 0.0, 6.0))
        key(arm, "UpperArm.R", f, rot=(22.0 * phase, 0.0, -6.0))
        key(arm, "Hips", f, rot=(0.0, 4.0 * phase, 0.0), loc=(0.0, 0.0, 0.0))
    for f in (7, 19):
        key(arm, "Hips", f, rot=(0.0, 0.0, 0.0), loc=(0.0, 0.035, 0.0))
    for f, l, r in ((1, -5, -10), (7, -35, -5), (13, -5, -5), (19, -10, -35), (25, -5, -10)):
        key(arm, "LowerLeg.L", f, rot=(l, 0.0, 0.0))
        key(arm, "LowerLeg.R", f, rot=(r, 0.0, 0.0))
    for f, hx in ((1, -2.0), (7, 3.0), (13, -2.0), (19, 3.0), (25, -2.0)):
        key(arm, "Head", f, rot=(hx, 0.0, 0.0))
        key(arm, "Spine", f, rot=(4.0, 0.0, 0.0))
    set_range(act, 1, 25)
    return act


def anim_talk(arm):
    act = new_action(arm, "Talk")
    for f, nod in ((1, 0.0), (10, 6.0), (19, 0.0), (28, 5.0), (37, 0.0)):
        key(arm, "Head", f, rot=(nod, 0.0, 0.0))
    for f, s in ((1, (1, 1, 1)), (5, (0.85, 1, 1.9)), (9, (1.05, 1, 0.9)), (14, (0.9, 1, 1.7)), (18, (1, 1, 1)),
                 (24, (0.85, 1, 1.8)), (30, (1, 1, 1)), (37, (1, 1, 1))):
        key(arm, "Mouth", f, scale=s)
    for f, ua, la in ((1, 0.0, 0.0), (12, 28.0, 45.0), (22, 20.0, 35.0), (30, 0.0, 0.0), (37, 0.0, 0.0)):
        key(arm, "UpperArm.R", f, rot=(ua, 0.0, -6.0))
        key(arm, "LowerArm.R", f, rot=(la, 0.0, 0.0))
    for f, b in ((1, 0.0), (10, 0.012), (19, 0.0), (37, 0.0)):
        key(arm, "Brow.L", f, loc=(0.0, 0.0, b))
        key(arm, "Brow.R", f, loc=(0.0, 0.0, b))
    set_range(act, 1, 37)
    return act


def anim_celebrate(arm):
    act = new_action(arm, "Celebrate")
    for f, hips, root in ((1, 0.0, 0.0), (7, -0.05, 0.0), (12, 0.02, 0.1), (16, 0.0, 0.22), (19, 0.0, 0.2),
                          (24, -0.045, 0.0), (30, 0.0, 0.0), (41, 0.0, 0.0)):
        key(arm, "Hips", f, loc=(0.0, hips, 0.0))
        key(arm, "Root", f, loc=(0.0, root, 0.0))
    for f, up, leg, knee in ((1, 0, 0, 0), (7, 10, 22, -42), (14, 150, 30, -50), (26, 150, 0, 0), (36, 30, 0, 0),
                             (41, 0, 0, 0)):
        key(arm, "UpperArm.L", f, rot=(0.0, 0.0, up))
        key(arm, "UpperArm.R", f, rot=(0.0, 0.0, -up))
        key(arm, "UpperLeg.L", f, rot=(leg, 0.0, 0.0))
        key(arm, "UpperLeg.R", f, rot=(leg, 0.0, 0.0))
        key(arm, "LowerLeg.L", f, rot=(knee, 0.0, 0.0))
        key(arm, "LowerLeg.R", f, rot=(knee, 0.0, 0.0))
    for side in ("L", "R"):
        for f, s in ((1, (1, 1, 1)), (8, (1, 1, 1)), (11, (1.1, 1, 0.3)), (30, (1.1, 1, 0.3)), (36, (1, 1, 1)),
                     (41, (1, 1, 1))):
            key(arm, f"Eye.{side}", f, scale=s)
        for f, b in ((1, 0.0), (10, 0.022), (30, 0.022), (36, 0.0), (41, 0.0)):
            key(arm, f"Brow.{side}", f, loc=(0.0, 0.0, b))
    for f, s in ((1, (1, 1, 1)), (9, (1, 1, 1)), (12, (1.35, 1, 2.1)), (30, (1.35, 1, 2.1)), (36, (1, 1, 1)),
                 (41, (1, 1, 1))):
        key(arm, "Mouth", f, scale=s)
    for f, hx in ((1, 0.0), (12, -10.0), (24, 0.0), (41, 0.0)):
        key(arm, "Head", f, rot=(hx, 0.0, 0.0))
    set_range(act, 1, 41)
    return act


def anim_disappointed(arm):
    act = new_action(arm, "Disappointed")
    for f, k in ((1, 0.0), (20, 1.0), (46, 1.0)):
        key(arm, "Chest", f, rot=(14.0 * k, 0.0, 0.0))
        key(arm, "Spine", f, rot=(6.0 * k, 0.0, 0.0))
        key(arm, "Head", f, rot=(20.0 * k, 0.0, 0.0))
        key(arm, "UpperArm.L", f, rot=(0.0, 0.0, -8.0 * k))
        key(arm, "UpperArm.R", f, rot=(0.0, 0.0, 8.0 * k))
        key(arm, "Hips", f, loc=(0.0, -0.02 * k, 0.0))
        key(arm, "Brow.L", f, rot=(0.0, -20.0 * k, 0.0))
        key(arm, "Brow.R", f, rot=(0.0, 20.0 * k, 0.0))
        key(arm, "Eye.L", f, scale=(1.0, 1.0, 1.0 - 0.2 * k))
        key(arm, "Eye.R", f, scale=(1.0, 1.0, 1.0 - 0.2 * k))
    # Frown: squash the smile flat, flip it 180 degrees about the forward axis while it is flat, open it
    # again. Never key a negative or zero scale: the FBX SDK turns it into NaN rotations.
    for f, s, flip in ((1, (1, 1, 1), 0.0), (12, (1, 1, 1), 0.0), (14, (1.05, 1, 0.12), 0.0), (15, (1.05, 1, 0.12), 180.0),
                       (17, (1, 1, 1), 180.0), (46, (1, 1, 1), 180.0)):
        key(arm, "Mouth", f, rot=(0.0, flip, 0.0), scale=s)
    set_range(act, 1, 46)
    return act


def anim_confused(arm):
    act = new_action(arm, "Confused")
    for f, k in ((1, 0.0), (10, 1.0), (30, 1.0), (37, 0.0)):
        key(arm, "Head", f, rot=(0.0, 0.0, 14.0 * k))
        key(arm, "Brow.L", f, loc=(0.0, 0.0, 0.025 * k))
        key(arm, "Brow.R", f, loc=(0.0, 0.0, -0.01 * k), rot=(0.0, 10.0 * k, 0.0))
        key(arm, "Eye.R", f, scale=(1.0 - 0.2 * k, 1.0, 1.0 - 0.25 * k))
        key(arm, "Eye.L", f, scale=(1.0 + 0.1 * k, 1.0, 1.0 + 0.15 * k))
        key(arm, "Mouth", f, scale=(1.0 - 0.3 * k, 1.0, 1.0 - 0.4 * k))
        key(arm, "UpperArm.R", f, rot=(20.0 * k, 0.0, -120.0 * k))
    for f, bend in ((1, 0.0), (12, 95.0), (16, 115.0), (20, 95.0), (24, 115.0), (28, 95.0), (37, 0.0)):
        key(arm, "LowerArm.R", f, rot=(bend, 0.0, 0.0))
    set_range(act, 1, 37)
    return act


def anim_impatient(arm):
    act = new_action(arm, "Impatient")
    for f in (1, 31):
        key(arm, "UpperArm.L", f, rot=(60.0, 0.0, -20.0))
        key(arm, "LowerArm.L", f, rot=(70.0, 0.0, 0.0))
        key(arm, "Brow.L", f, rot=(0.0, 15.0, 0.0))
        key(arm, "Brow.R", f, rot=(0.0, -15.0, 0.0))
        key(arm, "Eye.L", f, scale=(1.0, 1.0, 0.55))
        key(arm, "Eye.R", f, scale=(1.0, 1.0, 0.55))
        key(arm, "Mouth", f, scale=(1.2, 1.0, 0.5))
    for f, toe in ((1, 0.0), (5, 18.0), (9, 0.0), (13, 18.0), (17, 0.0), (31, 0.0)):
        key(arm, "Foot.R", f, rot=(toe, 0.0, 0.0))
    for f, hx in ((1, 15.0), (14, 15.0), (20, 0.0), (31, 15.0)):
        key(arm, "Head", f, rot=(hx, 0.0, 0.0))
    set_range(act, 1, 31)
    return act


ANIMS = (anim_idle, anim_walk, anim_talk, anim_celebrate, anim_disappointed, anim_confused, anim_impatient)


# --------------------------------------------------------------------------------------
# Build, export, render
# --------------------------------------------------------------------------------------

def build_variant(name, collection):
    rig = make_armature(f"CHR_PF_{name}_Rig", collection)
    parts = Parts()
    VARIANTS[name](parts)
    body = parts.assemble(f"CHR_PF_{name}_Body", collection, rig)
    return rig, body


def export_variant(project_root, name, rig, body, with_animation):
    out = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Characters", "PF", f"CHR_PF_{name}.fbx")
    rig_name, body_name = rig.name, body.name
    rig.name, body.name = "Armature", "Body"
    try:
        PF.export_fbx_skinned([rig, body], rig, out, with_animation)
    finally:
        rig.name, body.name = rig_name, body_name
    return out


def pose_static(rig, pose):
    if rig.animation_data is not None:
        rig.animation_data.action = None
    reset_pose(rig)
    for bone, (rot, loc, scale) in pose.items():
        pb = rig.pose.bones[bone]
        if rot is not None:
            pb.rotation_euler = Euler([math.radians(a) for a in rot], "XYZ")
        if loc is not None:
            pb.location = Vector(loc)
        if scale is not None:
            pb.scale = Vector(scale)


LINEUP_POSES = {
    "Base": {},
    "Fumi": {"Eye.L": (None, None, (1.1, 1, 0.3)), "Eye.R": (None, None, (1.1, 1, 0.3)),
             "Mouth": (None, None, (1.3, 1, 1.8)), "UpperArm.R": ((10, 0, -55), None, None),
             "LowerArm.R": ((40, 0, 0), None, None)},
    "Ren": {"Head": ((8, 0, -6), None, None), "Mouth": (None, None, (0.7, 1, 0.5))},
    "Sasaki": {"Mouth": ((0, 180, 0), None, (1, 1, 0.8)), "UpperArm.L": ((60, 0, -20), None, None),
               "LowerArm.L": ((70, 0, 0), None, None), "Head": ((12, 0, 0), None, None)},
    "Mio": {"UpperArm.R": ((25, 0, -118), None, None), "LowerArm.R": ((0, 0, -25), None, None), "Mouth": (None, None, (1.35, 1, 2.1)),
            "Eye.L": (None, None, (1.1, 1, 1.1)), "Eye.R": (None, None, (1.1, 1, 1.1)), "Root": (None, (0, 0.04, 0), None)},
    "Kanemochi": {"Head": ((-8, 0, 4), None, None), "Mouth": (None, None, (1.1, 1, 0.8))},
}


def render_lineup(project_root, rigs, preview_col):
    out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "PF", "CHR_PF_Lineup.png")
    PF.setup_beauty_render(resolution=(1800, 900), samples=48, world_hex="#F1DFC6", world_strength=0.75)
    PF.golden_hour_rig(preview_col, focus=(0.0, 0.0, 0.8), scale=1.4, sun_from=(-0.7, 1.0, 0.8), sun_energy=3.0,
                       fill_energy=35.0)
    PF.add_light(preview_col, "PF_Front", "AREA", (0.0, 6.0, 2.5), (0.0, 0.0, 0.9), "#FFF4E6", 420.0, size=5.0)
    PF.ground_plane(preview_col, "PreviewFloor", 20.0, 0.0, "M_PF_FloorOak")
    order = ["Base", "Fumi", "Ren", "Sasaki", "Mio", "Kanemochi"]
    for i, name in enumerate(order):
        rig = rigs[name]
        s = HEIGHT_SCALE[name]
        rig.location = ((i - 2.5) * 0.85, 0.0, 0.0)
        rig.scale = (s, s, s)
        pose_static(rig, LINEUP_POSES.get(name, {}))
    cam = PF.camera(preview_col, lens=50.0)
    PF.render_still(cam, (0.0, 7.6, 1.55), (0.0, 0.0, 0.82), out, lens=45.0)
    return out


def render_portraits(project_root, rigs, preview_col):
    files = []
    out_dir = os.path.join(project_root, "Assets", "_Project", "Art", "UI", "Portraits")
    scene = bpy.context.scene
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = 512
    cam = bpy.context.scene.camera
    floor = bpy.data.objects.get("PreviewFloor")
    if floor is not None:
        floor.hide_render = True
    for name, rig in rigs.items():
        if name == "Base":
            continue
        for other in rigs.values():
            other.hide_render = other is not rig
            for ch in other.children:
                ch.hide_render = other is not rig
        target = Vector((rig.location.x, rig.location.y, rig.location.z + 1.08 * HEIGHT_SCALE[name]))
        eye = target + Vector((0.55, 2.2, 0.25))
        files.append(PF.render_still(cam, tuple(eye), tuple(target), os.path.join(out_dir, f"PF_Portrait_{name}.png"),
                                     lens=85.0))
    for rig in rigs.values():
        rig.hide_render = False
        for ch in rig.children:
            ch.hide_render = False
    scene.render.film_transparent = False
    return files


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)
    P.reset_scene()
    scene = bpy.context.scene
    scene.render.fps = FPS
    PF.write_palette_json(project_root)
    col = P.get_collection("CHR_PF_Customers")
    preview_col = P.get_collection("Preview")

    rigs, bodies = {}, {}
    for name in VARIANTS:
        rigs[name], bodies[name] = build_variant(name, col)

    base = rigs["Base"]
    for anim in ANIMS:
        anim(base)
    base.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_start, scene.frame_end = 1, 61

    for name in VARIANTS:
        path = export_variant(project_root, name, rigs[name], bodies[name], with_animation=(name == "Base"))
        print(f"PRS_EXPORTED {path}  tris={PF.count_tris(rigs[name])}")

    if "--no-preview" not in args:
        print("PRS_PREVIEW " + render_lineup(project_root, rigs, preview_col))
        for f in render_portraits(project_root, rigs, preview_col):
            print("PRS_PORTRAIT " + f)
        for rig in rigs.values():
            rig.location = (0.0, 0.0, 0.0)
            rig.scale = (1.0, 1.0, 1.0)
            pose_static(rig, {})
        base.animation_data.action = bpy.data.actions["Idle"]
        for o in list(preview_col.objects):
            bpy.data.objects.remove(o)

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Characters", "CHR_PF_Customers.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")


if __name__ == "__main__":
    main()

"""
Phone Fixer art pass - shared palette, soft-chunky helpers and beauty renders (Blender 5.2+).

Builds on prs_bpy (axes, MeshBuilder, FBX export). Every Phone Fixer generator imports this:

    sys.path.append(os.path.dirname(os.path.abspath(__file__)))
    import prs_bpy as P
    import pf_bpy as PF

The palette below is THE colour contract of the art direction (see the "Phone Fixer Art Direction"
canvas). Material NAMES are what Unity remaps to; write_palette_json() exports the same table so
Unity's PhoneFixerMaterials creates matching URP materials. Smoothness/metallic use Unity's
convention (smoothness = 1 - roughness).
"""

import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402

# --------------------------------------------------------------------------------------
# Palette: name -> (hex, smoothness, metallic, emission, kind)
#   kind: "lit" (URP Lit), "unlit" (URP Unlit), "glass" (URP Lit transparent)
#   Reserved gameplay hues (orange #F2A65A, teal #6FA9A3, coral #DE8175, leaf #8CCB6E) are NOT
#   used on props, except coral on damage decals, where it means exactly that.
# --------------------------------------------------------------------------------------
PALETTE = {
    # grounds, paper, wood
    "M_PF_Plaster": ("#F5E8D3", 0.08, 0.0, 0.0, "lit"),
    "M_PF_PlasterWarm": ("#EAD8BE", 0.08, 0.0, 0.0, "lit"),
    "M_PF_FloorOak": ("#E4C8A2", 0.30, 0.0, 0.0, "lit"),
    "M_PF_FloorSeam": ("#CDA67C", 0.20, 0.0, 0.0, "lit"),
    "M_PF_Wood": ("#B88B61", 0.25, 0.0, 0.0, "lit"),
    "M_PF_WoodLight": ("#D2AE84", 0.30, 0.0, 0.0, "lit"),
    "M_PF_WoodWalnut": ("#8A6244", 0.30, 0.0, 0.0, "lit"),
    "M_PF_WoodDark": ("#6E4B36", 0.30, 0.0, 0.0, "lit"),
    "M_PF_Kraft": ("#E6D2B5", 0.10, 0.0, 0.0, "lit"),
    "M_PF_Cardboard": ("#D9B98E", 0.10, 0.0, 0.0, "lit"),
    "M_PF_Paper": ("#FFFDF8", 0.10, 0.0, 0.0, "lit"),
    "M_PF_PaperCream": ("#FBF4E8", 0.10, 0.0, 0.0, "lit"),
    # ink and darks
    "M_PF_Navy": ("#283747", 0.30, 0.0, 0.0, "lit"),
    "M_PF_NavyMat": ("#2F4256", 0.15, 0.0, 0.0, "lit"),
    "M_PF_MatLines": ("#5C7591", 0.15, 0.0, 0.0, "lit"),
    "M_PF_MatPrint": ("#F5E8D3", 0.15, 0.0, 0.0, "lit"),
    "M_PF_Charcoal": ("#3B3F46", 0.35, 0.0, 0.0, "lit"),
    "M_PF_Rubber": ("#34383F", 0.20, 0.0, 0.0, "lit"),
    "M_PF_ScreenOff": ("#1E2E3B", 0.90, 0.0, 0.0, "lit"),
    # soft accents
    "M_PF_Sage": ("#A9BFA8", 0.45, 0.0, 0.0, "lit"),
    "M_PF_DustyBlue": ("#8FA7B8", 0.45, 0.0, 0.0, "lit"),
    "M_PF_Butter": ("#EFD9A0", 0.50, 0.0, 0.0, "lit"),
    "M_PF_Blush": ("#E9C2B4", 0.45, 0.0, 0.0, "lit"),
    "M_PF_Plum": ("#8E5A7A", 0.45, 0.0, 0.0, "lit"),
    "M_PF_Wisteria": ("#7A6FA8", 0.10, 0.0, 0.0, "lit"),
    "M_PF_WisteriaDark": ("#6A5F98", 0.10, 0.0, 0.0, "lit"),
    "M_PF_Mustard": ("#D9B45A", 0.50, 0.0, 0.0, "lit"),
    "M_PF_Terracotta": ("#D9A88F", 0.20, 0.0, 0.0, "lit"),
    "M_PF_Lavender": ("#B9A6E8", 0.50, 0.0, 0.0, "lit"),
    "M_PF_CreamPlastic": ("#EFE3CF", 0.50, 0.0, 0.0, "lit"),
    # metals
    "M_PF_Brass": ("#C9A25E", 0.55, 1.0, 0.0, "lit"),
    "M_PF_Steel": ("#C7CDD6", 0.72, 1.0, 0.0, "lit"),
    "M_PF_SteelDark": ("#9AA4AD", 0.60, 0.9, 0.0, "lit"),
    "M_PF_Chrome": ("#DDE3EA", 0.85, 1.0, 0.0, "lit"),
    # street and shop extras
    "M_PF_Pavement": ("#BDB0A3", 0.15, 0.0, 0.0, "lit"),
    "M_PF_PavementSeam": ("#A89A8E", 0.10, 0.0, 0.0, "lit"),
    "M_PF_NavyFabric": ("#283747", 0.05, 0.0, 0.0, "lit"),
    "M_PF_Ginger": ("#D9A57A", 0.20, 0.0, 0.0, "lit"),
    "M_PF_PickMe": ("#F2A65A", 0.35, 0.0, 0.25, "lit"),
    # plants
    "M_PF_Leaf": ("#6F8F6B", 0.35, 0.0, 0.0, "lit"),
    "M_PF_LeafLight": ("#86A57F", 0.35, 0.0, 0.0, "lit"),
    "M_PF_Soil": ("#5A4636", 0.05, 0.0, 0.0, "lit"),
    # light emitters and screens
    "M_PF_LampBulb": ("#FFF2D6", 0.30, 0.0, 3.0, "lit"),
    "M_PF_ScreenText": ("#9DF0E4", 0.30, 0.0, 1.6, "lit"),
    "M_PF_ScreenWarn": ("#F0A99E", 0.30, 0.0, 1.3, "lit"),
    "M_PF_ScreenIdle": ("#CDEFF5", 0.30, 0.0, 1.0, "lit"),
    "M_PF_WindowGlow": ("#FFE3A8", 0.30, 0.0, 1.2, "lit"),
    "M_PF_Sky1": ("#FBE7C2", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_Sky2": ("#F9D9AE", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_Sky3": ("#F7C899", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_Sky4": ("#EFAE8E", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_SkylineFar": ("#E2AB9B", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_SkylineNear": ("#C99A96", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_Glass": ("#DCEFF0", 0.95, 0.0, 0.0, "glass"),
    # phone
    "M_PF_PhoneFrame": ("#F3E9DA", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneSakura": ("#F4C6D2", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneKinako": ("#E8D2A8", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneYomogi": ("#A9C29A", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneKurogoma": ("#4A4A55", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneSora": ("#B7D3E6", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PhoneGold": ("#D9B45A", 0.65, 0.5, 0.0, "lit"),
    "M_PF_PhoneLogo": ("#FBE2EA", 0.50, 0.0, 0.0, "lit"),
    "M_PF_PhoneGlass": ("#1E2A36", 0.93, 0.0, 0.0, "lit"),
    "M_PF_PhoneDisplay": ("#232B4D", 0.85, 0.0, 0.0, "lit"),
    "M_PF_PhoneCrack": ("#EAF6FF", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_PCB": ("#34466A", 0.55, 0.0, 0.0, "lit"),
    "M_PF_PCBGold": ("#E0B55E", 0.70, 1.0, 0.0, "lit"),
    "M_PF_Chip": ("#22252B", 0.60, 0.0, 0.0, "lit"),
    "M_PF_Kapton": ("#C7924A", 0.50, 0.0, 0.0, "lit"),
    "M_PF_BatteryWrap": ("#EFE3CF", 0.40, 0.0, 0.0, "lit"),
    "M_PF_LensDark": ("#141C26", 0.95, 0.0, 0.0, "lit"),
    "M_PF_LensBlue": ("#6C80A6", 0.95, 0.0, 0.0, "lit"),
    "M_PF_RingPink": ("#F4A7BE", 0.50, 0.0, 0.0, "lit"),
    "M_PF_RingCream": ("#EFE6D6", 0.50, 0.0, 0.0, "lit"),
    "M_PF_RingMatcha": ("#A9C29A", 0.50, 0.0, 0.0, "lit"),
    "M_PF_Corrosion": ("#C9D8C4", 0.05, 0.0, 0.0, "lit"),
    "M_PF_Rust": ("#A07850", 0.10, 0.0, 0.0, "lit"),
    "M_PF_Lint": ("#C8BBA6", 0.00, 0.0, 0.0, "lit"),
    "M_PF_Damage": ("#DE8175", 0.40, 0.0, 0.0, "lit"),
    # characters
    "M_PF_SkinPeach": ("#F3D2B8", 0.35, 0.0, 0.0, "lit"),
    "M_PF_SkinTan": ("#E8B999", 0.35, 0.0, 0.0, "lit"),
    "M_PF_SkinWarm": ("#D9A27F", 0.35, 0.0, 0.0, "lit"),
    "M_PF_SkinLight": ("#F6D9C4", 0.35, 0.0, 0.0, "lit"),
    "M_PF_SkinDeep": ("#C68B67", 0.35, 0.0, 0.0, "lit"),
    "M_PF_HairSilver": ("#D9D6E0", 0.45, 0.0, 0.0, "lit"),
    "M_PF_HairBlack": ("#2A2D3A", 0.45, 0.0, 0.0, "lit"),
    "M_PF_HairBrown": ("#4A3328", 0.45, 0.0, 0.0, "lit"),
    "M_PF_HairEspresso": ("#3B3238", 0.45, 0.0, 0.0, "lit"),
    "M_PF_HairPlum": ("#4A3B52", 0.45, 0.0, 0.0, "lit"),
    "M_PF_ClothSuit": ("#45506A", 0.20, 0.0, 0.0, "lit"),
    "M_PF_ClothDenim": ("#4E5D75", 0.15, 0.0, 0.0, "lit"),
    "M_PF_ClothSkirt": ("#5E7487", 0.15, 0.0, 0.0, "lit"),
    "M_PF_Fur": ("#EDE6F2", 0.05, 0.0, 0.0, "lit"),
    "M_PF_White": ("#FFFFFF", 0.40, 0.0, 0.0, "lit"),
    "M_PF_Eye": ("#283747", 0.80, 0.0, 0.0, "lit"),
    "M_PF_EyeShine": ("#FFFFFF", 0.0, 0.0, 0.0, "unlit"),
    "M_PF_Mouth": ("#8C4A4A", 0.30, 0.0, 0.0, "lit"),
    "M_PF_Cheek": ("#E9A99A", 0.30, 0.0, 0.0, "lit"),
}

PALETTE_JSON_RELATIVE = os.path.join("Assets", "_Project", "Art", "Materials", "PhoneFixer", "pf_palette.json")


def write_palette_json(project_root):
    """Exports PALETTE for Unity (Assets/_Project/Art/Materials/PhoneFixer/pf_palette.json)."""
    path = os.path.join(project_root, PALETTE_JSON_RELATIVE)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    rows = [{"name": n, "hex": h, "smoothness": s, "metallic": m, "emission": e, "kind": k}
            for n, (h, s, m, e, k) in sorted(PALETTE.items())]
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"materials": rows}, f, indent=1)
    return path


_MATS = {}


def M(name):
    """Palette material by name (created on first use). Unknown names fail loudly."""
    if name in _MATS and _MATS[name].name in bpy.data.materials:
        return _MATS[name]
    hex_value, smoothness, metallic, emission, kind = PALETTE[name]
    mat = P.make_material(name, hex_value, roughness=1.0 - smoothness, metallic=metallic,
                          emission_strength=emission)
    nodes = mat.node_tree.nodes
    bsdf = next((n for n in nodes if n.bl_idname == "ShaderNodeBsdfPrincipled"), None)
    if kind == "unlit" and bsdf is not None:
        bsdf.inputs["Emission Color"].default_value = P.hex_color(hex_value)
        bsdf.inputs["Emission Strength"].default_value = 1.0
        bsdf.inputs["Base Color"].default_value = (0.0, 0.0, 0.0, 1.0)
        bsdf.inputs["Roughness"].default_value = 1.0
    if kind == "glass" and bsdf is not None:
        bsdf.inputs["Alpha"].default_value = 0.18
        for attr, value in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(mat, attr, value)
                break
            except (AttributeError, TypeError):
                continue
    _MATS[name] = mat
    return mat


def mats(*names):
    return [M(n) for n in names]


# --------------------------------------------------------------------------------------
# Geometry helpers (all metres, Blender space: X right, Y away/forward, Z up)
# --------------------------------------------------------------------------------------

def capsule(b, p0, p1, radius, mat=0, segments=16):
    """Cylinder with round caps between two points (limbs, rails, handles)."""
    b.cylinder_between(p0, p1, radius, segments=segments, mat=mat)
    for p in (p0, p1):
        b.ellipsoid(p, (radius, radius, radius), mat=mat, u_segments=segments, v_segments=max(6, segments // 2))


def torus(b, center, major, minor, mat=0, axis="Z", major_segments=32, minor_segments=12, arc_deg=360.0,
          start_deg=0.0):
    """Ring around `axis` through `center`. arc_deg < 360 makes an open arc (handles, hooks, smiles);
    the arc starts at start_deg (0 = +X, counter-clockwise in the ring's plane)."""
    bm = b.bm
    closed = arc_deg >= 359.9
    count = major_segments if closed else major_segments + 1
    rings = []
    for i in range(count):
        a = math.radians(start_deg) + math.radians(arc_deg) * i / major_segments
        ring = []
        for j in range(minor_segments):
            t = 2.0 * math.pi * j / minor_segments
            r = major + minor * math.cos(t)
            local = Vector((r * math.cos(a), r * math.sin(a), minor * math.sin(t)))
            if axis == "X":
                local = Vector((local.z, local.x, local.y))
            elif axis == "Y":
                local = Vector((local.x, local.z, local.y))
            ring.append(bm.verts.new(Vector(center) + local))
        rings.append(ring)
    faces = []
    span = count if closed else count - 1
    for i in range(span):
        r0, r1 = rings[i], rings[(i + 1) % count]
        for j in range(minor_segments):
            k = (j + 1) % minor_segments
            faces.append(bm.faces.new((r0[j], r1[j], r1[k], r0[k])))
    if not closed:
        for ring, flip in ((rings[0], True), (rings[-1], False)):
            faces.append(bm.faces.new(list(reversed(ring)) if flip else ring))
    b._finish(faces, mat)
    return faces


def shell_frustum(b, z0, z1, r0, r1, thickness, mat=0, inner_mat=None, segments=32):
    """Open cone/cylinder shell along +Z (lamp shades, cups): outer wall, inner wall, rims, no caps,
    so a light or bulb placed inside is visible and unblocked."""
    bm = b.bm
    inner_mat = mat if inner_mat is None else inner_mat
    rings = []
    for z, r in ((z0, r0), (z1, r1), (z1, r1 - thickness), (z0, r0 - thickness)):
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / segments),
                                    r * math.sin(2 * math.pi * i / segments), z)) for i in range(segments)])
    faces = []
    for k in range(4):
        a, c = rings[k], rings[(k + 1) % 4]
        for i in range(segments):
            j = (i + 1) % segments
            f = bm.faces.new((a[i], a[j], c[j], c[i]))
            f.material_index = inner_mat if k == 2 else mat
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return faces


def flat_disc_y(b, x, y, z, radius, mat=0, segments=6):
    """Single-sided disc in the XZ plane facing -Y (pegboard holes, decals on walls)."""
    bm = b.bm
    verts = [bm.verts.new((x + radius * math.cos(2 * math.pi * i / segments), y,
                           z + radius * math.sin(2 * math.pi * i / segments))) for i in range(segments)]
    face = bm.faces.new(verts)
    face.material_index = mat
    return face


def rounded_slab(b, cx, cy, w, d, r, z0, z1, mat=0, segments=8):
    """Footprint with rounded corners extruded from z0 to z1."""
    return b.slab(P.rounded_rect(w, d, r, segments, center=(cx, cy)), z0, z1, mat=mat)


def text_object(text, size, depth, collection, name, mat, align_x="CENTER", offset=0.0, parent=None,
                location=(0.0, 0.0, 0.0), rotation=None):
    """Flat 3D text converted to a mesh (Blender's built-in font, permissive licence).
    Text lies in local XY facing +Z; rotate the result to stand it up."""
    curve = bpy.data.curves.new(name + "_Curve", type="FONT")
    curve.body = text
    curve.size = size
    curve.extrude = depth * 0.5
    curve.offset = offset
    curve.align_x = align_x
    curve.align_y = "CENTER"
    temp = bpy.data.objects.new(name + "_Tmp", curve)
    collection.objects.link(temp)
    deps = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(temp.evaluated_get(deps))
    mesh.name = name
    bpy.data.objects.remove(temp)
    bpy.data.curves.remove(curve)
    mesh.materials.clear()
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = Vector(location)
    if rotation is not None:
        obj.rotation_euler = rotation
    bm = bmesh.new()
    bm.from_mesh(mesh)
    P._box_project_uvs(bm)
    bm.to_mesh(mesh)
    bm.free()
    return obj


def empty(name, collection, location, parent=None, size=0.03):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = size
    collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = Vector(location)
    return obj


def look_rotation(direction, up=(0.0, 0.0, 1.0)):
    """Euler that points local -Z along `direction` (Blender camera / light convention)."""
    return Vector(direction).to_track_quat("-Z", "Y").to_euler()


def set_parent_keep_world(child, parent):
    world = child.matrix_world.copy()
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()
    child.matrix_world = world


def count_tris(root):
    deps = bpy.context.evaluated_depsgraph_get()
    total = 0
    for o in [root] + P.descendants(root):
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(deps)
        mesh = ev.to_mesh()
        total += sum(len(p.vertices) - 2 for p in mesh.polygons)
        ev.to_mesh_clear()
    return total


# --------------------------------------------------------------------------------------
# Beauty renders (EEVEE): golden hour key, teal sky fill, navy shadows
# --------------------------------------------------------------------------------------

def _try_set(obj, attr, value):
    try:
        setattr(obj, attr, value)
        return True
    except (AttributeError, TypeError, ValueError):
        return False


def setup_beauty_render(resolution=(1600, 900), world_hex="#F1DFC6", world_strength=0.55, samples=64,
                        transparent=False, exposure=0.0):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = transparent
    ee = scene.eevee
    _try_set(ee, "taa_render_samples", samples)
    _try_set(ee, "use_shadows", True)
    _try_set(ee, "shadow_ray_count", 2)
    _try_set(ee, "shadow_step_count", 8)
    _try_set(ee, "use_raytracing", True)
    _try_set(ee, "use_fast_gi", True)
    _try_set(ee, "fast_gi_distance", 0.6)
    _try_set(ee, "horizon_quality", 0.75)
    _try_set(ee, "use_gtao", True)
    _try_set(ee, "gtao_distance", 0.4)
    _try_set(ee, "use_bloom", True)
    scene.view_settings.view_transform = "Standard"
    _try_set(scene.view_settings, "look", "None")
    scene.view_settings.exposure = exposure
    scene.view_settings.gamma = 1.0
    world = scene.world or bpy.data.worlds.new("PF_World")
    scene.world = world
    if world.node_tree is not None:
        bg = next((n for n in world.node_tree.nodes if n.bl_idname == "ShaderNodeBackground"), None)
        if bg is not None:
            bg.inputs["Color"].default_value = P.hex_color(world_hex)
            bg.inputs["Strength"].default_value = world_strength
    world.color = P.hex_color(world_hex)[:3]
    return scene


def add_light(collection, name, kind, location, target, color_hex, energy, size=0.2, spot_deg=60.0,
              shadow_soft=0.1):
    data = bpy.data.lights.new(name, kind)
    data.color = P.hex_color(color_hex)[:3]
    data.energy = energy
    if kind == "SUN":
        data.angle = math.radians(max(0.5, shadow_soft * 30.0))
    elif kind == "AREA":
        data.size = size
    elif kind in ("POINT", "SPOT"):
        data.shadow_soft_size = shadow_soft
        if kind == "SPOT":
            data.spot_size = math.radians(spot_deg)
            data.spot_blend = 0.6
    obj = bpy.data.objects.new(name, data)
    collection.objects.link(obj)
    obj.location = Vector(location)
    obj.rotation_euler = look_rotation(Vector(target) - Vector(location))
    return obj


def golden_hour_rig(collection, focus=(0.0, 0.0, 0.8), scale=1.0, sun_from=(-1.0, -0.35, 0.55),
                    sun_energy=4.2, fill_energy=60.0, rim=True):
    """Key: warm low sun. Fill: cool teal area light opposite. Rim: soft peach from behind."""
    f = Vector(focus)
    d = Vector(sun_from).normalized()
    lights = [add_light(collection, "PF_Sun", "SUN", f + d * 10.0, f, "#FFC98B", sun_energy, shadow_soft=0.08)]
    fill_pos = f + Vector((2.6, -2.2, 1.6)) * scale
    lights.append(add_light(collection, "PF_SkyFill", "AREA", fill_pos, f, "#9CC3C9", fill_energy * scale * scale,
                            size=2.5 * scale))
    if rim:
        rim_pos = f + Vector((-0.6, 2.8, 2.0)) * scale
        lights.append(add_light(collection, "PF_Rim", "AREA", rim_pos, f, "#FFD9B0", fill_energy * 0.6 * scale * scale,
                                size=1.5 * scale))
    return lights


def camera(collection, name="PF_Cam", lens=50.0, ortho_scale=None):
    cam_data = bpy.data.cameras.new(name)
    cam_data.lens = lens
    cam_data.clip_start = 0.005
    cam_data.clip_end = 200.0
    if ortho_scale is not None:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho_scale
    cam = bpy.data.objects.new(name, cam_data)
    collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def render_still(cam, eye, target, filepath, lens=None):
    eye, target = Vector(eye), Vector(target)
    cam.location = eye
    cam.rotation_euler = (target - eye).to_track_quat("-Z", "Y").to_euler()
    if lens is not None:
        cam.data.lens = lens
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.filepath = filepath
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.render.render(write_still=True)
    return filepath


def export_fbx_skinned(objects, active, filepath, with_animation):
    """Characters: armature + skinned mesh, same axes/scale as prs_bpy.export_fbx_for_unity, plus
    every action baked as its own take when with_animation is set."""
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"ARMATURE", "MESH", "EMPTY"},
        use_mesh_modifiers=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="Y",
        axis_up="Z",
        bake_space_transform=False,
        mesh_smooth_type="OFF",
        use_tspace=False,
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        armature_nodetype="NULL",
        use_armature_deform_only=False,
        bake_anim=with_animation,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        use_custom_props=False,
        colors_type="NONE",
        path_mode="AUTO",
        embed_textures=False,
    )
    for o in objects:
        o.select_set(False)
    return filepath


def ground_plane(collection, name, size, z, mat_name, center=(0.0, 0.0)):
    b = P.MeshBuilder()
    b.box((center[0], center[1], z - 0.01), (size, size, 0.02))
    return b.build(name, collection, [M(mat_name)], smooth=False)

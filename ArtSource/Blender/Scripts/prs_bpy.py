"""
Phone Repair Shop - shared Blender helpers (Blender 5.2+).

Coordinate convention used by every asset script (Blender world space, metres):
    X = right, Y = forward / away from the player (phones: toward the TOP edge),
    Z = up (phones: out of the SCREEN).

Exported Z-up (Blender native) and imported with Unity's "Bake Axis Conversion" ON:
    Unity (x, y, z) = Blender (x, z, y)
i.e. Blender +Y becomes Unity +Z (forward), Blender +Z becomes Unity +Y (up), and every
object imports with a clean rotation and unit scale. (Characters, later: model them facing
-Y, Blender's usual front, and they will face a Unity camera that looks along +Z.)

NOTE: do NOT use the exporter's experimental "Apply Transform" (bake_space_transform): it
rotates grandchildren (e.g. a label parented to a part) by 90 degrees in Unity. And do not
export "-Z forward / Y up" either: the file then claims to be Y-up, so Unity has nothing to
bake and the root keeps a -90 degree X rotation.

All geometry is built with bmesh (no operators), so the scripts run headless:
    blender --background --factory-startup --python build_phone_mochi_one.py
"""

import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

MM = 0.001


def mm(*values):
    """Millimetres to metres. mm(5) -> 0.005, mm(1, 2, 3) -> (0.001, 0.002, 0.003)."""
    if len(values) == 1:
        return values[0] * MM
    return tuple(v * MM for v in values)


def d2b(v):
    """Tuple -> Blender Vector (design space IS Blender space; kept as one conversion point)."""
    return Vector((v[0], v[1], v[2]))


# --------------------------------------------------------------------------------------
# Scene helpers
# --------------------------------------------------------------------------------------

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    return scene


def get_collection(name, parent=None):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(col)
    return col


def make_empty(name, collection, location_design=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.02
    collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = d2b(location_design)
    return obj


def hex_color(h, alpha=1.0):
    """'#RRGGBB' (sRGB) -> linear RGBA tuple for Blender."""
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]

    def to_linear(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

    return tuple(to_linear(c) for c in srgb) + (alpha,)


def make_material(name, color_hex, roughness=0.5, metallic=0.0, emission_strength=0.0):
    """Principled material. Unity remaps by NAME to its own material assets, so the
    values here only drive Blender previews and the FBX fallback colours."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    rgba = hex_color(color_hex)
    mat.diffuse_color = rgba
    mat.roughness = roughness
    mat.metallic = metallic
    nodes = mat.node_tree.nodes
    bsdf = next((n for n in nodes if n.bl_idname == "ShaderNodeBsdfPrincipled"), None)
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission_strength > 0.0:
            bsdf.inputs["Emission Color"].default_value = rgba
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


# --------------------------------------------------------------------------------------
# 2D outlines (design-space XY, counter-clockwise)
# --------------------------------------------------------------------------------------

def rounded_rect(width, height, radius, segments=8, center=(0.0, 0.0)):
    radius = max(0.0, min(radius, width * 0.5 - 1e-6, height * 0.5 - 1e-6))
    cx, cy = center
    hw, hh = width * 0.5, height * 0.5
    corners = ((hw - radius, hh - radius, 0.0), (-hw + radius, hh - radius, 90.0),
               (-hw + radius, -hh + radius, 180.0), (hw - radius, -hh + radius, 270.0))
    pts = []
    for ox, oy, start in corners:
        if radius <= 1e-9:
            pts.append((cx + ox, cy + oy))
            continue
        for i in range(segments + 1):
            a = math.radians(start + 90.0 * i / segments)
            pts.append((cx + ox + radius * math.cos(a), cy + oy + radius * math.sin(a)))
    return pts


def circle(radius, segments=24, center=(0.0, 0.0)):
    cx, cy = center
    return [(cx + radius * math.cos(2 * math.pi * i / segments),
             cy + radius * math.sin(2 * math.pi * i / segments)) for i in range(segments)]


# --------------------------------------------------------------------------------------
# Mesh builder
# --------------------------------------------------------------------------------------

class MeshBuilder:
    """Accumulates geometry for ONE object in design-space coordinates that are LOCAL to
    the object's origin. Call build() to create the Blender object."""

    def __init__(self):
        self.bm = bmesh.new()
        self._closed_faces = []

    # -- primitives ---------------------------------------------------------------------

    def slab(self, outline, z0, z1, holes=(), mat=0, axis="Z", offset=(0.0, 0.0, 0.0)):
        """Extrude a 2D outline (with optional holes) between z0 and z1.
        axis='Y' lays the outline in the XZ plane and extrudes along +Y instead."""
        bm = self.bm
        loops = [(list(outline), False)] + [(list(h), True) for h in holes]
        created = []
        bottom_edges, top_edges = [], []
        for pts, is_hole in loops:
            bot = [bm.verts.new((x, y, z0)) for x, y in pts]
            top = [bm.verts.new((x, y, z1)) for x, y in pts]
            n = len(pts)
            for i in range(n):
                j = (i + 1) % n
                quad = (bot[i], bot[j], top[j], top[i])
                if is_hole:
                    quad = tuple(reversed(quad))
                created.append(bm.faces.new(quad))
            bm.edges.ensure_lookup_table()
            for i in range(n):
                j = (i + 1) % n
                bottom_edges.append(bm.edges.get((bot[i], bot[j])))
                top_edges.append(bm.edges.get((top[i], top[j])))
        for edges, normal in ((top_edges, (0, 0, 1)), (bottom_edges, (0, 0, -1))):
            res = bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges,
                                          normal=Vector(normal))
            created.extend(g for g in res["geom"] if isinstance(g, bmesh.types.BMFace))
        self._finish(created, mat, axis, offset)
        return created

    def box(self, center, size, mat=0):
        m = Matrix.LocRotScale(Vector(center), None, Vector(size))
        res = bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        faces = list({f for v in res["verts"] for f in v.link_faces})
        self._finish(faces, mat)
        return faces

    def cylinder(self, center, radius, z0, z1, segments=24, mat=0, radius_top=None):
        depth = z1 - z0
        m = Matrix.Translation(Vector((center[0], center[1], (z0 + z1) * 0.5)))
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                    radius1=radius, radius2=radius if radius_top is None else radius_top,
                                    depth=depth, matrix=m)
        faces = list({f for v in res["verts"] for f in v.link_faces})
        self._finish(faces, mat)
        return faces

    def frustum(self, z0, z1, radius_bottom, radius_top, segments=24, mat=0, center=(0.0, 0.0),
                scale_xy=(1.0, 1.0), rotate_z_deg=0.0):
        """Cone/cylinder section along +Z with optional flattening (scale_xy) and twist, e.g. a wedge tip."""
        m = (Matrix.Translation(Vector((center[0], center[1], (z0 + z1) * 0.5)))
             @ Matrix.Diagonal(Vector((scale_xy[0], scale_xy[1], 1.0, 1.0)))
             @ Matrix.Rotation(math.radians(rotate_z_deg), 4, "Z"))
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                    radius1=radius_bottom, radius2=radius_top, depth=z1 - z0, matrix=m)
        faces = list({f for v in res["verts"] for f in v.link_faces})
        self._finish(faces, mat)
        return faces

    def cylinder_between(self, p0, p1, radius, segments=16, mat=0, radius_end=None):
        p0, p1 = Vector(p0), Vector(p1)
        d = p1 - p0
        rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        m = Matrix.Translation((p0 + p1) * 0.5) @ rot
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                    radius1=radius, radius2=radius if radius_end is None else radius_end,
                                    depth=d.length, matrix=m)
        faces = list({f for v in res["verts"] for f in v.link_faces})
        self._finish(faces, mat)
        return faces

    def ellipsoid(self, center, radii, mat=0, u_segments=24, v_segments=12, rotation=None):
        """rotation: optional mathutils Quaternion/Matrix applied before translation."""
        m = Matrix.LocRotScale(Vector(center), rotation, Vector(radii))
        res = bmesh.ops.create_uvsphere(self.bm, u_segments=u_segments, v_segments=v_segments,
                                        radius=1.0, matrix=m)
        faces = list({f for v in res["verts"] for f in v.link_faces})
        self._finish(faces, mat)
        return faces

    def flat_polygon(self, outline, z, mat=0, facing_up=True):
        """Single-sided flat face (decals). Not part of a closed shell."""
        pts = outline if facing_up else list(reversed(outline))
        verts = [self.bm.verts.new((x, y, z)) for x, y in pts]
        face = self.bm.faces.new(verts)
        face.material_index = mat
        return [face]

    def ribbon(self, p0, p1, width, z, mat=0, facing_up=True):
        """Flat quad strip from p0 to p1 (XY), facing +Z (or -Z when facing_up=False)."""
        dx, dy = p1[0] - p0[0], p1[1] - p0[1]
        length = math.hypot(dx, dy)
        if length < 1e-9:
            return []
        nx, ny = -dy / length * width * 0.5, dx / length * width * 0.5
        quad = [(p0[0] - nx, p0[1] - ny), (p1[0] - nx, p1[1] - ny),
                (p1[0] + nx, p1[1] + ny), (p0[0] + nx, p0[1] + ny)]
        return self.flat_polygon(quad, z, mat, facing_up)

    # -- internals ----------------------------------------------------------------------

    def _finish(self, faces, mat, axis="Z", offset=(0.0, 0.0, 0.0)):
        for f in faces:
            f.material_index = mat
        verts = list({v for f in faces for v in f.verts})
        if axis == "Y":
            # outline XY -> XZ plane, extrusion Z -> Y (pure rotation, winding preserved)
            bmesh.ops.rotate(self.bm, verts=verts, cent=(0, 0, 0), matrix=Matrix.Rotation(-math.pi / 2, 3, "X"))
        if any(offset):
            bmesh.ops.translate(self.bm, verts=verts, vec=Vector(offset))
        self._closed_faces.extend(faces)

    def build(self, name, collection, materials, location_design=(0, 0, 0), parent=None,
              rotation_design=None, sharp_angle_deg=30.0, bevel=None, smooth=True):
        """Create the object. bevel = (width_m, segments) adds a non-destructive, angle-limited
        Bevel modifier with hardened normals (applied automatically on FBX export)."""
        bm = self.bm
        if self._closed_faces:
            bmesh.ops.recalc_face_normals(bm, faces=[f for f in self._closed_faces if f.is_valid])
        bm.normal_update()
        for f in bm.faces:
            f.smooth = smooth
        sharp = math.radians(sharp_angle_deg)
        for e in bm.edges:
            if len(e.link_faces) == 2 and e.calc_face_angle(0.0) >= sharp:
                e.smooth = False
        _box_project_uvs(bm)

        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        for m in materials:
            mesh.materials.append(m)
        mesh.update()

        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        if parent is not None:
            obj.parent = parent
        obj.location = d2b(location_design)
        if rotation_design is not None:
            obj.rotation_euler = rotation_design.to_euler()
        if bevel is not None:
            width, segments = bevel
            mod = obj.modifiers.new(name="Bevel", type="BEVEL")
            mod.width = width
            mod.segments = segments
            mod.limit_method = "ANGLE"
            mod.angle_limit = math.radians(30.0)
            mod.harden_normals = True
            mod.miter_outer = "MITER_ARC"
            mod.use_clamp_overlap = True
        return obj


def _box_project_uvs(bm, scale=10.0):
    """Cheap box projection (1 UV unit = 10 cm) so models can take detail textures later."""
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for loop in f.loops:
            co = loop.vert.co
            if ax == 0:
                loop[uv].uv = (co.y * scale, co.z * scale)
            elif ax == 1:
                loop[uv].uv = (co.x * scale, co.z * scale)
            else:
                loop[uv].uv = (co.x * scale, co.y * scale)


def add_boolean(obj, cutter, name="Cut"):
    mod = obj.modifiers.new(name=name, type="BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.solver = "EXACT"
    mod.object = cutter
    # keep the boolean BEFORE the bevel so the cut edges get rounded too
    move_modifier_first(obj, name)
    return mod


def move_modifier_first(obj, name):
    idx = obj.modifiers.find(name)
    if idx > 0:
        obj.modifiers.move(idx, 0)


def descendants(obj):
    out = []
    for c in obj.children:
        out.append(c)
        out.extend(descendants(c))
    return out


# --------------------------------------------------------------------------------------
# Export + preview
# --------------------------------------------------------------------------------------

def export_fbx_for_unity(root, filepath):
    """Export root + descendants with the project's standard Unity settings."""
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    objs = [root] + descendants(root)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        # Export in Blender's native Z-up axes; Unity's importer ("Bake Axis Conversion" ON) converts
        # the whole hierarchy to Y-up and leaves every object with a clean rotation.
        axis_forward="Y",
        axis_up="Z",
        bake_space_transform=False,
        mesh_smooth_type="OFF",
        use_tspace=False,
        use_triangles=False,
        add_leaf_bones=False,
        bake_anim=False,
        use_custom_props=False,
        colors_type="NONE",
        path_mode="AUTO",
        embed_textures=False,
    )
    for o in objs:
        o.select_set(False)
    return filepath


def setup_preview_render(resolution=(1280, 960), background_hex="#F3EBDD"):
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    if scene.render.engine == "BLENDER_WORKBENCH":
        shading = scene.display.shading
        shading.light = "STUDIO"
        shading.color_type = "MATERIAL"
        shading.show_cavity = True
        shading.cavity_type = "BOTH"
        shading.show_object_outline = True
        shading.show_specular_highlight = True
        scene.display.render_aa = "16"
    world = scene.world or bpy.data.worlds.new("PreviewWorld")
    scene.world = world
    world.color = hex_color(background_hex)[:3]
    return scene


def preview_camera(collection, name="PreviewCam", lens=50.0):
    cam_data = bpy.data.cameras.get(name) or bpy.data.cameras.new(name)
    cam_data.lens = lens
    cam_data.clip_start = 0.002
    cam_data.clip_end = 50.0
    cam = bpy.data.objects.get(name)
    if cam is None:
        cam = bpy.data.objects.new(name, cam_data)
        collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def render_from(cam, eye_design, target_design, filepath):
    eye, target = d2b(eye_design), d2b(target_design)
    cam.location = eye
    cam.rotation_euler = (target - eye).to_track_quat("-Z", "Y").to_euler()
    scene = bpy.context.scene
    scene.render.filepath = filepath
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.render.render(write_still=True)
    return filepath


def project_root_from(script_file):
    """<root>/ArtSource/Blender/Scripts/x.py -> <root>"""
    return os.path.abspath(os.path.join(os.path.dirname(script_file), "..", "..", ".."))


def seeded_random(seed):
    return random.Random(seed)

"""
Phone Fixer tool set (art direction v1): big friendly handles, one colour each, no reserved hues.

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_pf_tools.py

Outputs:
    ArtSource/Blender/Tools/TOOLS_PF.blend
    Assets/_Project/Art/Models/Tools/PF/TOOL_PF_<Name>.fbx     one FBX per tool
    ArtSource/Blender/Previews/PF/TOOLS_PF_quick.png

Gameplay convention (same as build_tools.py / ToolDefinition): the origin is the TIP (the point that
touches the phone; the lens centre for the magnifier) and the tool runs along +Z towards the handle
(Unity +Y). Real-world proportions in millimetres.
"""

import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
import pf_bpy as PF  # noqa: E402
from prs_bpy import mm  # noqa: E402


def build_screwdriver(col):
    b = P.MeshBuilder()
    b.box(mm(0.0, 0.0, 2.2), mm(2.6, 0.7, 4.4), mat=0)                          # cross tip
    b.box(mm(0.0, 0.0, 2.2), mm(0.7, 2.6, 4.4), mat=0)
    b.frustum(mm(1.0), mm(56.0), mm(1.3), mm(1.5), segments=16, mat=0)           # shaft
    b.frustum(mm(54.0), mm(63.0), mm(2.8), mm(3.8), segments=24, mat=0)          # ferrule
    b.frustum(mm(62.0), mm(138.0), mm(8.6), mm(9.6), segments=32, mat=1)         # chunky handle
    for z in (78.0, 92.0, 106.0):                                                # soft grip bands
        b.frustum(mm(z), mm(z + 6.0), mm(9.5), mm(9.8), segments=32, mat=2)
    b.frustum(mm(137.0), mm(146.0), mm(8.0), mm(6.8), segments=32, mat=2)        # swivel cap
    b.ellipsoid(mm(0.0, 0.0, 146.0), mm(6.8, 6.8, 2.2), mat=2, u_segments=24, v_segments=8)
    return b.build("TOOL_PF_Screwdriver", col, PF.mats("M_PF_Steel", "M_PF_Butter", "M_PF_Navy"), bevel=(mm(0.5), 2))


def build_spudger(col):
    b = P.MeshBuilder()
    b.frustum(0.0, mm(16.0), mm(1.2), mm(6.2), segments=4, mat=0, scale_xy=(1.0, 0.42), rotate_z_deg=45.0)  # blade
    b.slab(P.rounded_rect(mm(8.8), mm(3.8), mm(1.8), 6), mm(16.0), mm(116.0), mat=0)                       # body
    b.frustum(mm(116.0), mm(132.0), mm(6.2), mm(1.6), segments=4, mat=0, scale_xy=(1.0, 0.42), rotate_z_deg=45.0)
    b.slab(P.rounded_rect(mm(9.8), mm(4.8), mm(2.2), 6), mm(58.0), mm(90.0), mat=1)                        # grip band
    return b.build("TOOL_PF_Spudger", col, PF.mats("M_PF_Lavender", "M_PF_Navy"), bevel=(mm(0.4), 2))


def build_suction_cup(col):
    b = P.MeshBuilder()
    b.frustum(0.0, mm(8.0), mm(19.0), mm(8.0), segments=40, mat=0)              # rubber cup
    b.frustum(mm(7.0), mm(30.0), mm(4.2), mm(3.6), segments=20, mat=1)          # stem
    PF.torus(b, mm(0.0, 0.0, 42.0), mm(11.0), mm(3.0), mat=1, axis="Y", major_segments=28, minor_segments=10)
    return b.build("TOOL_PF_SuctionCup", col, PF.mats("M_PF_Rubber", "M_PF_Sage"), bevel=(mm(0.4), 2))


def build_tweezers(col):
    b = P.MeshBuilder()
    for side in (-1.0, 1.0):
        tip = (side * 0.35, 0.0, 0.0)
        back = (side * 5.5, 0.0, 118.0)
        b.cylinder_between(mm(*tip), mm(*back), mm(1.1), segments=8, mat=0, radius_end=mm(1.6))
        g0 = (side * (0.35 + 5.15 * 40.0 / 118.0) + side * 1.0, 0.0, 40.0)
        g1 = (side * (0.35 + 5.15 * 88.0 / 118.0) + side * 1.0, 0.0, 88.0)
        PF.capsule(b, mm(*g0), mm(*g1), mm(2.2), mat=1, segments=10)
    b.box(mm(0.0, 0.0, 119.0), mm(13.0, 3.4, 4.0), mat=0)                       # joined back end
    return b.build("TOOL_PF_Tweezers", col, PF.mats("M_PF_Steel", "M_PF_PaperCream"), bevel=(mm(0.3), 2))


def build_brush(col):
    b = P.MeshBuilder()
    b.frustum(0.0, mm(20.0), mm(7.8), mm(6.2), segments=16, mat=0, scale_xy=(1.0, 0.55))   # bristle tuft
    b.frustum(mm(19.0), mm(28.0), mm(6.0), mm(5.2), segments=20, mat=1, scale_xy=(1.0, 0.6))  # ferrule
    b.frustum(mm(27.0), mm(160.0), mm(4.8), mm(4.2), segments=16, mat=2)                     # handle
    b.ellipsoid(mm(0.0, 0.0, 160.0), mm(4.2, 4.2, 2.5), mat=2, u_segments=16, v_segments=6)
    b.cylinder((0.0, 0.0), mm(1.8), mm(146.0), mm(150.0), segments=12, mat=3)                # hanging hole hint
    return b.build("TOOL_PF_Brush", col, PF.mats("M_PF_Kraft", "M_PF_Brass", "M_PF_Wood", "M_PF_WoodDark"),
                   bevel=(mm(0.4), 2))


def build_magnifier(col):
    b = P.MeshBuilder()
    PF.torus(b, (0.0, 0.0, 0.0), mm(32.0), mm(3.6), mat=0, axis="Y", major_segments=48, minor_segments=10)
    b.slab(P.circle(mm(31.0), 48), mm(-0.9), mm(0.9), mat=1, axis="Y")                       # lens in the ring plane
    lens = b.build("TOOL_PF_Magnifier", col, PF.mats("M_PF_Brass", "M_PF_Glass", "M_PF_WoodWalnut"),
                   bevel=(mm(0.3), 2))
    h = P.MeshBuilder()
    h.frustum(mm(34.0), mm(46.0), mm(5.0), mm(5.6), segments=20, mat=0)                       # collar
    h.frustum(mm(45.0), mm(140.0), mm(6.4), mm(7.2), segments=20, mat=2)                      # handle
    h.ellipsoid(mm(0.0, 0.0, 140.0), mm(7.2, 7.2, 3.0), mat=2, u_segments=20, v_segments=6)
    handle = h.build("TOOL_PF_Magnifier_Handle", col, PF.mats("M_PF_Brass", "M_PF_Glass", "M_PF_WoodWalnut"),
                     parent=lens, bevel=(mm(0.4), 2))
    return lens


def build_tester(col):
    b = P.MeshBuilder()
    b.frustum(0.0, mm(16.0), mm(0.6), mm(1.2), segments=10, mat=0)                           # probe tip
    b.frustum(mm(15.0), mm(27.0), mm(2.0), mm(7.5), segments=24, mat=1)                      # collar
    b.frustum(mm(26.0), mm(122.0), mm(9.4), mm(9.8), segments=32, mat=2)                     # body
    b.frustum(mm(121.0), mm(132.0), mm(9.0), mm(7.0), segments=32, mat=1)                    # cap
    b.box(mm(0.0, -9.8, 70.0), mm(9.0, 1.4, 26.0), mat=3)                                    # LCD window
    b.box(mm(0.0, -10.5, 74.0), mm(6.0, 0.4, 3.0), mat=4)                                    # readout bars
    b.box(mm(0.0, -10.5, 67.0), mm(4.0, 0.4, 3.0), mat=4)
    b.box(mm(0.0, 10.2, 100.0), mm(4.0, 2.0, 34.0), mat=0)                                   # pocket clip
    return b.build("TOOL_PF_Tester", col,
                   PF.mats("M_PF_Steel", "M_PF_Navy", "M_PF_Butter", "M_PF_ScreenOff", "M_PF_ScreenText"),
                   bevel=(mm(0.4), 2))


BUILDERS = (build_screwdriver, build_spudger, build_suction_cup, build_tweezers, build_brush, build_magnifier,
            build_tester)


def build_all(col):
    return [f(col) for f in BUILDERS]


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)
    P.reset_scene()
    PF.write_palette_json(project_root)
    col = P.get_collection("TOOLS_PF")
    preview_col = P.get_collection("Preview")
    tools = build_all(col)

    out_dir = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Tools", "PF")
    for tool in tools:
        path = P.export_fbx_for_unity(tool, os.path.join(out_dir, f"{tool.name}.fbx"))
        print(f"PRS_EXPORTED {path}  tris={PF.count_tris(tool)}")

    if "--no-preview" not in args:
        for i, tool in enumerate(tools):
            tool.location = (mm(-105.0 + 35.0 * i), 0.0, 0.0)
        PF.setup_beauty_render(resolution=(1600, 900), samples=32, world_hex="#E9E2D6", world_strength=0.9)
        PF.golden_hour_rig(preview_col, focus=(0.0, 0.0, 0.07), scale=0.3, sun_from=(-0.5, -1.0, 0.8),
                           sun_energy=2.6, fill_energy=30.0)
        PF.ground_plane(preview_col, "PreviewMat", 2.0, mm(-22.0), "M_PF_NavyMat")
        cam = PF.camera(preview_col, lens=50.0)
        out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "PF", "TOOLS_PF_quick.png")
        print("PRS_PREVIEW " + PF.render_still(cam, (0.0, -0.42, 0.22), (0.0, 0.0, 0.065), out))
        for tool in tools:
            tool.location = (0.0, 0.0, 0.0)
        for o in list(preview_col.objects):
            bpy.data.objects.remove(o)

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Tools", "TOOLS_PF.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")


if __name__ == "__main__":
    main()

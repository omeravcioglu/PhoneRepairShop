"""
Repair tools (Milestone 2): precision screwdriver, spudger, suction cup.

Run from the project root (Blender 5.2+):
    blender --background --factory-startup --python ArtSource/Blender/Scripts/build_tools.py

Outputs:
    ArtSource/Blender/Tools/TOOLS_M2.blend
    Assets/_Project/Art/Models/Tools/TOOL_<Name>.fbx    one FBX per tool
    ArtSource/Blender/Previews/TOOLS_M2.png

Gameplay convention: the origin is the TIP (the point that touches the phone) and the tool runs
along +Z towards the handle (Unity +Y). The game scales tools to a constant on-screen size, so
real-world proportions matter more than absolute size.
"""

import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import prs_bpy as P  # noqa: E402
from prs_bpy import mm  # noqa: E402


def make_materials():
    m = P.make_material
    return {
        "Steel": m("M_Tool_Steel", "#C7CDD6", 0.25, 1.0),
        "Handle": m("M_Tool_Handle", "#F28C82", 0.45),
        "Grip": m("M_Tool_Grip", "#2B2433", 0.7),
        "Spudger": m("M_Tool_Spudger", "#B9A6E8", 0.5),
        "Cup": m("M_Tool_Cup", "#9CC9E8", 0.35),
    }


def build_screwdriver(col, M):
    b = P.MeshBuilder()
    b.box(mm(0.0, 0.0, 2.0), mm(2.4, 0.6, 4.0), mat=0)                    # cross tip
    b.box(mm(0.0, 0.0, 2.0), mm(0.6, 2.4, 4.0), mat=0)
    b.frustum(mm(1.0), mm(45.0), mm(1.25), mm(1.35), segments=16, mat=0)  # shaft
    b.frustum(mm(44.0), mm(49.0), mm(2.6), mm(3.2), segments=24, mat=0)   # ferrule
    b.frustum(mm(48.0), mm(108.0), mm(6.6), mm(7.2), segments=32, mat=1)  # handle
    for z in (58.0, 64.0, 70.0, 76.0):                                     # grip rings
        b.frustum(mm(z), mm(z + 2.2), mm(7.05), mm(7.15), segments=32, mat=2)
    b.frustum(mm(107.0), mm(112.0), mm(5.8), mm(5.2), segments=32, mat=2)  # swivel cap
    return b.build("TOOL_Screwdriver", col, [M["Steel"], M["Handle"], M["Grip"]], bevel=(mm(0.4), 2))


def build_spudger(col, M):
    b = P.MeshBuilder()
    # flat wedge tip: a 4-sided frustum turned 45 degrees and squashed into a blade
    b.frustum(0.0, mm(14.0), mm(1.4), mm(5.7), segments=4, mat=0, scale_xy=(1.0, 0.44), rotate_z_deg=45.0)
    b.slab(P.rounded_rect(mm(8.0), mm(3.5), mm(1.6), 6), mm(14.0), mm(118.0), mat=0)   # body
    b.slab(P.rounded_rect(mm(8.8), mm(4.3), mm(2.0), 6), mm(62.0), mm(92.0), mat=1)    # grip band
    return b.build("TOOL_Spudger", col, [M["Spudger"], M["Grip"]], bevel=(mm(0.35), 2))


def build_suction_cup(col, M):
    b = P.MeshBuilder()
    b.frustum(0.0, mm(7.0), mm(16.0), mm(7.0), segments=40, mat=0)          # cup
    b.frustum(mm(6.5), mm(21.0), mm(3.4), mm(3.0), segments=20, mat=1)     # stem
    ring_center_z, ring_radius, tube = 30.0, 9.0, 2.2                        # pull ring
    steps = 18
    pts = [(ring_radius * math.cos(2 * math.pi * i / steps), 0.0,
            ring_center_z + ring_radius * math.sin(2 * math.pi * i / steps)) for i in range(steps)]
    for i in range(steps):
        p0, p1 = pts[i], pts[(i + 1) % steps]
        b.cylinder_between(mm(*p0), mm(*p1), mm(tube), segments=12, mat=1)
    return b.build("TOOL_SuctionCup", col, [M["Cup"], M["Handle"]], bevel=(mm(0.4), 2))


def main():
    import bpy
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = P.project_root_from(__file__)

    P.reset_scene()
    col = P.get_collection("TOOLS_M2")
    preview_col = P.get_collection("Preview")
    M = make_materials()

    tools = [build_screwdriver(col, M), build_spudger(col, M), build_suction_cup(col, M)]
    out_dir = os.path.join(project_root, "Assets", "_Project", "Art", "Models", "Tools")
    for tool in tools:
        path = P.export_fbx_for_unity(tool, os.path.join(out_dir, f"{tool.name}.fbx"))
        print(f"PRS_EXPORTED {path}")

    if "--no-preview" not in args:
        # lay them side by side for the preview only
        for i, tool in enumerate(tools):
            tool.location = (mm(-40.0 + 40.0 * i), 0.0, 0.0)
        P.setup_preview_render()
        cam = P.preview_camera(preview_col, lens=50.0)
        out = os.path.join(project_root, "ArtSource", "Blender", "Previews", "TOOLS_M2.png")
        print("PRS_PREVIEW " + P.render_from(cam, (0.05, -0.32, 0.12), (0.0, 0.0, 0.055), out))
        for tool in tools:
            tool.location = (0.0, 0.0, 0.0)

    blend_path = os.path.join(project_root, "ArtSource", "Blender", "Tools", "TOOLS_M2.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"PRS_SAVED {blend_path}")


if __name__ == "__main__":
    main()

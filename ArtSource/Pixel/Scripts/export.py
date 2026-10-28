"""
export: write every sprite in the art contract to ArtSource/Pixel/Art/<file>.png at its exact size,
check it against the contract, and build contact sheets for review.

    python export.py            # writes ../Art and ../Preview/sheets
    python export.py --install  # also copies the PNGs over the placeholders in Assets (then run
                                #   Tools > Phone Repair > Run Setup in Unity)

Proposed additions that are not in the contract yet go to ../Art/_proposed.
"""

from __future__ import annotations

import os
import shutil
import sys

import bench as B
import cast
import phone as P
import px
import scene
import shop
import ui

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.normpath(os.path.join(HERE, "..", "Art"))
SHEETS = os.path.normpath(os.path.join(HERE, "..", "Preview", "sheets"))


def builders() -> dict[str, list[px.Canvas] | px.Canvas]:
    """Contract id -> canvas, or a list of frames."""
    out: dict = {
        "shop_bg": shop.build_shop_bg(),
        "shop_door_closed": shop.door(False),
        "shop_door_open": shop.door(True),
        "shop_counter": shop.build_counter(),
        "prop_register": shop.register(),
        "prop_bell": [shop.bell(False), shop.bell(True)],
        "prop_cat": [shop.cat(False), shop.cat(True)],
        "prop_sign_open": shop.sign("OPEN", False),
        "prop_sign_closed": shop.sign("CLOSED", True),
        "prop_phone_counter": shop.phone_on_counter(),
        "bench_bg": B.build_bench_bg(),
        "bench_spares_box": B.spares_box(),
        "phone_screw": [P.screw(f) for f in range(4)],
        "tool_hands": ui.tool_icon("hands"),
        "tool_screwdriver": ui.tool_icon("screwdriver"),
        "tool_spudger": ui.tool_icon("spudger"),
        "tool_suction": ui.tool_icon("suction"),
        "cursor_hand": ui.cursor("hand"),
        "cursor_hand_pressed": ui.cursor("hand_pressed"),
        "cursor_screwdriver": ui.cursor("screwdriver"),
        "cursor_spudger": ui.cursor("spudger"),
        "cursor_suction": ui.cursor("suction"),
        "ui_bubble": ui.bubble(),
        "ui_bubble_tail": ui.bubble_tail(),
        "ui_more": ui.more_arrow(),
        "ui_paper": ui.paper(),
        "ui_ticket": ui.ticket(),
        "ui_button": ui.button(),
        "ui_pill": ui.pill(),
        "ui_tool_slot": [ui.tool_slot(False), ui.tool_slot(True)],
        "ui_ring": [ui.ring(k) for k in range(9)],
        "ui_portrait_frame": ui.portrait_frame(),
        "ui_logo": ui.logo(256, 80, tagline=False),
    }
    for name, fn in P.LAYERS.items():
        out[name] = fn()
    for cid, (body, face) in cast.CAST.items():
        out[f"cust_{cid}_idle"] = [body("idle"), body("blink")]
        out[f"cust_{cid}_talk"] = [body("talk"), body("talk_open")]
        out[f"cust_{cid}_happy"] = body("happy")
        out[f"cust_{cid}_sad"] = body("sad")
        for f in cast.FACE_FRAMES:
            out[f"face_{cid}_{f}"] = face(f)
    return out


def proposed() -> dict[str, px.Canvas]:
    return {
        "UI/ui_logo_tagline": ui.logo(256, 104, tagline=True),
    }


def export(install: bool = False) -> list[str]:
    spec = scene.contract()
    made = builders()
    problems: list[str] = []
    written: list[str] = []
    for s in spec["sprites"]:
        sid = s["id"]
        if sid not in made:
            problems.append(f"{sid}: not drawn")
            continue
        frames = made[sid] if isinstance(made[sid], list) else [made[sid]]
        want = max(1, s.get("frames", 1))
        if len(frames) != want:
            problems.append(f"{sid}: {len(frames)} frames, contract wants {want}")
        for i, cv in enumerate(frames):
            if (cv.w, cv.h) != (s["w"], s["h"]):
                problems.append(f"{sid}[{i}]: {cv.w}x{cv.h}, contract wants {s['w']}x{s['h']}")
            if s.get("outline") and not px.ensure_padding(cv):
                problems.append(f"{sid}[{i}]: needs 1 px of clear border (outline)")
            if not cv.alpha_mask().any():
                problems.append(f"{sid}[{i}]: empty")
            name = s["file"] + (f"_{i}" if want > 1 else "") + ".png"
            path = os.path.join(ART, *name.split("/"))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            cv.save(path)
            written.append(path)
    for name, cv in proposed().items():
        path = os.path.join(ART, "_proposed", *(name + ".png").split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        cv.save(path)
    if problems:
        raise SystemExit("Contract problems:\n  " + "\n  ".join(problems))
    if install:
        root = os.path.join(scene.ROOT, spec["root"].replace("/", os.sep))
        for path in written:
            dst = os.path.join(root, os.path.relpath(path, ART))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(path, dst)
    return written


def contact_sheets() -> dict[str, str]:
    """One sheet per group, on a neutral ground, for the canvas."""
    spec = scene.contract()
    made = builders()
    groups: dict[str, list[px.Canvas]] = {}
    for s in spec["sprites"]:
        frames = made[s["id"]] if isinstance(made[s["id"]], list) else [made[s["id"]]]
        groups.setdefault(s["group"], []).extend(frames)
    os.makedirs(SHEETS, exist_ok=True)
    out = {}
    for g, items in groups.items():
        if g in ("Shop", "Bench"):
            items = [i for i in items if i.w < 400]                   # the big backgrounds get their own boards
        if not items:
            continue
        cols = {"Customers": 9, "Phone": 8, "Tools": 9, "UI": 10, "Shop": 6, "Bench": 2}.get(g, 8)
        sh = px.sheet(items, cols, 6, px.C("rice", 3))
        out[g] = sh.save(os.path.join(SHEETS, f"sheet_{g.lower()}.png"), 2)
    return out


if __name__ == "__main__":
    files = export(install="--install" in sys.argv)
    print(f"{len(files)} files written to {ART}")
    for g, p in contact_sheets().items():
        print("sheet", g, p)

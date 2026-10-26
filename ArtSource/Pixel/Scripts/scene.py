"""
scene: place contract sprites the way the game does (world pixels, y up, per-sprite pivots) to compose
640x360 screens for the canvas mockups. The shop view is world y 0..360; the bench view is y -360..0,
so bench screens use an offset of -360.
"""

from __future__ import annotations

import json
import os

import px

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
CONTRACT = os.path.join(ROOT, "Assets", "_Project", "Art", "Pixel", "art_contract.json")
SW, SH = 640, 360


def contract() -> dict:
    with open(CONTRACT, encoding="utf-8") as f:
        return json.load(f)


def anchors() -> dict[str, tuple[int, int]]:
    return {a["id"]: (a["x"], a["y"]) for a in contract()["anchors"]}


def place(screen: px.Canvas, sprite: px.Canvas, x: float, y: float, pivot: str = "BottomCenter", view_y0: int = 0,
          pixel_pivot: tuple[int, int] | None = None) -> None:
    """Blit sprite with its pivot at world (x, y). view_y0 = world y of the screen's bottom row.
    Pivots are whole pixels, as the importer sets them (Center = w // 2, h // 2)."""
    w, h = sprite.w, sprite.h
    if pixel_pivot is not None:
        pvx, pvy = pixel_pivot                     # from the bottom-left corner, as in the contract
    else:
        pvx, pvy = {
            "BottomCenter": (w // 2, 0),
            "BottomLeft": (0, 0),
            "Center": (w // 2, h // 2),
            "TopLeft": (0, h),
        }[pivot]
    left, bottom = x - pvx, y - pvy
    top_row = SH - (bottom - view_y0) - h
    screen.blit(sprite, int(round(left)), int(round(top_row)))


def row(world_y: float, view_y0: int = 0) -> int:
    """Image row of a world y (the row just below that line)."""
    return int(round(SH - (world_y - view_y0)))


def half(sprite: px.Canvas) -> px.Canvas:
    """Nearest-neighbour half size, as the game draws a customer in the doorway."""
    c = px.Canvas(sprite.w // 2, sprite.h // 2)
    c.a = sprite.a[::2, ::2].copy()
    return c

"""
store_assets: Steam store and library art for "Have You Tried Rice?", made from the game's own sprites.

Pixel art is composed at a low size and scaled up by whole numbers, so nothing gets mixels. The capsules
show the logo printed on an indigo shop curtain (noren) above the regulars lined up behind the counter.
The library hero is a wide view of the repair table.

Steam allows no words on capsules except the game's name, and none at all on the library hero, so
everything here is drawn with the props' labels, signs and notes left blank (textless).

    python store_assets.py      # writes Marketing/Steam/
"""

from __future__ import annotations

import contextlib
import os
import shutil

import numpy as np
from PIL import Image

import bench as B
import cast
import phone as P
import px
import pxfont as F
import scene
import shop
import ui

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(ROOT, "Marketing", "Steam")
SHOTS = os.path.join(ROOT, "Docs", "Images", "S2")


def C(n, i, a=255):
    return px.C(n, i, a)


@contextlib.contextmanager
def textless():
    """Draw the 5x7 and 3x5 faces as nothing (widths still measure), so labels and signs come out blank,
    and leave the print off Ren's hoodie. The logo is set in the stroke face and is not affected."""
    saved = F._draw, F.micro, cast.GG_PRINT
    F._draw = lambda cv, x, y, s, colour, spacing: F.measure(s, spacing)
    F.micro = lambda cv, x, y, s, colour, spacing=1: F.micro_width(s, spacing)
    cast.GG_PRINT = False
    try:
        yield
    finally:
        F._draw, F.micro, cast.GG_PRINT = saved


_BG = None


def shop_bg() -> px.Canvas:
    global _BG
    if _BG is None:
        _BG = shop.build_shop_bg()
    return _BG


def counter_scene(lineup, cat_x: int | None = 84, register: bool = True) -> px.Canvas:
    """The shop at 640x360 with customers standing behind the counter. lineup = [(id, expression, x)]."""
    w = px.Canvas(640, 360)
    w.blit(shop_bg(), 0, 0)
    scene.place(w, shop.door(False), 150, 112)
    scene.place(w, shop.sign("OPEN", False), 596, 330, "Center")
    for cid, expr, x in lineup:
        body, _ = cast.CAST[cid]
        scene.place(w, body(expr), x, 44)
    scene.place(w, shop.build_counter(), 0, 0, "BottomLeft")
    if register:
        scene.place(w, shop.register(), 548, 100)
    scene.place(w, shop.bell(False), 470, 100)
    if cat_x is not None:
        scene.place(w, shop.cat(False), cat_x, 100)
    return w


def noren(w: int, h: int, slit_from: int, panels: int = 3) -> px.Canvas:
    """An indigo shop curtain on a wooden rod, split into panels from slit_from down."""
    cv = px.Canvas(w, h)
    cloth = px.box(w, h, 0, 3, w - 1, h - 1)
    cv.fill(cloth, C("blue", 0))
    cv.fill(cloth & px.bayer(w, h, 0.14) & px.checker(w, h), C("ink", 2))
    cv.hline(0, w - 1, 4, C("blue", 1))
    cv.rect(0, h - 3, w - 1, h - 1, C("ink", 2))
    cv.hline(0, w - 1, h - 1, C("ink", 1))
    for k in range(1, panels):
        sx = round(k * w / panels)
        cv.rect(sx - 1, slit_from, sx, h - 1, (0, 0, 0, 0))
        cv.vline(sx - 2, slit_from, h - 1, C("ink", 2))
        cv.vline(sx + 1, slit_from, h - 1, C("blue", 1))
    cv.rect(0, 0, w - 1, 3, C("wood", 2))
    cv.hline(0, w - 1, 0, C("wood", 3))
    cv.hline(0, w - 1, 3, C("wood", 1))
    return cv


def hang(base: px.Canvas, curtain: px.Canvas, x: int, y: int) -> None:
    """Hang a curtain over a scene, with a soft shadow under its hem."""
    placed = px.Canvas(base.w, base.h)
    placed.blit(curtain, x, y)
    mask = placed.alpha_mask()
    shade = (px.shift(mask, -1, 2) | px.shift(mask, -1, 3)) & ~mask
    a = base.a.astype(np.float32)
    ink = np.array(C("ink", 0)[:3], np.float32)
    a[shade, :3] = a[shade, :3] * 0.6 + ink * 0.4
    base.a = a.round().astype(np.uint8)
    base.blit(curtain, x, y)


def logo_on(cv: px.Canvas, cx: int, top: int, tagline: bool = False, scale: float = 1.0) -> None:
    lg = ui.logo(round(256 * scale), round((104 if tagline else 80) * scale), tagline=tagline, scale=scale)
    cv.blit(lg, cx - lg.w // 2, top)


LINEUP_5 = [("madame_k", "happy", 120), ("sasaki", "happy", 220), ("fumi", "happy", 320),
            ("mio", "happy", 420), ("ren", "happy", 520)]


def main_capsule() -> px.Canvas:
    """1232x706 = 616x353 at 2x. The logo is drawn at 1.4x so it spans almost half the width."""
    world = counter_scene(LINEUP_5, cat_x=588, register=False)
    cv = world.crop(12, 3, 616, 353)
    hang(cv, noren(616, 128, 112), 0, 0)
    logo_on(cv, 308, 4, scale=1.4)
    return cv


def header_capsule() -> px.Canvas:
    """920x430 = 460x215 at 2x (also the library header)."""
    world = counter_scene([("sasaki", "happy", 240), ("fumi", "happy", 330), ("mio", "happy", 420)], cat_x=None,
                          register=False)
    cv = world.crop(90, 60, 460, 215)
    hang(cv, noren(460, 98, 92), 0, 0)
    logo_on(cv, 230, 10)
    return cv


def small_capsule() -> px.Canvas:
    """462x174 = 231x87 at 2x: the logo nearly filling the curtain, as Steam asks of small capsules."""
    cv = noren(231, 87, 87, 1)
    logo_on(cv, 115, 0)
    return cv


def tall_capsule(w: int, h: int, lineup, crop_x: int, curtain_h: int) -> px.Canvas:
    world = counter_scene(lineup, register=False)
    cv = px.Canvas(w, h)
    dy = h - 360
    cv.blit(world.crop(crop_x, 0, w, 360), 0, dy)
    # the room continues up behind the curtain
    cv.blit(world.crop(crop_x, 0, w, dy + 4), 0, 0)
    hang(cv, noren(w, curtain_h, curtain_h - 24, 3 if w > 320 else 2), 0, 0)
    logo_on(cv, w // 2, 22)
    return cv


def vertical_capsule() -> px.Canvas:
    """748x896 = 374x448 at 2x."""
    lineup = [("mio", "happy", 236), ("fumi", "happy", 320), ("madame_k", "happy", 410)]
    return tall_capsule(374, 448, lineup, 133, 128)


def library_capsule() -> px.Canvas:
    """600x900 = 300x450 at 2x."""
    lineup = [("mio", "happy", 262), ("fumi", "happy", 330), ("ren", "happy", 400)]
    return tall_capsule(300, 450, lineup, 180, 128)


def hero() -> px.Canvas:
    """3840x1240 = 960x310 at 4x: the repair table, mid-job. No logo or title."""
    saved = (B.W, B.H, B.CALM)
    try:
        B.W, B.H, B.CALM = 960, 310, []
        wood = px.Canvas(960, 310)
        B.butcher_block(wood)
    finally:
        B.W, B.H, B.CALM = saved
    things = px.Canvas(640, 360)
    B.teacup(things, 108, 290)
    B.slate(things)
    B.felt(things)
    B.bento(things)
    B.tool_roll(things)
    B.chopsticks(things)
    for i, y in enumerate((-100, -142, -184, -226)):
        scene.place(things, ui.tool_slot(i == 1), 32, y, "Center", -360)
        scene.place(things, ui.tool_icon(("hands", "screwdriver", "spudger", "suction")[i]), 32, y, "Center", -360)
    opened = P.assemble(["phone_back_body", "phone_board", "phone_battery_swollen", "phone_battery_conn_open",
                         "phone_display_conn"])
    scene.place(things, opened, 232, -200, "Center", -360)
    cover = P.cover()
    ys, xs = cover.alpha_mask().nonzero()
    part = cover.crop(xs.min(), ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
    scene.place(things, part, 366, -200, "Center", -360)
    screw = P.screw(0)
    for x, y in B.SLOTS[:4]:
        scene.place(things, screw, x, -y, "Center", -360)
    wood.blit(things, 160, -12)
    return wood


def library_logo() -> Image.Image:
    """1280x720 PNG, transparent: the logo at 5x, centred."""
    lg = ui.logo(256, 80, tagline=False).image(5)
    out = Image.new("RGBA", (1280, 720), (0, 0, 0, 0))
    out.paste(lg, (0, (720 - lg.height) // 2), lg)
    return out


def emblem(scale: float, size: int) -> px.Canvas:
    em = ui.rice_bowl_phone(size * 2, size * 2, size, size * 1.2, scale)
    ys, xs = em.alpha_mask().nonzero()
    part = em.crop(xs.min(), ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
    cv = px.Canvas(size, size)
    cv.blit(part, (size - part.w) // 2, (size - part.h) // 2)
    return cv


def app_icon() -> Image.Image:
    """184x184 JPG: the emblem on rice paper, 46x46 at 4x."""
    cv = px.Canvas(46, 46, C("rice", 4))
    cv.blit(emblem(1.12, 46), 0, 1)
    return cv.image(4).convert("RGB")


def shortcut_icon(path: str) -> None:
    big = emblem(1.0, 42).image(6)                       # 252x252, padded to 256
    img256 = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    img256.paste(big, (2, 2), big)
    img48 = emblem(1.0, 48).image(1)
    img32 = emblem(0.74, 32).image(1)
    img16 = emblem(0.38, 16).image(1)
    img256.save(path, format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (256, 256)],
                append_images=[img16, img32, img48])


def write() -> list[str]:
    os.makedirs(OUT, exist_ok=True)
    out = []

    def save(cv: px.Canvas, name: str, scale: int) -> None:
        out.append(cv.save(os.path.join(OUT, name), scale))

    with textless():
        save(main_capsule(), "capsule_main_1232x706.png", 2)
        header = header_capsule()
        save(header, "capsule_header_920x430.png", 2)
        save(header, "library_header_920x430.png", 2)
        save(small_capsule(), "capsule_small_462x174.png", 2)
        save(vertical_capsule(), "capsule_vertical_748x896.png", 2)
        save(library_capsule(), "library_capsule_600x900.png", 2)
        save(hero(), "library_hero_3840x1240.png", 4)
    p = os.path.join(OUT, "library_logo_1280x720.png")
    library_logo().save(p)
    out.append(p)
    p = os.path.join(OUT, "app_icon_184x184.jpg")
    app_icon().save(p, quality=95, subsampling=0)
    out.append(p)
    p = os.path.join(OUT, "shortcut_icon.ico")
    shortcut_icon(p)
    out.append(p)
    shots = os.path.join(OUT, "screenshots")
    os.makedirs(shots, exist_ok=True)
    for name in ("s2_02_counter_talk", "s2_03_choice", "s2_04_bench_front", "s2_06_unscrewing",
                 "s2_07_bench_opened", "s2_08_fixed", "s2_09_day_summary", "s2_01_title"):
        dst = os.path.join(shots, name + ".png")
        shutil.copyfile(os.path.join(SHOTS, name + ".png"), dst)
        out.append(dst)
    return out


if __name__ == "__main__":
    for p in write():
        with Image.open(p) as im:
            print(f"{os.path.relpath(p, ROOT)}  {im.size[0]}x{im.size[1]} {im.mode}")

"""
phone: the Mochi One as the game's full-canvas layers (104x200 each, body 96x192 centred).

Image coordinates (row 0 at the top). Where the game's numbers land on this canvas:
    screws        9x9 sprites, pivot (4,4), centred on pixels (12,11) (92,11) (92,187) (12,187)
    board         ~x 11..92, rows 10..69, notched around the camera, with a tab for the battery socket
    battery       64x84 at x 20..83, rows 84..167
    battery conn  around (52,77); it rides on the battery when the battery comes out
    display conn  around (82,53), its flex comes round the right edge from the screen
Prefab order: back body 0, board 5, battery 20, connectors 30, cover 40, screws 50; front body 0,
screen 10.

The Mochi One is sakura pink: pink band and back, silver midplate, navy glass front, teal board,
amber flex cables. Light comes from the shop window (upper right) as everywhere else.
"""

from __future__ import annotations

import math

import numpy as np

import px
import pxfont as F
from figure import Figure

W, H = 104, 200
SCREWS = [(12, 11), (92, 11), (92, 187), (12, 187)]
CAM = (18, 16, 42, 42)                   # camera module box, inclusive
BAT = (20, 84, 83, 167)                  # battery box, inclusive


def C(n, i, a=255):
    return px.C(n, i, a)


def disc(cx: float, cy: float, r: float) -> np.ndarray:
    """Circle centred ON pixel (cx, cy)."""
    return px.ellipse(W, H, cx + 0.5, cy + 0.5, r, r)


def body_mask() -> np.ndarray:
    return px.rrect(W, H, 4, 4, 99, 195, 12)


def lit_edges(mask: np.ndarray, w: int = 1) -> np.ndarray:
    return mask & (px.edge(mask, 0, -1, w) | px.edge(mask, 1, 0, w))


def dark_edges(mask: np.ndarray, w: int = 1) -> np.ndarray:
    return mask & (px.edge(mask, 0, 1, w) | px.edge(mask, -1, 0, w))


def plate(cv: px.Canvas, mask: np.ndarray, ramp: str, base: int = 2, line: int = 0, lit: int | None = 3,
          dark: int | None = 1, lit_w: int = 1, dark_w: int = 1) -> np.ndarray:
    """A raised flat part: base fill, lit top/right lip, darker bottom/left lip, 1 px line."""
    cv.fill(mask, C(ramp, base))
    inner = mask & ~px.inner(mask)
    if dark is not None:
        cv.fill(dark_edges(inner, dark_w), C(ramp, dark))
    if lit is not None:
        cv.fill(lit_edges(inner, lit_w), C(ramp, lit))
    cv.fill(px.inner(mask), C(ramp, line))
    return inner


def pit(cv: px.Canvas, mask: np.ndarray, floor, shadow, light=None, shadow_w: int = 1) -> None:
    """A recess: the walls nearest the light shade the floor along the top and right."""
    cv.fill(mask, floor)
    cv.fill(lit_edges(mask, shadow_w), shadow)
    if light is not None:
        cv.fill(dark_edges(mask, 1), light)


def lens(cv: px.Canvas, cx: int, cy: int, r: float = 5.0) -> None:
    ring = disc(cx, cy, r)
    cv.fill(ring, C("silver", 2))
    cv.fill(dark_edges(ring), C("silver", 0))
    cv.fill(lit_edges(ring), C("silver", 4))
    cv.fill(disc(cx, cy, r - 1.4), C("ink", 0))
    cv.fill(disc(cx, cy, r - 2.6), C("blue", 0))
    cv.put(cx, cy, C("ink", 0))
    cv.put(cx + 1, cy - 2, C("blue", 3))
    cv.put(cx + 2, cy - 1, C("rice", 5))
    cv.put(cx - 2, cy + 1, C("purple", 1))


def socket(cv: px.Canvas, x0: int, y0: int, x1: int, y1: int, vertical: bool = False) -> None:
    """Board-to-board socket: silver frame, dark well, a row of gold pins."""
    m = px.box(W, H, x0, y0, x1, y1)
    cv.fill(m, C("silver", 1))
    cv.fill(px.inner(m), C("silver", 0))
    cv.fill(lit_edges(m & ~px.inner(m)), C("silver", 2))
    well = px.box(W, H, x0 + 2, y0 + 2, x1 - 2, y1 - 2)
    cv.fill(well, C("ink", 0))
    if vertical:
        for y in range(y0 + 2, y1 - 1, 2):
            cv.put((x0 + x1) // 2, y, C("gold", 3))
    else:
        for x in range(x0 + 2, x1 - 1, 2):
            cv.put(x, (y0 + y1) // 2, C("gold", 3))


# --------------------------------------------------------------------------------------------------
# Back
# --------------------------------------------------------------------------------------------------

def back_body() -> px.Canvas:
    cv = px.Canvas(W, H)
    body = body_mask()
    cav = px.rrect(W, H, 8, 8, 95, 191, 9)
    rim = body & ~cav
    # the pink band: rounded, so each side is lit on the edge that faces the window
    cv.fill(rim, C("pink", 3))
    cv.fill(dark_edges(rim), C("pink", 2))
    cv.fill(lit_edges(rim), C("pink", 4))
    cv.fill(px.inner(body), C("pink", 0))
    # antenna breaks in the band
    for y in (30, 169):
        cv.hline(5, 7, y, C("pink", 1))
        cv.hline(96, 98, y, C("pink", 1))
    # charging port and the mic hole beside it, in the bottom band
    cv.rect(44, 192, 59, 194, C("ink", 0))
    cv.hline(46, 57, 193, C("silver", 1))
    cv.put(66, 193, C("ink", 0))
    cv.put(38, 193, C("ink", 0))

    # midplate
    cv.fill(cav, C("silver", 2))
    wall = px.inner(cav)
    floor = cav & ~wall
    cv.fill(lit_edges(floor, 2), C("silver", 1))
    cv.fill(dark_edges(floor), C("silver", 3))
    cv.fill(wall, C("pink", 1))
    # brushed-metal streaks
    rng = px.rng(11)
    for _ in range(26):
        x = rng.randint(12, 90)
        y = rng.randint(12, 186)
        n = rng.randint(3, 9)
        for k in range(n):
            if floor[y, x + k] if x + k < W else False:
                cv.put(x + k, y, C("silver", 3))

    # battery bay with its pull-tab adhesive strips
    bay = px.rrect(W, H, 19, 83, 84, 168, 3)
    pit(cv, bay, C("silver", 1), C("silver", 0), shadow_w=2)
    cv.fill(px.inner(bay), C("silver", 0))
    for x0 in (29, 70):
        cv.rect(x0, 95, x0 + 4, 156, C("rice", 4))
        cv.vline(x0, 95, 156, C("rice", 3))
        cv.rect(x0, 157, x0 + 4, 164, C("ink", 1))
        cv.hline(x0, x0 + 4, 157, C("ink", 2))
    F.micro_center(cv, 52, 120, "LI-ION", C("silver", 2))
    F.micro_center(cv, 52, 127, "ONLY", C("silver", 2))

    # under the board: a few screws, a foam pad
    for sx, sy in ((50, 74), (86, 74), (16, 74), (16, 176), (88, 176)):
        cv.put(sx, sy, C("silver", 0))
        cv.put(sx + 1, sy - 1, C("silver", 4))

    # camera module (it shows through the cover's window)
    x0, y0, x1, y1 = CAM
    mod = px.rrect(W, H, x0, y0, x1, y1, 5)
    plate(cv, mod, "ink", base=1, line=0, lit=2, dark=None)
    lens(cv, 25, 23)
    lens(cv, 25, 35)
    flash = disc(36, 23, 2.3)
    cv.fill(flash, C("gold", 3))
    cv.fill(dark_edges(flash), C("gold", 2))
    cv.put(36, 23, C("gold", 4))
    cv.put(37, 22, C("rice", 5))
    cv.put(36, 31, C("ink", 0))
    cv.put(36, 36, C("ink", 0))

    # loudspeaker, vibration motor
    spk = px.rrect(W, H, 30, 177, 72, 186, 2)
    plate(cv, spk, "silver", base=2, line=0, lit=3, dark=1)
    for row in (180, 183):
        for x in range(33, 70, 3):
            cv.put(x, row, C("ink", 1))
            cv.put(x + 1, row, C("silver", 1))
    mot = px.rrect(W, H, 77, 173, 90, 186, 2)
    plate(cv, mot, "ink", base=1, line=0, lit=2, dark=None)
    cv.hline(80, 87, 179, C("ink", 0))
    cv.put(80, 183, C("gold", 3))
    cv.put(82, 183, C("gold", 3))

    # screw bosses at the four corners
    for sx, sy in SCREWS:
        boss = disc(sx, sy, 3.6)
        cv.fill(px.outer(boss) & floor, C("silver", 0))
        cv.fill(boss, C("gold", 2))
        cv.fill(dark_edges(boss), C("gold", 1))
        cv.fill(lit_edges(boss), C("gold", 4))
        cv.fill(disc(sx, sy, 1.5), C("ink", 0))

    # the display flex comes round the right edge here
    cv.rect(93, 47, 95, 59, C("ink", 0))
    return cv


def board() -> px.Canvas:
    cv = px.Canvas(W, H)
    main = px.rrect(W, H, 11, 10, 92, 69, 3)
    tab = px.box(W, H, 43, 66, 61, 77)
    notch = px.rrect(W, H, 4, 4, 46, 46, 5)
    corner = disc(92, 11, 6.2)
    pcb = (main | tab) & ~notch & ~corner
    inner = plate(cv, pcb, "teal", base=1, line=0, lit=2, dark=None)

    # traces
    tr = C("teal", 2)
    for pts in (
        [(48, 44), (48, 52), (56, 60), (60, 60)],
        [(14, 50), (18, 50), (22, 46), (40, 46)],
        [(80, 20), (86, 26), (86, 44)],
        [(52, 64), (40, 64), (36, 60), (14, 60)],
        [(64, 62), (74, 62), (78, 58)],
        [(78, 42), (78, 46), (74, 46)],
    ):
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            for x, y in px.bresenham(ax, ay, bx, by):
                if inner[y, x]:
                    cv.put(x, y, tr)

    # application processor under a can (with a little mochi stamped in it)
    can = px.rrect(W, H, 50, 14, 78, 40, 2)
    plate(cv, can, "silver", base=2, line=0, lit=3, dark=1)
    cv.fill(px.inner(disc(64, 28, 5.0)) & px.box(W, H, 0, 24, W, H), C("silver", 1))
    cv.put(62, 28, C("silver", 0))
    cv.put(66, 28, C("silver", 0))
    cv.hline(59, 69, 33, C("silver", 1))
    for x in range(53, 77, 3):
        cv.put(x, 16, C("silver", 1))
    # memory chip, pins on two sides
    chip = px.box(W, H, 58, 46, 75, 57)
    plate(cv, chip, "ink", base=1, line=0, lit=2, dark=None)
    for y in range(47, 57, 2):
        cv.put(57, y, C("gold", 3))
        cv.put(76, y, C("gold", 3))
    cv.put(60, 48, C("ink", 3))
    # second can on the left, under the camera notch
    can2 = px.rrect(W, H, 14, 50, 40, 66, 2)
    plate(cv, can2, "silver", base=2, line=0, lit=3, dark=1)
    F.micro(cv, 18, 55, "M1", C("silver", 1))
    # small chips
    for (ax, ay, bx, by) in ((80, 14, 86, 19), (82, 30, 89, 37)):
        plate(cv, px.box(W, H, ax, ay, bx, by), "ink", base=1, line=0, lit=2, dark=None)
    # display socket (right edge) and battery socket (on the tab)
    socket(cv, 77, 48, 88, 58, vertical=True)
    socket(cv, 45, 69, 59, 75)
    # passives: tan and coral caps, gold test pads
    rng = px.rng(7)
    free = inner.copy()
    for m in (can, chip, can2, px.box(W, H, 77, 48, 88, 58), px.box(W, H, 45, 69, 59, 75),
              px.box(W, H, 80, 14, 86, 19), px.box(W, H, 82, 30, 89, 37)):
        free &= ~px.ndimage.binary_dilation(m, iterations=2)
    placed = 0
    tries = 0
    while placed < 34 and tries < 2000:
        tries += 1
        x = rng.randint(12, 90)
        y = rng.randint(11, 74)
        if not (free[y, x] and free[y, x + 1]):
            continue
        kind = rng.random()
        if kind < 0.5:
            cv.put(x, y, C("rice", 2))
            cv.put(x + 1, y, C("rice", 3))
        elif kind < 0.75:
            cv.put(x, y, C("coral", 1))
            cv.put(x + 1, y, C("coral", 2))
        else:
            cv.put(x, y, C("gold", 3))
        free[max(y - 1, 0):y + 2, max(x - 1, 0):x + 3] = False
        placed += 1
    # a tiny smiley sticker someone left on the board
    st = disc(85, 63, 2.6)
    cv.fill(st, C("gold", 4))
    cv.fill(dark_edges(st), C("gold", 3))
    cv.put(84, 62, C("ink", 1))
    cv.put(86, 62, C("ink", 1))
    cv.put(85, 64, C("ink", 1))
    return cv


def _battery_label(cv: px.Canvas, worried: bool) -> None:
    lab = px.rrect(W, H, 26, 93, 77, 152, 2)
    cv.fill(lab, C("rice", 4))
    cv.fill(lit_edges(lab), C("rice", 5))
    cv.fill(dark_edges(lab), C("rice", 3))
    band = lab & px.box(W, H, 0, 93, W, 103)
    cv.fill(band, C("pink", 3))
    cv.fill(band & px.edge(band, 0, 1, 1), C("pink", 2))
    F.micro_center(cv, 52, 96, "MOCHI", C("rice", 5))
    if worried:
        # the battery is not feeling well
        for ex in (44, 59):
            cv.rect(ex, 112, ex + 1, 114, C("ink", 1))
        for x, y in ((47, 121), (48, 120), (49, 120), (50, 121), (51, 122), (52, 121), (53, 120),
                     (54, 120), (55, 121), (56, 122)):
            cv.put(x, y, C("ink", 1))
        cv.put(39, 117, C("pink", 2))
        cv.put(40, 117, C("pink", 2))
        cv.put(63, 117, C("pink", 2))
        cv.put(64, 117, C("pink", 2))
        drop = [(66, 107), (66, 108), (65, 109), (66, 109), (67, 109), (65, 110), (66, 110), (67, 110)]
        cv.pixels(drop, C("blue", 3))
        cv.put(66, 110, C("blue", 2))
    else:
        bolt = px.poly(W, H, [(55, 106), (45, 121), (51, 121), (48, 133), (59, 116), (53, 116), (57, 106)])
        cv.fill(bolt, C("gold", 3))
        cv.fill(dark_edges(bolt), C("gold", 2))
        cv.fill(px.outer(bolt) & lab, C("gold", 1))
    F.micro_center(cv, 52, 137, "3000MAH", C("ink", 1))
    cv.hline(30, 73, 144, C("rice", 2))
    for i, x in enumerate(range(31, 73, 7)):
        cv.rect(x, 146, x + 3, 149, C("ink", 2) if i % 2 == 0 else C("coral", 2))


def battery() -> px.Canvas:
    cv = px.Canvas(W, H)
    x0, y0, x1, y1 = BAT
    pouch = px.rrect(W, H, x0, y0, x1, y1, 3)
    plate(cv, pouch, "ink", base=2, line=0, lit=3, dark=1)
    # sealed foil seam along the top, where the flex comes out
    cv.hline(x0 + 2, x1 - 2, y0 + 3, C("ink", 1))
    _battery_label(cv, worried=False)
    # coral pull tab at the bottom
    tab = px.rrect(W, H, 46, 160, 57, 171, 2)
    plate(cv, tab, "coral", base=2, line=0, lit=3, dark=1)
    cv.hline(49, 54, 168, C("coral", 1))
    return cv


def battery_swollen() -> px.Canvas:
    """Same footprint, puffed up like a pillow: dome shading, stretched label, a worried face."""
    fig = Figure(W, H)
    x0, y0, x1, y1 = BAT
    pouch = px.rrect(W, H, x0, y0, x1, y1, 11)
    fig.part(pouch, "ink", tones=(1, 2, 3), cuts=(0.55, 0.9), bulge=1.6, flat=0.0)
    cv = fig.finish()
    cv.fill(px.inner(pouch), C("ink", 0))
    # specular highlight on the bulge
    hi = px.ellipse(W, H, 70.5, 100.5, 5, 3) & pouch
    cv.fill(hi & px.checker(W, H), C("ink", 3))
    cv.put(71, 98, C("blue", 4))
    cv.put(72, 99, C("blue", 4))
    cv.put(73, 100, C("rice", 5))
    _battery_label(cv, worried=True)
    # the label stretches: creases from the corners
    for (ax, ay, bx, by) in ((27, 94, 33, 100), (76, 94, 70, 100), (27, 151, 33, 145), (76, 151, 70, 145)):
        for x, y in px.bresenham(ax, ay, bx, by):
            cv.put(x, y, C("rice", 2))
    # heat: a warm glow on the lower edge
    warm = dark_edges(pouch & ~px.inner(pouch), 2) & px.box(W, H, 0, 140, W, H)
    cv.fill(warm & px.checker(W, H), C("plum", 2))
    tab = px.rrect(W, H, 46, 160, 57, 171, 2)
    plate(cv, tab, "coral", base=2, line=0, lit=3, dark=1)
    cv.hline(49, 54, 168, C("coral", 1))
    return cv


def _flex(cv: px.Canvas, mask: np.ndarray, along: str = "y") -> None:
    """Amber polyimide flex with copper traces."""
    cv.fill(mask, C("orange", 3))
    cv.fill(dark_edges(mask), C("orange", 2))
    cv.fill(lit_edges(mask), C("orange", 4))
    ys, xs = np.nonzero(mask)
    if along == "y":
        for x in range(xs.min() + 2, xs.max() - 1, 2):
            for y in range(ys.min() + 1, ys.max()):
                if mask[y, x]:
                    cv.put(x, y, C("orange", 2))
    else:
        for y in range(ys.min() + 2, ys.max() - 1, 2):
            for x in range(xs.min() + 1, xs.max()):
                if mask[y, x]:
                    cv.put(x, y, C("orange", 2))
    cv.fill(px.inner(mask), C("orange", 1))


def battery_conn(open_: bool = False) -> px.Canvas:
    cv = px.Canvas(W, H)
    if not open_:
        _flex(cv, px.box(W, H, 47, 74, 57, 89))
        plug = px.rrect(W, H, 43, 67, 61, 77, 2)
        plate(cv, plug, "silver", base=2, line=0, lit=4, dark=1)
        cv.hline(46, 58, 70, C("silver", 3))
        cv.put(47, 73, C("silver", 0))
        cv.put(57, 73, C("silver", 0))
    else:
        # plug flipped back over its own flex: we see its underside, gold contacts up
        _flex(cv, px.box(W, H, 47, 80, 57, 89))
        plug = px.rrect(W, H, 43, 78, 61, 87, 2)
        cv.fill(px.box(W, H, 44, 88, 60, 88), C("ink", 1))       # its shadow on the battery
        plate(cv, plug, "ink", base=1, line=0, lit=2, dark=None)
        for x in range(46, 59, 2):
            cv.put(x, 81, C("gold", 3))
            cv.put(x, 82, C("gold", 2))
        cv.hline(45, 59, 85, C("ink", 2))
    return cv


def display_conn(open_: bool = False) -> px.Canvas:
    cv = px.Canvas(W, H)
    if not open_:
        _flex(cv, px.box(W, H, 86, 49, 95, 57), along="x")
        plug = px.rrect(W, H, 75, 46, 89, 60, 2)
        plate(cv, plug, "silver", base=2, line=0, lit=4, dark=1)
        cv.vline(78, 49, 57, C("silver", 3))
        cv.put(80, 49, C("silver", 0))
        cv.put(80, 57, C("silver", 0))
    else:
        _flex(cv, px.box(W, H, 89, 49, 95, 57), along="x")
        plug = px.rrect(W, H, 87, 46, 97, 60, 2)
        cv.fill(px.box(W, H, 86, 47, 86, 60), C("ink", 1))
        plate(cv, plug, "ink", base=1, line=0, lit=2, dark=None)
        for y in range(48, 59, 2):
            cv.put(91, y, C("gold", 3))
            cv.put(92, y, C("gold", 2))
    return cv


def cover() -> px.Canvas:
    cv = px.Canvas(W, H)
    shell = px.rrect(W, H, 6, 6, 97, 193, 10)
    win = px.rrect(W, H, 18, 16, 42, 42, 5)
    bump = px.rrect(W, H, 15, 13, 45, 45, 7)
    back = shell & ~win
    cv.fill(back, C("pink", 3))
    # glossy back glass: two diagonal sheens
    xx, yy = px.grid(W, H)
    d = xx + yy
    sheen = back & (((d > 112) & (d < 118)) | ((d > 122) & (d < 124)))
    cv.fill(sheen, C("pink", 4))
    cv.fill(dark_edges(back, 1), C("pink", 2))
    cv.fill(lit_edges(back, 1), C("pink", 4))
    cv.fill(px.inner(shell), C("pink", 1))
    # camera bump ring around the window
    ring = bump & ~win
    cv.fill(ring, C("pink", 2))
    cv.fill(lit_edges(ring) & ~px.inner(bump), C("pink", 4))
    cv.fill(px.inner(bump), C("pink", 1))
    cv.fill(px.outer(win) & ring, C("pink", 0))
    # screw holes: countersinks the screws sit in
    for sx, sy in SCREWS:
        cs = disc(sx, sy, 5.2)
        cv.fill(cs & shell, C("pink", 2))
        cv.fill(lit_edges(cs) & shell, C("pink", 1))
        cv.fill(disc(sx, sy, 2.2), C("ink", 1))
        cv.put(sx - 1, sy + 1, C("ink", 2))
    # the Mochi mascot and the name
    mo = px.ellipse(W, H, 52, 103, 16, 12.5) & px.box(W, H, 0, 0, W, 112) | px.rrect(W, H, 36, 100, 67, 113, 6)
    cv.fill(mo, C("rice", 5))
    cv.fill(dark_edges(mo, 2), C("rice", 3))
    cv.fill(px.outer(mo), C("pink", 1))
    cv.rect(44, 101, 45, 102, C("ink", 1))
    cv.rect(58, 101, 59, 102, C("ink", 1))
    cv.hline(40, 42, 106, C("pink", 3))
    cv.hline(61, 63, 106, C("pink", 3))
    cv.pixels([(50, 106), (51, 107), (52, 107), (53, 106)], C("ink", 1))
    F.text_center(cv, 52, 121, "mochi", C("pink", 1))
    F.micro_center(cv, 52, 180, "ONE", C("pink", 2))
    return cv


def screw(frame: int) -> px.Canvas:
    """9x9 with a 7x7 head (1 px clear border for the hover outline). The cross alternates + and x, the
    way a turning Phillips head reads at this size."""
    head = [
        ".........",
        "..oooOO..",
        ".obbbbbO.",
        ".obbbbbO.",
        ".obbbbbo.",
        ".osbbbbo.",
        ".ossssbo.",
        "..ooooo..",
        ".........",
    ]
    tone = {"o": C("silver", 0), "O": C("silver", 1), "b": C("silver", 3), "s": C("silver", 2)}
    cv = px.Canvas(9, 9)
    for r, row in enumerate(head):
        for c, v in enumerate(row):
            if v in tone:
                cv.put(c, r, tone[v])
    cv.put(5, 2, C("silver", 4))
    cv.put(6, 3, C("silver", 4))
    if frame % 2 == 0:
        recess = [(4, y) for y in range(2, 7)] + [(x, 4) for x in range(2, 7)]
    else:
        recess = [(2 + k, 2 + k) for k in range(5)] + [(6 - k, 2 + k) for k in range(5)]
    cv.pixels(recess, C("ink", 1))
    cv.put(4, 4, C("ink", 0))
    return cv


# --------------------------------------------------------------------------------------------------
# Front
# --------------------------------------------------------------------------------------------------

GLASS = (7, 7, 96, 192)


def front_body() -> px.Canvas:
    cv = px.Canvas(W, H)
    body = body_mask()
    x0, y0, x1, y1 = GLASS
    hole = px.rrect(W, H, x0, y0, x1, y1, 10)
    rim = body & ~hole
    cv.fill(rim, C("pink", 3))
    cv.fill(dark_edges(rim), C("pink", 2))
    cv.fill(lit_edges(rim), C("pink", 4))
    cv.fill(px.inner(body), C("pink", 0))
    # side buttons: a bump on the left band, the power key on the right
    cv.vline(4, 40, 52, C("pink", 1))
    cv.vline(4, 58, 70, C("pink", 1))
    cv.vline(99, 48, 64, C("pink", 1))
    # under the screen: midplate, adhesive gasket, earpiece/camera module, the display flex
    pit(cv, hole, C("silver", 2), C("silver", 1), C("silver", 3), shadow_w=2)
    cv.fill(px.inner(hole), C("pink", 1))
    gasket = px.inner(px.rrect(W, H, x0 + 2, y0 + 2, x1 - 2, y1 - 2, 8))
    cv.fill(gasket, C("rice", 4))
    top = px.rrect(W, H, 42, 11, 66, 18, 2)
    plate(cv, top, "ink", base=1, line=0, lit=2, dark=None)
    cv.fill(disc(62, 14, 1.6), C("blue", 0))
    cv.put(62, 13, C("blue", 3))
    cv.hline(46, 56, 14, C("ink", 0))
    for sx, sy in ((14, 40), (89, 40), (14, 160), (89, 160), (52, 100)):
        cv.put(sx, sy, C("silver", 0))
        cv.put(sx + 1, sy - 1, C("silver", 4))
    sticker = px.rrect(W, H, 30, 128, 74, 150, 1)
    cv.fill(sticker, C("rice", 4))
    cv.fill(dark_edges(sticker), C("rice", 3))
    F.micro_center(cv, 52, 131, "DO NOT", C("coral", 2))
    F.micro_center(cv, 52, 138, "MICROWAVE", C("coral", 2))
    cv.hline(34, 70, 145, C("rice", 2))
    _flex(cv, px.box(W, H, 86, 46, 95, 60), along="x")
    return cv


def _glass(cv: px.Canvas) -> tuple[np.ndarray, np.ndarray]:
    x0, y0, x1, y1 = GLASS
    glass = px.rrect(W, H, x0, y0, x1, y1, 10)
    cv.fill(glass, C("ink", 0))
    display = px.rrect(W, H, x0 + 4, y0 + 9, x1 - 4, y1 - 8, 5)
    cv.fill(display, C("ink", 1))
    # an off screen still reflects the window: a soft gradient and two diagonal sheens
    xx, yy = px.grid(W, H)
    cv.fill(display & (yy < 40) & px.bayer(W, H, 0.5), C("ink", 2))
    cv.fill(display & (yy < 28), C("ink", 2))
    d = xx * 1.0 + yy * 0.62
    cv.fill(display & (d > 86) & (d < 104), C("ink", 2))
    cv.fill(display & (d > 108) & (d < 112), C("ink", 2))
    cv.fill(display & (d > 86) & (d < 89), C("ink", 3))
    # bezel details: earpiece slit and front camera
    cv.hline(46, 57, 11, C("ink", 2))
    cv.fill(disc(63, 11, 1.5), C("blue", 0))
    cv.put(64, 10, C("blue", 3))
    # glass edge catching the light
    cv.fill(lit_edges(glass & ~px.inner(glass)), C("ink", 3))
    cv.fill(px.inner(glass), C("ink", 0))
    return glass, display


def screen() -> px.Canvas:
    cv = px.Canvas(W, H)
    _glass(cv)
    return cv


def screen_cracked(seed: int = 3) -> px.Canvas:
    cv = px.Canvas(W, H)
    glass, display = _glass(cv)
    rng = px.rng(seed)
    ix, iy = 68, 62                                   # impact point
    crack = px.empty(W, H)
    shade = px.empty(W, H)
    rays = []
    n = 11
    for i in range(n):
        ang = (i + rng.uniform(-0.3, 0.3)) * 2 * math.pi / n
        length = rng.uniform(40, 120)
        pts = [(ix, iy)]
        x, y = float(ix), float(iy)
        a = ang
        step = 7.0
        travelled = 0.0
        while travelled < length:
            a += rng.uniform(-0.25, 0.25)
            x += math.cos(a) * step
            y += math.sin(a) * step
            travelled += step
            pts.append((x, y))
            if not (6 <= x <= 97 and 6 <= y <= 193):
                break
        rays.append(pts)
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            for px_, py_ in px.bresenham(int(round(ax)), int(round(ay)), int(round(bx)), int(round(by))):
                if 0 <= px_ < W and 0 <= py_ < H:
                    crack[py_, px_] = True
                    if 0 <= py_ + 1 < H:
                        shade[py_ + 1, px_] = True
    # spider-web rings joining neighbouring rays
    for radius in (7, 15, 26):
        for i in range(n):
            ra, rb = rays[i], rays[(i + 1) % n]
            k = int(radius / 7)
            if k < len(ra) and k < len(rb) and rng.random() < 0.85:
                (ax, ay), (bx, by) = ra[k], rb[k]
                mx = (ax + bx) / 2 + rng.uniform(-1.5, 1.5)
                my = (ay + by) / 2 + rng.uniform(-1.5, 1.5)
                for (sx, sy), (tx, ty) in (((ax, ay), (mx, my)), ((mx, my), (bx, by))):
                    for px_, py_ in px.bresenham(int(round(sx)), int(round(sy)), int(round(tx)), int(round(ty))):
                        if 0 <= px_ < W and 0 <= py_ < H:
                            crack[py_, px_] = True
    crack &= glass & ~px.inner(glass)
    shade &= glass & ~crack & ~px.inner(glass)
    # shattered centre: a few lighter shards
    for _ in range(9):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(2, 9)
        sx, sy = ix + math.cos(a) * r, iy + math.sin(a) * r
        shard = px.poly(W, H, [(sx, sy), (sx + rng.uniform(-3, 3), sy + rng.uniform(-3, 3)),
                                (sx + rng.uniform(-3, 3), sy + rng.uniform(-3, 3))])
        cv.fill(shard & glass, C("ink", 3))
    cv.fill(shade & display, C("ink", 0))
    cv.fill(crack, C("silver", 3))
    cv.fill(crack & ~display, C("silver", 1))
    cv.fill(disc(ix, iy, 1.6), C("rice", 5))
    cv.put(ix + 2, iy - 2, C("silver", 4))
    return cv


# --------------------------------------------------------------------------------------------------

LAYERS = {
    "phone_front_body": front_body,
    "phone_screen": screen,
    "phone_screen_cracked": screen_cracked,
    "phone_back_body": back_body,
    "phone_board": board,
    "phone_battery": battery,
    "phone_battery_swollen": battery_swollen,
    "phone_battery_conn": lambda: battery_conn(False),
    "phone_battery_conn_open": lambda: battery_conn(True),
    "phone_display_conn": lambda: display_conn(False),
    "phone_display_conn_open": lambda: display_conn(True),
    "phone_cover": cover,
}


def assemble(layers: list[str], screws: bool = False, screw_frame: int = 0) -> px.Canvas:
    cv = px.Canvas(W, H)
    for name in layers:
        cv.blit(LAYERS[name](), 0, 0)
    if screws:
        s = screw(screw_frame)
        for sx, sy in SCREWS:
            cv.blit(s, sx - 4, sy - 4)
    return cv

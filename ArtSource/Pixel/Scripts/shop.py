"""
shop: the shop view of "Have You Tried Rice?" (contract group Shop).

Image coordinates (y down). The shop background is 640x360; world y = 360 - row. The floor line
(world y 112) is image row 248: the counter sprite hides everything below it. The doorway
(world x 110-190, y 112-244) is image x 110..190, rows 116..248 and is covered by the door sprites.

Light: late afternoon sun through the big street window on the right; the room is warm plaster and
wood; shadows lean violet.
"""

from __future__ import annotations

import math

import numpy as np

import figure as fg
import px
import pxfont as F
from figure import Figure

SW, SH = 640, 360
FLOOR = 248


def lift(cv: px.Canvas, n: int = 1) -> px.Canvas:
    """Move the art up n rows. Outlined props keep a clear bottom row (the hover outline goes there),
    so they stand 1 px back on the counter top."""
    out = px.Canvas(cv.w, cv.h)
    out.a[:-n] = cv.a[n:]
    return out


def C(n, i, a=255):
    return px.C(n, i, a)


# --------------------------------------------------------------------------------------------------
# little helpers
# --------------------------------------------------------------------------------------------------

def box3d(cv: px.Canvas, x0: int, y0: int, x1: int, y1: int, ramp: str, top: int = 0, light: int = 4, base: int = 3,
          dark: int = 2, outline: int = 0) -> None:
    """A lit box seen from the front: base face, lighter top strip, darker left edge, 1 px outline."""
    cv.rect(x0, y0, x1, y1, C(ramp, base))
    if top:
        cv.rect(x0, y0, x1, y0 + top - 1, C(ramp, light))
    cv.rect(x0, y0, x0, y1, C(ramp, dark))
    cv.frame(x0 - 1, y0 - 1, x1 + 1, y1 + 1, C(ramp, outline))


def label(cv: px.Canvas, x: int, y: int, text: str, ink, paper=None, pad: int = 1) -> int:
    w = F.measure(text)
    if paper is not None:
        cv.rect(x - pad, y - pad, x + w - 1 + pad, y + 6 + pad, paper)
    F.text(cv, x, y, text, ink)
    return w


def plant(cv: px.Canvas, x: int, y: int, size: int = 1, pot_ramp: str = "coral", seed: int = 3) -> None:
    """Small potted plant; (x, y) = centre of the pot's bottom."""
    r = px.rng(seed)
    pot = Figure(cv.w, cv.h)
    leaves = px.empty(cv.w, cv.h)
    for i in range(5 + size * 3):
        ang = math.radians(-160 + r.random() * 140)
        L = (7 + size * 4) * (0.6 + r.random() * 0.5)
        cx_, cy_ = x + math.cos(ang) * L * 0.7, y - 9 - size * 2 + math.sin(ang) * L * 0.8
        leaves |= px.ellipse(cv.w, cv.h, cx_, cy_, 2.4 + size * 0.8, 1.6 + size * 0.6)
    pot.part(leaves, "leaf", tones=(1, 2, 3), cuts=(0.4, 0.85), flat=0.2)
    p = px.poly(cv.w, cv.h, [(x - 4 - size, y - 8 - size), (x + 4 + size, y - 8 - size), (x + 3 + size, y), (x - 3 - size, y)])
    pot.part(p, pot_ramp, tones=(1, 2, 3), cuts=(0.42, 0.85))
    pot.cv.rect(x - 4 - size, y - 8 - size, x + 4 + size, y - 7 - size, C(pot_ramp, 3))
    cv.blit(pot.finish(), 0, 0)


def figure_blit(cv: px.Canvas, fig: Figure) -> None:
    cv.blit(fig.finish(), 0, 0)


# --------------------------------------------------------------------------------------------------
# Background
# --------------------------------------------------------------------------------------------------

def street(w: int, h: int) -> px.Canvas:
    """The view through the window: golden-hour sky, the shops across the street, a vending machine,
    a power pole. Drawn on its own canvas, then masked into the panes."""
    cv = px.Canvas(w, h)
    # sky: golden-hour gradient (cool at the top, peach and butter toward the rooftops), dithered seams
    bands = [C("blue", 3), C("blue", 4), C("pink", 4), C("gold", 4), C("orange", 4)]
    edges = [0, int(h * 0.14), int(h * 0.27), int(h * 0.38), int(h * 0.47), h]
    for i in range(5):
        cv.rect(0, edges[i], w - 1, edges[i + 1] - 1, bands[i])
        if i < 4:
            band = px.box(w, h, 0, edges[i + 1] - 3, w - 1, edges[i + 1] - 1) & px.bayer(w, h, 0.5)
            cv.fill(band, bands[i + 1])
    # a few soft clouds catching the low sun
    for (cx_, cy_, rw) in ((40, int(h * 0.10), 16), (150, int(h * 0.22), 20), (200, int(h * 0.08), 12)):
        cl = px.ellipse(w, h, cx_, cy_, rw, 3) | px.ellipse(w, h, cx_ - rw * 0.4, cy_ - 2, rw * 0.5, 3)
        cv.fill(cl, C("rice", 5))
        cv.fill(cl & px.edge(cl, 0, 1, 1), C("pink", 4))
    # far rooftops (cool silhouettes)
    far = px.empty(w, h)
    for x0, top, ww in ((0, 0.44, 40), (34, 0.38, 30), (60, 0.48, 50), (104, 0.36, 26), (126, 0.46, 60), (180, 0.40, 50)):
        far |= px.box(w, h, x0, int(h * top), x0 + ww, h)
    cv.fill(far, C("purple", 4))
    cv.fill(far & px.edge(far, 0, -1, 1), C("pink", 4))
    # the street side: a ramen shop with a noren and a red lantern, a pharmacy, a vending machine
    base = int(h * 0.56)
    cv.rect(0, base, 120, h - 1, C("rice", 2))                    # ramen shop wall
    cv.rect(0, base, 120, base + 3, C("wood", 2))                 # eave
    cv.rect(0, base + 4, 120, base + 5, C("wood", 1))
    for i in range(5):                                            # noren
        x = 14 + i * 18
        cv.rect(x, base + 8, x + 15, base + 26, C("ink", 2))
        cv.rect(x, base + 8, x + 15, base + 9, C("ink", 3))
    F.text(cv, 30, base + 13, "RAMEN", C("rice", 4))
    lantern = px.ellipse(w, h, 106, base + 18, 5, 7)
    cv.fill(lantern, C("coral", 2))
    cv.fill(lantern & px.edge(lantern, -1, 0, 2), C("coral", 1))
    cv.rect(103, base + 10, 109, base + 10, C("ink", 1))
    cv.rect(103, base + 26, 109, base + 26, C("ink", 1))
    cv.rect(0, base + 30, 120, h - 1, C("rice", 3))                 # shop front glass, lit warm
    for x in range(8, 120, 22):
        cv.rect(x, base + 30, x + 1, h - 1, C("wood", 2))
    cv.rect(0, base + 30, 120, base + 31, C("wood", 1))
    # pharmacy-ish building to the right
    cv.rect(124, base - 22, w - 1, h - 1, C("teal", 4))
    cv.rect(124, base - 22, w - 1, base - 19, C("teal", 3))
    for x in range(132, w - 8, 18):
        cv.rect(x, base - 12, x + 9, base - 3, C("blue", 2))
        cv.rect(x + 1, base - 11, x + 3, base - 9, C("blue", 4))
    cv.rect(124, base + 6, w - 1, base + 13, C("leaf", 2))
    F.text(cv, 140, base + 6, "MOCHI MOBILE", C("rice", 5))
    cv.rect(124, base + 16, w - 1, h - 1, C("teal", 3))
    # vending machine
    vx = 166
    vm = px.box(w, h, vx, base + 14, vx + 22, h - 1)
    cv.fill(vm, C("rice", 4))
    cv.fill(px.box(w, h, vx + 2, base + 17, vx + 20, base + 33), C("blue", 4))
    for r_ in range(3):
        for c_ in range(4):
            col = (C("coral", 3), C("teal", 3), C("gold", 3), C("blue", 2))[(r_ + c_) % 4]
            cv.rect(vx + 3 + c_ * 4, base + 18 + r_ * 5, vx + 5 + c_ * 4, base + 21 + r_ * 5, col)
    cv.frame(vx, base + 14, vx + 22, h - 1, C("rice", 1))
    # sidewalk and kerb
    cv.rect(0, h - 10, w - 1, h - 1, C("gray", 3))
    cv.rect(0, h - 10, w - 1, h - 10, C("gray", 4))
    # power pole and wires
    cv.rect(198, 0, 201, h - 1, C("wood", 1))
    cv.rect(199, 0, 199, h - 1, C("wood", 2))
    cv.rect(186, int(h * 0.18), 214, int(h * 0.18) + 2, C("wood", 1))
    for k, (y0, sag) in enumerate(((int(h * 0.18) + 1, 7), (int(h * 0.18) + 4, 9))):
        pts = [(x, y0 + int(sag * math.sin(math.pi * x / 198))) for x in range(0, 199)]
        cv.pixels(pts, C("ink", 2))
    # a cat on the far wall (of course)
    cv.pixels([(150, base - 23), (154, base - 23)], C("ink", 1))
    cv.rect(149, base - 22, 155, base - 20, C("ink", 1))
    cv.rect(150, base - 23, 150, base - 23, C("ink", 1))
    cv.pixels([(156, base - 21), (157, base - 22), (158, base - 23)], C("ink", 1))
    return cv


def wall_clock(cv: px.Canvas, cx: int, cy: int, r: int, hour: float = 4.5, minute: float = 30) -> None:
    fig = Figure(cv.w, cv.h)
    rim = px.ellipse(cv.w, cv.h, cx + 0.5, cy + 0.5, r, r)
    fig.part(rim, "wood", tones=(2, 3, 4), cuts=(0.45, 0.85))
    face = px.ellipse(cv.w, cv.h, cx + 0.5, cy + 0.5, r - 3, r - 3)
    fig.part(face, "rice", tones=(3, 4, 5), cuts=(0.3, 0.85), flat=0.5)
    clock = fig.finish()
    cv.blit(clock, 0, 0)
    for k in range(12):
        a = math.radians(k * 30 - 90)
        tx, ty = cx + math.cos(a) * (r - 5), cy + math.sin(a) * (r - 5)
        cv.put(tx, ty, C("ink", 2) if k % 3 else C("ink", 1))
    ah = math.radians(hour * 30 - 90)
    am = math.radians(minute * 6 - 90)
    cv.line(cx, cy, int(round(cx + math.cos(ah) * (r - 9))), int(round(cy + math.sin(ah) * (r - 9))), C("ink", 1))
    cv.line(cx, cy, int(round(cx + math.cos(am) * (r - 6))), int(round(cy + math.sin(am) * (r - 6))), C("ink", 1))
    cv.put(cx, cy, C("coral", 2))


def chalkboard(cv: px.Canvas, x0: int, y0: int, x1: int, y1: int) -> None:
    box3d(cv, x0, y0, x1, y1, "wood", top=2, light=4, base=3, dark=2, outline=0)
    cv.rect(x0 + 3, y0 + 3, x1 - 3, y1 - 3, C("ink", 2))
    cv.fill(px.scatter(px.box(cv.w, cv.h, x0 + 3, y0 + 3, x1 - 3, y1 - 3), 0.04, 7), C("ink", 3))
    chalk, gold, coral = C("rice", 4), C("gold", 4), C("coral", 4)
    F.text(cv, x0 + 8, y0 + 8, "REPAIRS", gold)
    rows = [("Screen", "3000"), ("Battery", "2400"), ("Port", "1800")]
    for i, (a, b) in enumerate(rows):
        y = y0 + 22 + i * 11
        F.text(cv, x0 + 8, y, a, chalk)
        F.text(cv, x1 - 8 - F.measure(b), y, b, chalk)
    y = y0 + 22 + 3 * 11 + 3
    F.text(cv, x0 + 8, y, "Rice", coral)
    F.text(cv, x1 - 8 - F.measure("NO!"), y, "NO!", coral)
    cv.line(x0 + 7, y + 3, x0 + 8 + F.measure("Rice"), y + 3, coral)
    # chalk tray
    cv.rect(x0 - 1, y1 + 1, x1 + 1, y1 + 3, C("wood", 2))
    cv.rect(x0 + 10, y1, x0 + 15, y1, C("rice", 5))
    cv.rect(x0 + 20, y1, x0 + 23, y1, C("pink", 4))


def no_rice_poster(cv: px.Canvas, x0: int, y0: int) -> None:
    w, h = 44, 64
    x1, y1 = x0 + w - 1, y0 + h - 1
    cv.rect(x0 + 1, y0 + 1, x1 + 1, y1 + 1, C("rice", 1))          # shadow
    cv.rect(x0, y0, x1, y1, C("rice", 4))
    cv.frame(x0, y0, x1, y1, C("rice", 1))
    # bowl of rice with a phone in it, under a red no-sign
    cx, cy = x0 + w // 2, y0 + 25
    fig = Figure(cv.w, cv.h)
    bowl = px.ellipse(cv.w, cv.h, cx, cy + 3, 11, 9) & px.box(cv.w, cv.h, 0, cy + 2, cv.w, cv.h)
    fig.part(bowl, "blue", tones=(1, 2, 3), cuts=(0.4, 0.82))
    rice = px.ellipse(cv.w, cv.h, cx, cy + 2, 11, 4)
    fig.part(rice, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    phone = px.rrect(cv.w, cv.h, cx - 3, cy - 10, cx + 3, cy + 1, 1)
    fig.part(phone, "pink", tones=(1, 2, 3), cuts=(0.4, 0.85), style="flat")
    fig.cv.rect(cx - 2, cy - 9, cx + 2, cy - 2, C("ink", 2))
    cv.blit(fig.finish(), 0, 0)
    ring = px.ellipse(cv.w, cv.h, cx + 0.5, cy - 1.5, 16, 16) & ~px.ellipse(cv.w, cv.h, cx + 0.5, cy - 1.5, 13.5, 13.5)
    slash = px.capsule(cv.w, cv.h, cx - 10, cy - 12, cx + 11, cy + 9, 1.3) & px.ellipse(cv.w, cv.h, cx + 0.5, cy - 1.5, 14, 14)
    cv.fill(ring | slash, C("coral", 2))
    F.text_center(cv, cx, y0 + 46, "NO", C("coral", 1))
    F.text_center(cv, cx, y0 + 54, "RICE", C("coral", 1))


def mochi_poster(cv: px.Canvas, x0: int, y0: int) -> None:
    w, h = 34, 46
    cv.rect(x0 + 1, y0 + 1, x0 + w, y0 + h, C("pink", 1))
    cv.rect(x0, y0, x0 + w - 1, y0 + h - 1, C("pink", 3))
    cv.frame(x0, y0, x0 + w - 1, y0 + h - 1, C("pink", 1))
    fig = Figure(cv.w, cv.h)
    mochi = px.ellipse(cv.w, cv.h, x0 + w / 2, y0 + 18, 10, 8)
    fig.part(mochi, "rice", tones=(3, 4, 5), cuts=(0.35, 0.82))
    cv.blit(fig.finish(), 0, 0)
    cx = x0 + w // 2
    cv.pixels([(cx - 4, y0 + 17), (cx + 3, y0 + 17), (cx - 1, y0 + 20), (cx, y0 + 20)], C("ink", 1))
    cv.pixels([(cx - 7, y0 + 20), (cx + 6, y0 + 20)], C("pink", 3))
    F.text_center(cv, cx, y0 + 30, "MOCHI", C("rice", 5))
    F.text_center(cv, cx, y0 + 38, "mobile", C("pink", 1))


def maneki_neko(cv: px.Canvas, x: int, y: int) -> None:
    """Beckoning cat figurine; (x, y) = bottom centre."""
    fig = Figure(cv.w, cv.h)
    body = px.ellipse(cv.w, cv.h, x, y - 7, 6.5, 7)
    fig.part(body, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    head = px.ellipse(cv.w, cv.h, x, y - 17, 6, 5)
    fig.part(head, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    for sx in (-1, 1):
        fig.part(px.poly(cv.w, cv.h, [(x + sx * 6, y - 18), (x + sx * 6, y - 24), (x + sx * 2, y - 21)]), "rice", tones=(3, 4), cuts=(0.5,))
    paw = px.ellipse(cv.w, cv.h, x + 7, y - 21, 2.2, 3)
    fig.part(paw, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    fig.cv.rect(x - 5, y - 11, x + 5, y - 11, C("coral", 2))
    fig.cv.put(x, y - 10, C("gold", 3))
    fig.cv.pixels([(x - 3, y - 17), (x + 2, y - 17)], C("ink", 1))
    fig.cv.pixels([(x - 1, y - 15), (x, y - 15)], C("coral", 2))
    cv.blit(fig.finish(), 0, 0)


def phone_case(cv: px.Canvas, x: int, y: int, ramp: str, deco: str, seed: int) -> None:
    """A phone case standing on a shelf, 9x16, (x, y) = top-left."""
    cv.rect(x, y, x + 8, y + 15, C(ramp, 3))
    cv.rect(x, y, x + 8, y, C(ramp, 4))
    cv.rect(x, y, x, y + 15, C(ramp, 2))
    cv.frame(x - 1, y - 1, x + 9, y + 16, C(ramp, 0))
    cv.rect(x + 1, y + 1, x + 3, y + 4, C("ink", 2))                 # camera cut-out
    cv.put(x + 2, y + 2, C("ink", 3))
    if deco == "dot":
        cv.rect(x + 4, y + 8, x + 6, y + 10, C("rice", 5))
    elif deco == "stripe":
        cv.rect(x + 1, y + 9, x + 7, y + 10, C("rice", 4))
    elif deco == "heart":
        cv.pixels([(x + 3, y + 8), (x + 5, y + 8), (x + 3, y + 9), (x + 4, y + 9), (x + 5, y + 9), (x + 4, y + 10)], C("coral", 3))
    elif deco == "cat":
        cv.pixels([(x + 3, y + 8), (x + 6, y + 8), (x + 3, y + 9), (x + 4, y + 9), (x + 5, y + 9), (x + 6, y + 9), (x + 4, y + 10), (x + 5, y + 10)], C("ink", 1))


def part_box(cv: px.Canvas, x: int, y: int, w: int, h: int, ramp: str, text: str | None = None) -> None:
    box3d(cv, x, y, x + w - 1, y + h - 1, ramp, top=2, light=4, base=3, dark=2, outline=0)
    if text:
        cv.rect(x + 3, y + 4, x + w - 4, y + 12, C("rice", 4))
        F.text(cv, x + 5, y + 5, text, C("ink", 2))


def shelf_unit(cv: px.Canvas) -> None:
    x0, x1 = 4, 100
    top = 34
    wood = "wood"
    # back panel in shadow
    cv.rect(x0, top, x1, FLOOR, C("wood", 1))
    cv.fill(px.scatter(px.box(SW, SH, x0, top, x1, FLOOR), 0.03, 11), C("wood", 0))
    # frame
    for x in (x0, x1 - 3):
        cv.rect(x, top - 4, x + 3, FLOOR, C(wood, 3))
        cv.rect(x, top - 4, x, FLOOR, C(wood, 2))
        cv.rect(x + 3, top - 4, x + 3, FLOOR, C(wood, 4))
    cv.rect(x0 - 2, top - 8, x1 + 2, top - 4, C(wood, 3))
    cv.rect(x0 - 2, top - 8, x1 + 2, top - 8, C(wood, 4))
    cv.frame(x0 - 3, top - 9, x1 + 3, FLOOR, C(wood, 0))
    shelves = [72, 112, 152, 196]
    for y in shelves:
        cv.rect(x0 + 4, y, x1 - 4, y + 3, C(wood, 3))
        cv.rect(x0 + 4, y, x1 - 4, y, C(wood, 4))
        cv.rect(x0 + 4, y + 4, x1 - 4, y + 5, C(wood, 0))
    # top: boxes on the left (the DAY plate sits over them), the maneki-neko and a plant on the right
    # (the DAY plate covers x 10..65 up here, so the shelf top keeps that corner empty)
    maneki_neko(cv, 77, top - 9)
    plant(cv, 94, top - 9, 1, "teal", seed=5)
    # row 1: phone cases
    cases = [("pink", "heart"), ("teal", "dot"), ("gold", "stripe"), ("purple", "cat"), ("coral", "dot"), ("blue", "stripe"),
             ("leaf", "heart"), ("rice", "cat")]
    for i, (rmp, deco) in enumerate(cases):
        phone_case(cv, 10 + i * 11, shelves[0] - 17, rmp, deco, i)
    # row 2: labelled part boxes
    part_box(cv, 10, shelves[1] - 22, 40, 22, "wood", "SCREEN")
    part_box(cv, 53, shelves[1] - 22, 41, 22, "teal", "BATT")
    # row 3: jars of screws, a cable coil, a stack of old phones
    for i, x in enumerate((12, 26)):
        jar = px.rrect(SW, SH, x, shelves[2] - 18, x + 11, shelves[2] - 1, 3)
        cv.fill(jar, C("blue", 4))
        cv.fill(jar & px.edge(jar, -1, 0, 1), C("blue", 3))
        cv.fill(px.scatter(jar & px.box(SW, SH, 0, shelves[2] - 10, SW, SH), 0.45, 21 + i), C("gray", 2))
        cv.rect(x, shelves[2] - 20, x + 11, shelves[2] - 18, C("coral", 2))
        cv.frame(x - 1, shelves[2] - 21, x + 12, shelves[2], C("blue", 1))
    coil = px.ellipse(SW, SH, 52, shelves[2] - 8, 9, 7) & ~px.ellipse(SW, SH, 52, shelves[2] - 8, 5, 3.5)
    cv.fill(coil, C("ink", 2))
    for k in range(-6, 7, 3):
        cv.put(52 + k, shelves[2] - 13, C("ink", 3))
    for k in range(4):
        y = shelves[2] - 4 - k * 4
        cv.rect(68 + (k % 2), y, 92 + (k % 2), y + 3, (C("gray", 3), C("pink", 3), C("ink", 3), C("gold", 3))[k])
        cv.rect(68 + (k % 2), y + 3, 92 + (k % 2), y + 3, C("ink", 1))
    # row 4: a rice sack (for emergencies) and a toolbox
    sack = px.poly(SW, SH, [(12, shelves[3] - 30), (40, shelves[3] - 32), (44, shelves[3] - 1), (9, shelves[3] - 1)])
    f = Figure(SW, SH)
    f.part(sack, "rice", tones=(2, 3, 4), cuts=(0.35, 0.82), flat=0.3)
    sack_cv = f.finish()
    cv.blit(sack_cv, 0, 0)
    F.text(cv, 15, shelves[3] - 21, "RICE", C("coral", 2))
    cv.rect(20, shelves[3] - 11, 34, shelves[3] - 11, C("rice", 1))
    F.text(cv, 17, shelves[3] - 9, "5kg", C("rice", 1))
    box3d(cv, 52, shelves[3] - 20, 92, shelves[3] - 1, "coral", top=3, light=4, base=2, dark=1, outline=0)
    cv.rect(66, shelves[3] - 24, 78, shelves[3] - 22, C("ink", 2))
    cv.rect(66, shelves[3] - 24, 66, shelves[3] - 20, C("ink", 2))
    cv.rect(78, shelves[3] - 24, 78, shelves[3] - 20, C("ink", 2))
    cv.rect(70, shelves[3] - 12, 74, shelves[3] - 10, C("gray", 4))


def build_shop_bg() -> px.Canvas:
    cv = px.Canvas(SW, SH)
    # --- plaster wall with a soft vertical gradient and texture
    cv.rect(0, 0, SW - 1, SH - 1, C("rice", 3))
    cv.fill(px.box(SW, SH, 0, 0, SW - 1, 60) & px.bayer(SW, SH, 0.5), C("rice", 2))
    cv.fill(px.box(SW, SH, 0, 0, SW - 1, 30), C("rice", 2))
    cv.fill(px.scatter(px.box(SW, SH, 0, 0, SW - 1, FLOOR), 0.025, 3), C("rice", 2))
    # --- ceiling beam
    cv.rect(0, 0, SW - 1, 13, C("wood", 2))
    cv.rect(0, 11, SW - 1, 13, C("wood", 1))
    cv.rect(0, 0, SW - 1, 1, C("wood", 3))
    for x in range(30, SW, 120):
        cv.rect(x, 2, x + 1, 10, C("wood", 1))
    # --- wainscot (lower wall panels)
    wy = 206
    cv.rect(0, wy, SW - 1, FLOOR, C("wood", 3))
    cv.rect(0, wy, SW - 1, wy + 2, C("wood", 4))
    cv.rect(0, wy + 3, SW - 1, wy + 3, C("wood", 1))
    for x in range(0, SW, 40):
        cv.rect(x, wy + 4, x, FLOOR, C("wood", 2))
        cv.rect(x + 1, wy + 4, x + 1, FLOOR, C("wood", 4))
    cv.rect(0, FLOOR - 3, SW - 1, FLOOR, C("wood", 1))
    # --- the street window (right)
    wx0, wy0, wx1, wy1 = 410, 22, 632, 226
    view = street(wx1 - wx0 - 7, wy1 - wy0 - 7)
    cv.blit(view, wx0 + 4, wy0 + 4)
    # glass reflections
    for k, x in enumerate((440, 470, 540, 565)):
        streak = px.poly(SW, SH, [(x, wy0 + 4), (x + 10 + k % 2 * 6, wy0 + 4), (x - 30, wy1 - 4), (x - 40 - k % 2 * 6, wy1 - 4)])
        streak &= px.box(SW, SH, wx0 + 4, wy0 + 4, wx1 - 4, wy1 - 4)
        cv.a[streak] = (cv.a[streak].astype(np.int32) * 3 + np.array(C("rice", 5), np.int32)) // 4
    # frame and mullions
    frame = px.box(SW, SH, wx0, wy0, wx1, wy1) & ~px.box(SW, SH, wx0 + 4, wy0 + 4, wx1 - 4, wy1 - 4)
    midx = (wx0 + wx1) // 2
    frame |= px.box(SW, SH, midx - 2, wy0, midx + 1, wy1)
    frame |= px.box(SW, SH, wx0, 118, wx1, 120)
    cv.fill(frame, C("wood", 3))
    cv.fill(frame & px.edge(frame, 0, -1, 1), C("wood", 4))
    cv.fill(frame & px.edge(frame, -1, 0, 1), C("wood", 2))
    cv.fill(px.outer(px.box(SW, SH, wx0, wy0, wx1, wy1)), C("wood", 0))
    # sill with a plant and a little rice cooker (the shopkeeper's lunch)
    cv.rect(wx0 - 6, wy1 + 1, wx1 + 6, wy1 + 6, C("wood", 3))
    cv.rect(wx0 - 6, wy1 + 1, wx1 + 6, wy1 + 1, C("wood", 4))
    cv.rect(wx0 - 6, wy1 + 7, wx1 + 6, wy1 + 7, C("wood", 1))
    plant(cv, 426, wy1 + 1, 2, "coral", seed=9)
    rc = Figure(SW, SH)
    pot = px.rrect(SW, SH, 600, wy1 - 16, 624, wy1, 6)
    rc.part(pot, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8), flat=0.3)
    rc.part(px.ellipse(SW, SH, 612, wy1 - 15, 11, 3), "gray", tones=(2, 3, 4), cuts=(0.4, 0.8))
    rc.cv.rect(606, wy1 - 8, 611, wy1 - 6, C("teal", 4))
    rc.cv.put(615, wy1 - 7, C("coral", 3))
    cv.blit(rc.finish(), 0, 0)
    # --- warm sunlight on the wall, left of the window (dithered edges)
    beam = px.poly(SW, SH, [(300, 60), (404, 40), (404, 200), (330, 206)])
    soft = beam & ~px.box(SW, SH, 0, wy, SW, SH)
    cv.a[soft & px.bayer(SW, SH, 0.75)] = (cv.a[soft & px.bayer(SW, SH, 0.75)].astype(np.int32) * 2 + np.array(C("gold", 4), np.int32)) // 3
    # --- doorway: frame around the opening (the door sprites cover the opening)
    dx0, dx1, dy0 = 110, 190, 116
    cv.rect(dx0, dy0, dx1 - 1, FLOOR, C("ink", 1))                 # opening (hidden by the door)
    jamb = px.box(SW, SH, dx0 - 7, dy0 - 8, dx1 + 6, FLOOR) & ~px.box(SW, SH, dx0, dy0, dx1 - 1, FLOOR)
    cv.fill(jamb, C("wood", 3))
    cv.fill(jamb & px.edge(jamb, 0, -1, 1), C("wood", 4))
    cv.fill(jamb & px.edge(jamb, -1, 0, 1), C("wood", 2))
    cv.fill(px.outer(jamb), C("wood", 0))
    # a little noren curtain above the door and the door chime
    for i in range(3):
        x = dx0 + 4 + i * 25
        cv.rect(x, dy0 - 22, x + 22, dy0 - 10, C("ink", 2))
        cv.rect(x, dy0 - 22, x + 22, dy0 - 21, C("ink", 3))
        cv.frame(x - 1, dy0 - 23, x + 23, dy0 - 9, C("ink", 0))
    for i, word in enumerate(("WE", "FIX", "IT")):
        F.text_center(cv, dx0 + 15 + i * 25, dy0 - 19, word, C("rice", 4))
    chime = Figure(SW, SH)
    chime.part(px.ellipse(SW, SH, dx1 + 14, dy0 - 3, 4, 4) & px.box(SW, SH, 0, 0, SW, dy0 - 1), "gold", tones=(1, 2, 3), cuts=(0.4, 0.8))
    cv.blit(chime.finish(), 0, 0)
    cv.rect(dx1 + 14, dy0 - 14, dx1 + 14, dy0 - 8, C("ink", 2))
    cv.rect(dx1 + 10, dy0 - 14, dx1 + 18, dy0 - 14, C("wood", 2))
    # --- shelves on the left
    shelf_unit(cv)
    # --- chalkboard price list, the clock behind the customer, posters
    chalkboard(cv, 214, 70, 296, 136)
    wall_clock(cv, 330, 70, 18)
    no_rice_poster(cv, 356, 132)
    mochi_poster(cv, 216, 150)
    # light switch and a calendar
    cv.rect(200, 160, 205, 168, C("rice", 4))
    cv.frame(199, 159, 206, 169, C("rice", 1))
    cv.put(202, 163, C("rice", 1))
    # --- a hanging pendant lamp (warm), drawn last so it overlaps the wall
    lamp = Figure(SW, SH)
    cv.rect(270, 14, 270, 30, C("ink", 2))
    shade = px.poly(SW, SH, [(264, 31), (276, 31), (283, 42), (257, 42)])
    lamp.part(shade, "teal", tones=(1, 2, 3), cuts=(0.42, 0.85))
    lamp.part(px.ellipse(SW, SH, 270, 43, 4, 2.5), "gold", tones=(3, 4), cuts=(0.4,))
    cv.blit(lamp.finish(), 0, 0)
    glow = px.ellipse(SW, SH, 270, 50, 26, 12) & ~px.box(SW, SH, 0, 0, SW, 42) & px.bayer(SW, SH, 0.25)
    cv.a[glow] = (cv.a[glow].astype(np.int32) + np.array(C("gold", 4), np.int32)) // 2
    return cv


# --------------------------------------------------------------------------------------------------
# Door
# --------------------------------------------------------------------------------------------------

def door(open_: bool) -> px.Canvas:
    w, h = 80, 132
    cv = px.Canvas(w, h)
    # what you see through glass: the street
    view = street(76, 128)
    if open_:
        cv.blit(view, 2, 2)
    else:
        cv.blit(view, 2, 2)
        # glass tint
        glass = px.box(w, h, 2, 2, w - 3, h - 3)
        cv.a[glass] = (cv.a[glass].astype(np.int32) * 2 + np.array(C("blue", 4), np.int32)) // 3
    wood = "wood"

    def panel(x0, x1, with_handle):
        fr = px.box(w, h, x0, 0, x1, h - 1) & ~px.box(w, h, x0 + 4, 5, x1 - 4, h - 26)
        fr &= ~px.box(w, h, x0 + 4, h - 22, x1 - 4, h - 6)
        cv.fill(fr, C(wood, 3))
        cv.fill(fr & px.edge(fr, 0, -1, 1), C(wood, 4))
        cv.fill(fr & px.edge(fr, -1, 0, 1), C(wood, 2))
        cv.rect(x0 + 4, h - 22, x1 - 4, h - 6, C(wood, 2))           # kick panel
        cv.rect(x0 + 4, h - 22, x1 - 4, h - 22, C(wood, 1))
        cv.frame(x0, 0, x1, h - 1, C(wood, 0))
        # cross bars in the glass (kumiko-ish)
        mid = (x0 + x1) // 2
        cv.rect(mid, 5, mid, h - 27, C(wood, 1))
        cv.rect(x0 + 4, 46, x1 - 4, 46, C(wood, 1))
        if with_handle:
            hx = x0 + 5
            cv.rect(hx, 58, hx + 1, 76, C("gold", 3))
            cv.rect(hx, 58, hx, 76, C("gold", 2))

    if open_:
        # both panels slid away into the wall pockets: only their edges show at the jambs, so a
        # customer stands in the middle of the opening with the street behind them
        for x0, x1 in ((0, 5), (74, 79)):
            m = px.box(w, h, x0, 0, x1, h - 1)
            cv.fill(m, C(wood, 3))
            cv.fill(m & px.edge(m, 0, -1, 1), C(wood, 4))
            cv.fill(m & px.edge(m, -1, 0, 1), C(wood, 2))
            cv.frame(x0, 0, x1, h - 1, C(wood, 0))
        cv.vline(3, 58, 76, C("gold", 3))
        cv.vline(6, 2, h - 1, C(wood, 1))
        cv.rect(6, h - 4, 73, h - 1, C(wood, 2))                      # threshold rail
        cv.hline(6, 73, h - 4, C(wood, 4))
        cv.hline(6, 73, h - 1, C(wood, 0))
    else:
        panel(0, 39, False)
        panel(40, 79, True)
        # a paper notice on the glass
        cv.rect(50, 14, 69, 38, C("rice", 4))
        cv.frame(50, 14, 69, 38, C("rice", 1))
        cv.rect(57, 13, 62, 14, C("gold", 3))
        F.text(cv, 52, 17, "WE", C("ink", 2))
        F.text(cv, 52, 25, "FIX", C("coral", 2))
        cv.rect(53, 33, 66, 33, C("rice", 1))
    return cv


# --------------------------------------------------------------------------------------------------
# Counter (shopkeeper's side) and props
# --------------------------------------------------------------------------------------------------

def build_counter() -> px.Canvas:
    w, h = 640, 112
    cv = px.Canvas(w, h)
    # top surface (rows 0..12 = world y 100..112): light oak seen at a low angle
    cv.rect(0, 0, w - 1, 9, C("wood", 4))
    cv.rect(0, 0, w - 1, 0, C("wood", 5))
    for x in range(0, w, 7):
        if (x * 37) % 11 < 5:
            cv.rect(x, 3 + (x % 3), x + 4, 3 + (x % 3), C("wood", 3))
    cv.rect(0, 10, w - 1, 12, C("wood", 3))
    cv.rect(0, 13, w - 1, 13, C("wood", 1))
    # cabinet face in the counter's own shadow
    cv.rect(0, 14, w - 1, h - 1, C("wood", 2))
    cv.fill(px.scatter(px.box(w, h, 0, 14, w - 1, h - 1), 0.03, 5), C("wood", 1))
    # drawers with labels
    labels = ["SCREWS", "CABLES", "???", "SNACKS", "PARTS", "MIKAN", "MANUALS", "SPARE CAT"]
    dw = 80
    for i in range(8):
        x0 = i * dw + 4
        box3d(cv, x0, 18, x0 + dw - 9, 38, "wood", top=1, light=4, base=3, dark=2, outline=1)
        cv.rect(x0 + 30, 28, x0 + 40, 30, C("gold", 3))
        cv.rect(x0 + 30, 30, x0 + 40, 30, C("gold", 1))
        t = labels[i]
        lw = F.measure(t)
        cv.rect(x0 + (dw - 9) // 2 - lw // 2 - 2, 20, x0 + (dw - 9) // 2 + lw // 2 + 2, 26, C("rice", 4))
        F.text(cv, x0 + (dw - 9) // 2 - lw // 2, 20 if False else 20, t, C("ink", 2))
    # open cubbies with things
    cv.rect(0, 44, w - 1, h - 1, C("wood", 1))
    for x in range(0, w, 128):
        cv.rect(x, 44, x + 3, h - 1, C("wood", 3))
        cv.rect(x, 44, x, h - 1, C("wood", 2))
    cv.rect(0, 42, w - 1, 44, C("wood", 3))
    cv.rect(0, 45, w - 1, 45, C("wood", 0))
    floor = h - 6
    cv.rect(0, floor, w - 1, h - 1, C("wood", 0))
    # the emergency rice sack, with a phone sticking out of it (it didn't work)
    f = Figure(w, h)
    sack = px.poly(w, h, [(24, 58), (70, 54), (78, floor), (16, floor)])
    f.part(sack, "rice", tones=(1, 2, 3), cuts=(0.35, 0.82), flat=0.3)
    f.part(px.rrect(w, h, 41, 44, 51, 62, 2), "pink", tones=(1, 2, 3), cuts=(0.4, 0.85), style="flat")
    f.cv.rect(43, 47, 49, 58, C("ink", 2))
    f.cv.pixels([(44, 49), (46, 51), (47, 50), (48, 53)], C("rice", 3))
    cv.blit(f.finish(), 0, 0)
    F.text(cv, 28, 72, "RICE", C("coral", 1))
    F.text(cv, 26, 82, "for", C("rice", 0))
    F.text(cv, 26, 91, "emerg.", C("rice", 0))
    # rice cooker-sized mystery: a toolbox, cables, a mug, receipts, a radio, a first-aid kit
    box3d(cv, 140, 70, 200, floor - 1, "coral", top=3, light=4, base=2, dark=1, outline=0)
    cv.rect(160, 63, 180, 65, C("ink", 2))
    cv.rect(160, 63, 160, 69, C("ink", 2))
    cv.rect(180, 63, 180, 69, C("ink", 2))
    F.text(cv, 151, 80, "TOOLS", C("rice", 4))
    coil = px.ellipse(w, h, 240, 88, 14, 12) & ~px.ellipse(w, h, 240, 88, 8, 6)
    cv.fill(coil, C("ink", 2))
    for k in range(-10, 11, 4):
        cv.put(240 + k, 78 + abs(k) // 3, C("ink", 3))
    cv.line(252, 96, 268, floor - 1, C("ink", 2))
    mug = px.rrect(w, h, 282, 80, 294, floor - 1, 2)
    cv.fill(mug, C("teal", 3))
    cv.fill(mug & px.edge(mug, -1, 0, 1), C("teal", 2))
    cv.fill(px.ellipse(w, h, 296, 90, 3, 4) & ~px.ellipse(w, h, 296, 90, 1.5, 2.5), C("teal", 2))
    cv.rect(283, 80, 293, 81, C("wood", 1))
    for i in range(4):
        cv.rect(310, floor - 6 - i * 4, 340, floor - 3 - i * 4, C("rice", 4 - i % 2))
        cv.rect(310, floor - 3 - i * 4, 340, floor - 3 - i * 4, C("rice", 1))
    F.text(cv, 384, 66, "PHONE ER", C("coral", 3))
    box3d(cv, 380, 76, 440, floor - 1, "rice", top=3, light=5, base=4, dark=3, outline=1)
    cv.rect(406, 82, 414, 98, C("coral", 2))
    cv.rect(400, 86, 420, 94, C("coral", 2))
    radio = px.rrect(w, h, 470, 70, 530, floor - 1, 5)
    rf = Figure(w, h)
    rf.part(radio, "teal", tones=(1, 2, 3), cuts=(0.4, 0.85), flat=0.4)
    rf.cv.fill(px.ellipse(w, h, 488, 88, 9, 9), C("teal", 1))
    for yy in range(81, 96, 3):
        rf.cv.rect(481, yy, 495, yy, C("teal", 0))
    rf.cv.rect(505, 80, 522, 86, C("gold", 4))
    rf.cv.rect(512, 80, 512, 86, C("coral", 2))
    cv.blit(rf.finish(), 0, 0)
    cv.line(520, 70, 534, 50, C("gray", 2))
    for i in range(3):
        box3d(cv, 566 + i * 3, floor - 14 - i * 12, 620 + i * 3, floor - 3 - i * 12, ("gold", "rice", "teal")[i], top=2, light=4, base=3, dark=2, outline=1)
    F.text(cv, 576, floor - 34, "MOCHI", C("ink", 2))
    return cv


def register() -> px.Canvas:
    w, h = 72, 56
    fig = Figure(w, h)
    # body: cream enamel with teal trim; display window at rows 6..20 (image y 36-50 from the bottom)
    body = px.poly(w, h, [(8, 24), (64, 24), (70, 46), (70, 54), (2, 54), (2, 46)])
    fig.part(body, "rice", tones=(2, 3, 4), cuts=(0.35, 0.85), flat=0.5)
    head = px.rrect(w, h, 6, 4, 65, 23, 4)
    fig.part(head, "teal", tones=(1, 2, 3), cuts=(0.4, 0.85), flat=0.5)
    drawer = px.box(w, h, 4, 46, 67, 54)
    fig.part(drawer, "teal", tones=(1, 2, 3), cuts=(0.4, 0.85), style="flat")
    cv = fig.finish()
    # LCD window for the money total (kept plain: the game writes the number)
    cv.rect(10, 7, 61, 20, C("teal", 0))
    cv.rect(11, 8, 60, 19, C("ink", 1))
    cv.rect(11, 8, 60, 8, C("ink", 2))
    cv.rect(58, 9, 59, 10, C("teal", 2))
    # keys
    for r in range(3):
        for c in range(6):
            x = 12 + c * 8 + r
            y = 28 + r * 6
            cv.rect(x, y, x + 5, y + 3, C("rice", 5) if (r + c) % 5 else C("coral", 3))
            cv.rect(x, y + 3, x + 5, y + 3, C("rice", 1))
    # drawer slot and a yen emblem
    cv.rect(26, 49, 46, 50, C("teal", 0))
    F.text(cv, 52, 47, "¥", C("gold", 3))
    return lift(cv)


def bell(pressed: bool) -> px.Canvas:
    w, h = 24, 20
    fig = Figure(w, h)
    base = px.ellipse(w, h, 12, 16.5, 9, 2.6)
    fig.part(base, "wood", tones=(0, 1, 2), cuts=(0.4, 0.8))
    dome = px.ellipse(w, h, 12, 15, 7.5, 7.5 if not pressed else 6.5) & px.box(w, h, 0, 0, w, 15)
    fig.part(dome, "gold", tones=(2, 3, 4), cuts=(0.4, 0.78))
    knob_y = 6 if not pressed else 8
    fig.part(px.box(w, h, 11, knob_y, 12, 8 if not pressed else 9), "gray", tones=(2, 3), cuts=(0.5,), style="flat")
    fig.part(px.ellipse(w, h, 12, knob_y - 0.5, 2, 1.4), "gray", tones=(2, 3, 4), cuts=(0.4, 0.8))
    cv = fig.finish()
    cv.put(15, 10 if not pressed else 11, C("gold", 4))
    cv.put(16, 11 if not pressed else 12, C("rice", 5))
    if pressed:
        for (x, y) in ((2, 6), (1, 9), (21, 6), (22, 9)):
            cv.put(x, y, C("gold", 4))
    return lift(cv)


def cat(blink: bool) -> px.Canvas:
    """Denchi, the calico shop cat, loafing on the counter. 48x36, bottom on the counter top."""
    w, h = 48, 36
    fig = Figure(w, h)
    body = px.ellipse(w, h, 25, 25.5, 17, 9.5) & px.box(w, h, 0, 0, w, 34)
    fig.part(body, "rice", tones=(3, 4, 5), cuts=(0.35, 0.82), flat=0.3)
    # calico patches on the back
    patch1 = body & px.ellipse(w, h, 33, 19, 8, 6)
    patch2 = body & px.ellipse(w, h, 16, 21, 5, 4)
    fig.solid(patch1, C("orange", 3))
    fig.solid(patch1 & px.edge(patch1, 0, 1, 1), C("orange", 2))
    fig.solid(patch2, C("ink", 2))
    # tail curled round the front
    tail = px.stroke(w, h, [(41, 30), (44, 26), (42, 22)], 2.0) | px.capsule(w, h, 18, 32.5, 40, 32.5, 1.8)
    fig.part(tail, "orange", tones=(2, 3, 4), cuts=(0.4, 0.85))
    # head
    head = px.ellipse(w, h, 13, 19, 9.5, 8)
    for sx, ramp in ((-1, "ink"), (1, "orange")):
        ear = px.poly(w, h, [(13 + sx * 9, 17), (13 + sx * 8.5, 8), (13 + sx * 3, 12)])
        fig.part(ear, ramp, tones=(1, 2, 3) if ramp == "ink" else (2, 3, 4), cuts=(0.42, 0.85))
        fig.solid(px.poly(w, h, [(13 + sx * 7.5, 14.5), (13 + sx * 7.5, 11), (13 + sx * 5, 13)]), C("pink", 3))
    fig.part(head, "rice", tones=(3, 4, 5), cuts=(0.35, 0.82), flat=0.35)
    fig.solid(head & px.ellipse(w, h, 17, 15, 5, 4), C("orange", 3))
    # face
    if blink:
        fig.pixels([(8, 19), (9, 20), (10, 19), (15, 19), (16, 20), (17, 19)], C("ink", 1))
    else:
        for x in (8, 15):
            fig.pixels([(x, 18), (x + 1, 18), (x + 2, 18), (x, 19), (x + 1, 19), (x + 2, 19)], C("ink", 1))
            fig.put(x + 1, 18, C("leaf", 4))
    fig.pixels([(12, 21), (13, 21)], C("pink", 2))
    fig.pixels([(11, 22), (12, 23), (13, 23), (14, 22)], C("ink", 2))
    for sy in (21, 23):
        fig.pixels([(2, sy), (3, sy), (4, sy)], C("rice", 1))
        fig.pixels([(23, sy), (24, sy), (25, sy)], C("rice", 1))
    # front paws
    for x in (9, 17):
        fig.part(px.ellipse(w, h, x, 32, 3.2, 2.2), "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    return lift(fig.finish())


def sign(text: str, closed: bool) -> px.Canvas:
    w, h = 64, 32
    cv = px.Canvas(w, h)
    # strings to a nail
    cv.line(10, 11, 31, 1, C("ink", 2))
    cv.line(53, 11, 32, 1, C("ink", 2))
    cv.rect(31, 0, 32, 1, C("gray", 3))
    fig = Figure(w, h)
    board = px.rrect(w, h, 3, 10, 60, 30, 4)
    fig.part(board, "wood", tones=(3, 4, 5), cuts=(0.35, 0.85), flat=0.5)
    cv.blit(fig.finish(), 0, 0)
    cv.rect(6, 13, 57, 27, C("rice", 4))
    cv.frame(6, 13, 57, 27, C("wood", 2))
    ink = C("coral", 2) if closed else C("teal", 2)
    mask = F.stroke_text(w, h, 0, 0, text, 9, 1.2, 1.0)
    tw = F.stroke_width(text, 9, 1.2, 1.0)
    m = F.stroke_text(w, h, 32 - tw / 2, 15.5, text, 9, 1.2, 1.0)
    cv.fill(m, ink)
    return cv


def phone_on_counter() -> px.Canvas:
    w, h = 24, 14
    cv = px.Canvas(w, h)
    # contact shadow, side edge (pink), top face (dark screen with a crack) seen at a low angle
    cv.rect(2, 12, 21, 12, C("wood", 2, 160))
    cv.rect(3, 9, 20, 11, C("pink", 2))
    cv.rect(3, 9, 20, 9, C("pink", 3))
    top = px.poly(w, h, [(5, 4), (19, 4), (21, 8), (2, 8)])
    cv.fill(top, C("pink", 3))
    cv.fill(px.poly(w, h, [(6, 5), (18, 5), (19, 7), (4, 7)]), C("ink", 2))
    cv.pixels([(9, 6), (10, 5), (11, 6), (12, 6)], C("rice", 4))
    cv.fill(px.outer(px.box(w, h, 2, 4, 21, 11) & (top | px.box(w, h, 3, 9, 20, 11))), C("pink", 0))
    return cv

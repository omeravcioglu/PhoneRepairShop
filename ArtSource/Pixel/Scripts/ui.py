"""
ui: the "Paper & Keys" kit in pixels, the logo, tool icons and cursors.

9-slice borders follow the contract (left, bottom, right, top). Middles are flat so stretching them in
the game never smears a pattern. Ink is sumi navy; paper is rice white; buttons are keycaps with a
hard navy drop.
"""

from __future__ import annotations

import math

import numpy as np

import px
import pxfont as F
from figure import Figure

INK = px.C("ink", 1)
INK0 = px.C("ink", 0)
PAPER = px.C("rice", 4)
PAPER_HI = px.C("rice", 5)


def C(n, i, a=255):
    return px.C(n, i, a)


def rounded_panel(w: int, h: int, r: float, fill, outline, drop=None, drop_h: int = 0, inner_hi=None) -> px.Canvas:
    """Rounded panel with a 1 px outline and an optional hard drop below it (drop_h rows)."""
    cv = px.Canvas(w, h)
    body_h = h - drop_h
    body = px.rrect(w, h, 0, 0, w - 1, body_h - 1, r)
    if drop is not None and drop_h:
        under = px.rrect(w, h, 0, drop_h, w - 1, h - 1, r)
        cv.fill(under, drop)
    cv.fill(body, outline)
    inside = px.rrect(w, h, 1, 1, w - 2, body_h - 2, max(r - 1, 0.5))
    cv.fill(inside, fill)
    if inner_hi is not None:
        cv.fill(inside & px.edge(inside, 0, -1, 1), inner_hi)
    return cv


# --------------------------------------------------------------------------------------------------
# Contract UI sprites
# --------------------------------------------------------------------------------------------------

def bubble() -> px.Canvas:
    # 24x24, border 8: warm paper, navy outline, a lit top lip. No drop below: the tail overlaps the
    # bottom outline row (SpeechBubble puts it at y = -height + 1) and opens it.
    cv = rounded_panel(24, 24, 5, PAPER, INK, inner_hi=PAPER_HI)
    return cv


def bubble_tail() -> px.Canvas:
    """12x10, top-left pivot. Row 0 sits on the bubble's bottom outline and opens it; the tip points
    down-left at the speaker."""
    rows = [
        "...#PPPPPPP#",
        "...#PPPPPP#.",
        "..#PPPPP##..",
        "..#PPPP#....",
        "..#PPP#.....",
        ".#PPP#......",
        ".#PP#.......",
        ".#P#........",
        "#P#.........",
        "##..........",
    ]
    cv = px.Canvas(12, 10)
    for r, row in enumerate(rows):
        for c, v in enumerate(row):
            if v == "#":
                cv.put(c, r, INK)
            elif v == "P":
                cv.put(c, r, PAPER)
    return cv


def more_arrow() -> px.Canvas:
    cv = px.Canvas(7, 5)
    rows = ["#######", ".#####.", "..###..", "...#...", "......."]
    for r, row in enumerate(rows):
        for c, v in enumerate(row):
            if v == "#":
                cv.put(c, r, INK)
    cv.put(3, 4, C("ink", 3))
    return cv


def paper() -> px.Canvas:
    # title card / day summary: paper with a navy outline and a hard navy drop
    return rounded_panel(24, 24, 4, PAPER, INK, drop=INK, drop_h=3, inner_hi=PAPER_HI)


def ticket() -> px.Canvas:
    # kraft job ticket, brown ink outline, fold shadow
    cv = rounded_panel(24, 24, 2, C("rice", 3), C("wood", 2), drop=C("wood", 1), drop_h=2, inner_hi=C("rice", 4))
    return cv


def button() -> px.Canvas:
    # keycap: paper face, navy outline, 3 px navy drop
    cv = rounded_panel(24, 20, 4, PAPER_HI, INK, drop=INK, drop_h=3)
    cv.rect(3, 1, 20, 1, C("rice", 5))
    return cv


def pill() -> px.Canvas:
    cv = rounded_panel(16, 16, 4, C("ink", 2), INK0)
    cv.rect(3, 1, 12, 1, C("ink", 3))
    return cv


def tool_slot(selected: bool) -> px.Canvas:
    w = h = 36
    cv = px.Canvas(w, h)
    outer = px.rrect(w, h, 1, 1, w - 2, h - 2, 5)
    inner = px.rrect(w, h, 3, 3, w - 4, h - 4, 4)
    if selected:
        cv.fill(px.rrect(w, h, 0, 0, w - 1, h - 1, 6), C("teal", 1))
        cv.fill(outer, C("teal", 4))
        cv.fill(inner, C("teal", 5))
        cv.fill(inner & px.edge(inner, 0, -1, 2), C("rice", 5))
        cv.fill(inner & px.edge(inner, 0, 1, 1), C("teal", 4))
    else:
        cv.fill(outer, C("wood", 1))
        cv.fill(inner, C("wood", 2))
        cv.fill(inner & px.edge(inner, 0, -1, 2), C("wood", 0))       # recessed: shadow on top
        cv.fill(inner & px.edge(inner, 0, 1, 1), C("wood", 3))        # lit lower lip
        cv.fill(px.outer(outer), C("wood", 0))
    return cv


def ring(k: int) -> px.Canvas:
    """Progress ring frame k of 8 (0 = empty, 8 = full), clockwise from the top."""
    w = h = 16
    cv = px.Canvas(w, h)
    xx, yy = px.grid(w, h)
    d = np.sqrt((xx - 8) ** 2 + (yy - 8) ** 2)
    band = (d <= 6.8) & (d >= 3.6)
    cv.fill(band, C("ink", 1))
    ang = (np.degrees(np.arctan2(xx - 8, -(yy - 8))) + 360) % 360       # 0 at the top, clockwise
    fill = band & (ang < k * 45) & (d <= 6.0) & (d >= 4.4)
    cv.fill(fill, C("orange", 3))
    cv.fill(fill & (d >= 5.4), C("orange", 4))
    if k == 8:
        cv.fill(band & (d <= 6.0) & (d >= 4.4), C("orange", 3))
        cv.fill(band & (d <= 6.0) & (d >= 5.4), C("orange", 4))
    return cv


def portrait_frame() -> px.Canvas:
    """52x52 frame for the face peeking in on the bench: a little window with a paper border and a
    soft sky behind the face (the frame is drawn behind the 48x48 face)."""
    w = h = 52
    cv = rounded_panel(w, h, 6, PAPER, INK, inner_hi=PAPER_HI)
    hole = px.rrect(w, h, 3, 3, w - 4, h - 4, 4)
    cv.fill(hole, C("blue", 4))
    cv.fill(hole & px.box(w, h, 0, 30, w, h), C("rice", 4))
    cv.fill(hole & px.box(w, h, 0, 28, w, 29) & px.bayer(w, h, 0.5), C("rice", 4))
    cv.fill(px.outer(hole) & px.rrect(w, h, 1, 1, w - 2, h - 2, 5), C("rice", 2))
    return cv


# --------------------------------------------------------------------------------------------------
# Tools and cursors
# --------------------------------------------------------------------------------------------------

def _screwdriver(cv_size: int, tip: tuple[float, float], angle_deg: float, length: float, handle_ramp: str = "teal") -> px.Canvas:
    w = h = cv_size
    fig = Figure(w, h)
    a = math.radians(angle_deg)
    ux, uy = math.cos(a), -math.sin(a)
    tx, ty = tip
    shaft_end = (tx + ux * length * 0.42, ty + uy * length * 0.42)
    handle_end = (tx + ux * length, ty + uy * length)
    fig.part(px.capsule(w, h, tx, ty, *shaft_end, 0.9), "gray", tones=(2, 3), cuts=(0.55,))
    fig.part(px.capsule(w, h, *shaft_end, shaft_end[0] + ux * 2.2, shaft_end[1] + uy * 2.2, 1.9), "gold", tones=(2, 3, 4), cuts=(0.4, 0.8))
    fig.part(px.capsule(w, h, shaft_end[0] + ux * 3, shaft_end[1] + uy * 3, *handle_end, 2.6), handle_ramp, tones=(2, 3, 4), cuts=(0.4, 0.82))
    out = fig.finish()
    # grip rings
    for k in (0.6, 0.75, 0.9):
        gx, gy = tx + ux * length * k, ty + uy * length * k
        out.put(gx - uy * 1.5, gy + ux * 1.5, px.C(handle_ramp, 1))
    return out


def tool_icon(kind: str) -> px.Canvas:
    w = h = 28
    if kind == "screwdriver":
        return _screwdriver(28, (5, 22), 45, 24)
    cv = px.Canvas(w, h)
    fig = Figure(w, h)
    if kind == "hands":
        palm = px.ellipse(w, h, 14, 17, 7, 7.5)
        fingers = px.empty(w, h)
        for i, (fx, top) in enumerate(((9, 6), (12.5, 4), (16, 4.5), (19.2, 7))):
            fingers |= px.capsule(w, h, fx, top + 2, fx, 14, 1.9)
        thumb = px.capsule(w, h, 7, 15, 3.5, 11, 2.0)
        fig.part(palm | fingers | thumb, "skin", tones=(2, 3, 4), cuts=(0.4, 0.86), flat=0.3)
        out = fig.finish()
        for fx in (11, 14, 18):
            out.put(fx, 12, px.C("skin", 2))
        return out
    if kind == "spudger":
        a = math.radians(45)
        body = px.capsule(w, h, 5, 23, 23, 5, 1.8)
        tipm = px.poly(w, h, [(2, 26), (6, 20), (8, 22)])
        fig.part(body | tipm, "purple", tones=(2, 3, 4), cuts=(0.4, 0.84))
        out = fig.finish()
        out.line(7, 20, 20, 7, px.C("purple", 4))
        return out
    if kind == "suction":
        cup = px.ellipse(w, h, 14, 21, 10, 5) & px.box(w, h, 0, 0, w, 23)
        stem = px.box(w, h, 12, 9, 16, 18)
        ring_ = px.ellipse(w, h, 14, 6, 4.5, 4.5) & ~px.ellipse(w, h, 14, 6, 2.2, 2.2)
        fig.part(ring_, "gray", tones=(2, 3, 4), cuts=(0.4, 0.8))
        fig.part(stem, "blue", tones=(1, 2, 3), cuts=(0.4, 0.85), style="flat")
        fig.part(cup, "blue", tones=(2, 3, 4), cuts=(0.42, 0.85))
        out = fig.finish()
        out.rect(6, 23, 22, 23, px.C("blue", 1))
        return out
    raise ValueError(kind)


def cursor(kind: str) -> px.Canvas:
    if kind in ("hand", "hand_pressed"):
        w = h = 16
        cv = px.Canvas(w, h)
        pressed = kind == "hand_pressed"
        # pointing hand, fingertip at pixel (4, 15) from the bottom-left = (4, 0) from the top
        rows = [
            "....##..........",
            "...#..#.........",
            "...#..#.........",
            "...#..#.........",
            "...#..###.......",
            "...#..#..##.....",
            ".###..#..#.##...",
            "#..#.....#..#...",
            "#........#..#...",
            "#...........#...",
            ".#..........#...",
            ".#.........#....",
            "..#........#....",
            "...#......#.....",
            "...########.....",
            "................",
        ]
        if pressed:
            rows = ["................"] + rows[:-1]
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    cv.put(c, r, INK0)
        inside = px.empty(w, h)
        filled = cv.alpha_mask()
        # flood the inside of the outline from a seed point
        from scipy import ndimage
        lab, n = ndimage.label(~filled)
        seed = lab[9 + (1 if pressed else 0), 6]
        inside = lab == seed
        cv.fill(inside, px.C("rice", 5))
        cv.fill(inside & px.edge(inside, -1, 0, 1), px.C("rice", 3))
        return cv
    w = h = 24
    if kind == "screwdriver":
        return _screwdriver(24, (1.5, 22.5), 45, 21)
    if kind == "spudger":
        fig = Figure(w, h)
        body = px.capsule(w, h, 3, 20, 20, 3, 1.7)
        tipm = px.poly(w, h, [(0.5, 23.5), (3.5, 18.5), (5.5, 20.5)])
        fig.part(body | tipm, "purple", tones=(2, 3, 4), cuts=(0.4, 0.84))
        out = fig.finish()
        out.line(5, 18, 18, 5, px.C("purple", 4))
        return out
    if kind == "suction":
        fig = Figure(w, h)
        cup = px.ellipse(w, h, 8, 19, 7, 4) & px.box(w, h, 0, 0, w, 21)
        stem = px.capsule(w, h, 9, 16, 17, 8, 1.6)
        ring_ = px.ellipse(w, h, 18.5, 6.5, 3.6, 3.6) & ~px.ellipse(w, h, 18.5, 6.5, 1.6, 1.6)
        fig.part(ring_, "gray", tones=(2, 3, 4), cuts=(0.4, 0.8))
        fig.part(stem, "blue", tones=(1, 2, 3), cuts=(0.4, 0.85))
        fig.part(cup, "blue", tones=(2, 3, 4), cuts=(0.42, 0.85))
        return fig.finish()
    raise ValueError(kind)


# --------------------------------------------------------------------------------------------------
# Logo: HAVE YOU TRIED RICE?
# --------------------------------------------------------------------------------------------------

def rice_bowl_phone(w: int, h: int, cx: float, cy: float, scale: float = 1.0) -> px.Canvas:
    """Emblem: a ceramic rice bowl with a sad phone sticking out of the rice, and steam."""
    s = scale
    fig = Figure(w, h)
    phone = px.rrect(w, h, int(cx - 5 * s), int(cy - 20 * s), int(cx + 5 * s), int(cy - 2 * s), 2 * s)
    fig.part(phone, "pink", tones=(1, 2, 3), cuts=(0.42, 0.85), style="dome", flat=0.5)
    scr = px.rrect(w, h, int(cx - 3 * s), int(cy - 18 * s), int(cx + 3 * s), int(cy - 6 * s), 1)
    fig.solid(scr, px.C("ink", 2))
    # sad face on the screen
    fig.pixels([(cx - 2 * s, cy - 13 * s), (cx + 1 * s, cy - 13 * s)], px.C("blue", 4))
    fig.pixels([(cx - 1 * s, cy - 9 * s), (cx, cy - 10 * s), (cx + 1 * s, cy - 9 * s)], px.C("blue", 4))
    rice = px.ellipse(w, h, cx, cy - 1 * s, 15 * s, 4.5 * s)
    fig.part(rice, "rice", tones=(3, 4, 5), cuts=(0.3, 0.8))
    bowl = px.ellipse(w, h, cx, cy - 1 * s, 15.5 * s, 13 * s) & px.box(w, h, 0, int(cy), w, h)
    bowl |= px.box(w, h, int(cx - 6 * s), int(cy + 11 * s), int(cx + 6 * s), int(cy + 13 * s))
    fig.part(bowl, "blue", tones=(2, 3, 4), cuts=(0.42, 0.82), flat=0.3)
    out = fig.finish()
    # blue-and-white band on the bowl and grains on the rice
    for x in range(int(cx - 13 * s), int(cx + 14 * s), 4):
        out.put(x, cy + 4 * s, px.C("rice", 5))
        out.put(x + 1, cy + 5 * s, px.C("rice", 5))
    rng = px.rng(4)
    for _ in range(int(14 * s)):
        gx = cx + rng.uniform(-12, 12) * s
        gy = cy - 1 * s + rng.uniform(-2.5, 2.5) * s
        if abs(gx - cx) > 6 * s or gy > cy:
            out.put(gx, gy, px.C("rice", 2))
    # steam wisps
    for sx_, ph in ((-9, 0), (9, 2)):
        for i in range(6):
            out.put(cx + sx_ * s + ((i + ph) % 3 - 1), cy - 12 * s - i * 2, px.C("rice", 2))
    return out


def logo(w: int = 256, h: int = 104, dark: bool = False, tagline: bool = True) -> px.Canvas:
    """The title lockup. 256x104 with the tagline, 256x80 without."""
    cv = px.Canvas(w, h)
    face = px.C("rice", 5)
    ink = px.C("ink", 1)
    extrude = px.C("ink", 0)
    # line 1
    l1 = "HAVE YOU"
    cap1, r1 = 15, 2.0
    w1 = F.stroke_width(l1, cap1, r1, 1.5)
    # line 2
    l2 = "TRIED RICE?"
    cap2, r2 = 25, 3.1
    w2 = F.stroke_width(l2, cap2, r2, 2.0)
    x2 = (w - w2) / 2
    y1, y2 = 26, 47
    bowl_r, gap = 13.2, 5
    left = (w - (2 * bowl_r + gap + w1)) / 2
    bowl_cx = left + bowl_r
    x1 = bowl_cx + bowl_r + gap
    m1 = F.stroke_text(w, h, x1, y1, l1, cap1, r1, 1.5)
    m2, letters2 = F.stroke_text(w, h, x2, y2, l2, cap2, r2, 2.0, per_letter=True)
    q = letters2[-1]
    rice_word = letters2[6] | letters2[7] | letters2[8] | letters2[9]
    text = m1 | m2
    # extrusion: 3 px down, then outline everything
    ext = px.empty(w, h)
    for k in range(1, 4):
        ext |= px.shift(text, 0, k)
    cv.fill(px.outer(text | ext, True), ink)
    cv.fill(ext & ~text, extrude)
    cv.fill(text, face)
    # light top edge and a warm bottom band on the letters
    cv.fill(text & px.edge(text, 0, -1, 1), px.C("rice", 5))
    cv.fill(text & px.edge(text, 0, 1, 2), px.C("rice", 3))
    # RICE gets a grain texture, the ? is orange
    grains = px.scatter(rice_word & ~px.edge(rice_word, 0, 1, 2) & ~px.edge(rice_word, 0, -1, 1), 0.12, 8)
    cv.fill(grains, px.C("rice", 3))
    cv.fill(q, px.C("orange", 3))
    cv.fill(q & px.edge(q, 0, 1, 2), px.C("orange", 2))
    cv.fill(q & px.edge(q, 0, -1, 1), px.C("orange", 4))
    # emblem: the bowl with the phone, tucked before HAVE YOU
    emblem = rice_bowl_phone(w, h, bowl_cx, 33, 0.85)
    cv.blit(emblem, 0, 0)
    # tagline
    if tagline:
        tag = "(we have. it doesn't work.)"
        tw = F.measure(tag)
        F.text(cv, (w - tw) // 2, y2 + cap2 + 9, tag, px.C("rice", 4) if dark else px.C("ink", 2))
    return cv

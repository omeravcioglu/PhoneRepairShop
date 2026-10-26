"""
cast: the customers of "Have You Tried Rice?" (96x160 body frames, 48x48 bench faces).

Contract: Customers/<id>/cust_<id>_{idle_0,idle_1,talk_0,talk_1,happy,sad}.png and
face_<id>_{neutral,happy,worried}.png. Pivot bottom-centre; at the counter only image rows 0..91 show
(the counter top hides the rest). In the doorway the whole body shows at half size, so the body is
complete down to the shoes.

Each character is a function body(expr) -> Canvas and face(expr) -> Canvas. Expressions:
    body: idle, blink, talk, talk_open, happy, sad
    face: neutral, happy, worried
"""

from __future__ import annotations

import math

import numpy as np

import figure as fg
import px
from figure import Figure

W, H = 96, 160
COUNTER_ROW = 92          # first image row hidden by the counter top at the counter anchor
FW = FH = 48              # bench face size


# --------------------------------------------------------------------------------------------------
# Shared pieces
# --------------------------------------------------------------------------------------------------

def scallop_line(x: np.ndarray, base: float, amp: float, period: float, phase: float = 0.0) -> np.ndarray:
    """A wavy line y(x) for fringes: bumps of `amp` px every `period` px."""
    return base + amp * np.abs(np.cos((x - phase) * math.pi / period))


def curls(w: int, h: int, cx: float, cy: float, rx: float, ry: float, r: float, a0: float, a1: float, n: int) -> np.ndarray:
    """Circles of radius r spaced along an elliptical arc: bumpy hair silhouettes."""
    m = px.empty(w, h)
    for i in range(n):
        a = math.radians(a0 + (a1 - a0) * i / max(n - 1, 1))
        m |= px.ellipse(w, h, cx + rx * math.cos(a), cy + ry * math.sin(a), r, r)
    return m


def hands_on_counter(fig: Figure, xs: tuple[int, int], y: int, skin: str = "skin", rx: float = 5, ry: float = 4) -> None:
    for x in xs:
        fig.part(px.ellipse(fig.w, fig.h, x, y, rx, ry), skin, tones=(2, 3, 4), cuts=(0.38, 0.85))
        # thumb on the inner side
        tx = x + (3 if x < fig.w // 2 else -3)
        fig.put(tx, y - 3, px.C(skin, 3))


def shoes_and_legs(fig: Figure, cx: int, top: int, leg_ramp: str, leg_tones, shoe_ramp: str, gap: int = 4,
                   leg_w: float = 2.6, shoe_w: float = 5.5) -> None:
    w, h = fig.w, fig.h
    for sx in (-1, 1):
        lx = cx + sx * (gap + 1)
        fig.part(px.capsule(w, h, lx, top, lx, h - 8, leg_w), leg_ramp, tones=leg_tones, cuts=(0.45, 0.85))
    for sx in (-1, 1):
        x = cx + sx * (gap + 2)
        fig.part(px.ellipse(w, h, x + sx * 1, h - 4.5, shoe_w, 4.0) & px.box(w, h, 0, h - 9, w, h - 1),
                 shoe_ramp, tones=(1, 2, 3), cuts=(0.45, 0.85))


def face_part(fig: Figure, mask: np.ndarray, skin: str = "skin") -> np.ndarray:
    """Faces stay mostly one flat light tone: a soft rim shadow away from the window, a lit cheek."""
    return fig.part(mask, skin, tones=(2, 3, 4), cuts=(0.3, 0.9), flat=0.62)


def cloth(fig: Figure, mask: np.ndarray, ramp: str, tones=(2, 3, 4), cuts=(0.36, 0.88), flat: float = 0.5, **kw) -> np.ndarray:
    """Clothes: flat base with shading only toward the edges (no big diagonal splits)."""
    return fig.part(mask, ramp, tones=tones, cuts=cuts, flat=flat, **kw)


def hair_shadow(fig: Figure, hair: np.ndarray, face: np.ndarray, skin: str = "skin", depth: int = 1) -> None:
    """The fringe casts a thin shadow on the forehead."""
    sh = px.empty(fig.w, fig.h)
    for k in range(1, depth + 1):
        sh |= px.shift(hair, 0, k)
    sh &= face & ~hair
    fig.solid(sh, px.C(skin, 2))


def glasses(fig: Figure, centres, r: float, frame, glint=True, lens_tint=None) -> None:
    w, h = fig.w, fig.h
    for (cx, cy) in centres:
        ring = px.ellipse(w, h, cx, cy, r + 0.5, r + 0.5) & ~px.ellipse(w, h, cx, cy, r - 0.6, r - 0.6)
        if lens_tint is not None:
            inside = px.ellipse(w, h, cx, cy, r - 0.6, r - 0.6)
            fig.cv.a[inside] = (fig.cv.a[inside].astype(np.int32) * 3 + np.array(lens_tint, np.int32)) // 4
        fig.solid(ring, frame)
        if glint:
            gx, gy = int(round(cx + r * 0.45)), int(round(cy - r * 0.45))
            fig.put(gx, gy, fg.WHITE)
            fig.put(gx - 1, gy + 1, px.C("rice", 4))
    (x0, y0), (x1, y1) = centres
    fig.solid(px.box(w, h, int(x0 + r), int(y0) - 1, int(x1 - r), int(y0) - 1), frame)


# --------------------------------------------------------------------------------------------------
# Fumi (Mrs. Fumiko Tanabe): cheerful regular, silver perm with a bun and a kanzashi, round gold
# glasses, sage cardigan over a lace-collared blouse, plum skirt. Her tip is a mikan.
# --------------------------------------------------------------------------------------------------

def fumi_head(fig: Figure, cx: int, hy: int, expr: str, rx: float = 21.5, ry: float = 21.0, small: bool = False) -> None:
    w, h = fig.w, fig.h
    s = rx / 21.5
    # hair volume behind the head (soft perm) + bun
    back = px.ellipse(w, h, cx, hy - 3 * s, rx + 3.5 * s, ry + 3 * s)
    back |= curls(w, h, cx, hy - 3 * s, rx + 2.5 * s, ry + 2.2 * s, 3.8 * s, 190, 350, 8)
    bun = px.ellipse(w, h, cx, hy - ry - 4.5 * s, 9 * s, 7.5 * s)
    fig.part(bun, "silver", tones=(1, 2, 3), cuts=(0.45, 0.82))
    fig.part(back, "silver", tones=(1, 2, 3), cuts=(0.42, 0.86), flat=0.4)
    # face
    face = px.ellipse(w, h, cx, hy + 1, rx, ry)
    face_part(fig, face)
    # fringe and side curls
    xx, yy = px.grid(w, h)
    # perm fringe: a row of round curls hanging over the forehead
    fringe = back & (yy < hy - 9 * s)
    for i in range(-3, 4):
        fringe |= px.ellipse(w, h, cx + i * 6.2 * s, hy - 8.5 * s + abs(i) * 0.6 * s, 3.9 * s, 3.4 * s)
    fringe &= back | px.ellipse(w, h, cx, hy - 3 * s, rx + 1, ry + 1)
    side = px.ellipse(w, h, cx - rx + 1 * s, hy + 1 * s, 5.4 * s, 9 * s) | px.ellipse(w, h, cx + rx - 1 * s, hy + 1 * s, 5.4 * s, 9 * s)
    side &= ~px.ellipse(w, h, cx, hy + 6 * s, rx - 5 * s, ry - 3 * s)
    hair_front = fringe | side
    fig.part(hair_front, "silver", tones=(1, 3, 4), cuts=(0.42, 0.84), flat=0.3)
    hair_shadow(fig, hair_front, face, depth=2 if not small else 1)
    # curl detail lines in the fringe
    for i in range(-2, 3):
        x = int(cx + i * 6.5 * s)
        fig.put(x, int(hy - 8 * s), px.C("silver", 2))
        fig.put(x + 1, int(hy - 9 * s), px.C("silver", 2))
    # kanzashi through the bun
    if not small:
        stick = px.capsule(w, h, cx - 13, hy - ry - 8, cx + 12, hy - ry - 1, 0.9)
        fig.part(stick, "gold", tones=(2, 3), cuts=(0.55,), line=True)
        fig.part(px.ellipse(w, h, cx + 13.5, hy - ry - 0.5, 2.2, 2.2), "pink", tones=(2, 3, 4), cuts=(0.4, 0.8))
    # features
    ex = int(round(9 * s))
    ey = int(hy + 3 * s)
    gl = [(cx - ex + 0.5, ey + 1.0), (cx + ex + 0.5, ey + 1.0)]
    brow = px.C("silver", 1)
    if expr == "sad" or expr == "worried":
        fig.pixels([(cx - ex - 3, ey - 6), (cx - ex - 2, ey - 6), (cx - ex - 1, ey - 7), (cx - ex, ey - 7)], brow)
        fig.pixels([(cx + ex + 3, ey - 6), (cx + ex + 2, ey - 6), (cx + ex + 1, ey - 7), (cx + ex, ey - 7)], brow)
    else:
        fig.pixels([(cx - ex - 2, ey - 6), (cx - ex - 1, ey - 7), (cx - ex, ey - 7), (cx - ex + 1, ey - 7), (cx - ex + 2, ey - 6)], brow)
        fig.pixels([(cx + ex - 2, ey - 6), (cx + ex - 1, ey - 7), (cx + ex, ey - 7), (cx + ex + 1, ey - 7), (cx + ex + 2, ey - 6)], brow)
    if expr in ("happy",):
        fg.eye_happy(fig, cx - ex - 1, ey + 1, 4)
        fg.eye_happy(fig, cx + ex - 1, ey + 1, 4)
    elif expr == "blink":
        fg.eye_closed(fig, cx - ex - 1, ey + 1, 3)
        fg.eye_closed(fig, cx + ex - 1, ey + 1, 3)
    elif expr == "worried":
        fg.eye_squeeze(fig, cx - ex - 1, ey - 1, True)
        fg.eye_squeeze(fig, cx + ex - 1, ey - 1, False)
    else:
        dy = 1 if expr == "sad" else 0
        fg.eye_round(fig, cx - ex - 1, ey + dy, 3)
        fg.eye_round(fig, cx + ex - 1, ey + dy, 3)
    glasses(fig, gl, 5.2 * s if not small else 4.6, px.C("gold", 1), lens_tint=px.C("rice", 5))
    fg.blush(fig, cx - ex - 5, ey + 7, 4)
    fg.blush(fig, cx + ex + 2, ey + 7, 4)
    my = int(hy + 12 * s)
    fig.put(cx, my - 3, px.C("skin", 2))          # nose
    kind = {"idle": "smile", "blink": "smile", "talk": "smile", "talk_open": "open", "happy": "grin",
            "sad": "wobble", "neutral": "smile", "worried": "wobble"}[expr]
    fg.mouth(fig, cx, my, kind)
    if expr in ("sad", "worried"):
        fg.sweat(fig, cx + rx - 1, hy - 8)


def fumi_body(expr: str = "idle") -> px.Canvas:
    fig = Figure(W, H)
    cx = 48
    oy = 6 + (2 if expr == "sad" else 0)
    hy = 36 + oy
    s0 = hy + 23                                      # shoulder line
    # legs, shoes, skirt
    shoes_and_legs(fig, cx, 128, "skin", (2, 3, 4), "wood", gap=4)
    skirt = px.poly(W, H, [(31, 104), (65, 104), (70, 141), (26, 141)]) | px.ellipse(W, H, 48, 141, 22, 3)
    cloth(fig, skirt, "plum", tones=(2, 3, 4), cuts=(0.38, 0.88))
    for x in (37, 43, 53, 59):
        fig.solid(px.capsule(W, H, x, 114, x + (x - 48) * 0.2, 142, 0.4), px.C("plum", 2))
    # cardigan torso: rounded shoulders, a little A-line, ribbed hem
    torso = px.poly(W, H, [(35, s0), (61, s0), (68, s0 + 8), (70, 112), (26, 112), (28, s0 + 8)])
    torso |= px.ellipse(W, H, 34.5, s0 + 8, 7.5, 8) | px.ellipse(W, H, 61.5, s0 + 8, 7.5, 8)
    cloth(fig, torso, "sage")
    fig.solid(torso & px.box(W, H, 0, 107, W, 107), px.C("sage", 2))
    for x in range(28, 70, 3):
        fig.put(x, 109, px.C("sage", 2))
    # blouse V and placket
    blouse = px.poly(W, H, [(41, s0 - 1), (55, s0 - 1), (52, s0 + 19), (48, s0 + 25), (44, s0 + 19)])
    cloth(fig, blouse, "rice", tones=(3, 4, 5), cuts=(0.3, 0.85))
    fig.solid(px.box(W, H, 48, s0 + 25, 48, 111), px.C("sage", 1))
    for by in range(s0 + 29, 106, 7):
        fig.put(46, by, px.C("rice", 4))
        fig.put(46, by + 1, px.C("rice", 1))
    # soft folds under the arms
    for sx in (-1, 1):
        fig.solid(px.capsule(W, H, cx + sx * 17, s0 + 14, cx + sx * 18, s0 + 30, 0.4), px.C("sage", 2))
    # arms (sleeves) and hands resting on the counter
    sad = expr == "sad"
    happy = expr == "happy"
    for sx in (-1, 1):
        sh = (cx + sx * 19, s0 + 7)
        if happy:
            pts = [sh, (cx + sx * 23, s0 + 17), (cx + sx * 18, s0 + 8)]
        elif sad:
            pts = [sh, (cx + sx * 21, s0 + 21), (cx + sx * 17, s0 + 34)]
        else:
            pts = [sh, (cx + sx * 22, s0 + 17), (cx + sx * 14, s0 + 25)]
        cloth(fig, px.stroke(W, H, pts, 4.6), "sage", tones=(1, 2, 3), cuts=(0.42, 0.86), flat=0.35)
        end = pts[-1]
        cuff = px.ellipse(W, H, end[0], end[1], 4.8, 3.4) & ~px.ellipse(W, H, end[0] + sx * 1.2, end[1] - 1.4, 4.8, 3.4)
        fig.solid(cuff & fig.painted(), px.C("sage", 1))
    if happy:
        # hands up at the shoulders, one holding a mikan
        hands_on_counter(fig, (cx - 18, cx + 18), s0 + 4, rx=4.6, ry=4.2)
        mk = px.ellipse(W, H, cx + 19, s0 - 4, 5.2, 4.8)
        fig.part(mk, "orange", tones=(2, 3, 4), cuts=(0.42, 0.85))
        fig.part(px.ellipse(W, H, cx + 20.5, s0 - 9, 2.2, 1.3), "leaf", tones=(2, 3), cuts=(0.5,))
        fig.put(cx + 19, s0 - 8, px.C("wood", 1))
        fig.put(cx + 21, s0 - 6, px.C("orange", 4))
    elif not sad:
        hands_on_counter(fig, (cx - 14, cx + 14), s0 + 27)
    # neck and lace collar
    fig.part(px.box(W, H, 44, hy + 16, 52, s0 + 1), "skin", tones=(1, 2), cuts=(0.7,), style="dome")
    for sx in (-1, 1):
        lobe = px.ellipse(W, H, cx + sx * 5.5, s0 + 2.5, 6.5, 4.2)
        fig.part(lobe, "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    for x in range(36, 61, 2):
        y = s0 + 6 if abs(x - 48) > 3 else s0 + 5
        fig.put(x, y, px.C("rice", 2))
    # brooch
    fig.part(px.ellipse(W, H, cx, s0 + 7.5, 2.6, 2.6), "gold", tones=(2, 3, 4), cuts=(0.4, 0.85))
    fig.put(cx, s0 + 7, px.C("pink", 3))
    # head on top
    fumi_head(fig, cx, hy, expr)
    return fig.finish()


def fumi_face(expr: str = "neutral") -> px.Canvas:
    fig = Figure(FW, FH)
    cx, hy = 24, 27
    # shoulders: cardigan + collar
    fig.part(px.ellipse(FW, FH, 24, 52, 21, 9), "sage", tones=(2, 3, 4), cuts=(0.45, 0.85))
    for sx in (-1, 1):
        fig.part(px.ellipse(FW, FH, cx + sx * 4.5, 44.5, 5, 3.2), "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    fumi_head(fig, cx, hy, expr, rx=17.5, ry=16.5, small=True)
    return fig.finish()


# --------------------------------------------------------------------------------------------------
# Shared body builders
# --------------------------------------------------------------------------------------------------

def torso_mask(s0: int, top: tuple[int, int], bottom: tuple[int, int], bottom_row: int, shoulder_r: float = 7.5) -> np.ndarray:
    (tl, tr), (bl, br) = top, bottom
    m = px.poly(W, H, [(tl, s0), (tr, s0), (tr + 7, s0 + 8), (br, bottom_row), (bl, bottom_row), (tl - 7, s0 + 8)])
    m |= px.ellipse(W, H, tl - 0.5, s0 + shoulder_r, shoulder_r, shoulder_r + 0.5)
    m |= px.ellipse(W, H, tr + 0.5, s0 + shoulder_r, shoulder_r, shoulder_r + 0.5)
    return m


def sleeve(fig: Figure, pts, ramp: str, r: float = 4.6, tones=(1, 2, 3), cuff=True, sx: int = 1) -> np.ndarray:
    m = px.stroke(W, H, pts, r)
    cloth(fig, m, ramp, tones=tones, cuts=(0.42, 0.86), flat=0.35)
    if cuff:
        end = pts[-1]
        c = px.ellipse(W, H, end[0], end[1], r + 0.2, r * 0.74) & ~px.ellipse(W, H, end[0] + sx * 1.2, end[1] - 1.4, r + 0.2, r * 0.74)
        fig.solid(c & m, px.C(ramp, tones[0] - 1 if tones[0] > 0 else 0))
    return m


def arm_pts(cx: int, s0: int, sx: int, pose: str) -> list[tuple[float, float]]:
    """Shoulder -> elbow -> wrist for the common poses."""
    sh = (cx + sx * 19, s0 + 7)
    if pose == "counter":
        return [sh, (cx + sx * 22, s0 + 17), (cx + sx * 14, s0 + 25)]
    if pose == "down":
        return [sh, (cx + sx * 21, s0 + 21), (cx + sx * 18, s0 + 34)]
    if pose == "up":
        return [sh, (cx + sx * 27, s0 - 2), (cx + sx * 22, s0 - 16)]
    if pose == "chest":
        return [sh, (cx + sx * 23, s0 + 17), (cx + sx * 8, s0 + 14)]
    if pose == "cheer":
        return [sh, (cx + sx * 23, s0 + 17), (cx + sx * 18, s0 + 8)]
    raise ValueError(pose)


def hand(fig: Figure, x: float, y: float, skin: str = "skin", rx: float = 4.6, ry: float = 4.0, ramp_tones=(2, 3, 4)) -> None:
    fig.part(px.ellipse(W, H, x, y, rx, ry), skin, tones=ramp_tones, cuts=(0.38, 0.85))


def brows(fig: Figure, cx: int, ex: int, y: int, colour, mood: str = "calm", length: int = 5) -> None:
    """Eyebrows above eyes at cx +- ex. mood: calm (slight arch), worried (inner ends up), cross (inner
    ends down), raised (one brow up), flat."""
    for sx in (-1, 1):
        x0 = cx + sx * ex
        pts = []
        for i in range(length):
            dx = i - length // 2
            if mood == "calm":
                dy = -1 if abs(dx) <= length // 2 - 1 else 0
            elif mood == "worried":
                dy = int(round(-dx * sx * 0.5)) - 1
            elif mood == "cross":
                dy = int(round(dx * sx * 0.5)) - 1
            elif mood == "raised":
                dy = (-2 if sx > 0 else 0) + (-1 if abs(dx) <= 1 else 0)
            else:
                dy = 0
            pts.append((x0 + dx, y + dy))
        fig.pixels(pts, colour)


# --------------------------------------------------------------------------------------------------
# Mr. Kenji Sasaki: office worker with a meeting in twenty minutes. Neat side part with one stubborn
# cowlick, navy suit, coral tie, teal lanyard, sweat on his temple, always checking his watch.
# --------------------------------------------------------------------------------------------------

def sasaki_head(fig: Figure, cx: int, hy: int, expr: str, rx: float = 20.5, ry: float = 21.0, small: bool = False) -> None:
    w, h = fig.w, fig.h
    s = rx / 20.5
    # ears
    for sx in (-1, 1):
        fig.part(px.ellipse(w, h, cx + sx * (rx - 0.5), hy + 3 * s, 3.2 * s, 4.2 * s), "tan", tones=(2, 3, 4), cuts=(0.4, 0.85))
    face = px.ellipse(w, h, cx, hy + 1, rx, ry)
    face_part(fig, face, "tan")
    # neat hair: a cap with a side part (left) and a sweep to the right
    cap = px.ellipse(w, h, cx, hy - 4 * s, rx + 1.5 * s, ry - 1 * s) & (px.grid(w, h)[1] < hy - 6 * s)
    sweep = px.poly(w, h, [(cx - rx - 1, hy - 6 * s), (cx - 5 * s, hy - 13 * s), (cx + rx + 1.5, hy - 9 * s),
                           (cx + rx + 1, hy - 1 * s), (cx + rx - 4 * s, hy - 5 * s), (cx - 2 * s, hy - 6 * s)])
    sideburn = px.box(w, h, int(cx - rx), int(hy - 7 * s), int(cx - rx + 2 * s), int(hy + 3 * s)) | \
        px.box(w, h, int(cx + rx - 2 * s), int(hy - 7 * s), int(cx + rx), int(hy + 3 * s))
    hair = (cap | sweep | sideburn) & px.ellipse(w, h, cx, hy, rx + 2, ry + 3)
    fig.part(hair, "ink", tones=(1, 2, 3), cuts=(0.5, 0.9), flat=0.3)
    hair_shadow(fig, hair, face, "tan", 1)
    # side part line and a sheen
    fig.solid(px.capsule(w, h, cx - 8 * s, hy - ry + 4, cx - 6 * s, hy - 9 * s, 0.4), px.C("ink", 0))
    for i in range(5):
        fig.put(cx + 3 * s + i * 2, hy - ry + 2 + i // 2, px.C("ink", 3))
    # the stubborn cowlick
    if not small:
        fig.solid(px.stroke(w, h, [(cx + 1, hy - ry), (cx + 2, hy - ry - 5), (cx + 6, hy - ry - 7)], 0.6), px.C("ink", 1))
    ex = int(round(8.5 * s))
    ey = int(hy + 3 * s)
    mood = {"sad": "worried", "worried": "worried", "happy": "calm", "idle": "cross", "blink": "cross",
            "talk": "cross", "talk_open": "cross", "neutral": "cross"}[expr]
    brows(fig, cx, ex, ey - 5, px.C("ink", 1), mood, 6)
    if expr == "happy":
        fg.eye_happy(fig, cx - ex - 2, ey + 1, 4)
        fg.eye_happy(fig, cx + ex - 1, ey + 1, 4)
    elif expr in ("blink",):
        fg.eye_closed(fig, cx - ex - 2, ey + 1, 4)
        fg.eye_closed(fig, cx + ex - 1, ey + 1, 4)
    elif expr == "worried":
        fg.eye_squeeze(fig, cx - ex - 1, ey - 1, True)
        fg.eye_squeeze(fig, cx + ex - 1, ey - 1, False)
    else:
        for x0 in (cx - ex - 2, cx + ex - 1):
            # wide-awake, slightly stressed eyes: white sclera, small dark pupil
            fig.pixels([(x0 + i, ey - 1) for i in range(4)], px.C("ink", 1))
            fig.pixels([(x0 + i, ey) for i in range(4)] + [(x0 + i, ey + 1) for i in range(4)], fg.WHITE)
            pupil_x = x0 + (2 if x0 < cx else 1)
            fig.pixels([(pupil_x, ey), (pupil_x, ey + 1)], px.C("ink", 0))
            if expr == "sad":
                fig.pixels([(x0 + i, ey - 1) for i in range(4)], px.C("tan", 1))
                fig.pixels([(x0 + i, ey) for i in range(4)], px.C("ink", 1))
    my = int(hy + 12 * s)
    fig.pixels([(cx, my - 4), (cx + 1, my - 3)], px.C("tan", 2))
    kind = {"idle": "flat", "blink": "flat", "talk": "flat", "talk_open": "open", "happy": "smile",
            "sad": "wobble", "neutral": "flat", "worried": "wobble"}[expr]
    fg.mouth(fig, cx, my, kind, "tan")
    if expr != "happy":
        fg.sweat(fig, cx - rx - 3, hy - 4)
    if expr in ("sad", "worried"):
        fg.sweat(fig, cx + rx + 2, hy - 1)


def sasaki_body(expr: str = "idle") -> px.Canvas:
    fig = Figure(W, H)
    cx = 48
    oy = -1 + (2 if expr == "sad" else 0)
    hy = 35 + oy
    s0 = hy + 23
    # trousers and shoes
    shoes_and_legs(fig, cx, 120, "ink", (1, 2, 3), "ink", gap=5, leg_w=4.2, shoe_w=6)
    torso = torso_mask(s0, (35, 61), (28, 68), 122)
    cloth(fig, torso, "ink", tones=(1, 2, 3), cuts=(0.4, 0.9))
    # shirt and tie
    shirt = px.poly(W, H, [(41, s0 - 1), (55, s0 - 1), (52, s0 + 22), (48, s0 + 30), (44, s0 + 22)])
    cloth(fig, shirt, "rice", tones=(3, 4, 5), cuts=(0.3, 0.85))
    tie = px.poly(W, H, [(46, s0 + 3), (50, s0 + 3), (51, s0 + 22), (48, s0 + 26), (45, s0 + 22)])
    cloth(fig, tie, "coral", tones=(1, 2, 3), cuts=(0.4, 0.88))
    fig.part(px.box(W, H, 46, s0 + 1, 50, s0 + 3), "coral", tones=(1, 2), cuts=(0.6,))
    for i in range(0, 20, 5):
        fig.solid(px.capsule(W, H, 45, s0 + 7 + i, 51, s0 + 4 + i, 0.4) & tie, px.C("gold", 3))
    # lapels
    for sx in (-1, 1):
        lap = px.poly(W, H, [(cx + sx * 7, s0 - 1), (cx + sx * 13, s0 + 1), (cx + sx * 10, s0 + 9), (cx + sx * 4, s0 + 25)])
        cloth(fig, lap, "ink", tones=(2, 3, 3), cuts=(0.45, 0.95))
    # lanyard and ID badge
    fig.solid(px.stroke(W, H, [(39, s0), (41, s0 + 14), (44, s0 + 22)], 0.6) | px.stroke(W, H, [(57, s0), (55, s0 + 14), (52, s0 + 22)], 0.6), px.C("teal", 3))
    badge = px.box(W, H, 41, s0 + 22, 55, s0 + 31)
    fig.part(badge, "rice", tones=(4, 5), cuts=(0.7,), style="flat")
    fig.solid(px.box(W, H, 43, s0 + 24, 46, s0 + 28), px.C("tan", 3))
    fig.solid(px.box(W, H, 48, s0 + 25, 53, s0 + 25) | px.box(W, H, 48, s0 + 27, 52, s0 + 27), px.C("ink", 2))
    fig.solid(px.box(W, H, 41, s0 + 22, 55, s0 + 22), px.C("teal", 3))
    # arms: right hand on the counter, left wrist up with the watch (always checking)
    pose_l = {"happy": "counter", "sad": "down"}.get(expr, "chest")
    pose_r = "down" if expr == "sad" else "counter"
    for sx, pose in ((-1, pose_r), (1, pose_l)):
        pts = arm_pts(cx, s0, sx, pose)
        sleeve(fig, pts, "ink", r=4.7, tones=(1, 2, 3), sx=sx)
        cx_, cy_ = pts[-1]
        if pose == "chest":
            hand(fig, cx_ - 2, cy_ - 1, "tan", 4.2, 3.6)
            watch = px.box(W, H, int(cx_) + 1, int(cy_) - 3, int(cx_) + 4, int(cy_) + 2)
            fig.part(watch, "gold", tones=(1, 2, 3), cuts=(0.4, 0.8))
            fig.solid(px.box(W, H, int(cx_) + 2, int(cy_) - 2, int(cx_) + 3, int(cy_) + 1), px.C("rice", 4))
            fig.put(int(cx_) + 2, int(cy_) - 1, px.C("ink", 0))
        elif pose == "counter":
            hand(fig, cx_, cy_ + 1, "tan")
    sasaki_head(fig, cx, hy, expr)
    return fig.finish()


def sasaki_face(expr: str = "neutral") -> px.Canvas:
    fig = Figure(FW, FH)
    fig.part(px.ellipse(FW, FH, 24, 52, 21, 9), "ink", tones=(1, 2, 3), cuts=(0.45, 0.85))
    fig.part(px.poly(FW, FH, [(18, 43), (30, 43), (27, 48), (21, 48)]), "rice", tones=(3, 4, 5), cuts=(0.35, 0.8))
    fig.part(px.poly(FW, FH, [(23, 44), (25, 44), (26, 48), (22, 48)]), "coral", tones=(1, 2, 3), cuts=(0.4, 0.85))
    sasaki_head(fig, 24, 26, expr, rx=16.5, ry=16.5, small=True)
    return fig.finish()


# --------------------------------------------------------------------------------------------------
# Mio Hayashi: student, dropped her phone in tonkotsu ramen. Brown bob with a star clip, big sparkly
# eyes, navy sailor collar and red ribbon under an oversized mustard cardigan, bag strap full of charms.
# --------------------------------------------------------------------------------------------------

def mio_head(fig: Figure, cx: int, hy: int, expr: str, rx: float = 20.5, ry: float = 19.5, small: bool = False) -> None:
    w, h = fig.w, fig.h
    s = rx / 20.5
    xx, yy = px.grid(w, h)
    # bob: back volume down to the jaw with an inward curl
    back = px.ellipse(w, h, cx, hy - 1 * s, rx + 4 * s, ry + 3 * s) | px.box(w, h, int(cx - rx - 4 * s), int(hy - 2 * s), int(cx + rx + 4 * s), int(hy + 13 * s))
    back &= px.rrect(w, h, int(cx - rx - 4 * s), int(hy - ry - 4 * s), int(cx + rx + 4 * s), int(hy + 15 * s), 6 * s)
    fig.part(back, "wood", tones=(1, 2, 3), cuts=(0.45, 0.88), flat=0.3)
    face = px.ellipse(w, h, cx, hy + 1, rx, ry)
    face_part(fig, face)
    # blunt bangs + side curtains
    bangs = px.ellipse(w, h, cx, hy - 3 * s, rx + 2 * s, ry + 1 * s) & (yy < hy - 4 * s)
    for i in range(-3, 4):
        bangs |= px.box(w, h, int(cx + i * 5.5 * s - 2), int(hy - 6 * s), int(cx + i * 5.5 * s + 2), int(hy - 3 * s + (i % 2)))
    bangs &= px.ellipse(w, h, cx, hy - 2 * s, rx + 2 * s, ry + 2 * s)
    sides = (px.box(w, h, int(cx - rx - 3 * s), int(hy - 6 * s), int(cx - rx + 4 * s), int(hy + 12 * s)) |
             px.box(w, h, int(cx + rx - 4 * s), int(hy - 6 * s), int(cx + rx + 3 * s), int(hy + 12 * s))) & back
    hair = bangs | sides
    fig.part(hair, "wood", tones=(1, 3, 4), cuts=(0.45, 0.86), flat=0.25)
    hair_shadow(fig, hair, face, "skin", 1)
    # star clip
    if not small:
        star = [(0, -2), (-1, -1), (0, -1), (1, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (-1, 1), (1, 1)]
        fig.pixels([(cx + 13 + a, hy - 9 + b) for a, b in star], px.C("gold", 3))
        fig.put(cx + 13, hy - 9, px.C("gold", 4))
        # ahoge
        fig.solid(px.stroke(w, h, [(cx - 3, hy - ry - 3), (cx - 6, hy - ry - 8), (cx - 2, hy - ry - 10)], 0.6), px.C("wood", 2))
    ex = int(round(8.5 * s))
    ey = int(hy + 1 * s)
    mood = {"sad": "worried", "worried": "worried"}.get(expr, "calm")
    brows(fig, cx, ex, ey - 4, px.C("wood", 1), mood, 4)
    if expr == "happy":
        fg.eye_happy(fig, cx - ex - 2, ey + 3, 5)
        fg.eye_happy(fig, cx + ex - 2, ey + 3, 5)
    elif expr == "blink":
        fg.eye_closed(fig, cx - ex - 2, ey + 4, 5)
        fg.eye_closed(fig, cx + ex - 2, ey + 4, 5)
    elif expr == "worried":
        fg.eye_squeeze(fig, cx - ex - 1, ey + 1, True)
        fg.eye_squeeze(fig, cx + ex - 1, ey + 1, False)
    else:
        fg.eye_big(fig, cx - ex - 2, ey + 1, iris=px.C("wood", 2))
        fg.eye_big(fig, cx + ex - 2, ey + 1, iris=px.C("wood", 2))
        if expr == "sad":
            fg.tear(fig, cx - ex - 2, ey + 7, 3)
            fg.tear(fig, cx + ex + 2, ey + 7, 2)
    fg.blush(fig, cx - ex - 5, ey + 9, 5, px.C("pink", 3))
    fg.blush(fig, cx + ex + 1, ey + 9, 5, px.C("pink", 3))
    my = int(hy + 11 * s)
    kind = {"idle": "tiny", "blink": "tiny", "talk": "tiny", "talk_open": "o", "happy": "grin",
            "sad": "sad_open", "neutral": "tiny", "worried": "wobble"}[expr]
    fg.mouth(fig, cx, my, kind)


def mio_body(expr: str = "idle") -> px.Canvas:
    fig = Figure(W, H)
    cx = 48
    oy = 9 + (2 if expr == "sad" else 0)
    hy = 34 + oy
    s0 = hy + 21
    # pleated navy skirt, socks, shoes
    shoes_and_legs(fig, cx, 126, "skin", (2, 3, 4), "wood", gap=4, leg_w=2.4)
    fig.solid(px.box(W, H, 39, 141, 43, 150) | px.box(W, H, 53, 141, 57, 150), px.C("rice", 4))
    skirt = px.poly(W, H, [(32, 112), (64, 112), (70, 132), (26, 132)])
    cloth(fig, skirt, "ink", tones=(2, 3, 3), cuts=(0.4, 0.9))
    for x in range(30, 67, 5):
        fig.solid(px.capsule(W, H, x + (x - 48) * -0.15, 113, x, 131, 0.35), px.C("ink", 1))
    # oversized mustard cardigan
    torso = torso_mask(s0, (36, 60), (29, 67), 116, 7.5)
    cloth(fig, torso, "gold", tones=(1, 2, 3), cuts=(0.38, 0.9))
    fig.solid(torso & px.box(W, H, 0, 112, W, 112), px.C("gold", 1))
    # sailor collar + ribbon in the cardigan opening
    inner = px.poly(W, H, [(40, s0 - 1), (56, s0 - 1), (53, s0 + 26), (43, s0 + 26)])
    cloth(fig, inner, "rice", tones=(3, 4, 5), cuts=(0.3, 0.85))
    for sx in (-1, 1):
        col = px.poly(W, H, [(cx + sx * 4, s0 - 1), (cx + sx * 15, s0 + 1), (cx + sx * 12, s0 + 9), (cx + sx * 2, s0 + 16)])
        fig.part(col, "ink", tones=(2, 3), cuts=(0.6,), style="dome", flat=0.5)
        fig.solid(px.capsule(W, H, cx + sx * 13, s0 + 3, cx + sx * 4, s0 + 13, 0.4) & col, px.C("rice", 4))
    rib = px.poly(W, H, [(42, s0 + 12), (48, s0 + 15), (54, s0 + 12), (55, s0 + 19), (48, s0 + 17), (41, s0 + 19)])
    fig.part(rib, "coral", tones=(1, 2, 3), cuts=(0.42, 0.85))
    fig.part(px.ellipse(W, H, 48, s0 + 16, 2.2, 2.2), "coral", tones=(2, 3), cuts=(0.5,))
    fig.solid(px.poly(W, H, [(46, s0 + 18), (50, s0 + 18), (52, s0 + 26), (48, s0 + 23), (44, s0 + 26)]), px.C("coral", 2))
    # bag strap with charms (ramen bowl + cat)
    fig.solid(px.capsule(W, H, 38, s0 + 1, 62, s0 + 30, 1.1), px.C("wood", 2))
    fig.solid(px.capsule(W, H, 38, s0 + 1, 62, s0 + 30, 0.3), px.C("wood", 3))
    bowl = px.ellipse(W, H, 36, s0 + 12.5, 4.0, 2.6) & px.box(W, H, 0, s0 + 12, W, H)
    fig.part(bowl, "rice", tones=(3, 4, 5), cuts=(0.4, 0.8))
    fig.solid(px.box(W, H, 33, s0 + 11, 39, s0 + 11), px.C("gold", 3))
    fig.solid(px.box(W, H, 33, s0 + 13, 39, s0 + 13), px.C("coral", 2))
    fig.put(36, s0 + 9, px.C("wood", 3))
    # sleeves (sleeve-paws) and hands
    pose = {"happy": "up", "sad": "down"}.get(expr, "counter")
    for sx in (-1, 1):
        pts = arm_pts(cx, s0, sx, pose)
        sleeve(fig, pts, "gold", r=5.0, tones=(1, 2, 3), sx=sx)
        ex_, ey_ = pts[-1]
        if pose == "up":
            hand(fig, ex_, ey_ - 3, "skin", 4.0, 3.8)
            fig.solid(px.box(W, H, int(ex_) - 4, int(ey_) - 1, int(ex_) + 4, int(ey_) + 1) & fig.painted(), px.C("gold", 1))
        elif pose == "counter":
            hand(fig, ex_, ey_ + 1.5, "skin", 4.0, 3.2)
            fig.solid(px.box(W, H, int(ex_) - 4, int(ey_) - 2, int(ex_) + 4, int(ey_)) & fig.painted(), px.C("gold", 2))
    if expr == "happy":
        for (x, y) in ((cx - 30, s0 - 26), (cx + 30, s0 - 24), (cx + 34, s0 - 10)):
            fig.pixels([(x, y - 1), (x - 1, y), (x, y), (x + 1, y), (x, y + 1)], px.C("gold", 4))
    mio_head(fig, cx, hy, expr)
    return fig.finish()


def mio_face(expr: str = "neutral") -> px.Canvas:
    fig = Figure(FW, FH)
    fig.part(px.ellipse(FW, FH, 24, 52, 21, 9), "gold", tones=(1, 2, 3), cuts=(0.45, 0.85))
    for sx in (-1, 1):
        fig.part(px.poly(FW, FH, [(24 + sx * 2, 42), (24 + sx * 12, 44), (24 + sx * 9, 48), (24 + sx * 1, 48)]), "ink", tones=(2, 3), cuts=(0.6,))
    fig.part(px.ellipse(FW, FH, 24, 46, 3, 2), "coral", tones=(1, 2, 3), cuts=(0.4, 0.8))
    mio_head(fig, 24, 25, expr, rx=16.5, ry=15.5, small=True)
    return fig.finish()


# --------------------------------------------------------------------------------------------------
# Ren Kuroda: gamer who hasn't slept since Tuesday. Messy spikes with a violet sheen, half-lidded eyes
# with bags, headphones round his neck, purple hoodie, a can of energy drink.
# --------------------------------------------------------------------------------------------------

def ren_head(fig: Figure, cx: int, hy: int, expr: str, rx: float = 20.5, ry: float = 20.5, small: bool = False) -> None:
    w, h = fig.w, fig.h
    s = rx / 20.5
    face = px.ellipse(w, h, cx, hy + 1, rx, ry)
    # messy spiky hair: a blob plus triangular spikes all round the top
    blob = px.ellipse(w, h, cx, hy - 5 * s, rx + 3 * s, ry - 1 * s)
    spikes = px.empty(w, h)
    for ang, length in ((-160, 9), (-135, 11), (-110, 12), (-85, 13), (-60, 12), (-35, 10), (-12, 8), (-188, 7)):
        a = math.radians(ang)
        bx, by = cx + (rx + 1) * math.cos(a) * s, hy - 5 * s + (ry - 2) * math.sin(a) * s
        tx, ty = cx + (rx + length) * math.cos(a) * s, hy - 5 * s + (ry + length - 2) * math.sin(a) * s
        nx, ny = -math.sin(a) * 4.5 * s, math.cos(a) * 4.5 * s
        spikes |= px.poly(w, h, [(bx - nx, by - ny), (tx, ty), (bx + nx, by + ny)])
    back = (blob | spikes) & (px.grid(w, h)[1] < hy + 4 * s)
    fig.part(back, "ink", tones=(1, 2, 3), cuts=(0.48, 0.9), flat=0.25)
    face_part(fig, face, "skin")
    # fringe: jagged spikes over the forehead, one hanging over his right eye
    xx, yy = px.grid(w, h)
    fringe = face & (yy < hy - 5 * s)
    for i, (bx_, tip) in enumerate(((-15, 2), (-9, 5), (-3, 3), (3, 6), (9, 4), (14, 1))):
        x0 = cx + bx_ * s
        fringe |= px.poly(w, h, [(x0 - 3.5 * s, hy - 6 * s), (x0 + tip * 0.3, hy - 5 * s + tip * s), (x0 + 3.5 * s, hy - 6 * s)])
    fringe &= px.ellipse(w, h, cx, hy, rx + 2, ry + 3)
    fig.part(fringe, "ink", tones=(1, 2, 3), cuts=(0.48, 0.9), flat=0.25)
    hair_shadow(fig, fringe, face, "skin", 1)
    # violet sheen
    for i in range(6):
        fig.put(cx + 4 * s + i * 2, hy - ry + 1 + (i % 2), px.C("purple", 3))
    ex = int(round(8.5 * s))
    ey = int(hy + 2 * s)
    brows(fig, cx, ex, ey - 5, px.C("ink", 1), {"sad": "worried", "worried": "worried", "happy": "calm"}.get(expr, "flat"), 5)
    bag = px.C("purple", 3)
    for x0 in (cx - ex - 2, cx + ex - 2):
        if expr == "happy":
            fg.eye_happy(fig, x0, ey + 2, 5)
        elif expr == "blink":
            fg.eye_closed(fig, x0, ey + 2, 5)
        elif expr == "worried":
            fg.eye_squeeze(fig, x0 + 1, ey, x0 < cx)
        else:
            # half-lidded: flat lid on top, iris below
            fig.pixels([(x0 + i, ey) for i in range(5)], px.C("ink", 0))
            fig.pixels([(x0 + 1, ey + 1), (x0 + 2, ey + 1), (x0 + 3, ey + 1), (x0 + 1, ey + 2), (x0 + 2, ey + 2), (x0 + 3, ey + 2)], px.C("ink", 1))
            fig.put(x0 + 3, ey + 1, px.C("purple", 4))
        fig.pixels([(x0, ey + 4), (x0 + 1, ey + 4), (x0 + 2, ey + 4), (x0 + 3, ey + 4)], bag)
    my = int(hy + 12 * s)
    kind = {"idle": "flat", "blink": "flat", "talk": "flat", "talk_open": "o", "happy": "smirk",
            "sad": "frown", "neutral": "flat", "worried": "wobble"}[expr]
    fg.mouth(fig, cx, my, kind)
    if expr == "sad":
        for x in range(cx - 8, cx + 9, 4):
            fig.solid(px.box(w, h, x, int(hy - 4 * s), x, int(hy + 0 * s)), px.C("blue", 2))


def ren_body(expr: str = "idle") -> px.Canvas:
    fig = Figure(W, H)
    cx = 48
    oy = 3 + (3 if expr == "sad" else 0)
    hy = 35 + oy
    s0 = hy + 22
    shoes_and_legs(fig, cx, 118, "blue", (0, 1, 2), "rice", gap=5, leg_w=4.0, shoe_w=6)
    # hoodie with the hood bunched behind the neck
    hood = px.ellipse(W, H, cx, s0 + 1, 15, 6)
    cloth(fig, hood, "purple", tones=(1, 2, 3))
    torso = torso_mask(s0, (35, 61), (28, 68), 120, 7.5)
    cloth(fig, torso, "purple", tones=(2, 3, 4), cuts=(0.38, 0.9))
    fig.solid(torso & px.box(W, H, 0, 114, W, 117), px.C("purple", 2))
    # kangaroo pocket (below the counter) and a little GG print
    fig.solid(px.rrect(W, H, 36, 100, 60, 112, 3) & torso & ~px.rrect(W, H, 37, 101, 59, 112, 2), px.C("purple", 2))
    gg = px.Canvas(1, 1)
    del gg
    for (x, y) in ((42, s0 + 21), (43, s0 + 20), (44, s0 + 20), (42, s0 + 22), (42, s0 + 23), (43, s0 + 24), (44, s0 + 24), (44, s0 + 23),
                   (48, s0 + 21), (49, s0 + 20), (50, s0 + 20), (48, s0 + 22), (48, s0 + 23), (49, s0 + 24), (50, s0 + 24), (50, s0 + 23)):
        fig.put(x, y, px.C("teal", 4))
    # drawstrings
    for sx in (-1, 1):
        fig.solid(px.box(W, H, cx + sx * 4, s0 + 3, cx + sx * 4, s0 + 13), px.C("rice", 4))
        fig.put(cx + sx * 4, s0 + 14, px.C("rice", 2))
    # arms
    pose_l = {"happy": "up", "sad": "down"}.get(expr, "counter")
    pose_r = "down" if expr == "sad" else "counter"
    for sx, pose in ((-1, pose_r), (1, pose_l)):
        pts = arm_pts(cx, s0, sx, pose)
        sleeve(fig, pts, "purple", r=4.8, tones=(1, 2, 3), sx=sx)
        ex_, ey_ = pts[-1]
        if pose == "counter":
            hand(fig, ex_, ey_ + 1, "skin")
        elif pose == "up":
            # peace sign: GG
            hand(fig, ex_, ey_ - 2, "skin", 3.8, 3.6)
            fig.solid(px.box(W, H, int(ex_) - 2, int(ey_) - 10, int(ex_) - 1, int(ey_) - 5) | px.box(W, H, int(ex_) + 1, int(ey_) - 10, int(ex_) + 2, int(ey_) - 5), px.C("skin", 3))
            fig.solid(px.box(W, H, int(ex_) - 3, int(ey_) - 10, int(ex_) - 3, int(ey_) - 5) | px.box(W, H, int(ex_) + 3, int(ey_) - 10, int(ex_) + 3, int(ey_) - 5), px.C("skin", 1))
    # energy drink can in his right hand (on the counter) while waiting
    if expr in ("idle", "blink", "talk", "talk_open"):
        can = px.rrect(W, H, 26, s0 + 17, 33, s0 + 29, 1.5)
        fig.part(can, "teal", tones=(2, 3, 4), cuts=(0.4, 0.82), style="dome", flat=0.3)
        fig.solid(px.box(W, H, 26, s0 + 17, 33, s0 + 18), px.C("gray", 3))
        bolt = [(30, s0 + 20), (29, s0 + 21), (28, s0 + 22), (29, s0 + 22), (30, s0 + 22), (29, s0 + 23), (28, s0 + 24)]
        fig.pixels(bolt, px.C("gold", 4))
        hand(fig, 32, s0 + 27, "skin", 3.6, 3.4)
    # headphones resting round the neck
    band = px.ellipse(W, H, cx, s0 - 1, 15, 8) & ~px.ellipse(W, H, cx, s0 - 2, 12.5, 6.5) & px.box(W, H, 0, s0 - 1, W, H)
    fig.part(band, "ink", tones=(1, 2), cuts=(0.6,))
    for sx in (-1, 1):
        cup = px.ellipse(W, H, cx + sx * 14, s0 + 1, 4.6, 5.4)
        fig.part(cup, "ink", tones=(1, 2, 3), cuts=(0.42, 0.85))
        fig.solid(px.ellipse(W, H, cx + sx * 14, s0 + 1, 2.4, 3.2) & cup, px.C("purple", 2))
        fig.put(cx + sx * 14 + 1, s0 - 1, px.C("teal", 5))
    ren_head(fig, cx, hy, expr)
    return fig.finish()


def ren_face(expr: str = "neutral") -> px.Canvas:
    fig = Figure(FW, FH)
    fig.part(px.ellipse(FW, FH, 24, 53, 21, 10), "purple", tones=(2, 3, 4), cuts=(0.45, 0.85))
    band = px.ellipse(FW, FH, 24, 44, 13, 6) & ~px.ellipse(FW, FH, 24, 43, 10.5, 4.5) & px.box(FW, FH, 0, 44, FW, FH)
    fig.part(band, "ink", tones=(1, 2), cuts=(0.6,))
    ren_head(fig, 24, 25, expr, rx=16.5, ry=16.5, small=True)
    return fig.finish()


# --------------------------------------------------------------------------------------------------
# Madame Reiko Kanemochi: grand. A giant plum hat with a pink plume, coiffed curls, pearls, white fur
# stole, long white gloves. Bijou, her very large fluffy dog, sits beside her (and sat on her phone).
# --------------------------------------------------------------------------------------------------

def madame_head(fig: Figure, cx: int, hy: int, expr: str, rx: float = 19.5, ry: float = 20.5, small: bool = False,
                lorgnette: bool = False) -> None:
    w, h = fig.w, fig.h
    s = rx / 19.5
    # coiffed curls framing the face
    curls_m = curls(w, h, cx, hy - 2 * s, rx + 2 * s, ry - 2 * s, 5 * s, 150, 390, 11)
    fig.part(curls_m, "plum", tones=(1, 2, 3), cuts=(0.45, 0.85), flat=0.2)
    face = px.ellipse(w, h, cx, hy + 1, rx, ry)
    face_part(fig, face, "skin")
    # hat: crown + wide brim + band + plume
    crown = px.ellipse(w, h, cx, hy - ry + 1 * s, 15 * s, 11 * s) & (px.grid(w, h)[1] < hy - ry + 6 * s)
    brim = px.ellipse(w, h, cx, hy - ry + 6 * s, 31 * s if not small else 23, 6.5 * s)
    fig.part(brim, "purple", tones=(1, 2, 3), cuts=(0.42, 0.88), flat=0.3)
    fig.part(crown, "purple", tones=(2, 3, 4), cuts=(0.42, 0.85))
    band = crown & px.box(w, h, 0, int(hy - ry + 2 * s), w, int(hy - ry + 4 * s))
    fig.solid(band, px.C("gold", 3))
    fig.solid(band & px.edge(band, 0, 1, 1), px.C("gold", 2))
    plume = px.ellipse(w, h, cx + 12 * s, hy - ry - 6 * s, 6 * s, 9 * s) | px.ellipse(w, h, cx + 16 * s, hy - ry - 11 * s, 4 * s, 6 * s)
    fig.part(plume & (px.grid(w, h)[1] >= 0), "pink", tones=(2, 3, 4), cuts=(0.4, 0.85))
    for i in range(5):
        fig.put(cx + 11 * s + i, hy - ry - 4 * s - i * 2, px.C("pink", 1))
    fig.part(px.ellipse(w, h, cx + 9 * s, hy - ry + 3 * s, 3.2 * s, 3.2 * s), "rice", tones=(3, 4, 5), cuts=(0.4, 0.8))
    # make-up and features
    ex = int(round(8 * s))
    ey = int(hy + 3 * s)
    brows(fig, cx, ex, ey - 5, px.C("plum", 1), {"sad": "raised", "worried": "worried"}.get(expr, "calm"), 5)
    for x0 in (cx - ex - 2, cx + ex - 2):
        if expr == "happy":
            fg.eye_happy(fig, x0, ey + 1, 5)
            fig.put(x0 - 1, ey + 1, px.C("ink", 1))
        elif expr == "blink":
            fg.eye_closed(fig, x0, ey + 1, 5)
            fig.put(x0 - 1, ey + 2, px.C("ink", 1))
        elif expr == "worried":
            fg.eye_squeeze(fig, x0 + 1, ey - 1, x0 < cx)
        else:
            # heavy lid with eyeshadow, a proper iris below it, lashes flicked out to the side
            fig.pixels([(x0 + i, ey - 2) for i in range(5)], px.C("purple", 3))
            fig.pixels([(x0 + i, ey - 1) for i in range(5)], px.C("ink", 0))
            fig.pixels([(x0 + 1, ey), (x0 + 2, ey), (x0 + 3, ey), (x0 + 1, ey + 1), (x0 + 2, ey + 1), (x0 + 3, ey + 1)], px.C("plum", 1))
            fig.pixels([(x0, ey), (x0 + 4, ey)], fg.WHITE)
            fig.put(x0 + 3, ey, fg.WHITE)
            lx = x0 - 1 if x0 < cx else x0 + 5
            fig.pixels([(lx, ey - 1), (lx + (-1 if x0 < cx else 1), ey - 2)], px.C("ink", 0))
    fig.put(cx + ex + 4, ey + 7, px.C("ink", 1))           # beauty mark
    fg.blush(fig, cx - ex - 5, ey + 7, 4, px.C("pink", 2))
    fg.blush(fig, cx + ex + 1, ey + 7, 4, px.C("pink", 2))
    my = int(hy + 12 * s)
    lip = px.C("coral", 2)
    if expr in ("talk_open",):
        fg.mouth(fig, cx, my - 1, "o")
        fig.pixels([(cx - 1, my - 1), (cx, my - 1)], lip)
    elif expr == "happy":
        fig.pixels([(cx - 3, my), (cx - 2, my + 1), (cx - 1, my + 1), (cx, my + 1), (cx + 1, my + 1), (cx + 2, my)], lip)
        fig.pixels([(cx - 2, my), (cx - 1, my), (cx, my), (cx + 1, my)], px.C("coral", 3))
    elif expr in ("sad", "worried"):
        fig.pixels([(cx - 2, my + 1), (cx - 1, my), (cx, my), (cx + 1, my), (cx + 2, my + 1)], lip)
    else:
        fig.pixels([(cx - 2, my), (cx - 1, my + 1), (cx, my + 1), (cx + 1, my + 1), (cx + 2, my)], lip)
        fig.pixels([(cx - 1, my), (cx, my)], px.C("coral", 3))
    # pearl earrings
    for sx in (-1, 1):
        fig.part(px.ellipse(w, h, cx + sx * (rx - 0.5), hy + 9 * s, 1.6, 1.6), "rice", tones=(4, 5), cuts=(0.6,))
    if lorgnette:
        lx, ly = cx + ex, ey + 1
        ring = px.ellipse(w, h, lx, ly, 5.2, 5.2) & ~px.ellipse(w, h, lx, ly, 4.1, 4.1)
        inside = px.ellipse(w, h, lx, ly, 4.1, 4.1)
        fig.cv.a[inside] = (fig.cv.a[inside].astype(np.int32) * 2 + np.array(px.C("rice", 5), np.int32)) // 3
        fig.pixels([(lx - 2, ly - 1), (lx - 1, ly - 1), (lx, ly - 1), (lx + 1, ly - 1), (lx + 2, ly - 1),
                    (lx - 1, ly), (lx, ly), (lx + 1, ly), (lx - 1, ly + 1), (lx, ly + 1), (lx + 1, ly + 1)], px.C("ink", 1))
        fig.put(lx + 1, ly, fg.WHITE)
        fig.solid(ring, px.C("gold", 2))
        fig.solid(px.capsule(w, h, lx + 4, ly + 4, lx + 9, ly + 15, 0.8), px.C("gold", 2))


def bijou(fig: Figure, x: int, y: int, expr: str) -> None:
    """Bijou: a very large, very fluffy white dog sitting; (x, y) = centre of the head."""
    # sitting: big haunch behind, fluffy chest, two front legs with paws on the floor
    haunch = px.ellipse(W, H, x + 4, y + 64, 15, 17)
    chest = px.ellipse(W, H, x + 1, y + 34, 13, 22) | curls(W, H, x + 1, y + 34, 12, 20, 3.4, 90, 270, 9)
    fig.part(haunch | curls(W, H, x + 4, y + 64, 14, 16, 3.2, 120, 300, 8), "rice", tones=(2, 3, 4), cuts=(0.42, 0.85), flat=0.25)
    fig.part(chest, "rice", tones=(2, 3, 4), cuts=(0.4, 0.84), flat=0.25)
    for lx in (x - 5, x + 5):
        leg = px.capsule(W, H, lx, y + 46, lx, y + 77, 3.6)
        fig.part(leg, "rice", tones=(2, 3, 4), cuts=(0.42, 0.84), flat=0.3)
        fig.part(px.ellipse(W, H, lx, y + 80, 4.6, 3.2) & px.box(W, H, 0, 0, W, H - 2), "rice", tones=(2, 3, 4), cuts=(0.4, 0.84))
        for t in (-1, 1):
            fig.put(lx + t * 2, y + 81, px.C("rice", 1))
    head = px.ellipse(W, H, x, y, 13, 11.5) | curls(W, H, x, y, 12, 10.5, 3.6, 0, 360, 18)
    fig.part(head, "rice", tones=(2, 3, 4), cuts=(0.4, 0.84), flat=0.2)
    for sx in (-1, 1):
        ear = px.ellipse(W, H, x + sx * 12, y + 2, 4.5, 8)
        fig.part(ear, "rice", tones=(1, 2, 3), cuts=(0.45, 0.85))
    fig.part(px.ellipse(W, H, x + 1, y - 9, 3.5, 2.4) | px.ellipse(W, H, x + 6, y - 9, 3.5, 2.4), "pink", tones=(2, 3, 4), cuts=(0.4, 0.85))
    fig.put(x + 3, y - 9, px.C("pink", 1))
    for ex in (x - 5, x + 4):
        if expr == "blink":
            fig.pixels([(ex, y + 1), (ex + 1, y + 1)], px.C("ink", 0))
        else:
            fig.pixels([(ex, y), (ex + 1, y), (ex, y + 1), (ex + 1, y + 1)], px.C("ink", 0))
            fig.put(ex + 1, y, fg.WHITE)
    fig.part(px.ellipse(W, H, x, y + 5.5, 2.4, 1.8), "ink", tones=(0, 1), cuts=(0.6,))
    if expr in ("happy", "talk_open"):
        fig.part(px.ellipse(W, H, x, y + 9.5, 2, 2.6), "coral", tones=(2, 3), cuts=(0.5,))


def madame_body(expr: str = "idle") -> px.Canvas:
    fig = Figure(W, H)
    cx = 54
    oy = 4 + (1 if expr == "sad" else 0)
    hy = 35 + oy
    s0 = hy + 22
    # long dress and heels
    shoes_and_legs(fig, cx, 136, "skin", (2, 3, 4), "purple", gap=3, leg_w=2.2, shoe_w=4.5)
    dress = px.poly(W, H, [(cx - 15, 100), (cx + 15, 100), (cx + 20, 146), (cx - 20, 146)])
    cloth(fig, dress, "purple", tones=(1, 2, 3), cuts=(0.38, 0.9))
    torso = torso_mask(s0, (cx - 12, cx + 12), (cx - 17, cx + 17), 112, 7)
    cloth(fig, torso, "purple", tones=(1, 2, 3), cuts=(0.38, 0.9))
    fig.solid(torso & px.box(W, H, 0, 106, W, 107), px.C("gold", 3))
    # long white gloves
    pose = {"happy": "cheer", "sad": "down"}.get(expr, "counter")
    for sx in (-1, 1):
        pts = arm_pts(cx, s0, sx, pose)
        pts = [(p[0] - (cx - 48) * 0 + 0, p[1]) for p in pts]
        arm = px.stroke(W, H, pts, 3.6)
        cloth(fig, arm, "rice", tones=(3, 4, 5), cuts=(0.4, 0.86), flat=0.3)
        ex_, ey_ = pts[-1]
        if pose != "down":
            hand(fig, ex_, ey_ + (1 if pose == "counter" else -1), "rice", 4.2, 3.6, (3, 4, 5))
            if pose == "counter":
                fig.part(px.ellipse(W, H, ex_ + sx * -1, ey_ - 2, 1.6, 1.6), "gold", tones=(2, 4), cuts=(0.6,))
    # fur stole over the shoulders
    stole = px.ellipse(W, H, cx, s0 + 4, 22, 7.5) | curls(W, H, cx, s0 + 4, 21, 6.5, 3.4, 0, 360, 24)
    stole &= ~px.ellipse(W, H, cx, s0 - 3, 9, 6)
    fig.part(stole, "rice", tones=(2, 4, 5), cuts=(0.35, 0.8), flat=0.2)
    # pearl necklace, below the stole on the dress
    for i in range(-4, 5):
        x = cx + i * 2.4
        y = s0 + 11 + (i * i) * 0.22
        fig.put(x, y, px.C("rice", 5))
        fig.put(x, y + 1, px.C("rice", 2))
    fig.part(px.ellipse(W, H, cx, s0 + 16.5, 2.0, 2.0), "rice", tones=(3, 5), cuts=(0.55,))
    madame_head(fig, cx, hy, expr)
    bijou(fig, 17, 76, expr)
    return fig.finish()


def madame_face(expr: str = "neutral") -> px.Canvas:
    fig = Figure(FW, FH)
    stole = px.ellipse(FW, FH, 24, 51, 20, 8) | curls(FW, FH, 24, 51, 19, 7, 3, 180, 360, 12)
    fig.part(stole, "rice", tones=(2, 4, 5), cuts=(0.35, 0.8), flat=0.2)
    madame_head(fig, 24, 30, expr, rx=14.5, ry=14.5, small=True, lorgnette=(expr == "neutral"))
    return fig.finish()


# --------------------------------------------------------------------------------------------------
# Registry
# --------------------------------------------------------------------------------------------------

BODY_FRAMES = {
    "idle_0": "idle", "idle_1": "blink", "talk_0": "talk", "talk_1": "talk_open", "happy": "happy", "sad": "sad",
}
FACE_FRAMES = ("neutral", "happy", "worried")

CAST = {
    "fumi": (fumi_body, fumi_face),
    "sasaki": (sasaki_body, sasaki_face),
    "mio": (mio_body, mio_face),
    "ren": (ren_body, ren_face),
    "madame_k": (madame_body, madame_face),
}


def preview_strip(cid: str, scale: int = 4) -> px.Canvas:
    """All frames side by side with a counter line, for review."""
    body, face = CAST[cid]
    frames = [body(e) for e in BODY_FRAMES.values()]
    faces = [face(e) for e in FACE_FRAMES]
    gap = 6
    out = px.Canvas(len(frames) * (W + gap) + 3 * (FW + gap) + gap, H + 2 * gap, px.C("rice", 3))
    for i, fr in enumerate(frames):
        x = gap + i * (W + gap)
        out.blit(fr, x, gap)
        out.rect(x, gap + COUNTER_ROW, x + W - 1, gap + COUNTER_ROW, px.C("coral", 2))
    for j, fc in enumerate(faces):
        x = gap + len(frames) * (W + gap) + j * (FW + gap)
        out.rect(x - 2, gap - 2, x + FW + 1, gap + FH + 1, px.C("rice", 1))
        out.rect(x, gap, x + FW - 1, gap + FH - 1, px.C("rice", 4))
        out.blit(fc, x, gap)
    return out

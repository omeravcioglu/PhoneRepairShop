"""
bench: the repair view, the counter top seen from above (world y -360..0, image row = -world y).

The shop used to be a noodle counter and the repair person kept the kitchen things: a butcher-block
top, a big slate serving board as the phone mat (chalk shows where things go, the phone has its own
chalk outline), a red lacquer bento box for screws, sage felt with sashiko stitching for parts, an
indigo sashiko tool roll, tea and chopsticks.

Where the game puts things on this image (all from the contract anchors):
    phone            centre (232,200), 104x200            on the slate (x 146..433)
    big spots        (366,200) and (372,206), 100x196     on the slate, right of the phone
    medium spots     (484,168) (572,168), 84x100          on the felt
    small spots      (484,256) (572,256), 60x50           on the felt
    screw tray       compartments 40x28 at x 484/540/596, rows 34/70 (the bento's wells)
    tool rack        x 8..56, rows 80..288; slots 36x36 centred at x 32, rows 100/142/184/226
    spares box       96x48 centred at (540,320)
    calm zones       ticket (8..197, 8..51), portrait (200..251, 6..57) and its name label,
                     buttons around rows 316..336, status plate (5..450, 338..353)

Surfaces under parts are mid-dark and cool (slate, felt, black lacquer) so the game's orange hover
outline and the pink phone both read.
"""

from __future__ import annotations

import math

import numpy as np

import px
import pxfont as F
from figure import Figure

W, H = 640, 360
SLATE = (146, 62, 433, 310)              # phone mat, inclusive
FELT = (440, 104, 621, 291)              # parts cloth
TRAY = (456, 12, 623, 91)                # bento box
RACK = (8, 80, 56, 288)                  # tool roll
SLOTS = [(x, y) for y in (34, 70) for x in (484, 540, 596)]
SPOTS = [(484, 168, 84, 100), (572, 168, 84, 100), (484, 256, 60, 50), (572, 256, 60, 50)]
CALM = [(8, 8, 197, 51), (200, 6, 251, 57), (256, 24, 372, 40), (168, 314, 476, 337), (5, 337, 451, 354)]


def C(n, i, a=255):
    return px.C(n, i, a)


def lit_edges(mask, w=1):
    return mask & (px.edge(mask, 0, -1, w) | px.edge(mask, 1, 0, w))


def dark_edges(mask, w=1):
    return mask & (px.edge(mask, 0, 1, w) | px.edge(mask, -1, 0, w))


def drop_shadow(cv: px.Canvas, mask: np.ndarray, colour, dx: int = -2, dy: int = 2) -> None:
    """Hard shadow cast down-left (the light comes from the window, upper right)."""
    sh = np.zeros_like(mask)
    for k in range(1, max(abs(dx), abs(dy)) + 1):
        sh |= px.shift(mask, int(np.sign(dx)) * min(k, abs(dx)), int(np.sign(dy)) * min(k, abs(dy)))
    cv.fill(sh & ~mask, colour)


def calm_mask() -> np.ndarray:
    m = px.empty(W, H)
    for x0, y0, x1, y1 in CALM:
        m |= px.box(W, H, x0, y0, x1, y1)
    return m


def segments(pts) -> np.ndarray:
    m = px.empty(W, H)
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        for x, y in px.bresenham(int(round(ax)), int(round(ay)), int(round(bx)), int(round(by))):
            if 0 <= x < W and 0 <= y < H:
                m[y, x] = True
    return m


def rrect_path(x0, y0, x1, y1, r, step=5.0, wobble=0.0, seed=0):
    """Points round a rounded rectangle (clockwise from the top-left), optionally hand-drawn."""
    cx0, cy0, cx1, cy1 = x0 + r, y0 + r, x1 - r, y1 - r
    pts = []
    for cx, cy, a0 in ((cx1, cy0, -90), (cx1, cy1, 0), (cx0, cy1, 90), (cx0, cy0, 180)):
        n = max(2, int(r * 1.6))
        pts += [(cx + r * math.cos(math.radians(a0 + 90 * i / n)), cy + r * math.sin(math.radians(a0 + 90 * i / n)))
                for i in range(n + 1)]
    pts.append(pts[0])
    # resample evenly, then nudge each point across the line
    out = [pts[0]]
    carry = 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        seg = math.hypot(bx - ax, by - ay)
        t = step - carry
        while t <= seg:
            out.append((ax + (bx - ax) * t / seg, ay + (by - ay) * t / seg))
            t += step
        carry = seg - (t - step)
    if wobble:
        rng = px.rng(seed)
        drift = 0.0
        for i in range(1, len(out)):
            drift = max(-wobble, min(wobble, drift + rng.uniform(-0.5, 0.5)))
            (px_, py_), (qx, qy) = out[i - 1], out[i]
            dx, dy = qx - px_, qy - py_
            ll = math.hypot(dx, dy) or 1
            out[i] = (qx - dy / ll * drift, qy + dx / ll * drift)
        out[-1] = out[0]
    return out


def chalk(cv: px.Canvas, mask: np.ndarray, seed: int, density: float = 0.8) -> None:
    """Chalk on slate: bright with grain, a share of pixels dropped, a few dim ones."""
    r = np.random.default_rng(seed).random(mask.shape)
    cv.fill(mask & (r < density), C("rice", 4))
    cv.fill(mask & (r >= density) & (r < density + 0.12), C("gray", 3))


def chalk_text(cv: px.Canvas, x: int, y: int, s: str, seed: int, center: bool = False) -> None:
    tmp = px.Canvas(W, H)
    if center:
        F.micro_center(tmp, x, y, s, C("rice", 4))
    else:
        F.micro(tmp, x, y, s, C("rice", 4))
    chalk(cv, tmp.alpha_mask(), seed, 0.88)


# --------------------------------------------------------------------------------------------------
# The counter top
# --------------------------------------------------------------------------------------------------

def butcher_block(cv: px.Canvas) -> None:
    """Oak strips glued edge to edge, running left-right; finger joints where strips meet end to end."""
    cv.rect(0, 0, W - 1, H - 1, C("wood", 4))
    rng = px.rng(31)
    calm = calm_mask()
    y = 0
    while y < H:
        h = rng.choice([11, 12, 13, 14, 15])
        y0, y1 = y, min(y + h - 1, H - 1)
        y += h
        kind = rng.random()
        # character of the strip: darker strips carry denser dark grain, light ones a pale streak
        n_dark = 3 if kind < 0.35 else 1
        n_light = 2 if kind > 0.7 else 1
        for tone, n in ((3, n_dark), (5, n_light)):
            for _ in range(n):
                gy = rng.uniform(y0 + 2, y1 - 2)
                amp = rng.uniform(0.3, 1.2)
                per = rng.uniform(90, 220)
                ph = rng.uniform(0, 6.28)
                x = rng.randint(-60, 80)
                while x < W:
                    run = rng.randint(20, 90) if tone == 3 else rng.randint(10, 40)
                    for k in range(run):
                        xx = x + k
                        yy = int(round(gy + amp * math.sin(xx / per * 6.28 + ph)))
                        if 0 <= xx < W and y0 < yy < y1:
                            cv.put(xx, yy, C("wood", tone))
                    x += run + rng.randint(8, 50)
        # glue line along the top of the strip
        if y0 > 0:
            cv.hline(0, W - 1, y0, C("wood", 3))
        # finger joints: a short zigzag across the strip
        for _ in range(rng.choice([0, 1, 1, 2])):
            jx = rng.randint(20, W - 20)
            for yy in range(y0 + 1, y1 + 1):
                cv.put(jx + (0 if (yy // 2) % 2 == 0 else 2), yy, C("wood", 2))
        # the odd knot, never under the UI
        if rng.random() < 0.18:
            kx, ky = rng.randint(30, W - 30), (y0 + y1) // 2
            if not calm[max(ky - 3, 0):ky + 4, max(kx - 8, 0):kx + 9].any():
                knot = px.ellipse(W, H, kx + 0.5, ky + 0.5, 4.5, 1.8)
                cv.fill(px.inner(knot), C("wood", 2))
                cv.fill(px.ellipse(W, H, kx + 0.5, ky + 0.5, 1.6, 0.8), C("wood", 1))
    # light knife and tool scratches
    for _ in range(16):
        sx, sy = rng.randint(4, W - 8), rng.randint(4, H - 8)
        if calm[max(sy - 2, 0):sy + 6, max(sx - 2, 0):sx + 8].any():
            continue
        n = rng.randint(3, 6)
        for k in range(n):
            cv.put(sx + k, sy + k // 2, C("wood", 5))


def burn_mark(cv: px.Canvas, x: int, y: int) -> None:
    """Where the soldering iron once rested, with two drops of solder."""
    iron = px.capsule(W, H, x - 7, y + 3, x + 6, y - 3, 2.4)
    halo = px.capsule(W, H, x - 7, y + 3, x + 6, y - 3, 4.2) & ~iron
    cv.fill(halo & px.checker(W, H), C("wood", 2))
    cv.fill(iron, C("wood", 1))
    cv.fill(px.capsule(W, H, x - 5, y + 2, x + 4, y - 2, 0.8), C("wood", 0))
    for sx, sy in ((x + 10, y + 4), (x + 14, y + 1)):
        cv.put(sx, sy, C("silver", 3))
        cv.put(sx + 1, sy, C("silver", 1))
        cv.put(sx, sy - 1, C("silver", 4))


# --------------------------------------------------------------------------------------------------
# Slate board (the phone mat)
# --------------------------------------------------------------------------------------------------

def slate(cv: px.Canvas) -> None:
    x0, y0, x1, y1 = SLATE
    rng = px.rng(41)
    slab = px.rrect(W, H, x0, y0, x1, y1, 4)
    # a natural edge: short runs of the outline nibbled back by one pixel, a few small chips
    edge = px.inner(slab)
    ys, xs = np.nonzero(edge)
    keep = np.ones(len(xs), bool)
    order = np.argsort(np.arctan2(ys - (y0 + y1) / 2, (xs - (x0 + x1) / 2) * (y1 - y0) / (x1 - x0)))
    i = 0
    while i < len(order):
        run = rng.randint(4, 14)
        if rng.random() < 0.45:
            keep[order[i:i + run]] = False
        i += run + rng.randint(3, 18)
    slab[ys[~keep], xs[~keep]] = False
    ring = np.argwhere(px.inner(slab))
    for _ in range(9):
        yy, xx = ring[rng.randrange(len(ring))]
        slab &= ~px.ellipse(W, H, xx + 0.5, yy + 0.5, 1.6, 1.2)
    drop_shadow(cv, slab, C("wood", 2), -4, 4)
    cv.fill(slab, C("gray", 1))
    inner = slab & ~px.inner(slab)
    # the stone's own layering: a few thin veins that wander a little
    for _ in range(7):
        vx, vy = rng.randint(x0 + 10, x1 - 50), rng.randint(y0 + 10, y1 - 10)
        for k in range(rng.randint(12, 44)):
            if rng.random() < 0.18:
                vy += rng.choice([-1, 1])
            if inner[vy, vx + k]:
                cv.put(vx + k, vy, C("gray", 0))
    # rim: the split edge catches the window light on top and right
    cv.fill(lit_edges(inner), C("gray", 3))
    cv.fill(dark_edges(inner, 2), C("gray", 0))
    cv.fill(px.inner(slab), C("ink", 0))

    # chalk: the phone's own outline, which side is up, where the cover and screen go
    phone_outline = segments(rrect_path(181, 101, 282, 298, 14, 4.0, 0.7, seed=5))
    chalk(cv, phone_outline, 51, 0.8)
    arrow = segments([(214, 92), (214, 82)]) | segments([(211, 85), (214, 82), (217, 85)])
    chalk(cv, arrow, 52, 0.9)
    chalk_text(cv, 221, 86, "TOP", 53)
    spot = segments(rrect_path(314, 100, 423, 305, 10, 4.0, 0.8, seed=6))
    chalk(cv, spot, 54, 0.72)
    chalk_text(cv, 368, 110, "COVER", 55, center=True)
    chalk_text(cv, 368, 117, "SCREEN", 56, center=True)
    # tally of phones fixed so far
    chalk_text(cv, 152, 246, "FIXED", 57)
    tally = px.empty(W, H)
    for g, (gx, gy, n) in enumerate(((153, 256, 5), (164, 256, 5), (153, 268, 3))):
        for k in range(min(n, 4)):
            tally |= segments([(gx + k * 2, gy), (gx + k * 2, gy + 7)])
        if n == 5:
            tally |= segments([(gx - 1, gy + 6), (gx + 7, gy + 1)])
    chalk(cv, tally, 58, 0.85)
    # the house joke: a rice bowl with a phone in it, crossed out
    doodle = segments([(402, 82), (425, 82)])
    doodle |= segments([(402 + 11.5 + 11.5 * math.cos(math.radians(a)), 82 + 8 * math.sin(math.radians(a)))
                        for a in range(0, 181, 15)])
    doodle |= segments([(404, 82), (406, 79), (409, 78), (412, 80)]) | segments([(417, 80), (420, 78), (423, 82)])
    doodle |= segments([(412, 81), (412, 71), (417, 71), (417, 81)])
    doodle |= segments([(398, 68), (429, 95)]) | segments([(429, 68), (398, 95)])
    chalk(cv, doodle, 59, 0.82)


# --------------------------------------------------------------------------------------------------
# Bento box (the screw tray)
# --------------------------------------------------------------------------------------------------

def bento(cv: px.Canvas) -> None:
    x0, y0, x1, y1 = TRAY
    box = px.rrect(W, H, x0, y0, x1, y1, 5)
    drop_shadow(cv, box, C("wood", 1), -3, 3)
    cv.fill(box, C("coral", 2))
    inner = box & ~px.inner(box)
    cv.fill(lit_edges(inner), C("coral", 3))
    cv.fill(dark_edges(inner), C("coral", 1))
    cv.fill(px.inner(box), C("coral", 0))
    # gold line painted round the rim
    gold = px.inner(px.rrect(W, H, x0 + 3, y0 + 3, x1 - 3, y1 - 3, 3))
    cv.fill(gold, C("gold", 3))
    cv.fill(gold & (px.edge(gold, 0, 1, 1) | px.edge(gold, -1, 0, 1)) & px.checker(W, H), C("gold", 2))
    # lacquer gloss on the rim
    for gx, gy in ((x1 - 9, y0 + 1), (x1 - 8, y0 + 1), (x1 - 12, y0 + 1), (x1 - 1, y0 + 9)):
        cv.put(gx, gy, C("rice", 5))
    # black lacquer wells
    for cx, cy in SLOTS:
        well = px.rrect(W, H, cx - 20, cy - 14, cx + 19, cy + 13, 3)
        cv.fill(px.outer(well) & box, C("coral", 1))
        cv.fill(well, C("ink", 1))
        cv.fill(lit_edges(well, 2), C("ink", 0))                  # floor shaded under the top and right walls
        cv.fill(dark_edges(well), C("ink", 2))                    # light bouncing off the lacquer
        for k in range(10):                                       # a soft window reflection
            if (k // 2) % 2 == 0:
                cv.put(cx + 6 + k, cy - 9 + k // 2, C("ink", 2))
    # a little gold crest on the front rim
    mon = [(540, 85), (539, 86), (541, 86), (540, 87), (538, 87), (542, 87), (540, 86)]
    cv.pixels(mon, C("gold", 3))
    cv.put(540, 86, C("gold", 4))


# --------------------------------------------------------------------------------------------------
# Sashiko felt (the parts cloth)
# --------------------------------------------------------------------------------------------------

def running_stitch(cv: px.Canvas, ring: np.ndarray, centre, colour, on: int = 3, off: int = 2) -> None:
    ys, xs = np.nonzero(ring)
    cx, cy = centre
    order = np.argsort(np.arctan2(ys - cy, xs - cx))
    for n, i in enumerate(order):
        if n % (on + off) < on:
            cv.put(xs[i], ys[i], colour)


def felt(cv: px.Canvas) -> None:
    x0, y0, x1, y1 = FELT
    cloth = px.rrect(W, H, x0, y0, x1, y1, 4)
    # the bottom-right corner is turned up
    fold = px.poly(W, H, [(x1 + 1, y1 - 15), (x1 + 1, y1 + 1), (x1 - 15, y1 + 1)])
    drop_shadow(cv, cloth & ~fold, C("wood", 2), -3, 3)
    flat = cloth & ~fold
    cv.fill(flat, C("sage", 2))
    rng = px.rng(61)
    for _ in range(120):
        fx, fy = rng.randint(x0 + 4, x1 - 6), rng.randint(y0 + 4, y1 - 4)
        tone = C("sage", 3) if rng.random() < 0.65 else C("sage", 1)
        dx = rng.choice([1, 1, 2])
        for k in range(rng.randint(2, 3)):
            if flat[fy + (k if dx == 2 else 0) // 2, fx + k]:
                cv.put(fx + k, fy + (k if dx == 2 else 0) // 2, tone)
    # indigo binding with a white running stitch
    binding = flat & ~px.rrect(W, H, x0 + 3, y0 + 3, x1 - 3, y1 - 3, 2)
    cv.fill(binding, C("blue", 0))
    cv.fill(lit_edges(binding) & px.inner(cloth), C("blue", 1))
    stitch_ring = px.inner(px.rrect(W, H, x0 + 1, y0 + 1, x1 - 1, y1 - 1, 3)) & flat
    running_stitch(cv, stitch_ring, ((x0 + x1) / 2, (y0 + y1) / 2), C("rice", 3), 2, 2)
    cv.fill(px.inner(cloth) & flat, C("ink", 1))
    # the flap shows the cloth's back, with its own shadow on the felt
    flap = px.poly(W, H, [(x1 - 15, y1 - 15), (x1 + 0.5, y1 - 15), (x1 - 15, y1 + 0.5)])
    cv.fill(px.shift(flap, -2, 2) & flat & ~flap, C("sage", 1))
    cv.fill(flap, C("sage", 3))
    cv.fill(dark_edges(flap), C("sage", 2))
    cv.fill(px.outer(flap) & ~flat, C("ink", 1))
    cv.fill(px.inner(flap) & lit_edges(flap), C("sage", 4))
    # sashiko outlines and stitched numbers where the parts go
    for n, (cx, cy, w, h) in enumerate(SPOTS):
        iw, ih = w - 8, h - 8
        ring = px.inner(px.rrect(W, H, cx - iw // 2, cy - ih // 2, cx - iw // 2 + iw - 1, cy - ih // 2 + ih - 1, 4))
        running_stitch(cv, ring, (cx, cy), C("rice", 4))
        F.micro(cv, cx - iw // 2 + 4, cy - ih // 2 + 4, str(n + 1), C("rice", 3))


# --------------------------------------------------------------------------------------------------
# Indigo tool roll
# --------------------------------------------------------------------------------------------------

def tool_roll(cv: px.Canvas) -> None:
    x0, y0, x1, y1 = RACK
    roll = px.rrect(W, H, x0, y0, x1, y1, 4)
    drop_shadow(cv, roll, C("wood", 2), -2, 2)
    cv.fill(roll, C("blue", 0))
    inner = roll & ~px.inner(roll)
    cv.fill(inner & px.bayer(W, H, 0.18) & px.checker(W, H, 1), C("ink", 2))          # dyed cotton
    cv.fill(lit_edges(inner), C("blue", 1))
    cv.fill(px.inner(roll), C("ink", 1))
    # a running stitch round the edge
    ring = px.inner(px.rrect(W, H, x0 + 2, y0 + 2, x1 - 2, y1 - 2, 3))
    running_stitch(cv, ring, ((x0 + x1) / 2, (y0 + y1) / 2), C("rice", 3), 2, 2)
    # pockets: shadowed mouth on top, stitched seam below
    for cy in (100, 142, 184, 226):
        pocket = px.rrect(W, H, x0 + 4, cy - 19, x1 - 4, cy + 19, 3)
        cv.fill(px.inner(pocket) & lit_edges(pocket), C("ink", 1))
        seam = px.inner(pocket) & dark_edges(pocket)
        cv.fill(seam & px.checker(W, H), C("rice", 3))
    # jujizashi: little stitched crosses on the bottom panel
    for row, yy in enumerate(range(250, 265, 6)):
        for xx in range(x0 + 7 + (row % 2) * 3, x1 - 5, 6):
            cv.pixels([(xx, yy), (xx - 1, yy), (xx + 1, yy), (xx, yy - 1), (xx, yy + 1)], C("rice", 4))
    # leather strap with a brass buckle and a mochi charm
    strap = px.rrect(W, H, x0 - 3, 271, 74, 279, 2)
    shade = (px.shift(strap, -1, 1) | px.shift(strap, -1, 2)) & ~strap
    cv.fill(shade & roll, C("ink", 1))
    cv.fill(shade & ~roll, C("wood", 2))
    cv.fill(strap, C("tan", 2))
    cv.fill(lit_edges(strap & ~px.inner(strap)), C("tan", 3))
    cv.fill(px.inner(strap), C("tan", 0))
    for xx in range(x0, 72, 3):
        cv.put(xx, 273, C("tan", 4))
    buckle = px.rrect(W, H, 58, 268, 66, 282, 2)
    cv.fill(buckle & ~px.rrect(W, H, 60, 270, 64, 280, 1), C("gold", 3))
    cv.fill(lit_edges(buckle & ~px.inner(buckle)), C("gold", 4))
    cv.fill(px.inner(buckle), C("gold", 1))
    cv.vline(62, 270, 280, C("gold", 2))
    cord = segments([(74, 276), (78, 279), (81, 278)])
    cv.fill(cord, C("pink", 2))
    mochi = px.ellipse(W, H, 87.5, 279, 6, 4.4)
    drop_shadow(cv, mochi, C("wood", 2), -1, 1)
    cv.fill(mochi, C("rice", 5))
    cv.fill(dark_edges(mochi), C("rice", 3))
    cv.fill(px.outer(mochi), C("rice", 0))
    cv.put(85, 278, C("ink", 1))
    cv.put(89, 278, C("ink", 1))
    cv.put(84, 280, C("pink", 3))
    cv.put(90, 280, C("pink", 3))
    cv.put(87, 280, C("ink", 2))


# --------------------------------------------------------------------------------------------------
# Small things on the counter
# --------------------------------------------------------------------------------------------------

def note(cv: px.Canvas, x0: int, y0: int) -> None:
    """A slightly crooked note with a strip of washi tape."""
    paper = px.poly(W, H, [(x0, y0 + 2), (x0 + 37, y0), (x0 + 39, y0 + 31), (x0 + 2, y0 + 33)])
    drop_shadow(cv, paper, C("wood", 2), -2, 2)
    cv.fill(paper, C("rice", 4))
    cv.fill(dark_edges(paper), C("rice", 2))
    F.micro_center(cv, x0 + 19, y0 + 11, "NO", C("coral", 2))
    F.micro_center(cv, x0 + 20, y0 + 18, "RICE!", C("coral", 2))
    cv.hline(x0 + 8, x0 + 31, y0 + 25, C("coral", 2))
    cv.hline(x0 + 10, x0 + 29, y0 + 27, C("coral", 2))
    tape = px.poly(W, H, [(x0 + 9, y0 - 3), (x0 + 30, y0 - 4), (x0 + 30.5, y0 + 4), (x0 + 9.5, y0 + 5)])
    cv.fill(tape, C("pink", 3))
    cv.fill(tape & px.bayer(W, H, 0.12), C("pink", 4))
    for tx in range(x0 + 12, x0 + 30, 4):
        cv.put(tx, y0, C("rice", 5))
    cv.fill(px.inner(tape) & dark_edges(tape), C("pink", 2))


def teacup(cv: px.Canvas, cx: int, cy: int) -> None:
    """A yunomi of green tea, from above, with a lucky tea stalk standing up in it."""
    cup = px.ellipse(W, H, cx + 0.5, cy + 0.5, 12, 12)
    drop_shadow(cv, cup, C("wood", 2), -3, 3)
    cv.fill(cup, C("rice", 4))
    cv.fill(dark_edges(cup, 2), C("rice", 2))
    cv.fill(px.inner(cup), C("blue", 1))
    cv.fill(px.inner(cup) & lit_edges(cup), C("blue", 2))
    tea = px.ellipse(W, H, cx + 0.5, cy + 0.5, 8.6, 8.6)
    cv.fill(tea, C("sage", 3))
    cv.fill(dark_edges(tea, 2), C("sage", 2))
    cv.fill(px.inner(tea), C("sage", 1))
    glint = px.ellipse(W, H, cx + 3.5, cy - 2.5, 3.2, 1.6) & tea & ~px.inner(tea)
    cv.fill(glint, C("sage", 4))
    cv.vline(cx - 1, cy, cy + 2, C("leaf", 1))
    cv.put(cx - 1, cy - 1, C("leaf", 3))


def chopsticks(cv: px.Canvas) -> None:
    """Chopsticks resting on a little fish-shaped hashioki."""
    fish = px.ellipse(W, H, 312.5, 54, 6, 2.6) | px.poly(W, H, [(318, 54), (322, 51), (322, 57)])
    drop_shadow(cv, fish, C("wood", 2), -1, 1)
    cv.fill(fish, C("rice", 5))
    cv.fill(dark_edges(fish), C("rice", 3))
    cv.fill(px.outer(fish), C("blue", 1))
    cv.put(309, 53, C("blue", 0))
    for (ax, ay), (bx, by) in (((302, 50), (377, 43)), ((303, 55), (378, 49))):
        stick = px.capsule(W, H, ax, ay, bx, by, 1.4) | px.capsule(W, H, (ax + bx) / 2, (ay + by) / 2, bx, by, 1.8)
        drop_shadow(cv, stick, C("wood", 2), -1, 2)
        cv.fill(stick, C("coral", 1))
        cv.fill(lit_edges(stick), C("coral", 3))
        tip = stick & px.box(W, H, 0, 0, ax + 11, H)
        cv.fill(tip, C("wood", 5))
        cv.fill(tip & dark_edges(stick), C("wood", 3))
        band = stick & px.box(W, H, bx - 14, 0, bx - 12, H)
        cv.fill(band, C("gold", 3))
        cv.fill(px.outer(stick) & ~stick & px.shift(stick, 0, 1), C("wood", 1))


def build_bench_bg() -> px.Canvas:
    cv = px.Canvas(W, H)
    butcher_block(cv)
    burn_mark(cv, 456, 303)
    teacup(cv, 104, 310)
    slate(cv)
    felt(cv)
    bento(cv)
    tool_roll(cv)
    chopsticks(cv)
    note(cv, 384, 9)
    # where the spares crate sits: its shadow on the wood
    crate = px.rrect(W, H, 494, 300, 585, 341, 2)
    cv.fill(px.shift(crate, -3, 3) & ~crate & ~px.rrect(W, H, *FELT, 4), C("wood", 2))
    cv.fill(crate, C("wood", 2))
    # two screws that got away
    for sx, sy in ((440, 40), (431, 52)):
        cv.rect(sx - 1, sy - 1, sx + 1, sy + 1, C("silver", 2))
        cv.put(sx, sy, C("ink", 1))
        cv.put(sx + 1, sy - 1, C("silver", 4))
        cv.put(sx - 2, sy + 1, C("wood", 2))
    return cv


# --------------------------------------------------------------------------------------------------

def spares_box() -> px.Canvas:
    """96x48: an old mikan crate full of new parts, seen from the front and above. Parts dive in at
    its top edge. 1 px clear border for the hover outline."""
    w, h = 96, 48
    cv = px.Canvas(w, h)
    # inside, with new parts peeking over the front slats
    inside = px.box(w, h, 7, 6, 88, 30)
    cv.fill(inside, C("wood", 1))
    cv.fill(px.box(w, h, 7, 6, 88, 8), C("wood", 0))
    cv.rect(13, 9, 38, 30, C("ink", 1))                     # a new screen, glass up
    cv.rect(15, 11, 36, 30, C("ink", 2))
    cv.line(19, 27, 29, 13, C("ink", 3))
    cv.rect(13, 9, 38, 9, C("ink", 3))
    cv.rect(42, 13, 60, 30, C("ink", 2))                    # a new battery
    cv.rect(44, 16, 58, 26, C("rice", 4))
    cv.rect(44, 16, 58, 17, C("pink", 3))
    cv.rect(50, 19, 52, 23, C("gold", 3))
    for k, (rx, ry) in enumerate(((71, 17), (74, 19))):     # a coil of flex
        ring = px.ellipse(w, h, rx + 0.5, ry + 0.5, 6, 4) & ~px.ellipse(w, h, rx + 0.5, ry + 0.5, 4, 2.4)
        cv.fill(ring, C("orange", 3 - k))
    cv.rect(66, 23, 85, 30, C("silver", 3))                 # a bag of screws
    cv.rect(66, 23, 85, 23, C("silver", 4))
    for x in (69, 73, 77, 81):
        cv.put(x, 27, C("silver", 1))
    # back rim and corner posts, light pine
    cv.rect(5, 2, 90, 5, C("tan", 3))
    cv.rect(5, 2, 90, 2, C("tan", 4))
    for px0 in (2, 89):
        post = px.box(w, h, px0, 2, px0 + 4, 45)
        cv.fill(post, C("tan", 2))
        cv.fill(px.box(w, h, px0, 2, px0 + 4, 3), C("tan", 3))
        cv.fill(px.box(w, h, px0 + 4, 4, px0 + 4, 45), C("tan", 1))
    # front slats with a gap between them
    for sy0, sy1 in ((30, 37), (40, 45)):
        slat = px.box(w, h, 7, sy0, 88, sy1)
        cv.fill(slat, C("tan", 3))
        cv.fill(px.box(w, h, 7, sy0, 88, sy0), C("tan", 4))
        cv.fill(px.box(w, h, 7, sy1, 88, sy1), C("tan", 2))
        for gx in range(10, 86, 9):
            cv.put(gx, sy0 + 3, C("tan", 2))
            cv.put(gx + 1, sy0 + 3, C("tan", 2))
    cv.rect(7, 38, 88, 39, C("wood", 0))
    # the old stamp, half worn off
    fruit = px.ellipse(w, h, 15.5, 33.5, 2.6, 2.6)
    cv.fill(fruit & (np.random.default_rng(71).random((h, w)) < 0.85), C("orange", 2))
    cv.put(16, 31, C("leaf", 2))
    F.micro(cv, 20, 32, "MIKAN", C("orange", 2))
    cv.put(25, 34, C("tan", 3))
    cv.put(34, 32, C("tan", 3))
    # the label that matters
    lab = px.box(w, h, 50, 31, 82, 37)
    cv.fill(lab, C("rice", 4))
    cv.fill(px.box(w, h, 50, 37, 82, 37), C("rice", 2))
    F.micro(cv, 54, 32, "SPARES", C("ink", 1))
    cv.rect(48, 30, 52, 31, C("pink", 3))
    cv.rect(80, 30, 84, 31, C("pink", 3))
    # outline everything
    alpha = cv.alpha_mask()
    cv.fill(px.inner(alpha), C("wood", 0))
    assert px.ensure_padding(cv), "spares box needs a clear border"
    return cv

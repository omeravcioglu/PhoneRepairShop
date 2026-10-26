"""
px: a small pixel-art toolkit for "Have You Tried Rice?".

Every sprite is drawn at its real size: 1 image pixel = 1 game pixel (the game runs at 640x360 and is
shown at 3x on 1080p). Coordinates are IMAGE coordinates: x to the right, y DOWN, origin top-left.

Building blocks:
    Canvas        an RGBA numpy image with put/rect/line/fill(mask)/blit/save
    masks         ellipse, rrect, poly, capsule, stroke, arc_pts  (boolean arrays, canvas sized)
    shading       dome() for round forms (cel bands from a light direction), edge() bands for flat forms
    outlines      outline() around a mask, with an optional lighter colour on the lit side
    palette       RAMPS (dark -> light) and C(name, i)

House style (see the Pixel rules board): light comes from the shop window, upper right; three tones per
material plus a rare highlight; outlines use the darkest tone of the object's own ramp; no mixels (never
scale pixels up inside the game image); dithering only for light and soft gradients.
"""

from __future__ import annotations

import math
import os
import random
from typing import Iterable, Sequence

import numpy as np
from PIL import Image
from scipy import ndimage

# --------------------------------------------------------------------------------------------------
# Palette: ramps from dark to light, hue-shifted (shadows lean violet/blue, lights lean warm).
# --------------------------------------------------------------------------------------------------

RAMPS: dict[str, list[str]] = {
    "ink":    ["#16141f", "#262840", "#3a4160", "#566186"],
    "blue":   ["#284a78", "#3a6aa6", "#5f95cf", "#98c2ec", "#d4ecfc"],
    "teal":   ["#143536", "#1f5453", "#2c7470", "#46998c", "#7dc4ad", "#c0ead6"],
    "leaf":   ["#1d3625", "#2c5532", "#457f3e", "#6fab55", "#a8d47e"],
    "wood":   ["#351e18", "#563223", "#7a4a30", "#a2683f", "#c88d57", "#e6b57e"],
    "rice":   ["#8b7660", "#b49e80", "#d6c3a3", "#ecdfc4", "#f9f2e3", "#ffffff"],
    "orange": ["#6b2d16", "#a5461e", "#d8682a", "#f19443", "#fbc477"],
    "coral":  ["#4d1a24", "#842a33", "#c04249", "#e56c64", "#f6a08f"],
    "pink":   ["#6b2a49", "#a6496e", "#d87699", "#f1a6be", "#fcd6e1"],
    "purple": ["#291c3c", "#443068", "#694a99", "#9477c3", "#c6b0eb"],
    "gold":   ["#5c3e12", "#94651e", "#cd982b", "#edc44d", "#fbe594"],
    "skin":   ["#7a4535", "#b06b51", "#db9877", "#f2c3a1", "#fde2cb"],
    "tan":    ["#5c3325", "#8b5237", "#b97851", "#dba278", "#f0c79c"],
    "gray":   ["#3b3e49", "#5e6472", "#8c93a2", "#bcc2cc", "#e5e8ed"],
    "sage":   ["#2b4034", "#46634f", "#688a70", "#93b293", "#c2d8bd"],
    "plum":   ["#2e1527", "#52253f", "#7a3a5c", "#a65a7c", "#cf8aa3"],
    "silver": ["#575c74", "#7f86a0", "#a9afc6", "#d2d6e6", "#f2f3fa"],
}

CLEAR = (0, 0, 0, 0)


def rgba(hexstr: str, a: int = 255) -> tuple[int, int, int, int]:
    s = hexstr.lstrip("#")
    return int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a


def C(name: str, i: int, a: int = 255) -> tuple[int, int, int, int]:
    """Colour i of ramp `name` (negative indices count from the light end)."""
    return rgba(RAMPS[name][i], a)


def ramp(name: str) -> list[tuple[int, int, int, int]]:
    return [rgba(h) for h in RAMPS[name]]


def all_colours() -> list[str]:
    return [h for r in RAMPS.values() for h in r]


# --------------------------------------------------------------------------------------------------
# Canvas
# --------------------------------------------------------------------------------------------------

class Canvas:
    def __init__(self, w: int, h: int, fill=CLEAR):
        self.a = np.zeros((h, w, 4), np.uint8)
        if fill != CLEAR:
            self.a[:, :] = fill

    @property
    def w(self) -> int:
        return self.a.shape[1]

    @property
    def h(self) -> int:
        return self.a.shape[0]

    # -- single pixels and simple shapes --------------------------------------------------------
    def put(self, x: int, y: int, c) -> None:
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.a[y, x] = c

    def get(self, x: int, y: int):
        return tuple(int(v) for v in self.a[y, x])

    def rect(self, x0: int, y0: int, x1: int, y1: int, c) -> None:
        """Filled rectangle, corners inclusive."""
        x0, x1 = sorted((x0, x1))
        y0, y1 = sorted((y0, y1))
        x0, y0 = max(x0, 0), max(y0, 0)
        x1, y1 = min(x1, self.w - 1), min(y1, self.h - 1)
        if x0 <= x1 and y0 <= y1:
            self.a[y0:y1 + 1, x0:x1 + 1] = c

    def frame(self, x0: int, y0: int, x1: int, y1: int, c) -> None:
        self.rect(x0, y0, x1, y0, c)
        self.rect(x0, y1, x1, y1, c)
        self.rect(x0, y0, x0, y1, c)
        self.rect(x1, y0, x1, y1, c)

    def hline(self, x0: int, x1: int, y: int, c) -> None:
        self.rect(x0, y, x1, y, c)

    def vline(self, x: int, y0: int, y1: int, c) -> None:
        self.rect(x, y0, x, y1, c)

    def line(self, x0: int, y0: int, x1: int, y1: int, c) -> None:
        for x, y in bresenham(x0, y0, x1, y1):
            self.put(x, y, c)

    def pixels(self, pts: Iterable[tuple[int, int]], c) -> None:
        for x, y in pts:
            self.put(x, y, c)

    # -- masks ---------------------------------------------------------------------------------
    def fill(self, mask: np.ndarray, c) -> None:
        self.a[mask] = c

    def fill_where(self, mask: np.ndarray, colours: np.ndarray) -> None:
        """colours: (h, w, 4) array; copy where mask."""
        self.a[mask] = colours[mask]

    def alpha_mask(self) -> np.ndarray:
        return self.a[:, :, 3] > 0

    # -- composition ---------------------------------------------------------------------------
    def blit(self, src: "Canvas", x: int, y: int, mask_only_opaque: bool = False) -> None:
        """Alpha-over src onto self with its top-left at (x, y)."""
        sx0, sy0 = max(0, -x), max(0, -y)
        dx0, dy0 = max(0, x), max(0, y)
        w = min(src.w - sx0, self.w - dx0)
        h = min(src.h - sy0, self.h - dy0)
        if w <= 0 or h <= 0:
            return
        s = src.a[sy0:sy0 + h, sx0:sx0 + w].astype(np.float32)
        d = self.a[dy0:dy0 + h, dx0:dx0 + w].astype(np.float32)
        sa = s[:, :, 3:4] / 255.0
        if mask_only_opaque:
            sa = (sa > 0).astype(np.float32)
        da = d[:, :, 3:4] / 255.0
        out_a = sa + da * (1 - sa)
        rgb = np.where(out_a > 0, (s[:, :, :3] * sa + d[:, :, :3] * da * (1 - sa)) / np.maximum(out_a, 1e-6), 0)
        self.a[dy0:dy0 + h, dx0:dx0 + w, :3] = np.clip(np.round(rgb), 0, 255).astype(np.uint8)
        self.a[dy0:dy0 + h, dx0:dx0 + w, 3] = np.clip(np.round(out_a[:, :, 0] * 255), 0, 255).astype(np.uint8)

    def copy(self) -> "Canvas":
        c = Canvas(self.w, self.h)
        c.a = self.a.copy()
        return c

    def crop(self, x0: int, y0: int, w: int, h: int) -> "Canvas":
        c = Canvas(w, h)
        c.a = self.a[y0:y0 + h, x0:x0 + w].copy()
        return c

    def flip_x(self) -> "Canvas":
        c = Canvas(self.w, self.h)
        c.a = self.a[:, ::-1].copy()
        return c

    def recolour(self, mapping: dict) -> "Canvas":
        """Swap exact colours (tuples) for others; used for palette variants."""
        c = self.copy()
        for src, dst in mapping.items():
            m = np.all(c.a == np.array(src, np.uint8), axis=2)
            c.a[m] = dst
        return c

    # -- output --------------------------------------------------------------------------------
    def image(self, scale: int = 1) -> Image.Image:
        im = Image.fromarray(self.a, "RGBA")
        if scale != 1:
            im = im.resize((self.w * scale, self.h * scale), Image.NEAREST)
        return im

    def save(self, path: str, scale: int = 1) -> str:
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        self.image(scale).save(path, optimize=True)
        return path

    @staticmethod
    def load(path: str) -> "Canvas":
        im = Image.open(path).convert("RGBA")
        c = Canvas(im.width, im.height)
        c.a = np.array(im, np.uint8)
        return c


# --------------------------------------------------------------------------------------------------
# Geometry helpers -> boolean masks the size of a canvas
# --------------------------------------------------------------------------------------------------

def grid(w: int, h: int) -> tuple[np.ndarray, np.ndarray]:
    yy, xx = np.mgrid[0:h, 0:w]
    return xx + 0.5, yy + 0.5          # pixel centres


def empty(w: int, h: int) -> np.ndarray:
    return np.zeros((h, w), bool)


def box(w: int, h: int, x0: int, y0: int, x1: int, y1: int) -> np.ndarray:
    """Axis-aligned box, corners inclusive."""
    m = empty(w, h)
    x0, x1 = sorted((x0, x1))
    y0, y1 = sorted((y0, y1))
    m[max(y0, 0):max(y1 + 1, 0), max(x0, 0):max(x1 + 1, 0)] = True
    return m


def ellipse(w: int, h: int, cx: float, cy: float, rx: float, ry: float, k: float = 1.0) -> np.ndarray:
    """Pixel centres inside the ellipse. Use half-integer centres for even sizes."""
    xx, yy = grid(w, h)
    return ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= k


def rrect(w: int, h: int, x0: int, y0: int, x1: int, y1: int, r: float) -> np.ndarray:
    """Rounded rectangle covering pixels x0..x1, y0..y1 (inclusive) with corner radius r."""
    xx, yy = grid(w, h)
    lx, rx_ = x0 + r, x1 + 1 - r
    ty, by = y0 + r, y1 + 1 - r
    cx = np.clip(xx, lx, rx_)
    cy = np.clip(yy, ty, by)
    inside = (xx >= x0) & (xx <= x1 + 1) & (yy >= y0) & (yy <= y1 + 1)
    return inside & (((xx - cx) ** 2 + (yy - cy) ** 2) <= r * r + 0.25)


def poly(w: int, h: int, pts: Sequence[tuple[float, float]]) -> np.ndarray:
    """Even-odd polygon fill over pixel centres. Points in pixel units (0,0 = top-left corner)."""
    xx, yy = grid(w, h)
    inside = np.zeros((h, w), bool)
    n = len(pts)
    for i in range(n):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % n]
        if y0 == y1:
            continue
        cond = ((y0 > yy) != (y1 > yy))
        xint = (x1 - x0) * (yy - y0) / (y1 - y0) + x0
        inside ^= cond & (xx < xint)
    return inside


def capsule(w: int, h: int, x0: float, y0: float, x1: float, y1: float, r: float) -> np.ndarray:
    xx, yy = grid(w, h)
    dx, dy = x1 - x0, y1 - y0
    ll = dx * dx + dy * dy
    if ll == 0:
        t = np.zeros_like(xx)
    else:
        t = np.clip(((xx - x0) * dx + (yy - y0) * dy) / ll, 0, 1)
    px_, py_ = x0 + t * dx, y0 + t * dy
    return (xx - px_) ** 2 + (yy - py_) ** 2 <= r * r


def stroke(w: int, h: int, pts: Sequence[tuple[float, float]], r: float) -> np.ndarray:
    m = empty(w, h)
    if len(pts) == 1:
        return capsule(w, h, pts[0][0], pts[0][1], pts[0][0], pts[0][1], r)
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        m |= capsule(w, h, x0, y0, x1, y1, r)
    return m


def arc_pts(cx: float, cy: float, rx: float, ry: float, a0: float, a1: float, n: int = 16) -> list[tuple[float, float]]:
    """Points on an elliptical arc. Angles in degrees: 0 = right, 90 = down, -90 = up."""
    out = []
    for i in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * i / n)
        out.append((cx + rx * math.cos(a), cy + ry * math.sin(a)))
    return out


def shift(mask: np.ndarray, dx: int, dy: int) -> np.ndarray:
    out = np.zeros_like(mask)
    h, w = mask.shape
    xs0, xs1 = max(0, -dx), min(w, w - dx)
    ys0, ys1 = max(0, -dy), min(h, h - dy)
    out[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx] = mask[ys0:ys1, xs0:xs1]
    return out


# --------------------------------------------------------------------------------------------------
# Lines
# --------------------------------------------------------------------------------------------------

def bresenham(x0: int, y0: int, x1: int, y1: int) -> list[tuple[int, int]]:
    pts = []
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        pts.append((x0, y0))
        if x0 == x1 and y0 == y1:
            return pts
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


# --------------------------------------------------------------------------------------------------
# Edges and outlines
# --------------------------------------------------------------------------------------------------

N4 = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], bool)
N8 = np.ones((3, 3), bool)


def outer(mask: np.ndarray, diagonal: bool = False) -> np.ndarray:
    """Pixels just outside the mask (4- or 8-connected ring)."""
    return ndimage.binary_dilation(mask, structure=N8 if diagonal else N4) & ~mask


def inner(mask: np.ndarray, diagonal: bool = False) -> np.ndarray:
    """Pixels on the inside border of the mask."""
    return mask & ~ndimage.binary_erosion(mask, structure=N8 if diagonal else N4, border_value=0)


def edge(mask: np.ndarray, dx: int, dy: int, width: int = 1) -> np.ndarray:
    """Pixels in the mask whose neighbour `k` steps along (dx, dy) leaves the mask (k = 1..width)."""
    out = np.zeros_like(mask)
    for k in range(1, width + 1):
        out |= mask & ~shift(mask, -dx * k, -dy * k)
    return out


def outline(cv: Canvas, mask: np.ndarray, colour, lit_colour=None, light=(1, -1), diagonal: bool = False) -> np.ndarray:
    """Draw a 1 px outline around `mask`. With `lit_colour`, outline pixels on the side facing the light
    use it (selective outlining). Returns the outline mask."""
    ring = outer(mask, diagonal)
    cv.fill(ring, colour)
    if lit_colour is not None:
        lx, ly = light
        # an outline pixel faces the light if the mask lies behind it, away from the light
        lit = ring & shift(mask, -int(np.sign(lx)), 0) | ring & shift(mask, 0, -int(np.sign(ly)))
        lit &= ring & ~(shift(mask, int(np.sign(lx)), 0) | shift(mask, 0, int(np.sign(ly))))
        cv.fill(lit, lit_colour)
    return ring


# --------------------------------------------------------------------------------------------------
# Shading
# --------------------------------------------------------------------------------------------------

LIGHT = (0.55, -0.62, 0.56)          # from the shop window: right, up, toward the viewer


def dome_lambert(mask: np.ndarray, light=LIGHT, bulge: float = 1.0, flat: float = 0.0) -> np.ndarray:
    """Lambert term of a soft dome inflated from the mask (round forms: heads, bodies, buns, cats).
    bulge > 1 rounds the edges harder; flat > 0 flattens the middle (pillow-free cel look)."""
    d = ndimage.distance_transform_edt(mask)
    if d.max() == 0:
        return np.zeros(mask.shape)
    hmax = d.max()
    hgt = np.sqrt(np.clip(d / hmax, 0, 1)) ** bulge * hmax
    if flat > 0:
        hgt = np.minimum(hgt, hmax * (1 - flat))
    gy, gx = np.gradient(ndimage.gaussian_filter(hgt, 0.7))
    nx, ny, nz = -gx, -gy, np.ones_like(gx)
    nl = np.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly, lz = light
    ll = math.sqrt(lx * lx + ly * ly + lz * lz)
    lam = (nx * lx + ny * ly + nz * lz) / (nl * ll)
    lam[~mask] = 0
    return lam


def bands(lam: np.ndarray, mask: np.ndarray, cuts: Sequence[float]) -> np.ndarray:
    """Quantise a lambert term into band indices 0..len(cuts)."""
    idx = np.zeros(mask.shape, np.int32)
    for c in cuts:
        idx += (lam > c)
    idx[~mask] = -1
    return idx


def paint_bands(cv: Canvas, idx: np.ndarray, colours: Sequence) -> None:
    for i, c in enumerate(colours):
        cv.fill(idx == i, c)


def dome(cv: Canvas, mask: np.ndarray, colours: Sequence, cuts: Sequence[float] = (0.42, 0.8),
         light=LIGHT, bulge: float = 1.0, flat: float = 0.0) -> np.ndarray:
    """Fill a round form with cel bands (colours dark -> light, len(cuts)+1 of them)."""
    idx = bands(dome_lambert(mask, light, bulge, flat), mask, cuts)
    paint_bands(cv, idx, colours)
    return idx


def flat_shade(cv: Canvas, mask: np.ndarray, base, dark=None, light=None, dark_w: int = 1, light_w: int = 1,
               ldir=(1, -1)) -> None:
    """Flat object: base fill, a light band on the edges facing the light, a dark band opposite."""
    cv.fill(mask, base)
    lx, ly = int(np.sign(ldir[0])), int(np.sign(ldir[1]))
    if dark is not None:
        d = np.zeros_like(mask)
        if lx:
            d |= edge(mask, -lx, 0, dark_w)
        if ly:
            d |= edge(mask, 0, -ly, dark_w)
        cv.fill(d, dark)
    if light is not None:
        l_ = np.zeros_like(mask)
        if lx:
            l_ |= edge(mask, lx, 0, light_w)
        if ly:
            l_ |= edge(mask, 0, ly, light_w)
        cv.fill(l_, light)


# --------------------------------------------------------------------------------------------------
# Patterns
# --------------------------------------------------------------------------------------------------

def checker(w: int, h: int, phase: int = 0) -> np.ndarray:
    yy, xx = np.mgrid[0:h, 0:w]
    return ((xx + yy + phase) % 2) == 0


BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def bayer(w: int, h: int, level: float) -> np.ndarray:
    """True where an ordered-dither of `level` (0..1) is on."""
    yy, xx = np.mgrid[0:h, 0:w]
    return BAYER4[yy % 4, xx % 4] < level


def scatter(mask: np.ndarray, density: float, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    return mask & (rng.random(mask.shape) < density)


def rng(seed: int) -> random.Random:
    return random.Random(seed)


# --------------------------------------------------------------------------------------------------
# Nine-slice (for compositing mockups the way the game stretches UI sprites)
# --------------------------------------------------------------------------------------------------

def nine_slice(src: Canvas, w: int, h: int, border: tuple[int, int, int, int]) -> Canvas:
    """border = (left, bottom, right, top) as in the contract. Middle parts are tiled (identical to a
    stretch for the flat middles the kit uses)."""
    l, b, r, t = border
    out = Canvas(w, h)
    sw, sh = src.w, src.h
    mw, mh = sw - l - r, sh - t - b

    def tile(sx, sy, swid, shei, dx, dy, dw, dh):
        if dw <= 0 or dh <= 0 or swid <= 0 or shei <= 0:
            return
        part = src.a[sy:sy + shei, sx:sx + swid]
        reps_y = -(-dh // shei)
        reps_x = -(-dw // swid)
        big = np.tile(part, (reps_y, reps_x, 1))[:dh, :dw]
        out.a[dy:dy + dh, dx:dx + dw] = big

    tile(0, 0, l, t, 0, 0, l, t)
    tile(l, 0, mw, t, l, 0, w - l - r, t)
    tile(sw - r, 0, r, t, w - r, 0, r, t)
    tile(0, t, l, mh, 0, t, l, h - t - b)
    tile(l, t, mw, mh, l, t, w - l - r, h - t - b)
    tile(sw - r, t, r, mh, w - r, t, r, h - t - b)
    tile(0, sh - b, l, b, 0, h - b, l, b)
    tile(l, sh - b, mw, b, l, h - b, w - l - r, b)
    tile(sw - r, sh - b, r, b, w - r, h - b, r, b)
    return out


# --------------------------------------------------------------------------------------------------
# Contact sheets and previews
# --------------------------------------------------------------------------------------------------

def sheet(items: Sequence[Canvas], cols: int, pad: int = 4, bg=None) -> Canvas:
    cw = max(i.w for i in items)
    ch = max(i.h for i in items)
    rows = -(-len(items) // cols)
    out = Canvas(cols * (cw + pad) + pad, rows * (ch + pad) + pad, bg or CLEAR)
    for n, it in enumerate(items):
        r, c = divmod(n, cols)
        out.blit(it, pad + c * (cw + pad) + (cw - it.w) // 2, pad + r * (ch + pad) + (ch - it.h))
    return out


def ensure_padding(cv: Canvas, px: int = 1) -> bool:
    """True if the outermost `px` rows/columns are fully transparent (contract rule for outlined sprites)."""
    a = cv.a[:, :, 3]
    return not (a[:px].any() or a[-px:].any() or a[:, :px].any() or a[:, -px:].any())

"""
figure: layered drawing for characters and round props.

A Figure paints parts back to front. Each part is a mask plus a colour ramp; it is cel-shaded (dome or
flat), gets a 1 px line in its darkest tone where it overlaps parts painted earlier, and records its
material so that finish() can draw a *selective* silhouette outline (each outline pixel takes the
darkest tone of the material it borders: warm brown around skin, deep navy around dark hair...).
"""

from __future__ import annotations

from typing import Sequence

import numpy as np

import px


def clean_bands(idx: np.ndarray, mask: np.ndarray, passes: int = 2) -> np.ndarray:
    """Remove lonely pixels from a band map: a pixel with at most one same-band neighbour (8-way) takes
    the most common band around it. Gives the clean clusters pixel art wants."""
    idx = idx.copy()
    values = [v for v in np.unique(idx[mask]) if v >= 0]
    if len(values) < 2:
        return idx
    for _ in range(passes):
        counts = {}
        for v in values:
            m = (idx == v)
            c = np.zeros(idx.shape, np.int32)
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    if dx or dy:
                        c += px.shift(m, dx, dy)
            counts[v] = c
        best = np.full(idx.shape, -1)
        best_n = np.full(idx.shape, -1)
        for v in values:
            better = counts[v] > best_n
            best[better] = v
            best_n[better] = counts[v][better]
        own = np.zeros(idx.shape, np.int32)
        for v in values:
            own[idx == v] = counts[v][idx == v]
        lonely = mask & (own <= 1) & (best >= 0)
        idx[lonely] = best[lonely]
    return idx


class Figure:
    def __init__(self, w: int, h: int):
        self.w, self.h = w, h
        self.cv = px.Canvas(w, h)
        self.mat = np.full((h, w), "", dtype=object)

    def painted(self) -> np.ndarray:
        return self.cv.alpha_mask()

    def part(self, mask: np.ndarray, ramp: str, tones: Sequence[int] = (1, 2, 3), cuts: Sequence[float] = (0.42, 0.8),
             style: str = "dome", line: bool = True, line_tone: int = 0, light=px.LIGHT, bulge: float = 1.0,
             flat: float = 0.0, clean: bool = True, line_diagonal: bool = False) -> np.ndarray:
        """Paint a shaded part. tones index the ramp from shadow to light (len(cuts) + 1 of them)."""
        mask = mask.copy()
        if not mask.any():
            return mask
        before = self.painted()
        colours = [px.C(ramp, t) for t in tones]
        if style == "dome":
            idx = px.bands(px.dome_lambert(mask, light, bulge, flat), mask, cuts)
        elif style == "flat":
            idx = np.full(mask.shape, -1)
            idx[mask] = min(1, len(colours) - 1)
            if len(colours) >= 2:
                idx[px.edge(mask, -1, 0, 1) & mask] = 0
                idx[px.edge(mask, 0, 1, 1) & mask] = 0
            if len(colours) >= 3:
                idx[px.edge(mask, 1, 0, 1) & mask] = 2
                idx[px.edge(mask, 0, -1, 1) & mask] = 2
        elif style == "solid":
            idx = np.full(mask.shape, -1)
            idx[mask] = 0
        else:
            raise ValueError(style)
        if clean and style == "dome":
            idx = clean_bands(idx, mask)
        for i, c in enumerate(colours):
            self.cv.fill((idx == i) & mask, c)
        if line:
            ring = px.outer(mask, line_diagonal)
            against = ring & before
            # the part's own border pixels that touch earlier paint get the line
            border = mask & px.shift(against, 1, 0) | mask & px.shift(against, -1, 0) | \
                mask & px.shift(against, 0, 1) | mask & px.shift(against, 0, -1)
            self.cv.fill(border, px.C(ramp, line_tone))
        self.mat[mask] = ramp
        return mask

    def solid(self, mask: np.ndarray, colour, ramp: str | None = None) -> None:
        """Flat colour detail (eyes, stripes). Keeps the material of what is underneath unless ramp is set."""
        self.cv.fill(mask, colour)
        if ramp:
            self.mat[mask] = ramp

    def put(self, x: int, y: int, colour) -> None:
        self.cv.put(x, y, colour)

    def pixels(self, pts, colour) -> None:
        for x, y in pts:
            self.cv.put(x, y, colour)

    def erase(self, mask: np.ndarray) -> None:
        self.cv.a[mask] = 0
        self.mat[mask] = ""

    def finish(self, default_ramp: str = "ink", diagonal: bool = False) -> px.Canvas:
        """Selective silhouette outline: every outline pixel takes the darkest tone of a neighbouring
        material."""
        alpha = self.painted()
        ring = px.outer(alpha, diagonal)
        ys, xs = np.nonzero(ring)
        for y, x in zip(ys, xs):
            ramp = ""
            for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (-1, 1), (1, -1), (-1, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < self.w and 0 <= ny < self.h and alpha[ny, nx] and self.mat[ny, nx]:
                    ramp = self.mat[ny, nx]
                    break
            self.cv.put(int(x), int(y), px.C(ramp or default_ramp, 0))
        return self.cv


# --------------------------------------------------------------------------------------------------
# Face kit (pixel sizes are explicit: features never scale)
# --------------------------------------------------------------------------------------------------

INK = px.C("ink", 0)
INK2 = px.C("ink", 1)
WHITE = px.C("rice", 5)


def eye_round(fig: Figure, x: int, y: int, size: int = 3, ink=INK2, glint=True) -> None:
    """Round dark eye; (x, y) = top-left. size 3 -> 3x3 with corners trimmed when >3."""
    for dy in range(size + 1):
        for dx in range(size):
            corner = size >= 4 and dy in (0, size) and dx in (0, size - 1)
            if not corner:
                fig.put(x + dx, y + dy, ink)
    if glint:
        fig.put(x + size - 2, y + 1, WHITE)


def eye_big(fig: Figure, x: int, y: int, ink=INK2, iris=None) -> None:
    """Big sparkly eye 5 wide x 6 tall (Mio). (x, y) = top-left."""
    shape = [".###.", "#####", "#####", "#####", "#####", ".###."]
    for r, row in enumerate(shape):
        for c, v in enumerate(row):
            if v == "#":
                fig.put(x + c, y + r, ink)
    if iris is not None:
        for (c, r) in ((1, 3), (2, 3), (3, 3), (1, 4), (2, 4), (3, 4)):
            fig.put(x + c, y + r, iris)
    fig.put(x + 3, y + 1, WHITE)
    fig.put(x + 3, y + 2, WHITE)
    fig.put(x + 2, y + 1, WHITE)
    fig.put(x + 1, y + 4, WHITE)


def eye_closed(fig: Figure, x: int, y: int, w: int = 3, colour=INK2) -> None:
    """Blink: a short line at the eye's middle row."""
    for dx in range(w):
        fig.put(x + dx, y, colour)


def eye_happy(fig: Figure, x: int, y: int, w: int = 4, colour=INK2) -> None:
    """^ shaped closed happy eye, w wide, (x, y) = left end of the arc's base."""
    if w <= 3:
        fig.pixels([(x, y), (x + 1, y - 1), (x + 2, y)], colour)
    else:
        fig.pixels([(x, y), (x + 1, y - 1), (x + 2, y - 1), (x + 3, y)], colour)
        if w >= 5:
            fig.pixels([(x + 4, y + 1)], colour)


def eye_squeeze(fig: Figure, x: int, y: int, left: bool, colour=INK2) -> None:
    """> < wince eye."""
    pts = [(0, 0), (1, 1), (2, 2), (1, 3), (0, 4)] if left else [(2, 0), (1, 1), (0, 2), (1, 3), (2, 4)]
    fig.pixels([(x + a, y + b) for a, b in pts], colour)


def blush(fig: Figure, x: int, y: int, w: int = 4, colour=None) -> None:
    colour = colour or px.C("pink", 3)
    for dx in range(w):
        fig.put(x + dx, y, colour)
    for dx in range(1, w - 1):
        fig.put(x + dx, y + 1, colour)


def mouth(fig: Figure, cx: int, y: int, kind: str, skin: str = "skin") -> None:
    """Mouths centred on cx with their top at y.
    smile, flat, open, grin, frown, wobble, o, smirk, tiny"""
    dark = px.C("coral", 0)
    line = px.C(skin, 0)
    tongue = px.C("coral", 3)
    if kind == "smile":
        fig.pixels([(cx - 2, y), (cx - 1, y + 1), (cx, y + 1), (cx + 1, y + 1), (cx + 2, y)], line)
    elif kind == "tiny":
        fig.pixels([(cx - 1, y + 1), (cx, y + 1), (cx + 1, y)], line)
    elif kind == "flat":
        fig.pixels([(cx - 1, y), (cx, y), (cx + 1, y)], line)
    elif kind == "smirk":
        fig.pixels([(cx - 2, y + 1), (cx - 1, y + 1), (cx, y + 1), (cx + 1, y), (cx + 2, y - 1)], line)
    elif kind == "frown":
        fig.pixels([(cx - 2, y + 1), (cx - 1, y), (cx, y), (cx + 1, y), (cx + 2, y + 1)], line)
    elif kind == "wobble":
        fig.pixels([(cx - 3, y + 1), (cx - 2, y), (cx - 1, y + 1), (cx, y), (cx + 1, y + 1), (cx + 2, y), (cx + 3, y + 1)], line)
    elif kind == "o":
        fig.pixels([(cx - 1, y), (cx, y), (cx - 2, y + 1), (cx + 1, y + 1), (cx - 1, y + 2), (cx, y + 2)], dark)
        fig.pixels([(cx - 1, y + 1), (cx, y + 1)], px.C("coral", 1))
    elif kind == "open":
        rows = [" ### ", "#####", "#ttt#", " ### "]
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    fig.put(cx - 2 + c, y + r, dark)
                elif v == "t":
                    fig.put(cx - 2 + c, y + r, tongue)
    elif kind == "grin":
        rows = ["#######", "#wwwww#", " #ttt# ", "  ###  "]
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    fig.put(cx - 3 + c, y + r, dark)
                elif v == "t":
                    fig.put(cx - 3 + c, y + r, tongue)
                elif v == "w":
                    fig.put(cx - 3 + c, y + r, WHITE)
    elif kind == "sad_open":
        rows = ["  ###  ", " #ttt# ", "#ttttt#"]
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v == "#":
                    fig.put(cx - 3 + c, y + r, dark)
                elif v == "t":
                    fig.put(cx - 3 + c, y + r, px.C("coral", 1))
    else:
        raise ValueError(kind)


def sweat(fig: Figure, x: int, y: int) -> None:
    """Little blue sweat drop, (x, y) = its tip."""
    c0, c1, c2 = px.C("blue", 1), px.C("blue", 3), px.C("blue", 4)
    fig.pixels([(x, y)], c1)
    fig.pixels([(x - 1, y + 1), (x, y + 1), (x + 1, y + 1)], c1)
    fig.pixels([(x - 1, y + 2), (x, y + 2), (x + 1, y + 2)], c1)
    fig.pixels([(x, y + 3)], c0)
    fig.pixels([(x - 1, y + 3), (x + 1, y + 3)], c0)
    fig.put(x + 1, y + 1, c2)


def tear(fig: Figure, x: int, y: int, n: int = 3) -> None:
    for i in range(n):
        fig.put(x, y + i, px.C("blue", 3))
    fig.put(x, y + n, px.C("blue", 2))

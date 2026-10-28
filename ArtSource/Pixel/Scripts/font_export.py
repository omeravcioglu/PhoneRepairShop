"""
font_export: writes the "Rice 5x7" face (pxfont.SMALL) into the game's glyph table,
Assets/_Project/Scripts/Editor/Pixel/PixelGlyphs.cs. Run Setup then rebuilds the font atlas from it.

Characters the art font doesn't draw keep the glyph the table already has, so the game never loses a
character. The table keeps its order; new characters go at the end.

    python font_export.py
"""

from __future__ import annotations

import os
import re

import pxfont as F

HERE = os.path.dirname(os.path.abspath(__file__))
TABLE = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Assets", "_Project", "Scripts", "Editor", "Pixel",
                                      "PixelGlyphs.cs"))
BS = chr(92)
START = "        private static readonly (char code, string rows)[] Table =\n        {\n"
END = "\n        };\n"
SUMMARY = """    /// <summary>
    /// The game's pixel font, "Rice 5x7": 5-pixel capitals, lower case with descenders, digits, ASCII
    /// punctuation and ¥ × · … — ♥ ♪ ★ → °. Rows go top to bottom; row 6 sits on the baseline and
    /// rows 7-8 are descenders. '#' is ink. Generated from the art source by
    /// ArtSource/Pixel/Scripts/font_export.py: edit the font there and re-run it. The atlas is built
    /// from this table, and the placeholder sprites are lettered with it.
    /// </summary>
"""


def literal(ch: str) -> str:
    if ch == BS:
        return "'" + BS + BS + "'"
    if ch == "'":
        return "'" + BS + "''"
    return "'" + ch + "'"


def parse(body: str) -> list[tuple[str, str]]:
    out = []
    for m in re.finditer(r"\('(.{1,2}?)',\s*\"([^\"]*)\"\)", body):
        ch = m.group(1)
        if ch == BS + BS:
            ch = BS
        elif ch == BS + "'":
            ch = "'"
        out.append((ch, m.group(2)))
    return out


def packed(rows: list[str]) -> str:
    rows = list(rows)
    while len(rows) > 1 and set(rows[-1]) <= {"."}:
        rows.pop()
    return "|".join(rows)


def main() -> None:
    src = open(TABLE, encoding="utf-8").read()
    a = src.index(START) + len(START)
    b = src.index(END, a)
    old = parse(src[a:b])
    art = {k: v for k, v in F.SMALL.items() if k != " "}
    lines = []
    seen = set()
    kept = []
    for ch, rows in old:
        seen.add(ch)
        if ch in art:
            rows = packed(art[ch])
        else:
            kept.append(ch)
        note = f"   // U+{ord(ch):04X}" if ord(ch) > 126 else ""
        lines.append(f"            ({literal(ch)}, \"{rows}\"),{note}")
    for ch in sorted(set(art) - seen, key=ord):
        lines.append(f"            ({literal(ch)}, \"{packed(art[ch])}\"),   // U+{ord(ch):04X}")
    src = src[:a] + "\n".join(lines) + src[b:]
    s0 = src.index("    /// <summary>")
    s1 = src.index("    /// </summary>\n", s0) + len("    /// </summary>\n")
    src = src[:s0] + SUMMARY + src[s1:]
    with open(TABLE, "w", encoding="utf-8", newline="\n") as f:
        f.write(src)
    print(f"{len(lines)} glyphs written; kept from the old table: {''.join(kept)}")


if __name__ == "__main__":
    main()

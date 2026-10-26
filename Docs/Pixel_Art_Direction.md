# Have You Tried Rice? — pixel art direction

The S2 art: every sprite in the art contract, drawn as pixel art at its exact size, plus the logo and
three pixel fonts. The canvas with all of it (cover, rules, 8 screen mockups, sprite boards, the cast)
is the Design artifact; the old 3D boards are on
its "Archive — 3D Pocket Diorama" page.

## The name

**Have You Tried Rice?** Everyone's first fix for a phone that took a swim is a bag of rice; this shop
has heard it a thousand times. The joke lives in the art: the NO RICE poster, "Rice — NO!" crossed
out on the price board, the emergency rice sack under the counter with a phone still stuck in it, the
sticky note on the bench, and the bowl in the logo. Tagline: *(we have. it doesn't work.)*

## Rules

1. **One unit is one pixel.** 640×360, scaled 3× on 1080p. Sprites are drawn at final size and never
   scaled inside the image. (The doorway customer is drawn at half size by code; see Next steps.)
2. **Light from the shop window, upper right**, on everything: lit edges top and right, shadows bottom
   and left, cast shadows down-left.
3. **Three tones and a glint** per material; the fourth, brightest pixel only where something shines.
4. **Selective outlines:** an outline pixel takes the darkest tone of what it borders (brown round
   skin, navy round hair, plum round the phone). Never flat black.
5. **Clean clusters:** no lonely pixels; faces and clothes keep flat middles.
6. **Dither only for light:** sky bands, window light, plank tone.
7. **Clickable sprites keep 1 px of clear border** for the game's hover outline (the contract's
   `outline` flag). `export.py` checks this.
8. **Signal colours belong to code:** hover orange, blocked coral and done green are drawn by the game.

Palette: 17 ramps, dark to light, in `ArtSource/Pixel/Scripts/px.py` (`RAMPS`). Shadows lean cool,
lights lean warm.

## Type

| Face | Use | Where |
|---|---|---|
| Strokes (display) | Logo, shop signs | `pxfont.STROKE`, `stroke_text()` |
| 5×7 | Everything the player reads | `pxfont.SMALL`, same cell as the game's font: caps 7, x-height 5, line 11 |
| 3×5 | Labels printed on props | `pxfont.MICRO`, `micro()` |

## Files

| Path | What |
|---|---|
| `ArtSource/Pixel/Art/` | The 103 contract PNGs, same names and folders as the placeholders in `Assets/_Project/Art/Pixel/` |
| `ArtSource/Pixel/Art/_proposed/` | `UI/ui_logo.png` (256×80) and `UI/ui_logo_tagline.png` (256×104), not in the contract yet |
| `ArtSource/Pixel/Preview/screens/` | The 8 screen mockups at 2× |
| `ArtSource/Pixel/Preview/sheets/` | One contact sheet per contract group |
| `ArtSource/Pixel/Scripts/` | The generators (below) |

| Script | Draws |
|---|---|
| `px.py` | Canvas, masks, cel shading, outlines, dithering, 9-slice, palette |
| `figure.py` | Layered parts with selective outlines; the face kit (eyes, mouths, blush, sweat) |
| `pxfont.py` | The three fonts |
| `cast.py` | The five customers: 6 body frames and 3 faces each |
| `shop.py` | Shop background, door, counter and props |
| `bench.py` | Bench background and spares box |
| `phone.py` | The 12 Mochi One layers and the screw frames |
| `ui.py` | UI kit, tool icons, cursors, logo |
| `scene.py`, `mockups.py` | Places sprites exactly as the scene builder does and composes the screens |
| `export.py` | Writes `Art/`, checks every file against `art_contract.json` (size, frames, clear border) |
| `canvas_assets.py` | Composites for the canvas |

Run from `ArtSource/Pixel/Scripts` (Python 3.12 with Pillow, NumPy, SciPy):

```
python export.py
python export.py --install
```

The first writes `Art/` and the sheets. The second also copies the PNGs over the placeholders in
`Assets/_Project/Art/Pixel/`; then run **Tools > Phone Repair > Run Setup** in Unity.

## Proposed contract changes

- **`UI/ui_logo.png`, 256×80.** The title card draws it instead of the "PHONE FIXER" text
  (`ShopSceneBuilder.CreatePanel`).
- **The 5×7 face as the game font.** It has the same cell and metrics as `PixelGlyphs`, so the
  atlas can be swapped without layout changes.
- **Far-away customer sprites.** The doorway shows the counter sprite at half size by dropping every
  other pixel. It works, but details break up; a hand-drawn 48×80 "far" frame per customer would be
  cleaner.
- **UI text colours.** The builder's Cream, Ink and Mint are close to the palette's `rice 4`
  (#f9f2e3), `ink 1` (#262840) and `teal 5` (#c0ead6); matching them exactly would tie the text to
  the art.

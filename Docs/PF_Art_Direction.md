# Phone Fixer: art direction v1, "Pocket Diorama"

*Golden hour, forever.* A cozy, slightly goofy, Japanese-inspired phone repair shop. Chunky little phones, odd neighbours, one big satisfying screw at a time.

The full visual reference is the **design canvas**: . Its boards are:

- Main
- Color & light
- Shapes, materials & lighting
- UI kit
- Characters
- Phone
- Workbench
- Shop
- the 11 UI screens

This file is the short version for day-to-day work. The Unity implementation is described in [PF_Unity_Integration.md](PF_Unity_Integration.md).

## Pillars

| Pillar | In practice |
|---|---|
| **Pocket diorama** | The shop is a cut-away box on a wooden plinth. It is seen from a fixed ¾ camera with a long lens (~20° vertical FOV, ~20 m away). Props are slightly oversized so they read. |
| **Soft-chunky shapes** | Bevel everything: 8–12% of the smallest side, 2–3 segments, hardened normals. One big form with a few big details, and no texture noise. Furniture is a little short and wide; phones are a little thick; tools have oversized handles. |
| **Golden hour, forever** | A low peach sun comes through the storefront glass, with a teal sky fill and warm lamp pools. Shadows are navy, never black. |
| **Colour is gameplay** | Saturated hues are reserved for play (see below). The world sits at 55–85 % lightness and is 30–40 % greyer than the play colours. |
| **Handmade warmth** | Sticky notes, tape, stickers, receipts and a shop cat (Denchi). Mismatched, but tidy. |

## Palette

The world colours are:

- **grounds:** washi cream `#F5E8D3` and rice paper `#FFFDF8`
- **kraft and wood:** kraft `#E6D2B5`, pale oak `#E4C8A2`, natural wood `#B88B61` and walnut `#8A6244`
- **ink:** sumi navy `#283747` and deep navy `#1C2733`
- **soft accents:** sage, dusty blue, butter, blush, plum, wisteria, brass and steel

All of them are in `ArtSource/Blender/Scripts/pf_bpy.py` → `PALETTE`, the single source for Blender and Unity.

The play colours are always paired with a shape or a word:

| Hue | Meaning | Where |
|---|---|---|
| **Orange `#F2A65A`** | you can pick it | hover glow, station labels, the upgrade floor tape, progress ring |
| **Teal `#6FA9A3`** | in use / selected | selected part, active tool keycap, primary buttons, diagnostic LCD |
| **Coral `#DE8175`** | damaged / blocked | fault chips, warning stickers, the "nope" flash |
| **Leaf `#8CCB6E`** | fixed | passed tests, "REPAIRED" stamp, money in |
| **Navy `#283747`** | information | panels, prices, timers; text is navy on paper or paper on navy |

## Materials

Stock URP Lit, with no custom toon shader in v1. The look comes from shapes, palette and light.

| Set | Smoothness / metallic | Used for |
|---|---|---|
| Matte wood | 0.25 / 0 | furniture, floor, frames |
| Plaster & paper | 0.08 / 0 | walls, posters, boxes |
| Soft plastic | 0.55 / 0 | phone shells, handles, toys |
| Glossy glass | 0.93, reflection probe | screens, lenses, windows (transparent, casts no shadows) |
| Bright metal | 0.72 / 1 | screws, shields, tool tips |
| Rubber | 0.2 / 0 | suction cup, grips, feet |
| Indigo PCB | 0.55, gold pads metallic | boards (deliberately not teal: teal glows on it) |
| Fabric | 0.05 / 0 | clothes, noren, cushions |
| Emissive screen | emission 1.5–2.5 | displays, signs, bulbs |

## Light script

| Time | Key | Notes |
|---|---|---|
| Morning, open 09:00 | sun 40° | lamps low |
| **Golden hour 16:30 (default)** | sun ~24° through the storefront | pendants on |
| Evening, closing 19:30 | moon | lamps, screens and the OPEN sign carry the image |
| Repair bench, all day | desk lamp spot | monitor glow; crisp contact shadows |

- **Shadow budget:** two shadowed lights at most, the sun and the bench lamp. The pendants and the monitor glow are unshadowed.
- **Post stack:** Neutral tonemap, bloom 0.35 @ 1.1, contrast +8 (diorama +12), saturation +6, warmth +6, navy vignette 0.18.
- **Not in v1:** no realtime GI, no SSR, no outline pass.

## Cast

All customers share one base: a gumdrop body about three heads tall (≈ 1.38 m), mitten hands, and faces made of a few big shapes.

| Customer | Who | Phone and problem |
|---|---|---|
| **Fumi** (Mrs. Fumiko Tanabe) | cheerful regular | old Mochi One in a flip case; battery "gets warm"; tips in mikan |
| **Ren** Kuroda | gamer, up all night | Mochi Dango that "won't charge" (crumbs in the port) |
| **Mr. Sasaki** (Kenji) | office worker | cracked screen, meeting in 20 minutes |
| **Mio** Hayashi | student | phone dropped in ramen, corroded board |
| **Madame K.** (Reiko Kanemochi) | wealthy | gold edition, scratched lens; her dog Bijou judges too |

Expressions are eye scale (blink, happy squint), brow tilt and mouth swaps. Motion is short loops: Idle, Walk, Talk, Impatient. Gestures are Celebrate, Disappointed and Confused.

## Brand: Mochi Mobile

Phones are named after wagashi. They are soft, chunky and repair-friendly. Every part is its own object and every fault is a visible mesh.

- **Mochi One:** the first phone.
- **Mochi Dango:** the hero phone. It has three rear lenses on a brass "skewer" in hanami-dango colours (pink, cream and matcha) on a cream island, and a sakura-pink back.

## UI: "Paper & Keys"

- **Paper cards** with a 2 px navy line and a hard navy drop.
- **Keycap buttons:** a navy base with a face that sinks when pressed, and the hotkey printed on a navy badge.
- **Tickets on a clothesline** for jobs.
- **LCD panels** for diagnostics.
- **Pills and chips** always pair colour with a word.
- **Fonts:** Dela Gothic One (display), M PLUS Rounded 1c (UI), DotGothic16 (numbers and LCD). All three are SIL OFL. They are not in the project yet; see the integration doc.

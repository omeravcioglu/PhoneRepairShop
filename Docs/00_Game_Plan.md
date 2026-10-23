# Phone Fixer: game plan (2D pixel art, behind the counter)

**Engine:** Unity 6000.0.32f1 (Unity 6 LTS), URP 17.0.3 with the **2D Renderer** and Pixel Perfect Camera, Input System 1.11.2
**Look:** pixel art at **640×360**, shown 3× on 1080p (4× 1440p, 6× 4K)
**Restructured:** 2026-10-22, from the earlier 3D prototype. That work is preserved; see *What happened to the 3D version* at the end.

---

## 1. The game in one breath

You run a tiny phone repair shop. You stand behind the counter, the door chimes, and someone goofy hops up to you with a broken phone and a ridiculous story ("I dropped it in ramen. Tonkotsu."). You take the job, the view slides down to your repair mat, and you open the phone up with real hands-on steps: screws (hold and circle the mouse), pry the cover, unplug, lift out, swap the broken part, put it all back. You ring the bell and hand it back. They switch it on, and they pay you or grumble at you. Next customer. At closing time you see how the day went.

## 2. Pillars

| Pillar | In practice |
|---|---|
| **One screen, little motion** | There are two fixed views: the shop (you at the counter) and the bench (the mat seen from above). One short camera slide connects them. Customers hop rather than walk, faces change with a few frames, and nothing scrolls. |
| **Goofy people** | Every customer has a face, a voice (talk blips at their own pitch), a dumb story, and a reaction to how you treat their phone. Their face peeks in at the top of the bench and winces when you do something you shouldn't. |
| **Hands-on repair** | The Milestone 2 rules engine is unchanged: the right tool, the right order, a screw minigame. Wrong moves get a clear "nope" with the reason. |
| **Data, not code** | Customers, phones, parts, rules, tools and the day calendar are assets. A new phone needs art layers plus data; a new customer needs a sprite set plus one asset. |
| **Pixel art, drop-in** | One art contract lists every sprite. Placeholders fill it today, and real art replaces them file by file. |

## 3. Core loop

```
Title ─► Day N
          └─ for each customer in the calendar:
               door chime → hops in → greeting → problem
               ├─ [2] Sorry, can't → sad line → leaves
               └─ [1] Leave it with me! → phone on the counter → slide down to the bench
                    take it apart → swap the broken part at the parts box → put it back
                    ring the bell / Enter → slide up → power test
                    ├─ fixed  → happy line → pays (+ a goofy tip) → leaves
                    └─ broken → grumbles → leaves (no pay)
          └─ CLOSED sign → day summary (repairs, money, tips) → Day N+1
```

## 4. How it is built

| Layer | What | Where |
|---|---|---|
| Rules (kept from M2) | `AssemblyGraph`, `PartDefinition`, `PhoneModelDefinition`, `PhoneDevice`, `PhonePart`, `ScrewTurnTracker`, `ToolBelt`, `ToolDefinition` | `Scripts/Runtime/Devices`, `Repair` |
| Pixel layer | `PixelText` + `PixelFont` (crisp bitmap text), `FlipbookAnimator`, `SpriteToggle`, `PixelGrid` snapping, `SortOrder` | `Scripts/Runtime/Pixel` |
| Interaction 2D | `PointerPicker2D` (front-most sprite wins), `SpriteHighlighter` (outline and flash shader), `PixelButton`, `IClickable` | `Scripts/Runtime/Interaction` |
| Bench | `BenchRepair` (the verbs), `PartView2D`, `Phone2D` (front/back flip), `PartsTray2D`, `PartFlights2D`, `SparesBox`, `ToolSlot`, `StatusLine`, `BenchFeedback` | `Scripts/Runtime/Repair` |
| Shop | `ShopDirector` (the loop), `CustomerActor`, `SpeechBubble`, `ChoicePrompt`, `BenchController`, `PointerRouter`, `CameraRig2D`, `Wallet`, `PanelView`, `PixelCursor`, `Pettable`, `CustomerPortrait`, `TicketView`, `ProgressRing` | `Scripts/Runtime/Shop` |
| Data | `CustomerDefinition` ×5, `ShopCalendar`, the Mochi One part and model assets, 4 tools | `Data/Customers`, `Data/Shop`, `Data/Phones`, `Data/Tools` |
| Art | `art_contract.json`, all sprites, the font atlas | `Art/Pixel` |
| Editor | `ProjectSetup` (Run Setup), the art contract, the importer, the placeholder painter, the font builder, the prefab and scene builders, the contract report | `Scripts/Editor` |

**Rendering choices.**
- URP 2D Renderer with Pixel Perfect Camera (reference 640×360, upscale render texture, stretch fill).
- 1 world unit = 1 pixel.
- Everything, the UI included, is sprites and bitmap text in the world. So the UI is pixelated exactly like the art, and a screenshot is just a camera render.
- One unlit sprite shader adds the hover outline and the flash.
- No Animator: flipbooks plus small coded hops.

## 5. Milestones (restructured)

| # | Milestone | Done when |
|---|---|---|
| **S1** | **Shop and bench, playable loop** ✅ | Customers walk in, talk, and hand over phones. A battery or screen repair works start to finish on the bench, they pay, and the day ends with a summary. Placeholder art, plus the art contract. Automated tests pass headlessly. See `Docs/S1_Shop2D.md`. |
| **S2** | **Art pass from the art artifact** | Every contract sprite is real art; real font; palette; the shop has a mood (2D lights optional). The game reads at 1080p. *All 103 sprites drawn 2026-10-22 in `ArtSource/Pixel/Art` (see `Docs/Pixel_Art_Direction.md`); not installed in the game yet.* |
| **S3** | **Diagnosis and more faults** | Hidden faults that need a tool to find (a dead battery that looks fine, which needs a tester), a cleaning minigame (lint in the port), a second phone, customer patience, and tips that depend on speed. |
| **S4** | **Money and progression** | Spare parts cost money, prices and haggling, upgrades (electric screwdriver, a second spot, a bigger mat), save/load. |
| **S5** | **Content and polish** | More customers and days with recurring characters and running gags, music, settings, a Steam demo build. |

## 6. Open decisions (yours)

- **Title.** Decided 2026-10-22: **Have You Tried Rice?** The logo sprite is drawn; the title card still writes "PHONE FIXER" as text until it is wired in.
- **Font.** The placeholder is a hand-made 5×7 pixel font with no Japanese. A designed pixel font (an OFL font such as DotGothic16 has Japanese) needs your OK to download.
- **Do you ever see the shopkeeper?** Currently no: the camera is the shopkeeper's eyes and hands.
- **Days and pacing.** The calendar is a fixed 3-day cycle of 3 customers. S3 and S4 can add random arrivals and patience.

## 7. What happened to the 3D version

- **Git.** Commit `e2d085f` on `main` is the complete 3D project as it was: Milestones 1-2, the Phone Fixer diorama art pass, and the trailer pipeline. The 2D work lives on the `2d-restructure` branch and is not committed yet.
- **Archive.** `Archive/3D/` holds the moved files at their original paths (with `.meta` files), outside `Assets/`, so Unity ignores them. To bring something back, move it to its old path.
- **Carried over unchanged:** the assembly rules and their tests, the Mochi One part and model data, the four tool assets (now with 2D icons), input, and sounds.

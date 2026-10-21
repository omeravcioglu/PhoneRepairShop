# Phone Repair Shop: First Playable Prototype Plan

**Engine:** Unity 6000.0.32f1 (Unity 6 LTS), URP 17.0.3, Input System 1.11.2, uGUI + TextMeshPro
**Art:** Blender 5.2, with procedural Python generators that export FBX
**Goal of the prototype:** one customer, one broken Mochi One, a full repair from walk-in to payment, and one tool upgrade.

---

## 1. Project folder structure

```
PhoneRepairShop/
├─ Assets/
│  ├─ _Project/                     everything we author (underscore = sorted first, separate from store assets)
│  │  ├─ Art/
│  │  │  ├─ Materials/  Phone/ Environment/ Tools/ FX/
│  │  │  ├─ Models/     Phones/<Model>/  Environment/  Tools/  (Characters/ later)
│  │  │  ├─ Shaders/    InteractableHighlight.shader
│  │  │  └─ UI/         UI_RoundedRect.png, UI_Ring.png (generated)
│  │  ├─ Data/                      ScriptableObject *instances* = game content
│  │  │  ├─ Phones/MochiOne/        PHN_MochiOne_Model.asset, Parts/PRT_*.asset
│  │  │  └─ Tools/                  TOOL_Hands, TOOL_Screwdriver, TOOL_Spudger, TOOL_SuctionCup
│  │  │     (Faults/ Customers/ Upgrades/ arrive in M3-M5)
│  │  ├─ Prefabs/   Phones/ Environment/ (Tools/ UI/ Systems/ later)
│  │  ├─ Scenes/    Sandbox/M1_Workbench.unity   (Game/Shop.unity in M4)
│  │  ├─ Settings/  Rendering/  (URP asset, renderer, volume profiles)
│  │  └─ Scripts/
│  │     ├─ Runtime/   PhoneRepair.Runtime.asmdef
│  │     │  ├─ Controls/      input -> game actions          (GameInput)
│  │     │  ├─ Cameras/       director + views               (CameraDirector, InspectionCameraView)
│  │     │  ├─ Interaction/   picking, hover/select, glow    (Interactable, SelectionController...)
│  │     │  ├─ Devices/       phones & parts (data + runtime) (PhoneDevice, PhonePart, *Definition)
│  │     │  ├─ Workbench/     repair-bench behaviour         (WorkbenchController, InspectionTurntable)
│  │     │  ├─ Repair/        tools, repair verbs, parts tray (RepairInteractor, ToolBelt, PartsArea, PartMover...)
│  │     │  ├─ Audio/         placeholder sound effects      (SfxPlayer, ProceduralSfx)
│  │     │  ├─ UI/            HUD widgets
│  │     │  └─ (Diagnosis/ Minigames/ Customers/ Economy/ Save/ in later milestones)
│  │     ├─ Editor/    PhoneRepair.Editor.asmdef  (setup, prefab/scene builders, import rules, capture)
│  │     └─ Tests/     EditMode/  PlayMode/
│  ├─ TextMesh Pro/                 Unity-managed essentials
│  └─ ThirdParty/                   store assets, never mixed into _Project
├─ ArtSource/Blender/               .blend sources + generator scripts (kept OUT of Assets on purpose)
├─ Docs/                            this file, milestone notes
└─ Packages/  ProjectSettings/
```

**Naming:** `PHN_` phone models/prefabs, `PRT_` part data, `ENV_` environment, `TOOL_` tools, `CHR_` characters, `M_` materials, `T_` textures, `UI_` sprites. Namespaces follow the folders: `PhoneRepair.Controls`, `PhoneRepair.Cameras`, and so on.

**Dependency direction** (enforced by assemblies and kept by convention inside them):
`Controls / Cameras / Interaction` (generic, game-agnostic) ← `Devices` (phone domain) ← `Workbench / Repair / Customers` (gameplay) ← `UI`. Lower layers never reference higher ones.

---

## 2. Modular phone repair architecture

The key idea: **a phone is data plus a prefab, never custom code.** Every phone model uses the same scripts.

### Data (ScriptableObjects, authored by designers)

| Asset | Holds | Milestone |
|---|---|---|
| `PartDefinition` | *type* of part: id, name, category, description · **repair block**: detach style (fixed / remove / release in place), interaction (hold / screw), tool kind per direction, verbs, times, screw turns, removal direction, hinge (later: price, icon, compatible models, spare prefab) | M1 ✅ · M2 ✅ |
| `PhoneModelDefinition` | model id, brand, name, assembled prefab, **slots** (model object name → PartDefinition, + `blockedBy` / `mountedOn` rules) | M1 ✅ · M2 ✅ |
| `ToolDefinition` | tool id, name, **kind** (Hands, Screwdriver, Spudger, SuctionCup; parts ask for a kind), 3D model, speed multiplier (later: icon, price) | M2 ✅ |
| `FaultDefinition` | symptoms, affected part category, visibility (visible/hidden), fix type (replace/clean/reconnect), readings per diagnostic tool | M3 |
| `CustomerDefinition` / `RepairJobDefinition` | portrait, dialogue lines, phone + faults, budget, patience | M4 |
| `UpgradeDefinition` | cost, unlock condition, effect (tool tier, speed multiplier, auto-action) | M5 |

### Runtime (components on the phone prefab, added automatically by `PhonePrefabBuilder`)

```
PHN_MochiOne (PhoneDevice)            model definition + part index + assembly state (AssemblyGraph)
 ├─ Screw_01 (PhonePart, Interactable, SphereCollider)
 ├─ BackCover (PhonePart, Interactable, MeshCollider)
 │   └─ BackCover_Logo                decorative child, highlights with its part
 ├─ Battery (PhonePart, Interactable, MeshCollider)
 │   └─ BatteryConnector (PhonePart, Interactable, BoxCollider)   pivot = hinge; rides along with the battery
 └─ ...
```

* **`PhonePart`**: one slot in one phone. It remembers its installed pose, a screw's looseness, and a busy flag while it flies. Its state (`Secured / Loose / Removed`) lives in the device's `AssemblyGraph`, and each slot in the `PhoneModelDefinition` carries two lists (M2 ✅):
  * `blockedBy`: parts that must be off first (BackCover ← 4 screws; Battery ← BackCover + BatteryConnector; Screen ← DisplayConnector).
  * `mountedOn`: what must be in place before this one can go back on (screws need the cover; the battery connector needs the battery).
* **`PhoneDevice`**: answers `CanDetach(part)` / `CanAttach(part)` with a `RuleCheck(Allowed, Reason)` from those lists. One generic rule set covers every phone, and the reverse order rebuilds it (a content test checks that every mount is mirrored by a blocker).
* **Repair actions (M2 ✅):** (tool in hand, pressed part) → `RepairInteractor` plans it (direction, tool kind, rules, room on the mat) → a hold action or the screw minigame → `PhoneDevice.SetState` → `PartMover` flies the part to the `PartsArea` (mat spots, screw tray) or back home.
* **Invalid actions** return a human reason ("First: unscrew P2 Mochi Screw (×4)"). The hover tooltip shows it in red before you even press; pressing plays the "nope": a part wiggle, a red flash, a sound and a toast. A tool that doesn't fit the part just inspects it.
* **Faults (M3)** are runtime `FaultInstance`s attached to parts (`Screen: CrackedGlass`, `Battery: Dead`). Visible ones are discovered by inspecting; hidden ones by tools. A repair succeeds when the power-on test finds no active faults and everything is attached.
* **Save data (M5)** stores part *ids* and slot *ids* (stable strings), never asset references.

### Why this shape

* New phone = Blender model with conventional object names + one `PhoneModelDefinition`. No new scripts.
* Selection, highlight and camera know nothing about phones, so tools, shop stations and customers reuse them.
* The rules are plain data, so they're testable in EditMode tests without a scene.

---

## 3. Minimum C# scripts

✅ = implemented and tested (Milestones 1-2).

| Area | Scripts | When |
|---|---|---|
| Input | `GameInput` ✅ | M1 |
| Camera | `CameraPose`, `CameraView`, `CameraDirector`, `InspectionCameraView` ✅ · `FixedCameraView` | M1 · M4 |
| Interaction | `Interactable`, `InspectionInfo`, `InteractionRaycaster`, `SelectionController`, `InteractionHighlighter` ✅ | M1 |
| Devices | `PartCategory`, `PartDefinition`, `PhoneModelDefinition`, `PhoneDevice`, `PhonePart` ✅ | M1 (+rules M2) |
| Workbench | `WorkbenchController`, `InspectionTurntable` ✅ | M1 |
| UI | `SelectionInfoPanel`, `HoverTooltip` ✅ · `ToolBarView`, `ToolButtonView`, `ToastView`, `ActionProgressView` ✅ · `MoneyLabel`, `DialogueBox` | M1 · M2 · M4-M5 |
| Devices (rules) | `PartState`, `AssemblyGraph` + `RuleCheck` ✅ | M2 |
| Repair | `ToolDefinition`, `ToolBelt`, `ToolPresenter`, `RepairInteractor` (+ `RepairPlan`), `ScrewTurnTracker`, `PartsArea`, `PartMover`, `RepairFeedback` ✅ | M2 |
| Audio | `SfxPlayer`, `ProceduralSfx` (placeholder sounds) ✅ | M2 |
| Minigames | screw gesture ✅ (inside `RepairInteractor` + `ScrewTurnTracker`) · `RepairMinigame` base, extracted when `CleaningMinigame` arrives | M2 · M3 |
| Diagnosis | `FaultDefinition`, `FaultInstance`, `DiagnosticReading`, `PowerOnTest` | M3 |
| Inventory | `PartInventory` (spare parts) | M3 |
| Shop | `GameModeController` (Shop ↔ Repair), `Station` | M4 |
| Customers | `CustomerDefinition`, `CustomerController`, `RepairJob` | M4 |
| Economy | `Wallet`, `PaymentCalculator`, `UpgradeDefinition`, `UpgradeShop` | M5 |
| Save | `SaveSystem` (JSON) | M5 |
| Editor | `ProjectSetup`, `PhonePrefabBuilder`, `EnvironmentPrefabBuilder`, `WorkbenchSceneBuilder`, `WorkbenchUiBuilder`, `ModelImportSetup`, `MochiOneDataSetup`, `SceneCapture` ✅ · `ToolDataSetup` ✅ | M1 · M2 |

After M2 there are 49 small runtime and editor scripts (plus 6 test files); expect roughly 75 for the full prototype. No managers-of-managers, no global singletons, and no event bus until two systems actually need one.

---

## 4. Blender asset list

### First phone: `PHN_MochiOne` ✅ (generated by `ArtSource/Blender/Scripts/build_phone_mochi_one.py`)

Chubby 74 × 152 × 10.8 mm phone, cream frame, sakura-pink back with a mochi mascot. Separate objects:
Frame (+ midplate, buttons) · Screen (+ display, front camera, **crack overlay**, DisplayConnector) · BackCover (+ logo) · Screw_01..04 · Battery (+ label, pull tab, BatteryConnector) · Logicboard · ChargingPort · RearCamera · Speaker.
Pivots match interactions: screws pivot on their axis, connectors on their hinge. Full table in `ArtSource/Blender/README.md`.

**Still needed for the prototype (M3):** replacement parts (new screen without crack, new battery; mostly duplicates) and an "on" display texture for the power-on test. (Removed screws simply lie on their side in the tray; no extra state needed.)

### Shop

| Asset | Status |
|---|---|
| Workbench corner: desk, repair mat (grid + ruler), desk lamp, pegboard wall, screw tray (on the mat, 6 compartments, since M2), mug, succulent | ✅ M1-M2 (`build_env_workbench.py`) |
| Tools: precision screwdriver, spudger, suction cup | ✅ M2 (`build_tools.py`) |
| Tools: tweezers, brush, battery tester, multimeter | M3 |
| Shop shell: floor, walls, big window, door with bell, counter with register, customer bench | M4 |
| Dressing: shelves with part boxes, shop sign ("Mochi Fix"?), lucky cat, posters, lights, plants | M4 polish |
| Customer: one stylized base body with swappable heads, hair and clothes; simple rig (idle, walk, talk); 2D expressive portraits | M4 |
| Upgrades with visible art: pro screwdriver, magnifier lamp, second shelf | M5 |

---

## 5. First five milestones

| # | Milestone | Objective | Done when |
|---|---|---|---|
| **M1** | **Interactive phone on a workbench** ✅ | Rotate, zoom and select phone components with the mouse | You can tumble and flip the phone, zoom to any screw, and click any part to see its name and description. Automated tests pass. |
| **M2** | **Take it apart, put it back** ✅ | Tools + part states + rules + screw minigame + reassembly | With the screwdriver you unscrew 4 screws (hold-and-turn minigame), pry the cover, unplug the battery and lift it out, then reverse it. Wrong-order clicks give clear "nope" feedback. |
| **M3** | **Diagnose, replace, test** | One visible fault (cracked screen) and one hidden fault (dead battery), a spare-part inventory, power-on test | The player finds the dead battery with the tester, swaps the parts, and the phone boots with a cute logo. A quality score comes out of it. |
| **M4** | **Customer and shop loop** | Shop scene, fixed shop camera, a customer walks in, portrait dialogue, accept job, blend to the bench, hand the phone back | The full loop works from door to bench to door with one scripted customer. |
| **M5** | **Money and progression** | Payment from quality and time, wallet HUD, buy spares, one tool upgrade (electric screwdriver), save/load | Earn money, buy the upgrade, and the next screw minigame is visibly faster. Progress survives a restart. |

The prototype is complete after M5. Each milestone ends with EditMode/PlayMode tests for its rules plus a manual checklist (see `Docs/M1_Workbench.md` and `Docs/M2_Disassembly.md`).

---

## 6. Camera and mouse interaction

### Cameras

* **`CameraDirector`** owns the only real `Camera`. **`CameraView`s** are virtual cameras that compute a pose; switching views blends position, rotation and FOV with an ease curve. Shop mode, repair mode, customer close-ups and minigame cams are just more views. There's no Cinemachine dependency, and blends work today (the M1 intro shot uses one).
* **Repair mode (`InspectionCameraView`):** fixed, comfortable viewing angle (58° down, 35° FOV for a product-shot look).
  * **The phone rotates, not the camera.** Lighting and the bench stay stable, and it feels like picking the phone up. The `InspectionTurntable` lifts it exactly as much as its rotated bounds need (it never clips the mat) and settles it flat when released near flat.
  * **Zoom towards the cursor:** the camera rig scales about the point under the cursor, so that point stays put on screen. The focus is clamped to a radius that shrinks to zero at max zoom-out, so **zooming out always recentres** and you can't get lost.
  * The focus is stored **in the phone's local space**, so a component you zoomed to stays centred while you rotate the phone.
  * Double-click or F frames the selected component.
* **Shop mode (M4):** `FixedCameraView`, slightly isometric. Clicking a station asks the director to blend to that station's view.

### Mouse and keyboard (M1 + M2)

| Input | Action |
|---|---|
| Left click | select part (empty space = deselect) |
| Double-click / **F** | frame the part |
| Right drag, **WASD** / arrows | rotate the phone (trackball) |
| Left drag on empty space | rotate (trackpad friendly) |
| Right click (no drag) / **Esc** | deselect |
| Wheel | zoom to cursor |
| Middle drag | pan |
| **Space** | flip over |
| **R** | reset view |
| **1-4** / tool bar | Hands · Screwdriver · Spudger · Suction Cup (M2) |
| **Hold left on a part** | use the tool in hand: unscrew, pry, unplug, lift out, put back (M2) |
| Screwdriver: **circle** while holding | counter-clockwise loosens faster, clockwise tightens (M2) |

Left button = "hands/tools" on purpose: since M2, pressing **on a part** uses the tool in hand (a quick click with a tool that doesn't fit still just inspects), so rotation lives on the right button.

### Picking pipeline

`GameInput` → **one** active mode controller (`WorkbenchController` now, `ShopController` in M4; minigames borrow input while running) → `InteractionRaycaster` (UI blocks the world, layer mask, nearest hit wins, so the cover hides the battery) → `SelectionController` (hover/selection state plus events) → `InteractionHighlighter` (additive rim glow drawn with `Graphics.RenderMesh`; it never touches object materials) and the UI.

---

## 7. Implementation order (fastest path to playable)

1. ✅ Project baseline: URP, folders, asmdefs, input, TMP, test assemblies.
2. ✅ Art pipeline: Blender generator → FBX → import rules → prefab builder by slot name.
3. ✅ **M1:** inspection camera, turntable, picking, selection, HUD.
4. ✅ **Part states + rules** (pure C# with EditMode tests first), then one tool: a screwdriver that instantly removes screws. The loop becomes playable at this point.
5. ✅ **Screw minigame + juice** (sound, flights, squash, screws dropping into the tray; particles still to come). This is the game's signature "feel", so iterate on it early.
6. ✅ Cover, connectors and battery, plus reassembly. **M2 done.**
7. Cracked screen → replace from inventory → power-on test. Hidden battery fault + tester. **M3 done.**
8. Text-only customer stub to prove the job flow, then the shop scene, walk-in, and portraits. **M4 done.**
9. Payment, wallet, one upgrade, save/load. **M5 done = prototype.**
10. Juice and readability pass, then a small playtest.

**Rules of thumb:** graybox every step end to end before polishing. Don't build systems for features that aren't in the next milestone. Keep everything data-driven where a second instance is guaranteed (phones, parts, tools, customers). Keep the rest simple.

### Risks to watch

* **Scope creep** (management sim features). Hold the line at M5.
* **Art pipeline drift** (axes, scale, renamed objects). EditMode tests already guard this.
* **Input conflicts** once minigames arrive. Only one input owner at a time.
* **Fiddly tiny parts.** Generous colliders (screws already get 1.45× the head radius), zoom to cursor, and frame-on-double-click.

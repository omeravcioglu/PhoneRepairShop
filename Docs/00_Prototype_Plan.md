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
│  │  │  ├─ Materials/  Phone/ Environment/ FX/
│  │  │  ├─ Models/     Phones/<Model>/  Environment/  (Tools/ Characters/ later)
│  │  │  ├─ Shaders/    InteractableHighlight.shader
│  │  │  └─ UI/         UI_RoundedRect.png
│  │  ├─ Data/                      ScriptableObject *instances* = game content
│  │  │  └─ Phones/MochiOne/        PHN_MochiOne_Model.asset, Parts/PRT_*.asset
│  │  │     (Tools/ Faults/ Customers/ Upgrades/ arrive in M2-M5)
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
│  │     │  ├─ UI/            HUD widgets
│  │     │  └─ (Repair/ Diagnosis/ Minigames/ Customers/ Economy/ Save/ in later milestones)
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
| `PartDefinition` | *type* of part: id, name, category, description (later: price, icon, compatible models, spare prefab) | M1 ✅ |
| `PhoneModelDefinition` | model id, brand, name, assembled prefab, **slots** (model object name → PartDefinition) | M1 ✅ |
| `ToolDefinition` | tool id, icon, which **actions** it enables (Unscrew, Pry, Unplug, Lift, Clean, Measure), speed, price | M2 |
| `FaultDefinition` | symptoms, affected part category, visibility (visible/hidden), fix type (replace/clean/reconnect), readings per diagnostic tool | M3 |
| `CustomerDefinition` / `RepairJobDefinition` | portrait, dialogue lines, phone + faults, budget, patience | M4 |
| `UpgradeDefinition` | cost, unlock condition, effect (tool tier, speed multiplier, auto-action) | M5 |

### Runtime (components on the phone prefab, added automatically by `PhonePrefabBuilder`)

```
PHN_MochiOne (PhoneDevice)            model definition + part index      (M2: assembly rules)
 ├─ Screw_01 (PhonePart, Interactable, SphereCollider)
 ├─ BackCover (PhonePart, Interactable, MeshCollider)
 │   └─ BackCover_Logo                decorative child, highlights with its part
 ├─ BatteryConnector (PhonePart, Interactable, BoxCollider)   pivot = hinge
 └─ ...
```

* **`PhonePart`**: one slot in one phone. M2 adds `AttachmentState { Attached, Detached, Removed }` plus two lists:
  * `blockedBy`: parts that must be detached before this one can come off (BackCover ← 4 screws; Battery ← BackCover + BatteryConnector; Screen ← DisplayConnector).
  * `mountedOn`: what must be attached for this one to go back on (screws need the cover; the battery connector needs the battery).
* **`PhoneDevice`**: answers `CanDetach(part, out reason)` and `CanAttach(part, out reason)` from those lists. This one generic rule set covers every phone. Reassembly is the reverse order for free.
* **Repair actions (M2):** `(selected tool, clicked part)` → `RepairAction` (Unscrew, PryOff, Unplug, Lift, Install...) → validated by `PhoneDevice` → runs a **minigame** or a short animation → changes the part's state.
* **Invalid actions** return a human reason ("Take out the 4 screws first"). The UI plays the "nope" feedback: a part wiggle, a red flash in the highlighter, a tooltip, and a sound.
* **Faults (M3)** are runtime `FaultInstance`s attached to parts (`Screen: CrackedGlass`, `Battery: Dead`). Visible ones are discovered by inspecting; hidden ones by tools. A repair succeeds when the power-on test finds no active faults and everything is attached.
* **Save data (M5)** stores part *ids* and slot *ids* (stable strings), never asset references.

### Why this shape

* New phone = Blender model with conventional object names + one `PhoneModelDefinition`. No new scripts.
* Selection, highlight and camera know nothing about phones, so tools, shop stations and customers reuse them.
* The rules are plain data, so they're testable in EditMode tests without a scene.

---

## 3. Minimum C# scripts

✅ = implemented and tested in Milestone 1.

| Area | Scripts | When |
|---|---|---|
| Input | `GameInput` ✅ | M1 |
| Camera | `CameraPose`, `CameraView`, `CameraDirector`, `InspectionCameraView` ✅ · `FixedCameraView` | M1 · M4 |
| Interaction | `Interactable`, `InspectionInfo`, `InteractionRaycaster`, `SelectionController`, `InteractionHighlighter` ✅ | M1 |
| Devices | `PartCategory`, `PartDefinition`, `PhoneModelDefinition`, `PhoneDevice`, `PhonePart` ✅ | M1 (+rules M2) |
| Workbench | `WorkbenchController`, `InspectionTurntable` ✅ | M1 |
| UI | `SelectionInfoPanel`, `HoverTooltip` ✅ · `ToolBar`, `MoneyLabel`, `DialogueBox` | M1 · M2-M5 |
| Repair | `ToolDefinition`, `ToolBelt`, `RepairAction`, `PartMover` (lift/remove/attach tweens), `ActionFeedback` | M2 |
| Minigames | `RepairMinigame` (base), `ScrewMinigame`, `CleaningMinigame` | M2-M3 |
| Diagnosis | `FaultDefinition`, `FaultInstance`, `DiagnosticReading`, `PowerOnTest` | M3 |
| Inventory | `PartInventory` (spare parts) | M3 |
| Shop | `GameModeController` (Shop ↔ Repair), `Station` | M4 |
| Customers | `CustomerDefinition`, `CustomerController`, `RepairJob` | M4 |
| Economy | `Wallet`, `PaymentCalculator`, `UpgradeDefinition`, `UpgradeShop` | M5 |
| Save | `SaveSystem` (JSON) | M5 |
| Editor | `ProjectSetup`, `PhonePrefabBuilder`, `EnvironmentPrefabBuilder`, `WorkbenchSceneBuilder`, `ModelImportSetup`, `SceneCapture` ✅ | M1 |

Roughly 45 small scripts for the full prototype. No managers-of-managers, no global singletons, and no event bus until two systems actually need one.

---

## 4. Blender asset list

### First phone: `PHN_MochiOne` ✅ (generated by `ArtSource/Blender/Scripts/build_phone_mochi_one.py`)

Chubby 74 × 152 × 10.8 mm phone, cream frame, sakura-pink back with a mochi mascot. Separate objects:
Frame (+ midplate, buttons) · Screen (+ display, front camera, **crack overlay**) · BackCover (+ logo) · Screw_01..04 · Battery (+ label, pull tab) · BatteryConnector · DisplayConnector · Logicboard · ChargingPort · RearCamera · Speaker.
Pivots match interactions: screws pivot on their axis, connectors on their hinge. Full table in `ArtSource/Blender/README.md`.

**Still needed for the prototype (M2-M3):** replacement parts (new screen without crack, new battery; mostly duplicates), a removed-screw state for the tray, and an "on" display texture for the power-on test.

### Shop

| Asset | Status |
|---|---|
| Workbench corner: desk, repair mat (grid + ruler), desk lamp, pegboard wall, screw tray, mug, succulent | ✅ M1 (`build_env_workbench.py`) |
| Tools: screwdriver, spudger, tweezers, suction cup, brush, battery tester, multimeter | M2-M3 |
| Shop shell: floor, walls, big window, door with bell, counter with register, customer bench | M4 |
| Dressing: shelves with part boxes, shop sign ("Mochi Fix"?), lucky cat, posters, lights, plants | M4 polish |
| Customer: one stylized base body with swappable heads, hair and clothes; simple rig (idle, walk, talk); 2D expressive portraits | M4 |
| Upgrades with visible art: pro screwdriver, magnifier lamp, second shelf | M5 |

---

## 5. First five milestones

| # | Milestone | Objective | Done when |
|---|---|---|---|
| **M1** | **Interactive phone on a workbench** ✅ | Rotate, zoom and select phone components with the mouse | You can tumble and flip the phone, zoom to any screw, and click any part to see its name and description. Automated tests pass. |
| **M2** | **Take it apart, put it back** | Tools + part states + rules + screw minigame + reassembly | With the screwdriver you unscrew 4 screws (hold-and-turn minigame), pry the cover, unplug the battery and lift it out, then reverse it. Wrong-order clicks give clear "nope" feedback. |
| **M3** | **Diagnose, replace, test** | One visible fault (cracked screen) and one hidden fault (dead battery), a spare-part inventory, power-on test | The player finds the dead battery with the tester, swaps the parts, and the phone boots with a cute logo. A quality score comes out of it. |
| **M4** | **Customer and shop loop** | Shop scene, fixed shop camera, a customer walks in, portrait dialogue, accept job, blend to the bench, hand the phone back | The full loop works from door to bench to door with one scripted customer. |
| **M5** | **Money and progression** | Payment from quality and time, wallet HUD, buy spares, one tool upgrade (electric screwdriver), save/load | Earn money, buy the upgrade, and the next screw minigame is visibly faster. Progress survives a restart. |

The prototype is complete after M5. Each milestone ends with EditMode/PlayMode tests for its rules plus a manual checklist (see `Docs/M1_Workbench.md` for the format).

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

### Mouse and keyboard (M1)

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

Left button = "hands/tools" on purpose: in M2 a left click or drag **on a part** becomes the tool action, so rotation lives on the right button.

### Picking pipeline

`GameInput` → **one** active mode controller (`WorkbenchController` now, `ShopController` in M4; minigames borrow input while running) → `InteractionRaycaster` (UI blocks the world, layer mask, nearest hit wins, so the cover hides the battery) → `SelectionController` (hover/selection state plus events) → `InteractionHighlighter` (additive rim glow drawn with `Graphics.RenderMesh`; it never touches object materials) and the UI.

---

## 7. Implementation order (fastest path to playable)

1. ✅ Project baseline: URP, folders, asmdefs, input, TMP, test assemblies.
2. ✅ Art pipeline: Blender generator → FBX → import rules → prefab builder by slot name.
3. ✅ **M1:** inspection camera, turntable, picking, selection, HUD.
4. **Part states + rules** (pure C# with EditMode tests first), then one tool: a screwdriver that instantly removes screws. The loop becomes playable at this point.
5. **Screw minigame + juice** (sound, particles, screws dropping into the tray). This is the game's signature "feel", so iterate on it early.
6. Cover, connectors and battery, plus reassembly. **M2 done.**
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

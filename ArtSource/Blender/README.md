# Blender pipeline

Source art lives here, **outside `Assets/`**. If a `.blend` sits inside `Assets/`, Unity tries to import it by running Blender, which is slow and fragile. Unity only ever sees the exported FBX files.

```
ArtSource/Blender/
  Scripts/        procedural generators + shared helpers (prs_bpy.py)
  Phones/         PHN_*.blend   (generated, then hand-editable)
  Environment/    ENV_*.blend
  Tools/          TOOLS_*.blend (repair tools, Milestone 2)
  Previews/       PNG renders written by the scripts (quick visual check)
```

## Regenerate an asset (Blender 5.2, headless)

Run these from the project root:

```bash
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" --background --factory-startup --python ArtSource/Blender/Scripts/build_phone_mochi_one.py
```

```bash
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" --background --factory-startup --python ArtSource/Blender/Scripts/build_env_workbench.py
```

```bash
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" --background --factory-startup --python ArtSource/Blender/Scripts/build_tools.py
```

Each script writes the `.blend`, exports the FBX into `Assets/_Project/Art/Models/...`, and renders previews. Add `-- --no-preview` to skip the renders. Unity picks the new FBX up on focus. The prefabs are **variants** of the FBX, so components survive a re-export as long as object names stay the same.

## Conventions (all assets)

| Rule | Why |
|---|---|
| Metres, scene unit scale 1.0 | 1 Blender unit = 1 Unity unit |
| **+Y = forward/away from the player, +Z = up** | exported Z-up and imported with Unity's *Bake Axis Conversion*: Unity (x, y, z) = Blender (x, z, y), clean rotations everywhere |
| Export via `prs_bpy.export_fbx_for_unity()` | one place for FBX settings (scale *FBX All*, Z-up, modifiers applied, **no** experimental *Apply Transform*, which rotates nested objects 90°) |
| Modifiers stay live in the .blend | bevels/booleans are applied only on export |
| Material **names** are the contract | Unity remaps `M_*` slots to its own URP materials by name. Blender colours are only for previews. |
| Prefixes: `PHN_` phone, `ENV_` environment, `TOOL_` tool, `CHR_` character, `M_` material | searchable, sortable |

## Phone-specific rules (gameplay first)

* **Every clickable or replaceable part is its own object**, parented to the phone root (`PHN_<Model>` empty at the phone's geometric centre), or to the part it physically belongs to and travels with. Example: a flex connector rides on its battery or screen, so it comes out with it.
* **Object names are slot ids.** `PhoneModelDefinition` maps them to `PartDefinition` assets. Renaming an object breaks the mapping (the EditMode test `EverySlot_HasAPartAnInteractableAndAPickableCollider` catches it).
* **Pivots are where the interaction happens:**
  * screws: origin at the head base, local **+Z = unscrew direction** (Unity local +Y)
  * connectors: origin on the hinge line, so a flip-up rotation unplugs them
  * everything else: centred
* Decorative sub-meshes (labels, logo, crack overlay, display) are **children of the part they belong to**. They highlight and move with it.
* Phones lie **screen up (+Z), top edge toward +Y** (upright in Blender's top view). In Unity that becomes screen +Y and top edge +Z.
* Characters face **+Y** in Blender, which is Unity **+Z = `transform.forward`** (see the Phone Fixer section below).

## PHN_MochiOne (first phone): part list

| Object | Category | Notes |
|---|---|---|
| `Frame` (+ `Frame_Midplate`, `Frame_Buttons`) | Chassis | cream band, port slot (boolean), screw bosses |
| `Screen` (+ `Screen_Display`, `Screen_FrontCam`, `Screen_Crack`) | Display | crack overlay is a separate child, so it can be toggled per condition |
| `BackCover` (+ `BackCover_Logo`) | Cover | camera window + 4 screw holes, mochi mascot logo |
| `Screw_01`..`Screw_04` | Fastener | numbered from the back, top-left, clockwise |
| `Battery` (+ `Battery_Label`, `Battery_PullTab`) | Battery | |
| `BatteryConnector` (child of `Battery`), `DisplayConnector` (child of `Screen`) | Connector | hinge pivots; unplugging flips them 70° about Blender X / Y (Unity local X / Z) |
| `Logicboard` | Logic Board | chips, shield can, test pads, smiley sticker |
| `ChargingPort` | Port | mouth sits in the frame slot |
| `RearCamera` | Camera | pokes through the cover window |
| `Speaker` | Audio | |

Size: 74 × 152 × 10.8 mm, about 10k vertices in total. Low-poly with bevels and hardened normals.

## Adding a new phone

1. Copy `build_phone_mochi_one.py` (or model by hand), keeping the conventions above.
2. Export the FBX to `Assets/_Project/Art/Models/Phones/<Model>/PHN_<Model>.fbx`.
3. In Unity, create the `PartDefinition`s (or reuse shared ones like `PRT_Screw_P2`) and a `PhoneModelDefinition` whose slots list the object names.
4. Build the prefab with `PhonePrefabBuilder` (the setup calls it for the Mochi One; a menu item for any definition will come with the second phone).
5. Fill each part's *Repair* block and each slot's *Blocked By* / *Mounted On* lists (see `Docs/M2_Disassembly.md`, section 3).

## Workbench (`build_env_workbench.py`)

* Desk top at 0.75 m, mat top at 0.754 m, mat centre (0, -0.05). Unity's `WorkbenchSceneBuilder` uses the same numbers.
* `ScrewTray` sits on the mat, behind and to the right of the phone. The empties `ScrewTray_Slot_0..5` mark its six compartment floors, inner column first. Unity's `PartsArea` drops removed screws there, so keep the names.

## Tools (`build_tools.py`, Milestone 2)

| Object | Notes |
|---|---|
| `TOOL_Screwdriver` | P2 precision driver: steel shaft and cross tip, coral handle with grip rings |
| `TOOL_Spudger` | lilac nylon pry stick with a flat blade |
| `TOOL_SuctionCup` | sky-blue cup with a ring pull |

* **Origin = the working tip** (what touches the phone). Each tool runs along **+Z towards the handle**, which is Unity +Y. The game seats the tip on the part and scales the model to a constant on-screen size, so proportions matter more than absolute size.
* One FBX per tool in `Assets/_Project/Art/Models/Tools/`. Materials are `M_Tool_*`, remapped to the URP materials in `Art/Materials/Tools/`.

---

# Phone Fixer art pass (art direction v1, "Pocket Diorama")

The art-direction canvas defines the look; these generators build it. They share `pf_bpy.py` on top of `prs_bpy.py`.

| Script | Output (FBX in `Assets/_Project/Art/Models/...`) | Blend / previews |
|---|---|---|
| `build_pf_repair_bench.py` | `Environment/PF/ENV_PF_RepairBench.fbx` | `Environment/ENV_PF_RepairBench.blend`, `Previews/PF/ENV_PF_RepairBench_quick.png` |
| `build_pf_tools.py` | `Tools/PF/TOOL_PF_{Screwdriver,Spudger,SuctionCup,Tweezers,Brush,Magnifier,Tester}.fbx` | `Tools/TOOLS_PF.blend` |
| `build_phone_mochi_dango.py` | `Phones/MochiDango/PHN_MochiDango.fbx` | `Phones/PHN_MochiDango.blend`, `Previews/PF/PHN_MochiDango_*.png` |
| `build_pf_shop.py` | `Environment/PF/ENV_PF_Shop.fbx` | `Environment/ENV_PF_Shop.blend`, `Previews/PF/ENV_PF_Shop_quick.png` |
| `build_pf_customers.py` | `Characters/PF/CHR_PF_{Base,Fumi,Ren,Sasaki,Mio,Kanemochi}.fbx` + `Art/UI/Portraits/PF_Portrait_*.png` | `Characters/CHR_PF_Customers.blend`, `Previews/PF/CHR_PF_Lineup.png` |

Run each like the others, from the project root:

```bash
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" --background --factory-startup --python ArtSource/Blender/Scripts/build_pf_shop.py
```

Then in Unity: **Tools > Phone Fixer > Run Art Setup** (or batch `-executeMethod PhoneRepair.EditorTools.PhoneFixerSetup.RunBatch`).

## Palette = materials

* `pf_bpy.PALETTE` is the single source of truth: `M_PF_<Name>: (hex, smoothness, metallic, emission, kind)`, with `kind` one of `lit`, `unlit`, `glass`.
* Every PF script writes `Assets/_Project/Art/Materials/PhoneFixer/pf_palette.json`. Unity's `PhoneFixerMaterials` turns each row into a URP material of the same name. Existing materials are kept, so tweaks made in Unity survive.
* Add a material: add a row to `PALETTE` and use `PF.M("M_PF_<Name>")`. The EditMode test `PfModels_UseOnlyPaletteMaterials` fails if a model uses a material that is not in the palette.
* Gameplay hues are reserved and never used for decoration:
  * orange `#F2A65A`: you can pick it (`M_PF_PickMe`)
  * teal `#6FA9A3`: selected / in use
  * coral `#DE8175`: damaged
  * leaf `#8CCB6E`: fixed

## Mochi Dango (hero phone)

Same contract as the Mochi One: same slot names, same pivots, same assembly graph. Damage states are child meshes named `<Part>_<State>`, all exported. Unity's `PhoneFaultVisuals` shows the active ones and hides the rest:

| Damage mesh | Shows |
|---|---|
| `Screen_Crack` | spider-web crack on the glass |
| `Battery_Bulge` | swollen pack |
| `Battery_Warning` | coral warning sticker |
| `ChargingPort_Lint` | lint and crumbs in the port |
| `Logicboard_Corrosion` | blue-green corrosion blooms |
| `DisplayConnector_Tear` | torn flex, bent pins |
| `RearCamera_Scratch` | scratched lens |

Size: 76 × 156 × 11 mm.

## Shop diorama (`ENV_PF_Shop`)

* **Layout.** The origin is the inner front-left corner of the room. The room is 6.0 × 4.5 m with 2.6 m walls.
  * Storefront: the left wall (X = 0), with the window and the sliding door.
  * Cut away: the front (Y = 0) and right (X = 6) walls.
* **Shadow proxies.** `ShadowProxy_Ceiling/Front/Right` close the box for the sun only. Unity sets them to *Shadows Only* with no collider, so sunlight enters only through the storefront.
* **Empties for the scene builder.**
  * `Spot_Bench` places the separate bench asset.
  * `Spot_Customer_Counter`, `Spot_Queue_1`, `Spot_Door_Inside/Outside` and `Spot_Waiting_*` place customers.
  * `Light_Pendant_*` mark the bulbs.
* **Upgrades.** The tier-0 room is built so tiers can be added as separate dressing sets. `Shop_UpgradeSlot` (orange floor tape) marks where the first upgrade goes.

## Customers (`CHR_PF_*`)

* **Rig.** One rig with 23 bones and rigid skinning (one bone per body part).
  * Every file has an `Armature` with a `Body` child.
  * Bone names are identical in all files, so the clips exported only in `CHR_PF_Base` drive every variant. The test `Customers_BaseClipsBindToEveryVariant` checks this.
* **Size.** Everyone is built at the base height (1.38 m) and faces +Y (Unity +Z). Unity scales the prefab root per character (`HEIGHT_SCALE`), so root and hip motion stay consistent.
* **Face bones.**
  * Eye scale Z: blink.
  * Brow rotation about Y: tilt.
  * Mouth rotated 180° about Y: frown.
  * Never key a negative or zero scale. The FBX SDK turns it into NaN rotations; Unity warns and drops the curve.
* **Clips** (30 fps):
  * Idle, Walk, Talk and Impatient loop.
  * Celebrate, Disappointed and Confused are one-shot gestures.

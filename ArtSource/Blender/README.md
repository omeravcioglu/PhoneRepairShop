# Blender pipeline

Source art lives here, **outside `Assets/`**. If a `.blend` sits inside `Assets/`, Unity tries to import it by running Blender, which is slow and fragile. Unity only ever sees the exported FBX files.

```
ArtSource/Blender/
  Scripts/        procedural generators + shared helpers (prs_bpy.py)
  Phones/         PHN_*.blend   (generated, then hand-editable)
  Environment/    ENV_*.blend
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

* **Every clickable or replaceable part is its own object**, parented directly to the phone root (`PHN_<Model>` empty at the phone's geometric centre).
* **Object names are slot ids.** `PhoneModelDefinition` maps them to `PartDefinition` assets. Renaming an object breaks the mapping (the EditMode test `EverySlot_HasAPartAnInteractableAndAPickableCollider` catches it).
* **Pivots are where the interaction happens:**
  * screws: origin at the head base, local **+Z = unscrew direction** (Unity local +Y)
  * connectors: origin on the hinge line, so a flip-up rotation unplugs them
  * everything else: centred
* Decorative sub-meshes (labels, logo, crack overlay, display) are **children of the part they belong to**. They highlight and move with it.
* Phones lie **screen up (+Z), top edge toward +Y** (upright in Blender's top view). In Unity that becomes screen +Y and top edge +Z.
* Characters (later): model them facing -Y as usual in Blender, so they face a Unity camera that looks along +Z.

## PHN_MochiOne (first phone): part list

| Object | Category | Notes |
|---|---|---|
| `Frame` (+ `Frame_Midplate`, `Frame_Buttons`) | Chassis | cream band, port slot (boolean), screw bosses |
| `Screen` (+ `Screen_Display`, `Screen_FrontCam`, `Screen_Crack`) | Display | crack overlay is a separate child, so it can be toggled per condition |
| `BackCover` (+ `BackCover_Logo`) | Cover | camera window + 4 screw holes, mochi mascot logo |
| `Screw_01`..`Screw_04` | Fastener | numbered from the back, top-left, clockwise |
| `Battery` (+ `Battery_Label`, `Battery_PullTab`) | Battery | |
| `BatteryConnector`, `DisplayConnector` | Connector | hinge pivots |
| `Logicboard` | Logic Board | chips, shield can, test pads, smiley sticker |
| `ChargingPort` | Port | mouth sits in the frame slot |
| `RearCamera` | Camera | pokes through the cover window |
| `Speaker` | Audio | |

Size: 74 × 152 × 10.8 mm, about 10k vertices in total. Low-poly with bevels and hardened normals.

## Adding a new phone

1. Copy `build_phone_mochi_one.py` (or model by hand), keeping the conventions above.
2. Export the FBX to `Assets/_Project/Art/Models/Phones/<Model>/PHN_<Model>.fbx`.
3. In Unity, create the `PartDefinition`s (or reuse shared ones like `PRT_Screw_P2`) and a `PhoneModelDefinition` whose slots list the object names.
4. Build the prefab with `PhonePrefabBuilder` (it is called by the setup for the Mochi One; M2 adds a menu item for any definition).

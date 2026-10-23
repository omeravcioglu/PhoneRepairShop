# Phone Repair Shop

**A hands-on repair sim made in Unity 6, in early production.** Broken phones come into your shop. You turn them over on the workbench, find what's wrong, open them up part by part with the right tools, fix them, and get paid.

| | |
|---|---|
| ![The workbench](Docs/Images/m1_01_intro.png) | ![Inspecting a cracked Mochi One](Docs/Images/m1_02_inspection.png) |
| ![The back of the phone](Docs/Images/m1_03_back.png) | ![A screw selected](Docs/Images/m1_04_screw_selected.png) |

---

## Status

The project started in October 2026.
- **The prototype's goal:** one customer and one broken *Mochi One* phone, repaired all the way from walk-in to payment, plus one tool upgrade.
- **Milestone 1 is done.** It's an interactive phone on a workbench:
  - rotate it like a trackball and zoom toward the cursor;
  - select any component (it highlights) and frame it;
  - pan, flip and reset.

  The scene is assembled by an editor setup tool and covered by automated tests.
- **Milestone 2 is under way.** The repair tools (precision screwdriver, spudger, suction cup) are modeled, and the repair code is in: unscrewing, moving parts aside and repair feedback.

## Tech stack

| Area | What it uses |
|---|---|
| Engine | **Unity 6** (6000.0.32f1), **URP** 17, Input System, uGUI + TextMesh Pro |
| Art | **Procedural Python generators for Blender** that build the phone, the workbench and the tools, then export FBX. They live outside `Assets/`, so Unity only ever imports the FBX. |
| Scene setup | Editor builders that create the scene, prefabs, materials and data assets headlessly; safe to run again |
| Audio | Sound effects synthesized in code |
| Testing | Unity Test Framework: 26 EditMode and 7 PlayMode tests, plus a verification log from a headless Unity and Blender run |

## What I built

All of it: 54 C# scripts (about 7.8k lines) and 4 Blender generator scripts (about 1.2k lines of Python).

| Area | Where |
|---|---|
| Runtime: cameras, controls, devices and parts, interaction (raycasting, selection, highlighting), the repair loop, UI, the workbench, audio | `Assets/_Project/Scripts/Runtime/` (36 scripts) |
| Repair tools and parts | `Runtime/Repair/` (`RepairInteractor`, `ScrewTurnTracker`, `PartMover`, `PartsArea`, `ToolBelt`, `ToolPresenter`, `RepairFeedback`) |
| Editor: scene and prefab builders, the milestone setup, tools | `Assets/_Project/Scripts/Editor/` (13 scripts) |
| Tests | `Assets/_Project/Scripts/Tests/` (EditMode and PlayMode) |
| Blender generators | `ArtSource/Blender/Scripts/`: `build_phone_mochi_one.py`, `build_env_workbench.py`, `build_tools.py`, and the shared helpers `prs_bpy.py` |

## Scenes

| Scene | Purpose |
|---|---|
| `Assets/_Project/Scenes/Sandbox/M1_Workbench.unity` | Milestone 1: the phone on the workbench |

## Integrated third-party assets

No Asset Store content. The only third-party content is **TextMesh Pro's** essential resources from Unity.

## Docs

- `Docs/00_Prototype_Plan.md`: the plan for the first playable prototype, covering the folder structure, systems and milestones
- `Docs/M1_Workbench.md`: Milestone 1, covering controls, the scene, how it works, testing and a verification log
- `ArtSource/Blender/README.md`: the Blender pipeline

## About this repository

This public repository is a **showcase**. It contains the documentation and the **83 source files I wrote** for this project. The complete Unity project, with the models, the generated .blend files and its Unity Cloud link, is kept in a private repository.

Copyright © Omer Avcioglu (McHunter Studio). **All rights reserved.** Viewing only; see [LICENSE](LICENSE).

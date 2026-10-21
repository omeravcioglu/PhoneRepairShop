using System;
using System.IO;
using System.Linq;
using PhoneRepair.Cameras;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Renders review screenshots of the workbench scene without entering Play mode (works headless).
    /// Milestone 2 shots stage a disassembly in memory; the scene file is never modified.
    /// Menu:  Tools > Phone Repair > Capture Workbench Screenshots   (writes to ./Screenshots)
    /// Batch: -executeMethod PhoneRepair.EditorTools.SceneCapture.CaptureBatch -captureOut &lt;folder&gt;
    /// </summary>
    public static class SceneCapture
    {
        private const int Width = 1600;
        private const int Height = 900;

        [MenuItem("Tools/Phone Repair/Capture Workbench Screenshots", priority = 20)]
        public static void CaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string folder = Path.GetFullPath("Screenshots");
            CaptureWorkbench(folder);
            EditorSceneManager.OpenScene(WorkbenchSceneBuilder.ScenePath); // discard the staged state
            EditorUtility.RevealInFinder(folder);
        }

        public static void CaptureBatch()
        {
            try
            {
                CaptureWorkbench(GetArgument("-captureOut") ?? Path.GetFullPath("Screenshots"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureWorkbench(string folder)
        {
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene(WorkbenchSceneBuilder.ScenePath, OpenSceneMode.Single);
            DynamicGI.UpdateEnvironment();

            Camera camera = Camera.main ?? throw new InvalidOperationException("No MainCamera in the scene.");
            var view = Object.FindFirstObjectByType<InspectionCameraView>() ?? throw new InvalidOperationException("No InspectionCameraView.");
            var device = Object.FindFirstObjectByType<PhoneDevice>() ?? throw new InvalidOperationException("No PhoneDevice.");
            var area = Object.FindFirstObjectByType<PartsArea>();
            Transform pivot = view.Anchor;
            _ = device.Parts; // index parts and capture their installed poses before anything moves

            var highlight = AssetDatabase.LoadAssetAtPath<Material>(MaterialLibrary.HighlightMaterialPath);
            var block = new MaterialPropertyBlock();
            var selectedColor = new Color(0.5f, 1.05f, 1.2f);

            // The very first render after loading has no colour-grading LUT yet; render once and discard.
            Render(camera, CameraPose.FromCamera(camera), null);
            Render(camera, CameraPose.FromCamera(camera), Path.Combine(folder, "m1_01_intro.png"));

            view.ResetView(instant: true);
            Render(camera, view.GetPose(), Path.Combine(folder, "m1_02_inspection.png"));

            Quaternion rest = pivot.rotation;
            pivot.rotation = Quaternion.AngleAxis(180f, Vector3.forward) * rest; // back side up
            view.ResetView(instant: true);
            Render(camera, view.GetPose(), Path.Combine(folder, "m1_03_back.png"));

            // --- Milestone 2: screwdriver seated in a screw ----------------------------------------
            PhonePart screw = FindPart(device, "Screw_02");
            view.FocusOn(screw.GetComponent<Interactable>().GetWorldBounds(), screw.transform);
            view.Zoom(-2.5f, Vector2.zero);
            view.SnapToTarget();
            CameraPose closeUp = view.GetPose();
            GameObject tool = PlaceToolOnScrew("Assets/_Project/Data/Tools/TOOL_Screwdriver.asset", screw, closeUp);
            Render(camera, closeUp, Path.Combine(folder, "m2_01_screwdriver.png"));
            if (tool != null)
            {
                Object.DestroyImmediate(tool);
            }

            // --- Milestone 2: opened up, parts laid out ----------------------------------------------
            if (area != null)
            {
                foreach (string id in new[] { "Screw_01", "Screw_02", "Screw_03", "Screw_04", "BackCover" })
                {
                    Remove(device, area, FindPart(device, id));
                }

                PhonePart connector = FindPart(device, "BatteryConnector");
                device.SetState(connector, PartState.Loose);
                connector.transform.localRotation = connector.InstalledLocalRotation *
                                                    Quaternion.AngleAxis(connector.Definition.ReleaseAngle, connector.Definition.ReleaseAxis);
                Remove(device, area, FindPart(device, "Battery"));

                view.ResetView(instant: true);
                view.Zoom(-1.5f, Vector2.zero);
                view.SnapToTarget();
                Render(camera, view.GetPose(), Path.Combine(folder, "m2_02_opened.png"));

                // Overlay shot LAST: outside Play mode, Graphics.RenderMesh draws stay queued for every
                // render in the same editor tick, so an earlier overlay would bleed into later shots.
                PhonePart board = FindPart(device, "Logicboard");
                Interactable boardTarget = board.GetComponent<Interactable>();
                view.FocusOn(boardTarget.GetWorldBounds(), board.transform);
                view.SnapToTarget();
                InteractionHighlighter.DrawOverlay(boardTarget, highlight, block, selectedColor);
                Render(camera, view.GetPose(), Path.Combine(folder, "m2_03_inside_selected.png"));
            }

            pivot.rotation = rest;
            // The scene is intentionally not saved: captures never modify it.
        }

        private static void Remove(PhoneDevice device, PartsArea area, PhonePart part)
        {
            if (!area.TryReserve(part, out PartsArea.Slot slot, out Pose pose))
            {
                Debug.LogWarning($"[PhoneRepair] Capture: no room for {part.name}.");
                return;
            }

            part.transform.SetParent(slot.Anchor, false);
            part.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
            device.SetState(part, PartState.Removed);
        }

        private static GameObject PlaceToolOnScrew(string toolPath, PhonePart screw, CameraPose cameraPose)
        {
            var tool = AssetDatabase.LoadAssetAtPath<ToolDefinition>(toolPath);
            if (tool == null || tool.Model == null)
            {
                return null;
            }

            GameObject go = Object.Instantiate(tool.Model);
            float length = go.GetComponentsInChildren<Renderer>().Max(r => r.localBounds.max.y);
            float head = screw.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null ? filter.sharedMesh.bounds.max.y : 0f;
            Transform t = screw.transform;
            Vector3 tip = t.TransformPoint(new Vector3(0f, head, 0f));
            Vector3 axis = t.up;
            Vector3 forward = Vector3.ProjectOnPlane(cameraPose.Rotation * Vector3.forward, axis).normalized;
            go.transform.SetPositionAndRotation(tip, Quaternion.LookRotation(forward, axis));
            float distance = Vector3.Distance(cameraPose.Position, tip);
            float worldHeight = 2f * distance * Mathf.Tan(cameraPose.FieldOfView * 0.5f * Mathf.Deg2Rad);
            go.transform.localScale = Vector3.one * (worldHeight * 0.2f / Mathf.Max(0.001f, length));
            return go;
        }

        private static PhonePart FindPart(PhoneDevice device, string slotId)
        {
            return device.TryGetPart(slotId, out PhonePart part)
                ? part
                : throw new InvalidOperationException($"No part '{slotId}' in the scene.");
        }

        private static void Render(Camera camera, CameraPose pose, string path)
        {
            camera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            camera.fieldOfView = pose.FieldOfView;
            camera.aspect = Width / (float)Height;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else
                {
                    camera.targetTexture = target;
                    camera.Render();
                    camera.targetTexture = null;
                }

                if (path == null)
                {
                    return; // warm-up render
                }

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log($"[PhoneRepair] Captured {path}");
            }
            finally
            {
                camera.ResetAspect();
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}

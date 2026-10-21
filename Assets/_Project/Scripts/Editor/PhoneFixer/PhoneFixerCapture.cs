using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PhoneRepair.Cameras;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using PhoneRepair.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Review screenshots of the Phone Fixer scenes without Play mode (works headless). The 3D view is
    /// rendered with its post stack; the UI is rendered separately (world-space canvases, orthographic
    /// camera, no post-processing) and composited on top, so UI colours stay exact and no screen size is
    /// needed. Scenes are never saved; staged states (open panels, removed parts) exist only in memory.
    /// Menu:  Tools > Phone Fixer > Capture Screenshots   (writes to Docs/Images/PF)
    /// Batch: -executeMethod PhoneRepair.EditorTools.PhoneFixerCapture.CaptureBatch [-captureOut &lt;folder&gt;]
    /// </summary>
    public static class PhoneFixerCapture
    {
        private const int Width = 1600;
        private const int Height = 900;
        private const float CanvasWidth = 1920f;
        private const float CanvasHeight = 1080f;
        private static readonly Vector3 RigOrigin = new(5000f, 5000f, 5000f);
        private static readonly Color ReadyGlow = new(1.0f, 0.6f, 0.28f);
        private static RenderPipelineAsset originalPipeline;

        [MenuItem("Tools/Phone Fixer/Capture Screenshots", priority = 41)]
        public static void CaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string folder = Path.GetFullPath("Docs/Images/PF");
            CaptureAll(folder);
            EditorSceneManager.OpenScene(PhoneFixerPaths.ShopScene); // drop the staged state
            EditorUtility.RevealInFinder(folder);
        }

        public static void CaptureBatch()
        {
            try
            {
                CaptureAll(GetArgument("-captureOut") ?? Path.GetFullPath("Docs/Images/PF"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureAll(string folder)
        {
            Directory.CreateDirectory(folder);
            originalPipeline = QualitySettings.renderPipeline;
            try
            {
                CaptureMenu(folder);
                CaptureShop(folder);
                CaptureBench(folder);
                // LAST: outside Play mode, Graphics.RenderMesh overlay draws stay queued for every later
                // render in the same editor tick, so the hover glow must be the final shot.
                CaptureShopHover(folder);
            }
            finally
            {
                QualitySettings.renderPipeline = originalPipeline;
            }
        }

        // ---- scenes -------------------------------------------------------------------------------------

        private static void CaptureMenu(string folder)
        {
            Camera camera = Open(PhoneFixerPaths.MenuScene);
            PoseCustomers(0.6f);
            CameraPose pose = View("View_Menu");
            using var ui = new UiRig();
            Render(camera, pose, null, null);
            Render(camera, pose, ui, Path.Combine(folder, "pf_01_main_menu.png"));
        }

        private static void CaptureShop(string folder)
        {
            Camera camera = Open(PhoneFixerPaths.ShopScene);
            PoseCustomers(0.3f);
            CameraPose diorama = View("View_Diorama");
            CameraPose counter = View("View_Counter");
            using var ui = new UiRig();
            Render(camera, diorama, null, null);
            Render(camera, diorama, null, Path.Combine(folder, "pf_02_shop_diorama.png"));
            Render(camera, diorama, ui, Path.Combine(folder, "pf_03_shop_hud.png"));

            ShowPanel("Dialogue", true);
            Render(camera, counter, ui, Path.Combine(folder, "pf_04_dialogue.png"));
            ShowPanel("Dialogue", false);

            foreach ((string panel, string file) in new[] { ("Inventory", "pf_05_inventory.png"), ("Upgrades", "pf_06_upgrades.png"), ("Pause", "pf_07_pause.png") })
            {
                ShowPanel(panel, true);
                Render(camera, diorama, ui, Path.Combine(folder, file));
                ShowPanel(panel, false);
            }
        }

        private static void CaptureBench(string folder)
        {
            Camera camera = Open(PhoneFixerPaths.BenchScene);
            var view = Object.FindFirstObjectByType<InspectionCameraView>() ?? throw new InvalidOperationException("No InspectionCameraView.");
            var device = Object.FindFirstObjectByType<PhoneDevice>() ?? throw new InvalidOperationException("No PhoneDevice.");
            var area = Object.FindFirstObjectByType<PartsArea>();
            var belt = Object.FindFirstObjectByType<ToolBelt>();
            Transform pivot = view.Anchor;
            _ = device.Parts; // index parts and their installed poses before anything moves

            using var ui = new UiRig();
            CallPrivate(Object.FindFirstObjectByType<ToolBarView>(), "Start");            // build the keycaps like Play mode does
            CallPrivate(Object.FindFirstObjectByType<ToolCaddyDisplay>(), "OnEnable");      // follow the equipped tool

            CameraPose intro = CameraPose.FromCamera(camera);
            Render(camera, intro, null, null);
            Render(camera, intro, ui, Path.Combine(folder, "pf_08_bench_intro.png"));
            view.ResetView(instant: true);
            Render(camera, view.GetPose(), ui, Path.Combine(folder, "pf_09_bench_hud.png"));

            // Back up, screwdriver seated in screw 4, tooltip + progress ring.
            Quaternion rest = pivot.rotation;
            pivot.rotation = Quaternion.AngleAxis(180f, Vector3.forward) * rest;
            belt.Equip(1);
            PhonePart screw = FindPart(device, "Screw_04");
            view.FocusOn(screw.GetComponent<Interactable>().GetWorldBounds(), screw.transform);
            view.Zoom(-2.5f, Vector2.zero);
            view.SnapToTarget();
            CameraPose close = view.GetPose();
            Aim(camera, close);
            GameObject tool = PlaceToolOnScrew(belt.Current, screw, close);
            Vector2 at = CanvasPoint(camera, screw.transform.position);
            ShowTooltip(screw.GetComponent<Interactable>().GetInfo().Title, "Hold · Unscrew", at + new Vector2(20f, -20f));
            ShowRing(0.62f, at + new Vector2(-34f, 30f));
            Render(camera, close, ui, Path.Combine(folder, "pf_10_bench_unscrew.png"));
            if (tool != null)
            {
                Object.DestroyImmediate(tool);
            }

            HideByName("HoverTooltip");
            HideByName("ActionProgress");

            // Opened up: screws in the screw map, cover and battery laid out, battery selected.
            if (area != null)
            {
                UnpackInMemory(device.gameObject);
                foreach (PartsArea.Slot slot in area.Slots)
                {
                    if (slot.Anchor != null)
                    {
                        UnpackInMemory(slot.Anchor.gameObject);
                    }
                }

                foreach (string id in new[] { "Screw_01", "Screw_02", "Screw_03", "Screw_04", "BackCover" })
                {
                    Remove(device, area, FindPart(device, id));
                }

                // Every damage state at once while the battery is still in: bulge, warning, corrosion, lint, tear.
                var faults = device.GetComponent<PhoneFaultVisuals>();
                if (faults != null)
                {
                    foreach (string state in PhoneFaultVisuals.KnownStates)
                    {
                        faults.SetState(state, true);
                    }

                    view.ResetView(instant: true);
                    view.FocusOn(EditorAssetUtil.RendererBounds(device.gameObject), device.transform); // the open phone only
                    view.SnapToTarget();
                    Render(camera, view.GetPose(), null, Path.Combine(folder, "pf_13_damage_inside.png"));
                    foreach (string state in PhoneFaultVisuals.KnownStates)
                    {
                        faults.SetState(state, state == "ChargingPort_Lint"); // back to Ren's actual fault
                    }
                }

                PhonePart connector = FindPart(device, "BatteryConnector");
                device.SetState(connector, PartState.Loose);
                connector.transform.localRotation = connector.InstalledLocalRotation *
                                                    Quaternion.AngleAxis(connector.Definition.ReleaseAngle, connector.Definition.ReleaseAxis);
                PhonePart battery = FindPart(device, "Battery");
                Remove(device, area, battery);

                view.ResetView(instant: true);
                view.Zoom(-1.5f, Vector2.zero);
                view.SnapToTarget();
                var card = Object.FindFirstObjectByType<SelectionInfoPanel>();
                if (card != null)
                {
                    CallPrivate(card, "Show", battery.GetComponent<Interactable>());
                    SetAlpha(card.gameObject, 1f);
                }

                Render(camera, view.GetPose(), ui, Path.Combine(folder, "pf_11_bench_opened.png"));
                ShowPanel("Receipt", true);
                Render(camera, view.GetPose(), ui, Path.Combine(folder, "pf_12_receipt.png"));
                ShowPanel("Receipt", false);
                if (card != null)
                {
                    SetAlpha(card.gameObject, 0f);
                }
            }

            // The cracked screen: phone face up again.
            var screenFaults = device.GetComponent<PhoneFaultVisuals>();
            if (screenFaults != null)
            {
                screenFaults.SetState("Screen_Crack", true);
                pivot.rotation = rest;
                view.ResetView(instant: true);
                view.Zoom(-1.2f, Vector2.zero);
                view.SnapToTarget();
                Render(camera, view.GetPose(), null, Path.Combine(folder, "pf_14_damage_screen.png"));
            }
        }

        private static void CaptureShopHover(string folder)
        {
            Camera camera = Open(PhoneFixerPaths.ShopScene);
            PoseCustomers(0.3f);
            CameraPose diorama = View("View_Diorama");
            using var ui = new UiRig();
            Render(camera, diorama, null, null);

            ShopStation station = Object.FindObjectsByType<ShopStation>(FindObjectsSortMode.None)
                .FirstOrDefault(s => s.SceneToLoad == PhoneFixerPaths.BenchSceneName);
            if (station == null)
            {
                Debug.LogWarning("[PhoneFixer] Capture: no workbench station in the shop.");
                return;
            }

            var target = station.GetComponent<Interactable>();
            var highlight = AssetDatabase.LoadAssetAtPath<Material>(MaterialLibrary.HighlightMaterialPath);
            InteractionHighlighter.DrawOverlay(target, highlight, new MaterialPropertyBlock(), ReadyGlow);

            var label = Object.FindFirstObjectByType<StationLabel>();
            if (label != null)
            {
                var so = new SerializedObject(label);
                ((TMP_Text)so.FindProperty("nameLabel").objectReferenceValue).text = target.DisplayName;
                ((TMP_Text)so.FindProperty("verbLabel").objectReferenceValue).text = station.Verb;
                Aim(camera, diorama);
                Bounds b = target.GetWorldBounds();
                var pill = (RectTransform)so.FindProperty("pill").objectReferenceValue;
                pill.anchoredPosition = CanvasPoint(camera, b.center + Vector3.up * b.extents.y) + new Vector2(0f, 18f);
                SetAlpha(pill.gameObject, 1f);
            }

            Render(camera, diorama, ui, Path.Combine(folder, "pf_15_shop_hover.png"));
        }

        // ---- staging helpers ------------------------------------------------------------------------------

        private static Camera Open(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var pipeline = Object.FindFirstObjectByType<PipelineOverride>();
            QualitySettings.renderPipeline = pipeline != null && pipeline.Pipeline != null ? pipeline.Pipeline : originalPipeline;
            DynamicGI.UpdateEnvironment();
            Camera camera = Camera.main ?? throw new InvalidOperationException($"No MainCamera in {scenePath}.");
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            return camera;
        }

        private static CameraPose View(string name)
        {
            FixedCameraView view = Object.FindObjectsByType<FixedCameraView>(FindObjectsSortMode.None).FirstOrDefault(v => v.name == name)
                                   ?? throw new InvalidOperationException($"No camera view '{name}'.");
            return view.GetPose();
        }

        /// <summary>The Animator does not run in edit mode: sample each customer's loop clip at a staggered time.</summary>
        private static void PoseCustomers(float phase)
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(PhoneFixerPaths.BaseCharacterFbx).OfType<AnimationClip>())
            {
                if (!clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    clips.TryAdd(clip.name, clip);
                }
            }

            int i = 0;
            foreach (CustomerActor actor in Object.FindObjectsByType<CustomerActor>(FindObjectsSortMode.InstanceID))
            {
                if (clips.TryGetValue(actor.LoopState, out AnimationClip clip))
                {
                    clip.SampleAnimation(actor.gameObject, clip.length * Mathf.Repeat(phase + i * 0.37f, 1f));
                }

                i++;
            }
        }

        private static void ShowPanel(string name, bool visible)
        {
            UiPanel panel = Object.FindObjectsByType<UiPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.name == name);
            if (panel == null)
            {
                Debug.LogWarning($"[PhoneFixer] Capture: no panel '{name}'.");
                return;
            }

            SetAlpha(panel.gameObject, visible ? 1f : 0f);
            foreach (HudFader hud in Object.FindObjectsByType<HudFader>(FindObjectsSortMode.None))
            {
                SetAlpha(hud.gameObject, visible ? 0f : 1f); // as in Play mode: screens replace the HUD
            }
        }

        private static void SetAlpha(GameObject go, float alpha)
        {
            if (go.TryGetComponent(out CanvasGroup group))
            {
                group.alpha = alpha;
            }
        }

        private static void HideByName(string name)
        {
            foreach (CanvasGroup group in Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))
            {
                if (group.name == name)
                {
                    group.alpha = 0f;
                }
            }
        }

        private static void ShowTooltip(string title, string hint, Vector2 canvasPosition)
        {
            var tooltip = Object.FindFirstObjectByType<HoverTooltip>();
            if (tooltip == null)
            {
                return;
            }

            var so = new SerializedObject(tooltip);
            ((TMP_Text)so.FindProperty("label").objectReferenceValue).text = title;
            var hintLabel = (TMP_Text)so.FindProperty("hintLabel").objectReferenceValue;
            hintLabel.text = hint;
            hintLabel.color = so.FindProperty("readyColor").colorValue;
            ((RectTransform)tooltip.transform).anchoredPosition = canvasPosition;
            SetAlpha(tooltip.gameObject, 1f);
        }

        private static void ShowRing(float progress, Vector2 canvasPosition)
        {
            var view = Object.FindFirstObjectByType<ActionProgressView>();
            if (view == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            ((Image)so.FindProperty("ring").objectReferenceValue).fillAmount = progress;
            ((RectTransform)view.transform).anchoredPosition = canvasPosition;
            SetAlpha(view.gameObject, 1f);
        }

        private static void CallPrivate(Object target, string method, params object[] args)
        {
            if (target == null)
            {
                return;
            }

            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                              ?? throw new MissingMethodException(target.GetType().Name, method);
            info.Invoke(target, args);
        }

        private static void UnpackInMemory(GameObject member)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(member);
            if (root != null)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }

        private static void Remove(PhoneDevice device, PartsArea area, PhonePart part)
        {
            if (!area.TryReserve(part, out PartsArea.Slot slot, out Pose pose))
            {
                Debug.LogWarning($"[PhoneFixer] Capture: no room for {part.name}.");
                return;
            }

            part.transform.SetParent(slot.Anchor, false);
            part.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
            device.SetState(part, PartState.Removed);
        }

        private static PhonePart FindPart(PhoneDevice device, string slotId) =>
            device.TryGetPart(slotId, out PhonePart part) ? part : throw new InvalidOperationException($"No part '{slotId}' in the scene.");

        private static GameObject PlaceToolOnScrew(ToolDefinition tool, PhonePart screw, CameraPose cameraPose)
        {
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

        // ---- rendering -------------------------------------------------------------------------------------

        private static void Aim(Camera camera, CameraPose pose)
        {
            camera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            camera.fieldOfView = pose.FieldOfView;
            camera.aspect = Width / (float)Height;
        }

        /// <summary>World point -> position in a 1920x1080 canvas whose anchors sit at its centre.</summary>
        private static Vector2 CanvasPoint(Camera camera, Vector3 world)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return new Vector2((viewport.x - 0.5f) * CanvasWidth, (viewport.y - 0.5f) * CanvasHeight);
        }

        private static void Render(Camera camera, CameraPose pose, UiRig ui, string path)
        {
            Aim(camera, pose);
            Texture2D image = RenderCamera(camera, TextureFormat.RGB24);
            try
            {
                if (path == null)
                {
                    return; // warm-up: the first render after loading has no colour-grading LUT yet
                }

                if (ui != null)
                {
                    ui.Refresh();
                    ui.Camera.backgroundColor = Color.black;
                    Texture2D overBlack = RenderCamera(ui.Camera, TextureFormat.RGB24);
                    ui.Camera.backgroundColor = Color.white;
                    Texture2D overWhite = RenderCamera(ui.Camera, TextureFormat.RGB24);
                    Composite(image, overBlack, overWhite);
                    Object.DestroyImmediate(overBlack);
                    Object.DestroyImmediate(overWhite);
                }

                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log($"[PhoneFixer] Captured {path}");
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        private static Texture2D RenderCamera(Camera camera, TextureFormat format)
        {
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

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, format, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
                return image;
            }
            finally
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// UI over scene from two UI renders, over black (B = premultiplied colour) and over white
        /// (W = B + 1 - alpha), so the result is right whatever blend modes the UI shaders use. In linear space.
        /// </summary>
        private static void Composite(Texture2D scene, Texture2D overBlack, Texture2D overWhite)
        {
            Color32[] under = scene.GetPixels32();
            Color32[] black = overBlack.GetPixels32();
            Color32[] white = overWhite.GetPixels32();
            var toLinear = new float[256];
            for (int i = 0; i < 256; i++)
            {
                toLinear[i] = Mathf.GammaToLinearSpace(i / 255f);
            }

            static byte ToByte(float linear) => (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(linear)) * 255f), 0, 255);

            for (int i = 0; i < under.Length; i++)
            {
                Color32 b = black[i];
                Color32 w = white[i];
                if (b.r == 0 && b.g == 0 && b.b == 0 && w.r == 255 && w.g == 255 && w.b == 255)
                {
                    continue; // no UI here
                }

                float br = toLinear[b.r], bg = toLinear[b.g], bb = toLinear[b.b];
                float keep = Mathf.Clamp01(((toLinear[w.r] - br) + (toLinear[w.g] - bg) + (toLinear[w.b] - bb)) / 3f);
                Color32 u = under[i];
                under[i] = new Color32(
                    ToByte(br + toLinear[u.r] * keep),
                    ToByte(bg + toLinear[u.g] * keep),
                    ToByte(bb + toLinear[u.b] * keep),
                    255);
            }

            scene.SetPixels32(under);
            scene.Apply();
        }

        /// <summary>Moves every root canvas of the open scene into world space in front of an orthographic camera.</summary>
        private sealed class UiRig : IDisposable
        {
            public readonly Camera Camera;
            private readonly GameObject cameraObject;

            public UiRig()
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                cameraObject = new GameObject("CaptureUiCamera") { hideFlags = HideFlags.DontSave };
                cameraObject.transform.SetPositionAndRotation(RigOrigin + new Vector3(0f, 0f, -10f), Quaternion.identity);
                Camera = cameraObject.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.orthographic = true;
                Camera.orthographicSize = CanvasHeight * 0.5f;
                Camera.aspect = Width / (float)Height;
                Camera.nearClipPlane = 0.1f;
                Camera.farClipPlane = 100f;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                Camera.cullingMask = 1 << uiLayer;
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = false;

                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas)
                    {
                        continue;
                    }

                    if (canvas.TryGetComponent(out CanvasScaler scaler))
                    {
                        scaler.enabled = false; // world-space canvas: 1 unit = 1 reference pixel
                    }

                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = Camera;
                    var rect = (RectTransform)canvas.transform;
                    rect.SetPositionAndRotation(RigOrigin, Quaternion.identity);
                    rect.localScale = Vector3.one;
                    rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
                }

                Refresh();
            }

            public void Refresh()
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    Canvas.ForceUpdateCanvases();
                    foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    {
                        if (canvas.isRootCanvas)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                        }
                    }

                    foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    {
                        text.ForceMeshUpdate(true);
                    }
                }

                Canvas.ForceUpdateCanvases();
            }

            public void Dispose() => Object.DestroyImmediate(cameraObject);
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhoneRepair.Cameras;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using PhoneRepair.Workbench;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// Milestone 2 end to end, through a simulated mouse and keyboard: tool hotkeys, the
    /// screwdriver minigame, the assembly rules, the tray and the trip back into the phone.
    /// Requires the M1_Workbench scene in Build Settings (the setup adds it).
    /// </summary>
    public class RepairFlowTests : InputTestFixture
    {
        private const string SceneName = "M1_Workbench";
        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        private readonly List<string> messages = new();
        private Mouse mouse;
        private Keyboard keyboard;
        private Camera camera;
        private CameraDirector director;
        private InspectionCameraView view;
        private InspectionTurntable turntable;
        private SelectionController selection;
        private PhoneDevice device;
        private ToolBelt toolBelt;
        private RepairFeedback feedback;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            messages.Clear();
        }

        public override void TearDown()
        {
            if (feedback != null)
            {
                feedback.MessagePosted -= OnMessage;
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Screwdriver_TurnsAScrewOut_AndItLandsInTheTray()
        {
            yield return LoadWorkbench();
            yield return FlipOntoTheScreen();
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);

            PhonePart screw = Part("Screw_01");
            yield return Point(screw);
            Assert.That(selection.Hint.Tone, Is.EqualTo(InteractionTone.Ready));
            Assert.That(selection.Hint.Text, Does.Contain("Unscrew"));

            yield return Turn(screw, counterClockwise: true, until: () => screw.State == PartState.Removed);
            yield return WaitForFlights();

            Assert.That(screw.transform.parent.name, Does.StartWith("ScrewTray_Slot"));
            Assert.That(Part("BackCover").State, Is.EqualTo(PartState.Secured), "three screws still hold it");
        }

        [UnityTest]
        public IEnumerator PryingTheCoverWhileItIsScrewedOn_SaysNope()
        {
            yield return LoadWorkbench();
            yield return FlipOntoTheScreen();
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);

            PhonePart cover = Part("BackCover");
            Vector3 position = cover.transform.localPosition;
            Quaternion rotation = cover.transform.localRotation;
            yield return Point(cover);
            Assert.That(selection.Hint.Tone, Is.EqualTo(InteractionTone.Blocked), "the hover already warns");
            Assert.That(selection.Hint.Text, Does.Contain("unscrew"));

            yield return HoldButton(1.2f);
            Assert.That(cover.State, Is.EqualTo(PartState.Secured));
            Assert.That(messages, Has.Some.Contains("unscrew"), "the toast says what to do first");

            yield return new WaitForSeconds(0.5f); // let the wiggle finish
            Assert.That(Vector3.Distance(cover.transform.localPosition, position), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(cover.transform.localRotation, rotation), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator TheWrongTool_JustInspectsThePart()
        {
            yield return LoadWorkbench();
            yield return FlipOntoTheScreen();
            Assert.That(toolBelt.CurrentKind, Is.EqualTo(ToolKind.Hands));

            PhonePart screw = Part("Screw_02");
            yield return Point(screw);
            Assert.That(selection.Hint.Text, Does.Contain("Screwdriver"), "the tooltip names the tool it needs");
            Assert.That(selection.Hint.Tone, Is.EqualTo(InteractionTone.Neutral));

            yield return HoldButton(0.5f);
            Assert.That(screw.State, Is.EqualTo(PartState.Secured));
            Assert.That(screw.Looseness, Is.EqualTo(0f));
            Assert.That(selection.Selected, Is.SameAs(screw.GetComponent<Interactable>()), "a press still selects");
            Assert.That(messages, Is.Empty, "no 'nope' for simply looking");
        }

        [UnityTest]
        public IEnumerator BatteryOut_AndEverythingBackIn_LeavesThePhoneAsItWas()
        {
            yield return LoadWorkbench();
            var installed = device.Parts.ToDictionary(p => p, p => (p.transform.localPosition, p.transform.localRotation, p.transform.localScale));
            yield return FlipOntoTheScreen();

            // --- take it apart -------------------------------------------------------------------
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(id);
                yield return Turn(screw, counterClockwise: true, until: () => screw.State == PartState.Removed);
            }

            yield return WaitForFlights();

            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            PhonePart cover = Part("BackCover");
            yield return Hold(cover, until: () => cover.State == PartState.Removed);
            yield return WaitForFlights();

            PhonePart battery = Part("Battery");
            PhonePart connector = Part("BatteryConnector");
            yield return Equip(keyboard.digit1Key, ToolKind.Hands);
            yield return Point(battery);
            Assert.That(selection.Hint.Text, Does.Contain("unplug"), "the battery is still plugged in");

            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Hold(connector, until: () => connector.State == PartState.Loose);

            yield return Equip(keyboard.digit1Key, ToolKind.Hands);
            yield return Hold(battery, until: () => battery.State == PartState.Removed);
            yield return WaitForFlights();
            Assert.That(battery.transform.parent, Is.Not.SameAs(battery.InstalledParent), "the battery is on the mat");
            Assert.That(connector.transform.IsChildOf(battery.transform), Is.True, "its connector came along");

            // --- and back together, in reverse ------------------------------------------------------
            yield return Hold(battery, until: () => battery.State == PartState.Secured);
            yield return WaitForFlights();
            yield return Hold(connector, until: () => connector.State == PartState.Secured);
            yield return Hold(cover, until: () => cover.State == PartState.Secured);
            yield return WaitForFlights();

            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(id);
                yield return Turn(screw, counterClockwise: false, until: () => screw.State == PartState.Secured);
            }

            yield return WaitForFlights();
            Assert.That(device.IsFullyAssembled(), Is.True);
            foreach (var pair in installed)
            {
                Transform t = pair.Key.transform;
                string id = pair.Key.SlotId;
                Assert.That(t.parent, Is.SameAs(pair.Key.InstalledParent), id);
                Assert.That(Vector3.Distance(t.localPosition, pair.Value.localPosition), Is.LessThan(1e-4f), $"{id} position");
                Assert.That(Quaternion.Angle(t.localRotation, pair.Value.localRotation), Is.LessThan(0.5f), $"{id} rotation");
                Assert.That(Vector3.Distance(t.localScale, pair.Value.localScale), Is.LessThan(1e-4f), $"{id} scale");
            }
        }

        [UnityTest]
        public IEnumerator HudLabels_HaveAFontAndProduceGlyphs()
        {
            yield return LoadWorkbench();
            TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            Assert.That(labels, Is.Not.Empty);
            foreach (TMP_Text label in labels)
            {
                Assert.That(label.font, Is.Not.Null, $"{label.name} has no font asset");
                if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text))
                {
                    continue;
                }

                label.ForceMeshUpdate();
                Assert.That(label.textInfo.characterCount, Is.GreaterThan(0), $"{label.name} ('{label.text}') produced no characters");
                Assert.That(label.mesh != null && label.mesh.vertexCount > 0, Is.True, $"{label.name} has no text mesh");
            }
        }

        /// <summary>
        /// Review screenshots of the live UI (tool bar, hover hints, toast, progress ring, tool model).
        /// Opt-in: -runTests -testPlatform PlayMode -testFilter CaptureRepairUi -captureOut &lt;folder&gt;
        /// </summary>
        [UnityTest, Explicit("Writes review screenshots; run it on purpose."), Timeout(120000)]
        public IEnumerator CaptureRepairUi()
        {
            if (Application.isBatchMode && CommandLineArgument("-captureOut") == null)
            {
                // command-line runs ignore [Explicit]; only capture when a folder was asked for
                Assert.Ignore("Pass -captureOut <folder> to write the HUD screenshots.");
            }

            string folder = CommandLineArgument("-captureOut") ?? Path.GetFullPath("Screenshots");
            Directory.CreateDirectory(folder);
            yield return LoadWorkbench();
            yield return FlipOntoTheScreen();

            // the spudger on a cover that is still screwed down: red hint, wiggle, toast
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Point(Part("BackCover"));
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return new WaitForSeconds(0.12f);
            yield return Screenshot(Path.Combine(folder, "m2_ui_01_nope.png"));

            // the screwdriver halfway through a screw: tool seated in the head, progress ring
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            PhonePart screw = Part("Screw_02");
            yield return Point(screw);
            Vector2 center = mouse.position.ReadValue();
            Press(mouse.leftButton);
            yield return null;
            for (int i = 1; screw.Looseness < 0.4f && i < 600; i++)
            {
                float angle = i * 12f * Mathf.Deg2Rad;
                Move(mouse.position, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 40f);
                yield return null;
            }

            yield return new WaitForSeconds(0.25f); // batch frames are very short: let the ring finish fading in
            yield return Screenshot(Path.Combine(folder, "m2_ui_02_unscrewing.png"));
            Release(mouse.leftButton);
            yield return null;
        }

        /// <summary>
        /// Writes what the player sees (scene + HUD) to a PNG. Batch mode has no game view, so there
        /// is no ScreenCapture/WaitForEndOfFrame: the scene is rendered on its own, then the HUD is
        /// rendered by a scratch camera over black and over white, which gives its exact colour and
        /// coverage, and is composited on top. Same pixel size as the screen, so the layout matches.
        /// </summary>
        private IEnumerator Screenshot(string path)
        {
            yield return null; // let this frame's HUD update run first
            int width = Screen.width;
            int height = Screen.height;
            Color[] scene = Render(camera, width, height);

            var canvas = Object.FindFirstObjectByType<Canvas>();
            RenderMode mode = canvas.renderMode;
            var hudCamera = new GameObject("HudCaptureCamera").AddComponent<Camera>();
            hudCamera.transform.position = new Vector3(0f, -500f, 0f); // looks at empty space
            hudCamera.clearFlags = CameraClearFlags.SolidColor;
            hudCamera.cullingMask = 1 << canvas.gameObject.layer;
            hudCamera.nearClipPlane = 0.1f;
            hudCamera.farClipPlane = 10f;
            hudCamera.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = hudCamera;
            canvas.planeDistance = 1f;
            try
            {
                yield return null; // TextMeshPro re-scales its distance-field text on the next canvas update
                Canvas.ForceUpdateCanvases();
                hudCamera.backgroundColor = Color.black;
                Color[] overBlack = Render(hudCamera, width, height);
                hudCamera.backgroundColor = Color.white;
                Color[] overWhite = Render(hudCamera, width, height);

                var result = new Color[scene.Length];
                for (int i = 0; i < scene.Length; i++)
                {
                    // linear: overX = hud + (1 - coverage) * X  =>  coverage = 1 - (overWhite - overBlack)
                    Color b = overBlack[i].linear;
                    Color w = overWhite[i].linear;
                    float coverage = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
                    Color s = scene[i].linear;
                    result[i] = new Color(b.r + (1f - coverage) * s.r, b.g + (1f - coverage) * s.g, b.b + (1f - coverage) * s.b).gamma;
                }

                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.SetPixels(result);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
                Debug.Log($"[PhoneRepair] Captured {path} ({width}x{height})");
            }
            finally
            {
                canvas.renderMode = mode;
                canvas.worldCamera = null;
                Object.Destroy(hudCamera.gameObject);
            }
        }

        private static Color[] Render(Camera source, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(source, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
                return image.GetPixels();
            }
            finally
            {
                Object.Destroy(image);
                target.Release();
                Object.Destroy(target);
            }
        }

        private static string CommandLineArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
        }

        // --- helpers -------------------------------------------------------------------------

        private IEnumerator LoadWorkbench()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null; // Awake/OnEnable
            yield return null; // Start

            director = Object.FindFirstObjectByType<CameraDirector>();
            view = Object.FindFirstObjectByType<InspectionCameraView>();
            turntable = Object.FindFirstObjectByType<InspectionTurntable>();
            selection = Object.FindFirstObjectByType<SelectionController>();
            device = Object.FindFirstObjectByType<PhoneDevice>();
            toolBelt = Object.FindFirstObjectByType<ToolBelt>();
            feedback = Object.FindFirstObjectByType<RepairFeedback>();
            Assert.That(director, Is.Not.Null, "scene not set up - run Tools > Phone Repair > Run Setup");
            Assert.That(device, Is.Not.Null, "no phone in the scene");
            Assert.That(toolBelt, Is.Not.Null, "no tool belt - rerun the setup for Milestone 2");
            Assert.That(feedback, Is.Not.Null, "no repair feedback - rerun the setup for Milestone 2");
            Assert.That(toolBelt.Tools.Select(t => t != null ? t.Kind : (ToolKind)(-1)),
                Is.EqualTo(new[] { ToolKind.Hands, ToolKind.Screwdriver, ToolKind.Spudger, ToolKind.SuctionCup }),
                "tool belt (hotkeys 1-4) - rerun the setup");
            camera = director.Camera;
            feedback.MessagePosted += OnMessage;

            director.SnapToActiveView(); // skip the intro camera move
            yield return null;
        }

        private void OnMessage(string message, FeedbackTone tone) => messages.Add(message);

        private PhonePart Part(string slotId)
        {
            Assert.That(device.TryGetPart(slotId, out PhonePart part), Is.True, $"no part '{slotId}'");
            return part;
        }

        private IEnumerator FlipOntoTheScreen()
        {
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);

            float timeout = Time.time + 3f;
            while ((Vector3.Dot(turntable.transform.up, Vector3.down) < 0.999f || turntable.CurrentLift > 0.001f) && Time.time < timeout)
            {
                yield return null;
            }

            Assert.That(Vector3.Dot(turntable.transform.up, Vector3.down), Is.GreaterThan(0.999f), "the phone didn't flip");
        }

        private IEnumerator Equip(KeyControl key, ToolKind expected)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
            Assert.That(toolBelt.CurrentKind, Is.EqualTo(expected), $"key {key.name}");
        }

        /// <summary>Frames the part the way a double-click does, then puts the cursor on it.</summary>
        private IEnumerator Point(PhonePart part)
        {
            var target = part.GetComponent<Interactable>();
            view.FocusOn(target.GetWorldBounds(), part.transform);
            view.SnapToTarget();
            director.SnapToActiveView();
            yield return null;

            Vector3 p = camera.WorldToScreenPoint(target.GetWorldBounds().center);
            Move(mouse.position, new Vector2(p.x, p.y));
            yield return null;
            yield return null;
            Assert.That(selection.Hovered, Is.SameAs(target),
                $"the cursor should be on {part.SlotId} but is on {(selection.Hovered != null ? selection.Hovered.name : "nothing")}");
        }

        private IEnumerator HoldButton(float seconds)
        {
            Press(mouse.leftButton);
            yield return null;
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                yield return null;
            }

            Release(mouse.leftButton);
            yield return null;
            yield return null;
        }

        /// <summary>Points at the part and holds the button until the condition is met (or time runs out).</summary>
        private IEnumerator Hold(PhonePart part, Func<bool> until, float timeout = 5f)
        {
            yield return Point(part);
            Press(mouse.leftButton);
            yield return null;
            float end = Time.time + timeout;
            while (!until() && Time.time < end)
            {
                yield return null;
            }

            Release(mouse.leftButton);
            yield return null;
            yield return null;
            Assert.That(until(), Is.True, $"{part.SlotId}: didn't finish (state {part.State}, last message '{messages.LastOrDefault()}')");
        }

        /// <summary>Points at a screw, holds the button and circles the cursor around it, 30 degrees a frame.</summary>
        private IEnumerator Turn(PhonePart screw, bool counterClockwise, Func<bool> until, float timeout = 10f)
        {
            yield return Point(screw);
            Vector2 center = mouse.position.ReadValue();
            Press(mouse.leftButton);
            yield return null;

            float end = Time.time + timeout;
            for (int i = 1; !until() && Time.time < end; i++)
            {
                float angle = (counterClockwise ? i : -i) * 30f * Mathf.Deg2Rad;
                Move(mouse.position, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 40f);
                yield return null;
            }

            Release(mouse.leftButton);
            yield return null;
            yield return null;
            Assert.That(until(), Is.True,
                $"{screw.SlotId}: still {screw.State} at looseness {screw.Looseness:0.00} (last message '{messages.LastOrDefault()}')");
        }

        private IEnumerator WaitForFlights(float timeout = 3f)
        {
            float end = Time.time + timeout;
            while (device.Parts.Any(p => p.IsBusy) && Time.time < end)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f); // landing squash, hinge settle
            Assert.That(device.Parts.Any(p => p.IsBusy), Is.False, "a part is still flying");
        }
    }
}

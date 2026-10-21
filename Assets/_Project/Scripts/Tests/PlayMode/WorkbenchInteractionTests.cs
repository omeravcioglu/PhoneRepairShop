using System.Collections;
using System.Linq;
using NUnit.Framework;
using PhoneRepair.Cameras;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Workbench;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// End-to-end checks of Milestone 1 in the real scene, driven through the Input System with
    /// simulated devices (so the whole path input -> controller -> raycast -> selection is covered).
    /// Requires the M1_Workbench scene in Build Settings (the setup adds it).
    /// </summary>
    public class WorkbenchInteractionTests : InputTestFixture
    {
        private const string SceneName = "M1_Workbench";

        private Mouse mouse;
        private Keyboard keyboard;
        private Camera camera;
        private CameraDirector director;
        private InspectionCameraView view;
        private InspectionTurntable turntable;
        private SelectionController selection;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTest]
        public IEnumerator PointingAtTheScreen_HoversIt_AndClickingSelectsIt()
        {
            yield return LoadWorkbench();
            Interactable screen = Part("Screen");

            yield return MoveTo(ScreenPointOf(screen));
            Assert.That(selection.Hovered, Is.SameAs(screen), "hover");

            yield return ClickLeft();
            Assert.That(selection.Selected, Is.SameAs(screen), "selection");
        }

        [UnityTest]
        public IEnumerator ClickingEmptySpace_ClearsTheSelection()
        {
            yield return LoadWorkbench();
            yield return MoveTo(ScreenPointOf(Part("Screen")));
            yield return ClickLeft();
            Assert.That(selection.Selected, Is.Not.Null);

            yield return MoveTo(new Vector2(Screen.width * 0.8f, Screen.height * 0.12f)); // the mat/desk, not the phone
            yield return ClickLeft();
            Assert.That(selection.Selected, Is.Null);
        }

        [UnityTest]
        public IEnumerator EscapeKey_Deselects()
        {
            yield return LoadWorkbench();
            yield return MoveTo(ScreenPointOf(Part("Screen")));
            yield return ClickLeft();
            Assert.That(selection.Selected, Is.Not.Null);

            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;
            Assert.That(selection.Selected, Is.Null);
        }

        [UnityTest]
        public IEnumerator RightDrag_RotatesThePhone()
        {
            yield return LoadWorkbench();
            Quaternion before = turntable.TargetRotation;
            var start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            yield return MoveTo(start);

            Press(mouse.rightButton);
            yield return null;
            for (int i = 1; i <= 6; i++)
            {
                Move(mouse.position, start + new Vector2(i * 20f, 0f), new Vector2(20f, 0f));
                yield return null;
            }

            Release(mouse.rightButton);
            yield return null;
            Assert.That(Quaternion.Angle(before, turntable.TargetRotation), Is.GreaterThan(10f));
        }

        [UnityTest]
        public IEnumerator MouseWheel_ZoomsIn()
        {
            yield return LoadWorkbench();
            float before = view.TargetDistance;
            yield return MoveTo(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

            Set(mouse.scroll, new Vector2(0f, 1f));
            yield return null;
            Set(mouse.scroll, Vector2.zero);
            yield return null;
            Assert.That(view.TargetDistance, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator DoubleClick_FramesThePart()
        {
            yield return LoadWorkbench();
            Interactable screen = Part("Screen");

            // Zoom in close first, so framing the whole screen has to pull the camera back out.
            yield return MoveTo(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            for (int i = 0; i < 4; i++)
            {
                Set(mouse.scroll, new Vector2(0f, 1f));
                yield return null;
                Set(mouse.scroll, Vector2.zero);
                yield return null;
            }

            float zoomedIn = view.TargetDistance;
            view.SnapToTarget();
            director.SnapToActiveView();
            yield return MoveTo(ScreenPointOf(screen));
            yield return ClickLeft();
            yield return ClickLeft();

            Assert.That(selection.Selected, Is.SameAs(screen));
            Assert.That(view.TargetDistance, Is.GreaterThan(zoomedIn + 0.05f), "double-click should frame the whole screen");
        }

        [UnityTest]
        public IEnumerator SpaceBar_FlipsThePhoneOntoItsScreen()
        {
            yield return LoadWorkbench();
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);

            // It lifts while turning over, then settles; wait for both to finish.
            float timeout = Time.time + 3f;
            bool peakedAboveMat = false;
            while ((Vector3.Dot(turntable.transform.up, Vector3.down) < 0.999f || turntable.CurrentLift > 0.001f) && Time.time < timeout)
            {
                peakedAboveMat |= turntable.CurrentLift > 0.02f;
                yield return null;
            }

            Assert.That(peakedAboveMat, Is.True, "should lift off the mat while flipping (no clipping through the bench)");
            Assert.That(Vector3.Dot(turntable.transform.up, Vector3.down), Is.GreaterThan(0.999f), "screen should face the mat");
            Assert.That(turntable.CurrentLift, Is.LessThan(0.001f), "should settle back onto the mat");
        }

        // --- helpers -------------------------------------------------------------------------

        private IEnumerator LoadWorkbench()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null; // Awake/OnEnable
            yield return null; // Start (director activates the inspection view)

            director = Object.FindFirstObjectByType<CameraDirector>();
            view = Object.FindFirstObjectByType<InspectionCameraView>();
            turntable = Object.FindFirstObjectByType<InspectionTurntable>();
            selection = Object.FindFirstObjectByType<SelectionController>();
            Assert.That(director, Is.Not.Null, "scene not set up - run the Milestone 1 setup");
            camera = director.Camera;

            director.SnapToActiveView(); // skip the intro camera move
            yield return null;
        }

        private static Interactable Part(string slotId)
        {
            PhonePart part = Object.FindObjectsByType<PhonePart>(FindObjectsSortMode.None).First(p => p.SlotId == slotId);
            return part.GetComponent<Interactable>();
        }

        private Vector2 ScreenPointOf(Interactable target)
        {
            Vector3 p = camera.WorldToScreenPoint(target.GetWorldBounds().center);
            return new Vector2(p.x, p.y);
        }

        private IEnumerator MoveTo(Vector2 position)
        {
            Move(mouse.position, position);
            yield return null;
            yield return null;
        }

        private IEnumerator ClickLeft()
        {
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
            yield return null;
        }
    }
}

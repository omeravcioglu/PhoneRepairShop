using NUnit.Framework;
using PhoneRepair.Cameras;
using PhoneRepair.Workbench;
using UnityEngine;

namespace PhoneRepair.Tests
{
    public class InspectionMathTests
    {
        private static readonly Vector3 PhoneHalfExtents = new(0.037f, 0.006f, 0.076f);

        // Away from any scene content (so physics raycasts in the zoom code hit nothing), but close
        // enough to the origin that float precision stays far below a pixel.
        private static readonly Vector3 FarAway = new(100f, 100f, 100f);

        [Test]
        public void ScaleAbout_KeepsThePivotFixedAndScalesDistances()
        {
            var pivot = new Vector3(0.1f, 0.2f, 0.3f);
            var point = new Vector3(1f, 0f, -1f);
            Vector3 result = InspectionCameraView.ScaleAbout(pivot, point, 0.5f);
            Assert.That(Vector3.Distance(result, pivot), Is.EqualTo(Vector3.Distance(point, pivot) * 0.5f).Within(1e-5f));
            Assert.That(Vector3.Angle(result - pivot, point - pivot), Is.LessThan(0.01f));
        }

        [Test]
        public void RequiredLift_IsZeroLyingFlatOnEitherFace()
        {
            float rest = InspectionTurntable.VerticalHalfExtent(Quaternion.identity, PhoneHalfExtents);
            Assert.That(InspectionTurntable.RequiredLift(Quaternion.identity, PhoneHalfExtents, rest), Is.EqualTo(0f).Within(1e-6f));
            Quaternion faceDown = Quaternion.AngleAxis(180f, Vector3.forward);
            Assert.That(InspectionTurntable.RequiredLift(faceDown, PhoneHalfExtents, rest), Is.EqualTo(0f).Within(1e-6f));
            Quaternion spun = Quaternion.AngleAxis(73f, Vector3.up);
            Assert.That(InspectionTurntable.RequiredLift(spun, PhoneHalfExtents, rest), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void RequiredLift_StandingOnItsEnd_ClearsHalfTheLength()
        {
            float rest = InspectionTurntable.VerticalHalfExtent(Quaternion.identity, PhoneHalfExtents);
            Quaternion upright = Quaternion.AngleAxis(90f, Vector3.right);
            float lift = InspectionTurntable.RequiredLift(upright, PhoneHalfExtents, rest);
            Assert.That(lift, Is.EqualTo(PhoneHalfExtents.z - PhoneHalfExtents.y).Within(1e-5f));
        }

        [Test]
        public void ZoomTowardsCursor_KeepsTheWorldPointUnderTheCursorStill()
        {
            var anchor = new GameObject("Anchor") { transform = { position = FarAway } };
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var viewObject = new GameObject("View");
            var target = new RenderTexture(800, 600, 16);
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.targetTexture = target; // fixed pixel size, independent of the batch-mode "screen"
                var director = cameraObject.AddComponent<CameraDirector>();
                var view = viewObject.AddComponent<InspectionCameraView>();
                view.Anchor = anchor.transform;
                view.ResetView(instant: true); // Awake does not run in edit mode
                director.Activate(view, 0f);

                var cursor = new Vector2(560f, 230f);
                Ray before = camera.ScreenPointToRay(cursor);
                var focusPlane = new Plane(-camera.transform.forward, view.TargetFocusWorld);
                Assert.That(focusPlane.Raycast(before, out float enter), Is.True);
                Vector3 worldPoint = before.GetPoint(enter);

                float distanceBefore = view.TargetDistance;
                view.Zoom(2f, cursor);
                view.SnapToTarget();
                director.SnapToActiveView();

                Assert.That(view.TargetDistance, Is.LessThan(distanceBefore));
                Vector3 after = camera.WorldToScreenPoint(worldPoint);
                Assert.That(Vector2.Distance(after, cursor), Is.LessThan(0.5f), "point under the cursor drifted");
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(anchor);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ZoomingAllTheWayOut_RecentresOnTheAnchor()
        {
            var anchor = new GameObject("Anchor") { transform = { position = FarAway } };
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var viewObject = new GameObject("View");
            var target = new RenderTexture(800, 600, 16);
            try
            {
                cameraObject.GetComponent<Camera>().targetTexture = target;
                var director = cameraObject.AddComponent<CameraDirector>();
                var view = viewObject.AddComponent<InspectionCameraView>();
                view.Anchor = anchor.transform;
                view.ResetView(instant: true);
                director.Activate(view, 0f);

                for (int i = 0; i < 8; i++)
                {
                    view.Zoom(1f, new Vector2(700f, 500f));
                }

                Assert.That(Vector3.Distance(view.TargetFocusWorld, anchor.transform.position), Is.GreaterThan(0.001f));
                view.Zoom(-40f, new Vector2(400f, 300f));
                Assert.That(view.TargetDistance, Is.EqualTo(view.MaxDistance).Within(1e-5f));
                Assert.That(Vector3.Distance(view.TargetFocusWorld, anchor.transform.position), Is.LessThan(1e-4f));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(anchor);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}

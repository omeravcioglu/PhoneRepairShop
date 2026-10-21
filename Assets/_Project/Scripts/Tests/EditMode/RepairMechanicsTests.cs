using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Tests
{
    public class RepairMechanicsTests
    {
        // ------------------------------------------------------------------ screwdriver input

        private static float Circle(ScrewTurnTracker tracker, int direction, bool counterClockwise, float stepDegrees = 10f, int steps = 36)
        {
            var center = new Vector2(400f, 300f);
            float total = 0f;
            for (int i = 0; i < steps; i++)
            {
                float a = (counterClockwise ? i : -i) * stepDegrees * Mathf.Deg2Rad;
                total += tracker.Step(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 60f, center, 0.01f, direction);
            }

            return total;
        }

        [Test]
        public void HoldingStill_TurnsSlowly()
        {
            var tracker = new ScrewTurnTracker(holdTurnsPerSecond: 0.6f, turnsPerCircle: 1f);
            float turns = tracker.Step(new Vector2(460f, 300f), new Vector2(400f, 300f), 1f, 1);
            Assert.That(turns, Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void CirclingCounterClockwise_LoosensAboutOneTurnPerCircle()
        {
            var tracker = new ScrewTurnTracker(0.6f, 1f);
            float turns = Circle(tracker, direction: 1, counterClockwise: true);
            // 35 measured steps of 10 degrees = 0.972 turns, plus 0.36 s of holding = 0.216 turns
            Assert.That(turns, Is.EqualTo(0.972f + 0.216f).Within(0.01f));
        }

        [Test]
        public void CirclingTheWrongWay_DoesNotHelp()
        {
            var tracker = new ScrewTurnTracker(0.6f, 1f);
            float turns = Circle(tracker, direction: 1, counterClockwise: false);
            Assert.That(turns, Is.EqualTo(0.216f).Within(0.01f), "clockwise doesn't loosen");

            tracker.Reset();
            float tighten = Circle(tracker, direction: -1, counterClockwise: false);
            Assert.That(tighten, Is.GreaterThan(1f), "clockwise tightens");
        }

        [Test]
        public void CursorOnTopOfTheScrew_OnlyCountsHolding()
        {
            var tracker = new ScrewTurnTracker(0.6f, 1f, minRadiusPixels: 10f);
            var center = new Vector2(400f, 300f);
            float total = 0f;
            for (int i = 0; i < 20; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                total += tracker.Step(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3f, center, 0.01f, 1);
            }

            Assert.That(total, Is.EqualTo(0.12f).Within(1e-3f));
        }

        // ------------------------------------------------------------------ parts area

        private static PhonePart MakePart(string name, Vector3 size)
        {
            var root = new GameObject(name);
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = size;
            return root.AddComponent<PhonePart>();
        }

        private static PartsArea MakeArea(out GameObject root)
        {
            var areaRoot = new GameObject("Area");
            root = areaRoot;
            var area = areaRoot.AddComponent<PartsArea>();
            Transform Spot(string n, float x)
            {
                var t = new GameObject(n).transform;
                t.SetParent(areaRoot.transform, false);
                t.localPosition = new Vector3(x, 0f, 0f);
                return t;
            }

            area.AddSlot(Spot("Small", 0f), new Vector2(0.05f, 0.05f), false);
            area.AddSlot(Spot("Medium", 1f), new Vector2(0.1f, 0.1f), false);
            area.AddSlot(Spot("Tall", 2f), new Vector2(0.04f, 0.09f), false);
            area.AddSlot(Spot("Tray", 3f), new Vector2(0.03f, 0.03f), true);
            return area;
        }

        [Test]
        public void Parts_GoToTheSmallestSpotTheyFit_AndRestOnIt()
        {
            PartsArea area = MakeArea(out GameObject areaRoot);
            PhonePart part = MakePart("Part", new Vector3(0.06f, 0.01f, 0.04f));
            try
            {
                Assert.That(area.TryReserve(part, out PartsArea.Slot slot, out Pose pose), Is.True);
                Assert.That(slot.Anchor.name, Is.EqualTo("Medium"), "too big for Small, Medium is the smallest that fits");
                Assert.That(pose.position.y, Is.EqualTo(0.005f).Within(1e-4f), "bottom face rests on the anchor");
                Assert.That(area.Holds(part), Is.True);

                area.Release(part);
                Assert.That(area.Holds(part), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(part.gameObject);
                Object.DestroyImmediate(areaRoot);
            }
        }

        [Test]
        public void LongParts_TurnNinetyDegreesToFit()
        {
            PartsArea area = MakeArea(out GameObject areaRoot);
            PhonePart medium = MakePart("Blocker", new Vector3(0.09f, 0.01f, 0.09f));
            PhonePart longPart = MakePart("Long", new Vector3(0.08f, 0.01f, 0.03f));
            try
            {
                Assert.That(area.TryReserve(medium, out _, out _), Is.True, "fills Medium");
                Assert.That(area.TryReserve(longPart, out PartsArea.Slot slot, out Pose pose), Is.True);
                Assert.That(slot.Anchor.name, Is.EqualTo("Tall"));
                Assert.That(Quaternion.Angle(pose.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(medium.gameObject);
                Object.DestroyImmediate(longPart.gameObject);
                Object.DestroyImmediate(areaRoot);
            }
        }

        [Test]
        public void WhenNothingFits_ThereIsNoRoom()
        {
            PartsArea area = MakeArea(out GameObject areaRoot);
            PhonePart huge = MakePart("Huge", new Vector3(0.3f, 0.01f, 0.3f));
            try
            {
                Assert.That(area.HasRoomFor(huge), Is.False);
                Assert.That(area.TryReserve(huge, out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(huge.gameObject);
                Object.DestroyImmediate(areaRoot);
            }
        }
    }
}

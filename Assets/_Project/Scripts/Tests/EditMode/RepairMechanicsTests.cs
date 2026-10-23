using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Repair;
using UnityEditor;
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

        // ------------------------------------------------------------------ parts tray (2D)

        private static PhonePart MakePart(string name, int width, int height, PartCategory category)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            PhonePart part = go.AddComponent<PhonePart>();
            var definition = ScriptableObject.CreateInstance<PartDefinition>();
            var definitionData = new SerializedObject(definition);
            definitionData.FindProperty("category").intValue = (int)category;
            definitionData.ApplyModifiedPropertiesWithoutUndo();
            var partData = new SerializedObject(part);
            partData.FindProperty("definition").objectReferenceValue = definition;
            partData.ApplyModifiedPropertiesWithoutUndo();
            var view = go.AddComponent<PartView2D>();
            var viewData = new SerializedObject(view);
            viewData.FindProperty("part").objectReferenceValue = part;
            viewData.FindProperty("normal").objectReferenceValue = sprite;
            viewData.ApplyModifiedPropertiesWithoutUndo();
            return part;
        }

        private static PartsTray2D MakeTray(out GameObject root)
        {
            var trayRoot = new GameObject("Tray");
            root = trayRoot;
            var tray = trayRoot.AddComponent<PartsTray2D>();
            Transform Spot(string n, float x)
            {
                var t = new GameObject(n).transform;
                t.SetParent(trayRoot.transform, false);
                t.localPosition = new Vector3(x, 0f, 0f);
                return t;
            }

            tray.AddSpot(Spot("Small", 0f), new Vector2Int(50, 50), false, 380);
            tray.AddSpot(Spot("Medium", 100f), new Vector2Int(90, 110), false, 380);
            tray.AddSpot(Spot("Big", 200f), new Vector2Int(100, 196), false, 380);
            tray.AddSpot(Spot("Screw", 300f), new Vector2Int(40, 28), true, 390);
            return tray;
        }

        private static void Destroy(params Object[] things)
        {
            foreach (Object thing in things)
            {
                if (thing != null)
                {
                    Object.DestroyImmediate(thing is Component c ? c.gameObject : thing);
                }
            }
        }

        [Test]
        public void Parts_GoToTheSmallestSpotTheyFit_CentredOnIt()
        {
            PartsTray2D tray = MakeTray(out GameObject root);
            PhonePart battery = MakePart("Battery", 64, 84, PartCategory.Battery);
            try
            {
                Assert.That(tray.TryReserve(battery, out PartsTray2D.Spot spot, out Vector3 local), Is.True);
                Assert.That(spot.Anchor.name, Is.EqualTo("Medium"), "too big for Small; Medium is the smallest that fits");
                Assert.That(local, Is.EqualTo(Vector3.zero), "a centred sprite rests on the anchor");
                Assert.That(tray.Holds(battery), Is.True);

                tray.Release(battery);
                Assert.That(tray.Holds(battery), Is.False);
            }
            finally
            {
                Destroy(battery, root);
            }
        }

        [Test]
        public void Screws_GoToTheScrewTray_AndBigPartsToTheBigSpot()
        {
            PartsTray2D tray = MakeTray(out GameObject root);
            PhonePart screw = MakePart("Screw", 9, 9, PartCategory.Fastener);
            PhonePart cover = MakePart("Cover", 96, 192, PartCategory.Cover);
            try
            {
                Assert.That(tray.TryReserve(screw, out PartsTray2D.Spot screwSpot, out _), Is.True);
                Assert.That(screwSpot.Anchor.name, Is.EqualTo("Screw"));
                Assert.That(tray.TryReserve(cover, out PartsTray2D.Spot coverSpot, out _), Is.True);
                Assert.That(coverSpot.Anchor.name, Is.EqualTo("Big"));
                Assert.That(tray.Occupants, Is.EquivalentTo(new[] { screw, cover }));
            }
            finally
            {
                Destroy(screw, cover, root);
            }
        }

        [Test]
        public void WhenNothingFits_ThereIsNoRoom()
        {
            PartsTray2D tray = MakeTray(out GameObject root);
            PhonePart huge = MakePart("Huge", 300, 300, PartCategory.Other);
            try
            {
                Assert.That(tray.HasRoomFor(huge), Is.False);
                Assert.That(tray.TryReserve(huge, out _, out _), Is.False);
            }
            finally
            {
                Destroy(huge, root);
            }
        }
    }
}

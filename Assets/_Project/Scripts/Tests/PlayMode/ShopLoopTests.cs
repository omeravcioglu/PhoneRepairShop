using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using PhoneRepair.Shop;
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
    /// The 2D shop end to end, through a simulated mouse and keyboard in the real scene: the title,
    /// customers walking in and talking, taking or refusing jobs, full battery and screen repairs on the
    /// bench (screws, cover, connectors, the suction cup on the front, swap at the parts box, everything
    /// back), the power test, payment, and the end of the day. Requires the Shop scene in Build Settings
    /// (the setup adds it).
    /// </summary>
    public class ShopLoopTests : InputTestFixture
    {
        private const string SceneName = "Shop";
        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        private Mouse mouse;
        private Keyboard keyboard;
        private Camera camera;
        private ShopDirector director;
        private CameraRig2D rig;
        private BenchController bench;
        private BenchRepair repair;
        private BenchFeedback feedback;
        private PointerRouter router;
        private Wallet wallet;
        private SpeechBubble bubble;
        private StatusLine status;
        private SparesBox spares;
        private PartFlights2D flights;
        private ToolBelt toolBelt;
        private PanelView title;
        private PanelView summary;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTest]
        public IEnumerator Title_ThenTheFirstCustomerWalksInAndTalks()
        {
            yield return LoadShop();
            Assert.That(director.Phase, Is.EqualTo(ShopPhase.Title));

            yield return Confirm(title);
            yield return WaitUntil(() => director.Phase == ShopPhase.Talking, 5f, "first customer talking");
            Assert.That(director.CurrentCustomer, Is.Not.Null);
            Assert.That(director.CurrentCustomer.CustomerId, Is.EqualTo("fumi"), "day 1 starts with Fumi");
            Assert.That(bubble.IsShowing, Is.True);
            Assert.That(bubble.CurrentText, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator SayingNo_SendsThemAway_AndTheNextCustomerComesIn()
        {
            yield return OpenShop();
            yield return AdvanceTo(ShopPhase.Choosing);
            yield return Tap(keyboard.digit2Key);
            yield return WaitUntil(() => director.CurrentCustomer != null && director.CurrentCustomer.CustomerId == "sasaki" &&
                                         director.Phase == ShopPhase.Talking, 20f, "the next customer", advance: true);
            Assert.That(director.CustomersToday, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator BatteryJob_TakeApartSwapPutBack_GetsPaid()
        {
            yield return StartFirstJob();
            Phone2D phone = bench.Phone;
            PhonePart battery = Part(phone, "Battery");
            PhonePart connector = Part(phone, "BatteryConnector");
            PhonePart cover = Part(phone, "BackCover");
            Assert.That(battery.Condition, Is.EqualTo(PartCondition.Broken), "Fumi's battery is swollen");

            // open the back
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, counterClockwise: true, until: () => screw.State == PartState.Removed);
            }

            yield return WaitForBench();
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Hold(cover, () => cover.State == PartState.Removed);
            yield return WaitForBench();
            yield return Hold(connector, () => connector.State == PartState.Loose);
            yield return Equip(keyboard.digit1Key, ToolKind.Hands);
            yield return Hold(battery, () => battery.State == PartState.Removed);
            yield return WaitForBench();

            // swap the swollen battery at the parts box
            yield return Click(spares.transform.position);
            yield return WaitUntil(() => battery.Condition == PartCondition.Good && !spares.IsSwapping, 4f, "swap at the parts box");
            yield return WaitForBench();

            // and everything back, in reverse
            yield return Hold(battery, () => battery.State == PartState.Secured);
            yield return WaitForBench();
            yield return Hold(connector, () => connector.State == PartState.Secured);
            yield return Hold(cover, () => cover.State == PartState.Secured);
            yield return WaitForBench();
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, counterClockwise: false, until: () => screw.State == PartState.Secured);
            }

            yield return WaitForBench();
            Assert.That(phone.Device.IsFullyAssembled(), Is.True);

            yield return Tap(keyboard.enterKey);
            yield return WaitUntil(() => director.Phase == ShopPhase.Returning, 5f, "phone handed back");
            Assert.That(director.LastResult, Is.EqualTo(JobResult.Fixed));
            yield return WaitUntil(() => director.Phase == ShopPhase.Leaving, 15f, "customer pays and leaves", advance: true);
            Assert.That(wallet.Yen, Is.EqualTo(2400));
            Assert.That(director.FixedToday, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ScreenJob_UnplugTheBack_LiftTheFront_SwapAndCloseUp_GetsPaid()
        {
            yield return TakeJobFrom("sasaki");
            Phone2D phone = bench.Phone;
            PhonePart screen = Part(phone, "Screen");
            PhonePart cable = Part(phone, "DisplayConnector");
            PhonePart cover = Part(phone, "BackCover");
            Assert.That(screen.Condition, Is.EqualTo(PartCondition.Broken), "Mr. Sasaki's screen is cracked");

            // the screen won't lift while its cable is plugged in on the other side
            yield return Equip(keyboard.digit4Key, ToolKind.SuctionCup);
            yield return Point(screen);
            Assert.That(status.Text, Does.Contain("Display Connector"), "the status line says what's in the way");

            // open the back and unplug the display cable
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, counterClockwise: true, until: () => screw.State == PartState.Removed);
            }

            yield return WaitForBench();
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Hold(cover, () => cover.State == PartState.Removed);
            yield return WaitForBench();
            yield return Hold(cable, () => cable.State == PartState.Loose);

            // lift the cracked screen off the front, swap it at the parts box, press the new one on
            yield return FlipTo(phone, Phone2D.Side.Front);
            yield return Equip(keyboard.digit4Key, ToolKind.SuctionCup);
            yield return Hold(screen, () => screen.State == PartState.Removed);
            yield return WaitForBench();
            yield return Click(spares.transform.position);
            yield return WaitUntil(() => screen.Condition == PartCondition.Good && !spares.IsSwapping, 4f, "swap at the parts box");
            yield return WaitForBench();
            yield return Equip(keyboard.digit1Key, ToolKind.Hands);
            yield return Hold(screen, () => screen.State == PartState.Secured);
            yield return WaitForBench();

            // plug the cable back in and close up
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Hold(cable, () => cable.State == PartState.Secured);
            yield return Hold(cover, () => cover.State == PartState.Secured);
            yield return WaitForBench();
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, counterClockwise: false, until: () => screw.State == PartState.Secured);
            }

            yield return WaitForBench();
            Assert.That(phone.Device.IsFullyAssembled(), Is.True);

            yield return Tap(keyboard.enterKey);
            yield return WaitUntil(() => director.Phase == ShopPhase.Returning, 5f, "phone handed back");
            Assert.That(director.LastResult, Is.EqualTo(JobResult.Fixed));
            yield return WaitUntil(() => director.Phase == ShopPhase.Leaving, 15f, "customer pays and leaves", advance: true);
            Assert.That(wallet.Yen, Is.EqualTo(3000));
        }

        [UnityTest]
        public IEnumerator PryingTheCoverWhileItIsScrewedOn_SaysNope()
        {
            yield return StartFirstJob();
            Phone2D phone = bench.Phone;
            PhonePart cover = Part(phone, "BackCover");
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);

            yield return Point(cover);
            Assert.That(status.Text, Does.Contain("unscrew"), "the status line warns before pressing");

            Press(mouse.leftButton);
            yield return Frames(20);
            Release(mouse.leftButton);
            yield return Frames(2);
            Assert.That(cover.State, Is.EqualTo(PartState.Secured));
            Assert.That(feedback.LastMessage, Does.Contain("unscrew"));
        }

        [UnityTest]
        public IEnumerator HandingBackAPhoneInPieces_IsRefused()
        {
            yield return StartFirstJob();
            Phone2D phone = bench.Phone;
            PhonePart screw = Part(phone, "Screw_01");
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            yield return Turn(screw, counterClockwise: true, until: () => screw.State == PartState.Removed);
            yield return WaitForBench();

            yield return Tap(keyboard.enterKey);
            Assert.That(bench.HandedBack, Is.False);
            Assert.That(feedback.LastMessage, Does.Contain("pieces"));
        }

        [UnityTest]
        public IEnumerator HandingBackWithoutFixingIt_EarnsNothing()
        {
            yield return StartFirstJob();
            yield return Tap(keyboard.enterKey); // assembled, but the battery is still swollen
            yield return WaitUntil(() => director.Phase == ShopPhase.Returning, 5f, "phone handed back");
            Assert.That(director.LastResult, Is.EqualTo(JobResult.StillBroken));
            yield return WaitUntil(() => director.Phase == ShopPhase.Leaving, 15f, "customer leaves", advance: true);
            Assert.That(wallet.Yen, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AfterTheLastCustomer_TheDayEnds_AndTheNextDayBegins()
        {
            yield return OpenShop();
            float end = Time.time + 60f;
            while (director.Phase != ShopPhase.DayEnd && Time.time < end)
            {
                yield return Tap(director.Phase == ShopPhase.Choosing ? keyboard.digit2Key : keyboard.enterKey);
                yield return Frames(2);
            }

            Assert.That(director.Phase, Is.EqualTo(ShopPhase.DayEnd));
            Assert.That(director.Day, Is.EqualTo(1), "the Enter mashed through the last goodbye skipped the day-1 summary");
            Assert.That(summary.IsOpen, Is.True);
            Assert.That(summary.BodyText, Does.Contain("Repairs: 0 of 3"));

            yield return Confirm(summary);
            yield return WaitUntil(() => director.Day == 2, 5f, "day 2");
        }

        /// <summary>
        /// Review screenshots of the real game (3× pixels, 1920×1080). Opt-in:
        /// -runTests -testPlatform PlayMode -testFilter CaptureShopScreenshots -captureOut &lt;folder&gt;
        /// </summary>
        [UnityTest, Explicit("Writes review screenshots; run it on purpose."), Timeout(300000)]
        public IEnumerator CaptureShopScreenshots()
        {
            string folder = CommandLineArgument("-captureOut");
            if (Application.isBatchMode && folder == null)
            {
                Assert.Ignore("Pass -captureOut <folder> to write the screenshots.");
            }

            folder ??= Path.GetFullPath("Screenshots");
            Directory.CreateDirectory(folder);

            yield return LoadShop();
            yield return Frames(10);
            yield return Shot(folder, "s1_01_title");
            yield return Confirm(title);
            yield return WaitUntil(() => director.Phase == ShopPhase.Talking && !bubble.IsTyping, 10f, "first line typed");
            yield return Shot(folder, "s1_02_counter_talk");
            yield return AdvanceTo(ShopPhase.Choosing);
            yield return Frames(5);
            yield return Shot(folder, "s1_03_choice");
            yield return Tap(keyboard.digit1Key);
            yield return AdvanceTo(ShopPhase.Repairing);
            yield return WaitUntil(() => rig.Current == CameraRig2D.View.Bench && !rig.IsSliding, 5f, "at the bench");
            yield return Frames(5);
            yield return Shot(folder, "s1_04_bench_front");

            Phone2D phone = bench.Phone;
            PhonePart cover = Part(phone, "BackCover");
            yield return FlipTo(phone, Phone2D.Side.Back);
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Point(cover);
            Press(mouse.leftButton);
            yield return Frames(3);
            Release(mouse.leftButton);
            yield return Frames(4);
            yield return Shot(folder, "s1_05_nope");

            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            PhonePart first = Part(phone, "Screw_01");
            yield return Point(first);
            Vector2 centre = mouse.position.ReadValue();
            Press(mouse.leftButton);
            yield return Frames(1);
            for (int i = 1; first.Looseness < 0.45f && i < 600; i++)
            {
                float a = i * 12f * Mathf.Deg2Rad;
                Move(mouse.position, centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 30f);
                yield return null;
            }

            yield return Shot(folder, "s1_06_unscrewing");
            for (int i = 1; first.State != PartState.Removed && i < 600; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Move(mouse.position, centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 30f);
                yield return null;
            }

            Release(mouse.leftButton);
            foreach (string id in Screws.Skip(1))
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, true, () => screw.State == PartState.Removed);
            }

            yield return WaitForBench();
            PhonePart connector = Part(phone, "BatteryConnector");
            PhonePart battery = Part(phone, "Battery");
            yield return Equip(keyboard.digit3Key, ToolKind.Spudger);
            yield return Hold(cover, () => cover.State == PartState.Removed);
            yield return WaitForBench();
            yield return Hold(connector, () => connector.State == PartState.Loose);
            yield return Equip(keyboard.digit1Key, ToolKind.Hands);
            yield return Hold(battery, () => battery.State == PartState.Removed);
            yield return WaitForBench();
            yield return Point(spares.GetComponent<Interactable>());
            yield return Shot(folder, "s1_07_bench_opened");

            // finish quickly through the code paths the full-repair test drives by input
            yield return Click(spares.transform.position);
            yield return WaitUntil(() => battery.Condition == PartCondition.Good && !spares.IsSwapping, 4f, "swap");
            yield return WaitForBench();
            yield return Hold(battery, () => battery.State == PartState.Secured);
            yield return WaitForBench();
            yield return Hold(connector, () => connector.State == PartState.Secured);
            yield return Hold(cover, () => cover.State == PartState.Secured);
            yield return WaitForBench();
            yield return Equip(keyboard.digit2Key, ToolKind.Screwdriver);
            foreach (string id in Screws)
            {
                PhonePart screw = Part(phone, id);
                yield return Turn(screw, false, () => screw.State == PartState.Secured);
            }

            yield return WaitForBench();
            yield return Tap(keyboard.enterKey);
            yield return WaitUntil(() => director.Phase == ShopPhase.Returning && !bubble.IsTyping, 10f, "happy customer");
            yield return Frames(20);
            yield return Shot(folder, "s1_08_fixed");

            float end = Time.time + 60f;
            while (director.Phase != ShopPhase.DayEnd && Time.time < end)
            {
                yield return Tap(director.Phase == ShopPhase.Choosing ? keyboard.digit2Key : keyboard.enterKey);
                yield return Frames(2);
            }

            yield return Frames(5);
            yield return Shot(folder, "s1_09_day_summary");
        }

        // --- helpers -------------------------------------------------------------------------

        private IEnumerator LoadShop()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            director = Object.FindFirstObjectByType<ShopDirector>();
            rig = Object.FindFirstObjectByType<CameraRig2D>();
            bench = Object.FindFirstObjectByType<BenchController>();
            repair = Object.FindFirstObjectByType<BenchRepair>();
            feedback = Object.FindFirstObjectByType<BenchFeedback>();
            router = Object.FindFirstObjectByType<PointerRouter>();
            wallet = Object.FindFirstObjectByType<Wallet>();
            bubble = Object.FindFirstObjectByType<SpeechBubble>();
            status = Object.FindFirstObjectByType<StatusLine>();
            spares = Object.FindFirstObjectByType<SparesBox>();
            flights = Object.FindFirstObjectByType<PartFlights2D>();
            toolBelt = Object.FindFirstObjectByType<ToolBelt>();
            PanelView[] panels = Object.FindObjectsByType<PanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            title = panels.First(p => p.name == "TitleCard");
            summary = panels.First(p => p.name == "DaySummary");
            Assert.That(director, Is.Not.Null, "scene not set up - run Tools > Phone Repair > Run Setup");
            camera = rig.GetComponent<Camera>();
        }

        private IEnumerator OpenShop()
        {
            yield return LoadShop();
            yield return Confirm(title);
            yield return WaitUntil(() => director.Phase == ShopPhase.Talking, 5f, "first customer talking");
        }

        /// <summary>Waits until a panel takes input (it ignores keys for a moment after opening), then presses Enter.</summary>
        private IEnumerator Confirm(PanelView panel)
        {
            yield return WaitUntil(() => panel.IsReady, 3f, $"{panel.name} ready");
            yield return Tap(keyboard.enterKey);
        }

        /// <summary>Opens the shop, takes Fumi's job and waits until the camera is at the bench.</summary>
        private IEnumerator StartFirstJob()
        {
            yield return OpenShop();
            yield return AdvanceTo(ShopPhase.Choosing);
            yield return Tap(keyboard.digit1Key);
            yield return AdvanceTo(ShopPhase.Repairing);
            yield return WaitUntil(() => rig.Current == CameraRig2D.View.Bench && !rig.IsSliding, 5f, "camera at the bench");
            Assert.That(bench.Phone, Is.Not.Null);
        }

        /// <summary>Opens the shop, sends everyone before <paramref name="customerId"/> away, takes their job and waits for the bench.</summary>
        private IEnumerator TakeJobFrom(string customerId)
        {
            yield return OpenShop();
            while (director.CurrentCustomer.CustomerId != customerId)
            {
                CustomerDefinition refused = director.CurrentCustomer;
                yield return AdvanceTo(ShopPhase.Choosing);
                yield return Tap(keyboard.digit2Key);
                // the sad goodbye is Talking too: wait for someone new at the counter
                yield return WaitUntil(() => director.Phase == ShopPhase.DayEnd ||
                                             (director.CurrentCustomer != null && director.CurrentCustomer != refused &&
                                              director.Phase == ShopPhase.Talking), 20f, "the next customer", advance: true);
                Assert.That(director.Phase, Is.Not.EqualTo(ShopPhase.DayEnd), $"{customerId} never came in");
            }

            yield return AdvanceTo(ShopPhase.Choosing);
            yield return Tap(keyboard.digit1Key);
            yield return AdvanceTo(ShopPhase.Repairing);
            yield return WaitUntil(() => rig.Current == CameraRig2D.View.Bench && !rig.IsSliding, 5f, "camera at the bench");
            Assert.That(bench.Phone, Is.Not.Null);
        }

        /// <summary>Presses Enter (finish the line, then continue) until the director reaches a phase.</summary>
        private IEnumerator AdvanceTo(ShopPhase phase, float timeout = 20f)
        {
            float end = Time.time + timeout;
            while (director.Phase != phase && Time.time < end)
            {
                yield return Tap(keyboard.enterKey);
                yield return Frames(2);
            }

            Assert.That(director.Phase, Is.EqualTo(phase), $"waiting for {phase}");
        }

        private IEnumerator WaitUntil(Func<bool> condition, float timeout, string what, bool advance = false)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
            {
                if (advance && director.Phase != ShopPhase.Repairing)
                {
                    yield return Tap(director.Phase == ShopPhase.Choosing ? keyboard.digit2Key : keyboard.enterKey);
                }

                yield return null;
            }

            Assert.That(condition(), Is.True, $"timed out waiting for: {what}");
        }

        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private IEnumerator Equip(KeyControl key, ToolKind kind)
        {
            yield return Tap(key);
            Assert.That(toolBelt.CurrentKind, Is.EqualTo(kind), $"key {key.name}");
        }

        private IEnumerator FlipTo(Phone2D phone, Phone2D.Side side)
        {
            if (phone.Showing != side)
            {
                yield return Tap(keyboard.spaceKey);
            }

            yield return WaitUntil(() => phone.Showing == side && !phone.IsFlipping, 3f, $"phone showing its {side}");
        }

        private IEnumerator WaitForBench()
        {
            yield return WaitUntil(() => !flights.AnyFlying && !spares.IsSwapping && !repair.IsBusy, 4f, "parts to land");
            yield return new WaitForSeconds(0.15f);
        }

        private static PhonePart Part(Phone2D phone, string slot)
        {
            Assert.That(phone.Device.TryGetPart(slot, out PhonePart part), Is.True, slot);
            return part;
        }

        private Vector2 ScreenPoint(Vector3 world)
        {
            Vector3 p = camera.WorldToScreenPoint(world);
            return new Vector2(p.x, p.y);
        }

        private IEnumerator Point(PhonePart part)
        {
            Move(mouse.position, ScreenPoint(part.GetComponent<PartView2D>().VisualCenterWorld));
            yield return Frames(2);
            Interactable hovered = router.Hovered;
            Assert.That(hovered, Is.SameAs(part.GetComponent<Interactable>()),
                $"the cursor should be on {part.SlotId} but is on {(hovered != null ? hovered.name : "nothing")}");
        }

        private IEnumerator Point(Interactable target)
        {
            Move(mouse.position, ScreenPoint(target.transform.position));
            yield return Frames(2);
        }

        private IEnumerator Click(Vector3 world)
        {
            Move(mouse.position, ScreenPoint(world));
            yield return Frames(2);
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return Frames(2);
        }

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
            yield return Frames(2);
            Assert.That(until(), Is.True, $"{part.SlotId}: didn't finish (state {part.State}, last message '{feedback.LastMessage}')");
        }

        /// <summary>Points at a screw, holds, and circles the cursor around the press point, 30° a frame.</summary>
        private IEnumerator Turn(PhonePart screw, bool counterClockwise, Func<bool> until, float timeout = 10f)
        {
            yield return Point(screw);
            Vector2 centre = mouse.position.ReadValue();
            Press(mouse.leftButton);
            yield return null;
            float end = Time.time + timeout;
            for (int i = 1; !until() && Time.time < end; i++)
            {
                float angle = (counterClockwise ? i : -i) * 30f * Mathf.Deg2Rad;
                Move(mouse.position, centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 30f);
                yield return null;
            }

            Release(mouse.leftButton);
            yield return Frames(2);
            Assert.That(until(), Is.True, $"{screw.SlotId}: still {screw.State} at looseness {screw.Looseness:0.00} ('{feedback.LastMessage}')");
        }

        /// <summary>Renders the camera at 640×360 and saves it 3× up with hard pixels (1920×1080).</summary>
        private IEnumerator Shot(string folder, string name)
        {
            yield return null;
            var pixelPerfect = camera.GetComponent("PixelPerfectCamera") as Behaviour;
            bool wasEnabled = pixelPerfect != null && pixelPerfect.enabled;
            if (pixelPerfect != null)
            {
                pixelPerfect.enabled = false;
            }

            float size = camera.orthographicSize;
            camera.orthographicSize = 180f;
            const int width = 640;
            const int height = 360;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                // the first render request after a scene load can bind stale sprite textures: render twice
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;

                const int scale = 3;
                Color32[] source = image.GetPixels32();
                var big = new Texture2D(width * scale, height * scale, TextureFormat.RGB24, false);
                var pixels = new Color32[width * scale * height * scale];
                for (int y = 0; y < height * scale; y++)
                {
                    for (int x = 0; x < width * scale; x++)
                    {
                        pixels[y * width * scale + x] = source[(y / scale) * width + x / scale];
                    }
                }

                big.SetPixels32(pixels);
                big.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), big.EncodeToPNG());
                Object.Destroy(big);
                Debug.Log($"[PhoneRepair] Captured {name}.png");
            }
            finally
            {
                camera.orthographicSize = size;
                if (pixelPerfect != null)
                {
                    pixelPerfect.enabled = wasEnabled;
                }

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
    }
}

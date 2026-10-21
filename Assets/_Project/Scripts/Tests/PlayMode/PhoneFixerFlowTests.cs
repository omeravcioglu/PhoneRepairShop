using System.Collections;
using System.Linq;
using NUnit.Framework;
using PhoneRepair.Cameras;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// The Phone Fixer vertical slice in the real scenes, driven through simulated mouse/keyboard:
    /// menu -> shop -> (hover, click stations, dialogue, pause) -> bench -> receipt -> shop.
    /// Requires the PF scenes in Build Settings (Tools > Phone Fixer > Run Art Setup adds them).
    /// </summary>
    public class PhoneFixerFlowTests : InputTestFixture
    {
        private const string MenuScene = "PF_MainMenu";
        private const string ShopScene = "PF_Shop";
        private const string BenchScene = "PF_RepairBench";

        private Mouse mouse;
        private Keyboard keyboard;
        private RenderPipelineAsset pipelineBefore;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            pipelineBefore = QualitySettings.renderPipeline;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            QualitySettings.renderPipeline = pipelineBefore;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MainMenu_EnterOpensTheShop()
        {
            yield return Load(MenuScene);
            yield return Tap(keyboard.enterKey);
            yield return WaitForScene(ShopScene);
        }

        [UnityTest]
        public IEnumerator Shop_UsesTheLongShadowPipeline_AndTheBenchDoesNot()
        {
            yield return Load(ShopScene);
            var pipeline = Object.FindFirstObjectByType<PipelineOverride>();
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipeline.Pipeline), "shop should switch to PF_URP_Shop");

            yield return Load(BenchScene);
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipelineBefore), "leaving the shop restores the project pipeline");
        }

        [UnityTest]
        public IEnumerator Shop_HoveringTheWorkbench_ShowsRepair_AndClickingOpensTheBench()
        {
            yield return Load(ShopScene);
            ShopStation bench = Station(s => s.SceneToLoad == BenchScene);
            var selection = Object.FindFirstObjectByType<SelectionController>();

            yield return MoveTo(ScreenPointOf(bench.GetComponent<Interactable>()));
            Assert.That(selection.Hovered, Is.SameAs(bench.GetComponent<Interactable>()), "hover the workbench");
            Assert.That(selection.Hint.Text, Is.EqualTo("Repair"));
            Assert.That(selection.Hint.Tone, Is.EqualTo(InteractionTone.Ready), "orange = you can pick it");

            yield return ClickLeft();
            yield return WaitForScene(BenchScene);
        }

        [UnityTest]
        public IEnumerator Shop_CounterOpensTheDialogue_EscapeClosesIt_WithoutPausing()
        {
            yield return Load(ShopScene);
            UiPanel dialogue = Panel("Dialogue");
            UiPanel pause = Panel("Pause");
            var director = Object.FindFirstObjectByType<CameraDirector>();

            yield return MoveTo(ScreenPointOf(Station(s => s.Verb == "Talk").GetComponent<Interactable>()));
            yield return ClickLeft();
            Assert.That(dialogue.IsOpen, Is.True, "clicking the counter starts the conversation");
            Assert.That(director.ActiveView.name, Is.EqualTo("View_Counter"), "camera moves to the counter close-up");

            yield return Tap(keyboard.escapeKey);
            Assert.That(dialogue.IsOpen, Is.False, "Esc closes the dialogue");
            Assert.That(pause.IsOpen, Is.False, "the same Esc must not also open the pause menu");
            Assert.That(director.ActiveView.name, Is.EqualTo("View_Diorama"), "camera goes back to the diorama");
        }

        [UnityTest]
        public IEnumerator Shop_EscapeOpensPause_AndStopsTime()
        {
            yield return Load(ShopScene);
            UiPanel pause = Panel("Pause");

            yield return Tap(keyboard.escapeKey);
            Assert.That(pause.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            yield return Tap(keyboard.escapeKey);
            Assert.That(pause.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Shop_CustomersPlayTheirLoops()
        {
            yield return Load(ShopScene);
            yield return null;
            CustomerActor[] actors = Object.FindObjectsByType<CustomerActor>(FindObjectsSortMode.None);
            Assert.That(actors.Length, Is.EqualTo(5));
            foreach (CustomerActor actor in actors)
            {
                var animator = actor.GetComponent<Animator>();
                Assert.That(animator.isActiveAndEnabled && animator.runtimeAnimatorController != null, Is.True, actor.name);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(actor.LoopState), Is.True, $"{actor.name} should play {actor.LoopState}");
            }
        }

        [UnityTest]
        public IEnumerator Bench_HasFourToolKeys_PowerTestShowsTheReceipt_AndEnterGoesBackToTheShop()
        {
            yield return Load(BenchScene);
            ToolButtonView[] keys = Object.FindObjectsByType<ToolButtonView>(FindObjectsSortMode.None);
            Assert.That(keys.Length, Is.EqualTo(4), "Hands, Screwdriver, Spudger, Suction Cup (the template is hidden)");

            UiPanel receipt = Panel("Receipt");
            yield return Tap(keyboard.enterKey);
            Assert.That(receipt.IsOpen, Is.True, "Enter = power test = receipt");

            yield return Tap(keyboard.enterKey);
            yield return WaitForScene(ShopScene);
        }

        [UnityTest]
        public IEnumerator Bench_B_GoesBackToTheShop()
        {
            yield return Load(BenchScene);
            yield return Tap(keyboard.bKey);
            yield return WaitForScene(ShopScene);
        }

        // ---- helpers -------------------------------------------------------------------------------

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null; // Awake/OnEnable
            yield return null; // Start
            var director = Object.FindFirstObjectByType<CameraDirector>();
            Assert.That(director, Is.Not.Null, $"{scene} not set up - run Tools > Phone Fixer > Run Art Setup");
            director.SnapToActiveView(); // skip the intro camera move
            yield return null;
        }

        private static IEnumerator WaitForScene(string scene, float timeout = 5f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        }

        private static ShopStation Station(System.Func<ShopStation, bool> match) =>
            Object.FindObjectsByType<ShopStation>(FindObjectsSortMode.None).First(match);

        private static UiPanel Panel(string name) =>
            Object.FindObjectsByType<UiPanel>(FindObjectsSortMode.None).First(p => p.name == name);

        private static Vector2 ScreenPointOf(Interactable target)
        {
            Camera camera = Object.FindFirstObjectByType<CameraDirector>().Camera;
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

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
            yield return null;
        }
    }
}

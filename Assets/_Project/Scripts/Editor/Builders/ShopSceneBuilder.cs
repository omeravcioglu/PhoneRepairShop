using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhoneRepair.Audio;
using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using PhoneRepair.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/Shop.unity, the whole game in one scene: the shop view (y 0..360,
    /// the counter with the customer) and the bench view right below it (y -360..0, the repair mat),
    /// every system wired. Positions come from the art contract. The scene is REGENERATED on every
    /// setup run; make a copy before hand-editing it.
    /// </summary>
    internal static class ShopSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Shop.unity";

        // text colours from the art palette (ArtSource/Pixel/Scripts/px.py): ink 0, ink 1, rice 4, teal 5
        private static readonly Color DeepNavy = new Color32(22, 20, 31, 255);
        private static readonly Color Navy = new Color32(38, 40, 64, 255);
        private static readonly Color Cream = new Color32(249, 242, 227, 255);
        private static readonly Color Ink = new Color32(38, 40, 64, 255);
        private static readonly Color Mint = new Color32(192, 234, 214, 255);
        private static readonly Color Shadow = new(22 / 255f, 20 / 255f, 31 / 255f, 0.85f);

        private static Material spriteMaterial;
        private static PixelFont font;

        public static void Build(Material material, PixelFont pixelFont, IReadOnlyList<ToolDefinition> tools, ShopCalendar calendar)
        {
            spriteMaterial = material;
            font = pixelFont;
            // Opening a new scene unloads assets only C# variables point at; remember them by id.
            int[] toolIds = tools.Select(t => t.GetInstanceID()).ToArray();
            int calendarId = calendar.GetInstanceID();
            int materialId = material.GetInstanceID();
            int fontId = pixelFont.GetInstanceID();

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            List<ToolDefinition> liveTools = toolIds.Select(id => (ToolDefinition)EditorUtility.InstanceIDToObject(id)).ToList();
            calendar = (ShopCalendar)EditorUtility.InstanceIDToObject(calendarId);
            spriteMaterial = (Material)EditorUtility.InstanceIDToObject(materialId);
            font = (PixelFont)EditorUtility.InstanceIDToObject(fontId);
            if (liveTools.Any(t => t == null) || calendar == null || spriteMaterial == null || font == null)
            {
                throw new System.InvalidOperationException("Setup assets could not be reloaded after opening the new scene.");
            }

            // --- systems --------------------------------------------------------------------------
            Transform systems = new GameObject("Systems").transform;
            var input = Add<GameInput>(systems, "GameInput");
            var selection = Add<SelectionController>(systems, "SelectionController");
            var picker = Add<PointerPicker2D>(systems, "PointerPicker2D");
            var highlighter = Add<SpriteHighlighter>(systems, "SpriteHighlighter");
            var sfx = Add<SfxPlayer>(systems, "SfxPlayer");
            var toolBelt = Add<ToolBelt>(systems, "ToolBelt");
            var flights = Add<PartFlights2D>(systems, "PartFlights2D");
            var feedback = Add<BenchFeedback>(systems, "BenchFeedback");
            var repair = Add<BenchRepair>(systems, "BenchRepair");
            var wallet = Add<Wallet>(systems, "Wallet");
            var router = Add<PointerRouter>(systems, "PointerRouter");
            var bench = Add<BenchController>(systems, "BenchController");
            var director = Add<ShopDirector>(systems, "ShopDirector");
            EditorAssetUtil.WireArray(toolBelt, "tools", liveTools.Cast<Object>().ToList());

            // --- camera ---------------------------------------------------------------------------
            Camera camera = CreateCamera();
            var rig = camera.gameObject.AddComponent<CameraRig2D>();
            EditorAssetUtil.Wire(picker, ("targetCamera", camera));

            // --- the shop view --------------------------------------------------------------------
            Transform shop = new GameObject("Shop").transform;
            Placed("Background", shop, "shop_bg", SortOrder.ShopBackground);
            SpriteToggle door = Toggle("Door", shop, "shop_door_closed", "shop_door_open", SortOrder.ShopDoor);
            SpriteToggle sign = Toggle("Sign", shop, "prop_sign_closed", "prop_sign_open", SortOrder.ShopWallProps);

            Transform doorSpot = Point("DoorSpot", shop, ArtContract.Anchor("shop.customer_door"));
            Transform counterSpot = Point("CounterSpot", shop, ArtContract.Anchor("shop.customer_counter"));
            SpriteRenderer customerRenderer = Sprite("Customer", shop, null, SortOrder.Customer);
            customerRenderer.transform.position = counterSpot.position;
            var customer = customerRenderer.gameObject.AddComponent<CustomerActor>();
            EditorAssetUtil.Wire(customer, ("doorSpot", doorSpot), ("counterSpot", counterSpot));

            Placed("Counter", shop, "shop_counter", SortOrder.Counter);
            SpriteRenderer register = Placed("Register", shop, "prop_register", SortOrder.CounterProps);
            PixelText money = Text("Money", shop, ArtContract.Anchor("shop.money"), "¥0", Mint, PixelText.Align.Center, SortOrder.CounterProps + 10);
            var walletLabel = money.gameObject.AddComponent<WalletLabel>();
            EditorAssetUtil.Wire(walletLabel, ("wallet", wallet), ("label", money));
            register.gameObject.name = "Register";

            SpriteRenderer bell = Placed("Bell", shop, "prop_bell", SortOrder.CounterProps);
            MakePettable(bell.gameObject, sfx, SfxId.Bell, "Ding the bell");
            SpriteRenderer cat = Placed("Cat", shop, "prop_cat", SortOrder.CounterProps);
            MakePettable(cat.gameObject, sfx, SfxId.Meow, "Pet Denchi");
            var catAnimation = cat.gameObject.AddComponent<FlipbookAnimator>();
            catAnimation.SetFrames(PixelArtSetup.LoadFrames("prop_cat"), 7f, true, 2.6f);
            SpriteRenderer counterPhone = Placed("CounterPhone", shop, "prop_phone_counter", SortOrder.CounterProps + 5);
            counterPhone.enabled = false;

            SpeechBubble bubble = CreateBubble(shop, sfx);
            ChoicePrompt choices = CreateChoices(shop, input);

            SpriteRenderer dayPlate = Sprite("DayPlate", shop, PixelArtSetup.LoadSprite("ui_pill"), SortOrder.Ui);
            dayPlate.transform.position = ArtContract.Anchor("shop.day");
            Slice(dayPlate, 56, 15);
            PixelText dayLabel = Text("DayLabel", shop, ArtContract.Anchor("shop.day") + new Vector3(6f, -3f, 0f), "DAY 1", Cream, PixelText.Align.Left, SortOrder.UiText);

            Vector3 panelCentre = ArtContract.Anchor("shop.panel");
            PanelView title = CreatePanel("TitleCard", shop, input, panelCentre, new Vector2Int(300, 178), withLogo: true);
            PanelView summary = CreatePanel("DaySummary", shop, input, panelCentre, new Vector2Int(250, 160), withLogo: false);
            summary.gameObject.SetActive(false);

            // --- the bench view -------------------------------------------------------------------
            Transform benchRoot = new GameObject("Bench").transform;
            Placed("Background", benchRoot, "bench_bg", SortOrder.BenchBackground);
            Transform phoneAnchor = Point("PhoneAnchor", benchRoot, ArtContract.Anchor("bench.phone"));

            var tray = Add<PartsTray2D>(benchRoot, "PartsTray");
            Spot(tray, "Spot_Big_1", "bench.spot_big_1", new Vector2Int(100, 196), false, SortOrder.PartsOnMat);
            Spot(tray, "Spot_Big_2", "bench.spot_big_2", new Vector2Int(100, 196), false, SortOrder.PartsOnMat + 6);
            Spot(tray, "Spot_Medium_1", "bench.spot_medium_1", new Vector2Int(84, 100), false, SortOrder.PartsOnMat);
            Spot(tray, "Spot_Medium_2", "bench.spot_medium_2", new Vector2Int(84, 100), false, SortOrder.PartsOnMat);
            Spot(tray, "Spot_Small_1", "bench.spot_small_1", new Vector2Int(60, 50), false, SortOrder.PartsOnMat);
            Spot(tray, "Spot_Small_2", "bench.spot_small_2", new Vector2Int(60, 50), false, SortOrder.PartsOnMat);
            for (int i = 1; i <= 6; i++)
            {
                Spot(tray, $"ScrewSlot_{i}", $"bench.screw_slot_{i}", new Vector2Int(40, 28), true, SortOrder.PartsOnMat + 12);
            }

            SpriteRenderer sparesRenderer = Placed("SparesBox", benchRoot, "bench_spares_box", SortOrder.BenchProps);
            sparesRenderer.gameObject.AddComponent<Interactable>();
            var sparesCollider = sparesRenderer.gameObject.AddComponent<BoxCollider2D>();
            sparesCollider.size = new Vector2(92f, 44f);
            var spares = sparesRenderer.gameObject.AddComponent<SparesBox>();
            Transform mouth = Point("Mouth", benchRoot, ArtContract.Anchor("bench.spares_mouth"));
            mouth.SetParent(sparesRenderer.transform, true);
            EditorAssetUtil.Wire(spares, ("tray", tray), ("flights", flights), ("feedback", feedback), ("mouth", mouth));

            for (int i = 0; i < liveTools.Count && i < 4; i++)
            {
                CreateToolSlot(benchRoot, toolBelt, liveTools[i], i);
            }

            TicketView ticket = CreateTicket(benchRoot);
            StatusLine status = CreateStatusLine(benchRoot);
            CustomerPortrait portrait = CreatePortrait(benchRoot, feedback);
            PixelButton flip = CreateButton(benchRoot, "FlipButton", ArtContract.Anchor("bench.flip"), "Turn over  [Space]", "Click · Turn the phone over");
            PixelButton done = CreateButton(benchRoot, "DoneButton", ArtContract.Anchor("bench.done"), "Hand it back  [Enter]", "Click · Ring the bell and hand the phone back");

            // --- cursor and progress ring ---------------------------------------------------------
            SpriteRenderer ring = Sprite("ProgressRing", null, PixelArtSetup.LoadSprite("ui_ring"), SortOrder.Cursor - 1);
            var progress = ring.gameObject.AddComponent<ProgressRing>();
            EditorAssetUtil.Wire(progress, ("repair", repair), ("input", input), ("viewCamera", camera));
            EditorAssetUtil.WireArray(progress, "frames", PixelArtSetup.LoadFrames("ui_ring"));

            SpriteRenderer cursorRenderer = Sprite("Cursor", null, PixelArtSetup.LoadSprite("cursor_hand"), SortOrder.Cursor);
            var cursor = cursorRenderer.gameObject.AddComponent<PixelCursor>();
            EditorAssetUtil.Wire(cursor,
                ("input", input), ("viewCamera", camera), ("toolBelt", toolBelt), ("rig", rig),
                ("hand", PixelArtSetup.LoadSprite("cursor_hand")), ("handPressed", PixelArtSetup.LoadSprite("cursor_hand_pressed")));

            // --- wiring ---------------------------------------------------------------------------
            EditorAssetUtil.Wire(feedback, ("sfx", sfx), ("highlighter", highlighter), ("flights", flights), ("status", status));
            EditorAssetUtil.Wire(repair, ("toolBelt", toolBelt), ("tray", tray), ("flights", flights), ("feedback", feedback));
            EditorAssetUtil.Wire(router,
                ("input", input), ("picker", picker), ("selection", selection), ("highlighter", highlighter),
                ("repair", repair), ("status", status), ("rig", rig));
            EditorAssetUtil.Wire(bench,
                ("input", input), ("toolBelt", toolBelt), ("repair", repair), ("feedback", feedback), ("router", router),
                ("rig", rig), ("flights", flights), ("spares", spares), ("ticket", ticket), ("portrait", portrait),
                ("flipButton", flip), ("doneButton", done), ("phoneAnchor", phoneAnchor));
            EditorAssetUtil.Wire(director,
                ("calendar", calendar), ("input", input), ("rig", rig), ("customer", customer), ("bubble", bubble),
                ("choices", choices), ("door", door), ("sign", sign), ("counterPhone", counterPhone), ("wallet", wallet),
                ("dayLabel", dayLabel), ("titleCard", title), ("daySummary", summary), ("sfx", sfx), ("bench", bench));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"Could not save {ScenePath}.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ pieces

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(320f, 180f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = PixelGrid.ScreenHeight / 2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DeepNavy;
            go.AddComponent<AudioListener>();

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;

            var pixelPerfect = go.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = PixelGrid.PixelsPerUnit;
            pixelPerfect.refResolutionX = PixelGrid.ScreenWidth;
            pixelPerfect.refResolutionY = PixelGrid.ScreenHeight;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            return camera;
        }

        private static T Add<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static Transform Point(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        private static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = spriteMaterial;
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>A sprite at the position the art contract gives it.</summary>
        private static SpriteRenderer Placed(string name, Transform parent, string id, int order)
        {
            SpriteSpec spec = ArtContract.Sprite(id);
            SpriteRenderer sr = Sprite(name, parent, PixelArtSetup.LoadSprite(id), order);
            sr.transform.position = spec.Position;
            return sr;
        }

        private static SpriteToggle Toggle(string name, Transform parent, string offId, string onId, int order)
        {
            SpriteRenderer sr = Placed(name, parent, offId, order);
            var toggle = sr.gameObject.AddComponent<SpriteToggle>();
            EditorAssetUtil.Wire(toggle, ("offSprite", PixelArtSetup.LoadSprite(offId)), ("onSprite", PixelArtSetup.LoadSprite(onId)), ("isOn", false));
            return toggle;
        }

        private static void Slice(SpriteRenderer sr, int width, int height)
        {
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(width, height);
        }

        private static PixelText Text(string name, Transform parent, Vector3 position, string text, Color color, PixelText.Align align, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var label = go.AddComponent<PixelText>();
            label.Font = font;
            label.Alignment = align;
            label.Color = color;
            label.SortingOrder = order;
            label.Text = text;
            return label;
        }

        private static void MakePettable(GameObject go, SfxPlayer sfx, SfxId sound, string hint)
        {
            go.AddComponent<Interactable>();
            go.AddComponent<PolygonCollider2D>();
            var pettable = go.AddComponent<Pettable>();
            EditorAssetUtil.Wire(pettable, ("sfx", sfx), ("sound", sound), ("hint", hint));
        }

        private static void Spot(PartsTray2D tray, string name, string anchor, Vector2Int size, bool fasteners, int order)
        {
            Transform t = Point(name, tray.transform, ArtContract.Anchor(anchor));
            tray.AddSpot(t, size, fasteners, order);
        }

        private static SpeechBubble CreateBubble(Transform parent, SfxPlayer sfx)
        {
            Transform root = Point("SpeechBubble", parent, ArtContract.Anchor("shop.bubble"));
            SpriteRenderer box = Sprite("Box", root, PixelArtSetup.LoadSprite("ui_bubble"), SortOrder.SpeechBubble);
            Slice(box, 120, 40);
            SpriteRenderer tail = Sprite("Tail", root, PixelArtSetup.LoadSprite("ui_bubble_tail"), SortOrder.SpeechBubble + 1);
            SpriteRenderer more = Sprite("More", root, PixelArtSetup.LoadSprite("ui_more"), SortOrder.SpeechBubble + 2);
            PixelText text = Text("Text", root, root.position + new Vector3(8f, -7f, 0f), string.Empty, Ink, PixelText.Align.Left, SortOrder.SpeechBubble + 10);
            var bubble = root.gameObject.AddComponent<SpeechBubble>();
            EditorAssetUtil.Wire(bubble, ("box", box), ("tail", tail), ("moreArrow", more), ("text", text), ("sfx", sfx));
            return bubble;
        }

        private static ChoicePrompt CreateChoices(Transform parent, GameInput input)
        {
            Transform root = new GameObject("Choices").transform;
            root.SetParent(parent, false);
            PixelButton first = CreateButton(root, "Choice1", ArtContract.Anchor("shop.choice_1"), "[1] Leave it with me!", "Click · Take the job");
            PixelButton second = CreateButton(root, "Choice2", ArtContract.Anchor("shop.choice_2"), "[2] Sorry, can't", "Click · Turn them away");
            var prompt = root.gameObject.AddComponent<ChoicePrompt>();
            EditorAssetUtil.Wire(prompt, ("input", input), ("first", first), ("second", second));
            return prompt;
        }

        private static PixelButton CreateButton(Transform parent, string name, Vector3 centre, string label, string hint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.AddComponent<Interactable>();
            go.AddComponent<BoxCollider2D>();
            SpriteRenderer background = Sprite("Background", go.transform, PixelArtSetup.LoadSprite("ui_button"), SortOrder.Ui);
            PixelText text = Text("Label", go.transform, centre, label, Ink, PixelText.Align.Center, SortOrder.UiText);
            var button = go.AddComponent<PixelButton>();
            EditorAssetUtil.Wire(button, ("background", background), ("label", text), ("hint", hint));
            button.Resize();
            return button;
        }

        private static PanelView CreatePanel(string name, Transform parent, GameInput input, Vector3 centre, Vector2Int size, bool withLogo)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            SpriteRenderer paper = Sprite("Paper", go.transform, PixelArtSetup.LoadSprite("ui_paper"), SortOrder.Panels);
            Slice(paper, size.x, size.y);
            int top = size.y / 2 - 12;
            if (withLogo)
            {
                // the logo's top sits 4 px above the first text line; the welcome text starts under it
                SpriteRenderer logo = Sprite("Logo", go.transform, PixelArtSetup.LoadSprite("ui_logo"), SortOrder.PanelText);
                logo.transform.position = centre + new Vector3(0f, top - 36f, 0f);
                top -= 74;
            }

            // a long tips line wraps inside the paper; wrapped text is laid out from the box's left edge
            int wrap = size.x - 32;
            PixelText body = Text("Body", go.transform, centre + new Vector3(-(wrap / 2), top, 0f), string.Empty, Ink, PixelText.Align.Center, SortOrder.PanelText);
            body.WrapWidth = wrap;
            PixelButton button = CreateButton(go.transform, "Button", centre + new Vector3(0f, -size.y / 2f + 22f, 0f), "OK  [Enter]", "Click");
            const int lift = SortOrder.Panels - SortOrder.Ui + 30; // above the paper
            foreach (SpriteRenderer r in button.GetComponentsInChildren<SpriteRenderer>(true))
            {
                r.sortingOrder += lift;
            }

            foreach (PixelText t in button.GetComponentsInChildren<PixelText>(true))
            {
                t.SortingOrder += lift;
            }

            var panel = go.AddComponent<PanelView>();
            EditorAssetUtil.Wire(panel, ("input", input), ("body", body), ("button", button));
            return panel;
        }

        private static void CreateToolSlot(Transform parent, ToolBelt belt, ToolDefinition tool, int index)
        {
            Vector3 centre = ArtContract.Anchor($"bench.tool_{index + 1}");
            var go = new GameObject($"ToolSlot_{index + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.AddComponent<Interactable>();
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(36f, 36f);
            Sprite[] frames = PixelArtSetup.LoadFrames("ui_tool_slot");
            SpriteRenderer frame = Sprite("Frame", go.transform, frames[0], SortOrder.BenchProps + 10);
            SpriteRenderer icon = Sprite("Icon", go.transform, tool.Icon, SortOrder.BenchProps + 11);
            PixelText key = Text("Key", go.transform, centre + new Vector3(-17f, 18f, 0f), (index + 1).ToString(), Cream, PixelText.Align.Left, SortOrder.BenchProps + 12);
            key.ShadowColor = Shadow;
            var slot = go.AddComponent<ToolSlot>();
            EditorAssetUtil.Wire(slot,
                ("toolBelt", belt), ("index", index), ("frame", frame), ("icon", icon), ("keyLabel", key),
                ("frameNormal", frames[0]), ("frameSelected", frames.Length > 1 ? frames[1] : frames[0]));
        }

        private static TicketView CreateTicket(Transform parent)
        {
            Vector3 corner = ArtContract.Anchor("bench.ticket");
            Transform root = Point("Ticket", parent, corner);
            SpriteRenderer paper = Sprite("Paper", root, PixelArtSetup.LoadSprite("ui_ticket"), SortOrder.Ui);
            Slice(paper, 190, 44);
            PixelText title = Text("Title", root, corner + new Vector3(7f, -6f, 0f), "CUSTOMER", Navy, PixelText.Align.Left, SortOrder.UiText);
            PixelText body = Text("Body", root, corner + new Vector3(7f, -19f, 0f), string.Empty, Ink, PixelText.Align.Left, SortOrder.UiText);
            var view = root.gameObject.AddComponent<TicketView>();
            EditorAssetUtil.Wire(view, ("title", title), ("body", body));
            root.gameObject.SetActive(false);
            return view;
        }

        private static StatusLine CreateStatusLine(Transform parent)
        {
            Vector3 corner = ArtContract.Anchor("bench.status");
            Transform root = Point("StatusLine", parent, corner);
            SpriteRenderer plate = Sprite("Plate", root, PixelArtSetup.LoadSprite("ui_pill"), SortOrder.Ui);
            plate.transform.position = corner + new Vector3(-5f, 4f, 0f);
            Slice(plate, 446, 16);
            PixelText text = Text("Text", root, corner, string.Empty, Cream, PixelText.Align.Left, SortOrder.UiText);
            var status = root.gameObject.AddComponent<StatusLine>();
            EditorAssetUtil.Wire(status, ("label", text));
            return status;
        }

        private static CustomerPortrait CreatePortrait(Transform parent, BenchFeedback feedback)
        {
            Vector3 centre = ArtContract.Anchor("bench.portrait");
            Transform root = Point("Portrait", parent, centre);
            Sprite("Frame", root, PixelArtSetup.LoadSprite("ui_portrait_frame"), SortOrder.Ui);
            SpriteRenderer face = Sprite("Face", root, null, SortOrder.Ui + 2);
            PixelText name = Text("Name", root, centre + new Vector3(32f, 4f, 0f), string.Empty, Cream, PixelText.Align.Left, SortOrder.UiText);
            name.ShadowColor = Shadow;
            var portrait = root.gameObject.AddComponent<CustomerPortrait>();
            EditorAssetUtil.Wire(portrait, ("face", face), ("nameLabel", name), ("feedback", feedback));
            root.gameObject.SetActive(false);
            return portrait;
        }
    }
}

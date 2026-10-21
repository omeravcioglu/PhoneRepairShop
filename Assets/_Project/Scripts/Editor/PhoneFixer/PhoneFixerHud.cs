using PhoneRepair.Cameras;
using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using PhoneRepair.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using K = PhoneRepair.EditorTools.PhoneFixerUiKit;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds the Phone Fixer screens from the canvas designs (UI01-UI11) with the Paper &amp; Keys kit:
    /// main menu, shop HUD + station label, counter dialogue, parts drawers, upgrade catalog, pause,
    /// repair HUD (restyled bench HUD wired to the existing views) and the "Fixed!" receipt.
    /// Numbers, names and stock in the screens are placeholder content until the economy, queue and
    /// fault systems exist (see Docs/PF_Unity_Integration.md).
    /// </summary>
    internal static class PhoneFixerHud
    {
        public sealed class ShopScreens
        {
            public UiPanel Dialogue;
            public UiPanel Inventory;
            public UiPanel Upgrades;
            public UiPanel Pause;
            public DialogueBox DialogueBox;
        }

        // ---- shared -------------------------------------------------------------------------------

        public static void CreateEventSystem()
        {
            // InputSystemUIInputModule assigns its default UI actions at runtime when none are set.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>Cream fade between scenes (fades in on start). Hidden in the editor.</summary>
        public static SceneFader CreateFader()
        {
            Canvas canvas = K.CreateOverlayCanvas("Fader", 100);
            Image veil = K.Image(canvas.transform, "Veil", null, EditorAssetUtil.Hex("#F1DFC6"));
            K.Stretch(veil.rectTransform);
            var group = veil.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            var fader = canvas.gameObject.AddComponent<SceneFader>();
            EditorAssetUtil.Wire(fader, ("overlay", group));
            return fader;
        }

        private static void Hide(CanvasGroup group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static UiPanel Modal(Transform canvas, string name, float dim, out RectTransform root)
        {
            UiPanel panel = K.Modal(canvas, name, dim, out root);
            Hide(root.GetComponent<CanvasGroup>());
            return panel;
        }

        private static void Shortcut(GameObject host, Key key, Button button, UiPanel whenOpen)
        {
            var shortcut = host.AddComponent<KeyShortcut>();
            EditorAssetUtil.Wire(shortcut, ("key", key), ("button", button), ("whenOpen", whenOpen));
        }

        private static TextMeshProUGUI Line(Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal, bool wrap = false) =>
            K.Text(parent, "Text", text, size, color, style, TextAlignmentOptions.Left, wrap);

        /// <summary>"Offer ........ ¥3,200" row.</summary>
        private static void ValueRow(Transform parent, string label, string value, float size, Color ink, bool boldValue = true)
        {
            RectTransform row = K.Row(parent, "Row_" + label, 12f);
            K.Text(row, "Label", label, size, ink);
            K.Spacer(row);
            K.Text(row, "Value", value, size, ink, boldValue ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Right);
        }

        // ---- main menu (UI01) ------------------------------------------------------------------------

        public static void BuildMainMenu(UiActions actions)
        {
            CreateEventSystem();
            Canvas canvas = K.CreateOverlayCanvas("MainMenu", 0);
            Transform root = canvas.transform;

            // Logo plaque, top-left, tilted like a shop sign.
            RectTransform plaque = K.Card(root, "Logo", K.PanelHard, Color.white, new RectOffset(48, 48, 33, 39), 6f);
            K.Corner(plaque, new Vector2(0f, 1f), new Vector2(96f, -84f));
            plaque.localRotation = Quaternion.Euler(0f, 0f, 2f);
            K.Text(plaque, "Kicker", "SCREENS  ·  BATTERIES  ·  PORTS", 21f, K.Slate, FontStyles.Bold);
            K.Text(plaque, "Title", "PHONE FIXER", 108f, K.Navy, FontStyles.Bold);
            RectTransform tagRow = K.Row(plaque, "Tags", 15f);
            K.Chip(tagRow, "OPEN", K.Teal, 21f);
            K.Text(tagRow, "Tagline", "a tiny repair shop, golden hour, forever", 24f, K.Slate, FontStyles.Italic);

            // Menu keys.
            RectTransform keys = K.Rect(root, "Menu");
            K.VLayout(keys.gameObject, new RectOffset(), 18f, TextAnchor.UpperLeft, fitWidth: true, fitHeight: true);
            K.Corner(keys, new Vector2(0f, 0f), new Vector2(96f, 120f));

            K.Key open = K.Keycap(keys, "OpenShop", "Open the shop", "Enter", K.KeyStyle.Teal, new Vector2(570f, 102f), 33f,
                TextAlignmentOptions.Left);
            RectTransform sub = K.Row(keys, "SaveInfo", 12f, TextAnchor.MiddleLeft, new RectOffset(24, 0, 0, 6));
            K.Icon(sub, "Coin", K.Coin, 27f);
            K.Text(sub, "Text", "Day 12  ·  ¥48,200  ·  34", 21f, K.Navy, FontStyles.Bold);
            K.Icon(sub, "Star", K.Star, 27f);
            K.Keycap(keys, "NewShop", "New shop", null, K.KeyStyle.Disabled, new Vector2(570f, 78f), 26f, TextAlignmentOptions.Left);
            K.Keycap(keys, "Settings", "Settings", null, K.KeyStyle.Disabled, new Vector2(570f, 78f), 26f, TextAlignmentOptions.Left);
            K.Keycap(keys, "Credits", "Credits", null, K.KeyStyle.Disabled, new Vector2(570f, 78f), 26f, TextAlignmentOptions.Left);
            K.Key quit = K.Keycap(keys, "Quit", "Quit", null, K.KeyStyle.Paper, new Vector2(570f, 78f), 26f, TextAlignmentOptions.Left);

            K.OnClick(open.Button, actions.LoadScene, PhoneFixerPaths.ShopSceneName);
            K.OnClick(quit.Button, actions.Quit);
            Shortcut(canvas.gameObject, Key.Enter, open.Button, null);

            TextMeshProUGUI footer = K.Text(root, "Footer", "open again today  ·  Phone Fixer art pass v1", 19f, K.Navy, FontStyles.Bold,
                TextAlignmentOptions.Right);
            K.Place(footer.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(900f, 30f));
        }

        // ---- shop HUD (UI02) + screens -------------------------------------------------------------

        public static ShopScreens BuildShop(SelectionController selection, Camera worldCamera, CameraDirector director, CameraView dioramaView,
            CameraView counterView, UiActions actions)
        {
            CreateEventSystem();
            var screens = new ShopScreens();
            Canvas hud = K.CreateOverlayCanvas("ShopHUD", 0);
            Canvas modal = K.CreateOverlayCanvas("ShopScreens", 10);

            screens.Dialogue = BuildDialogue(modal.transform, out screens.DialogueBox);
            screens.Inventory = BuildInventory(modal.transform);
            screens.Upgrades = BuildUpgrades(modal.transform);
            screens.Pause = BuildPause(modal.transform, actions);

            // The dialogue is a customer close-up: the camera moves in while it is open.
            if (director != null && counterView != null && dioramaView != null)
            {
                K.On(screens.Dialogue.Opened, director.Activate, counterView);
                K.On(screens.Dialogue.Closed, director.Activate, dioramaView);
            }

            hud.gameObject.AddComponent<HudFader>(); // screens replace the HUD while open
            Transform root = hud.transform;
            BuildStatusBar(root);
            BuildShopKeys(root, screens);
            BuildJobLine(root);
            BuildShopNotice(root);
            BuildStationLabel(root, selection, worldCamera);
            return screens;
        }

        private static void BuildStatusBar(Transform root)
        {
            Image bar = K.Image(root, "StatusBar", K.Panel, Color.white, raycast: true);
            K.HLayout(bar.gameObject, new RectOffset(6, 6, 3, 12), 0f, TextAnchor.MiddleLeft);
            K.Corner(bar.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -27f));

            RectTransform day = K.Row(bar.transform, "Day", 15f, TextAnchor.MiddleLeft, new RectOffset(21, 24, 12, 12));
            K.Icon(day, "Sun", K.Sun, 45f);
            RectTransform dayText = K.Column(day, "Text", 0f);
            K.Text(dayText, "Weekday", "DAY 3  ·  WED", 18f, K.Slate, FontStyles.Bold);
            TextMeshProUGUI clock = K.Text(dayText, "Clock", "16:30", 34f, K.Navy, FontStyles.Bold);

            K.Divider(bar.transform, vertical: true, 54f);
            RectTransform money = K.Row(bar.transform, "Wallet", 15f, TextAnchor.MiddleLeft, new RectOffset(24, 24, 12, 12));
            K.Icon(money, "Coin", K.Coin, 42f);
            K.Text(money, "Amount", "¥12,480", 36f, K.Navy, FontStyles.Bold);

            K.Divider(bar.transform, vertical: true, 54f);
            RectTransform rep = K.Row(bar.transform, "Reputation", 12f, TextAnchor.MiddleLeft, new RectOffset(24, 24, 12, 12));
            K.Icon(rep, "Star", K.Star, 40f);
            K.Text(rep, "Value", "4.2", 36f, K.Navy, FontStyles.Bold);

            // Opening-hours bar with the sun travelling along it.
            Image track = K.Image(root, "DayBar", K.Pill, K.Paper);
            K.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(750f, -36f), new Vector2(420f, 24f));
            Image fill = K.Image(track.transform, "Fill", K.PillSolid, K.Butter);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(0f, -3f);
            Image sun = K.Image(track.transform, "SunMarker", K.CircleOutlined, K.Peach);
            K.Place(sun.rectTransform, new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(33f, 33f));
            TextMeshProUGUI open = K.Text(track.transform, "Open", "9:00 open", 18f, K.Navy, FontStyles.Bold);
            K.Place(open.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -6f), new Vector2(200f, 24f));
            TextMeshProUGUI close = K.Text(track.transform, "Close", "closing 19:00", 18f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Right);
            K.Place(close.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(200f, 24f));

            var clockView = bar.gameObject.AddComponent<DayClockLabel>();
            EditorAssetUtil.Wire(clockView, ("label", clock), ("sunMarker", sun.rectTransform), ("dayFill", fill.rectTransform));
        }

        private static void BuildShopKeys(Transform root, ShopScreens screens)
        {
            RectTransform keys = K.Rect(root, "ScreenKeys");
            K.HLayout(keys.gameObject, new RectOffset(), 18f, TextAnchor.UpperRight);
            K.Corner(keys, new Vector2(1f, 1f), new Vector2(-30f, -27f));
            K.Key parts = K.Keycap(keys, "Parts", "Parts", "I", K.KeyStyle.Paper, new Vector2(168f, 78f), 25f);
            K.Key upgrades = K.Keycap(keys, "Upgrades", "Upgrades", "U", K.KeyStyle.Paper, new Vector2(198f, 78f), 25f);
            Image dot = K.Image(upgrades.Face.transform, "NewDot", K.CircleOutlined, K.Orange);
            K.Place(dot.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(3f, 3f), new Vector2(22f, 22f));
            K.Key menu = K.Keycap(keys, "Menu", "Menu", "Esc", K.KeyStyle.Paper, new Vector2(168f, 78f), 25f);
            K.OnClick(parts.Button, screens.Inventory.Show);
            K.OnClick(upgrades.Button, screens.Upgrades.Show);
            K.OnClick(menu.Button, screens.Pause.Show);
        }

        private static void BuildJobLine(Transform root)
        {
            Image line = K.Image(root, "Clothesline", K.Rounded, K.Walnut);
            K.Place(line.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 276f), new Vector2(870f, 5f));

            RectTransform tickets = K.Rect(root, "JobTickets");
            K.HLayout(tickets.gameObject, new RectOffset(), 33f, TextAnchor.UpperLeft);
            K.Corner(tickets, new Vector2(0f, 0f), new Vector2(60f, 54f));
            JobTicket(tickets, "Fumi", "Mrs. Tanabe", "Mochi One", "Battery", "due Thu", 0.82f, K.Teal, true, -2f);
            JobTicket(tickets, "Ren", "Ren", "Mochi Dango", "Port", "45 min", 0.38f, K.Orange, false, 1.5f);
            FreeSlot(tickets, -1f);
        }

        private static void JobTicket(Transform parent, string customer, string who, string phone, string fault, string due, float progress,
            Color progressColor, bool active, float rotation)
        {
            RectTransform holder = K.Rect(parent, "Ticket_" + who);
            K.Size(holder.gameObject, 252f, 204f);
            if (active)
            {
                Image ring = K.Image(holder, "ActiveRing", K.Rounded, K.Teal);
                K.Stretch(ring.rectTransform, -6f, -6f, -6f, 2f);
                ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            }

            Image card = K.Image(holder, "Card", K.Panel, Color.white, raycast: true);
            K.Stretch(card.rectTransform);
            card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            K.VLayout(card.gameObject, new RectOffset(18, 18, 24, 21), 10f, TextAnchor.UpperLeft, fitWidth: false, fitHeight: false);

            RectTransform head = K.Row(card.transform, "Who", 12f);
            K.PortraitStamp(head, customer, 54f, K.Cream);
            RectTransform names = K.Column(head, "Names", 0f);
            K.Text(names, "Name", who, 21f, K.Navy, FontStyles.Bold);
            K.Text(names, "Phone", phone, 18f, K.Slate);

            RectTransform state = K.Row(card.transform, "State", 9f);
            K.Chip(state, fault, K.Coral, 18f);
            K.Spacer(state);
            K.Text(state, "Due", due, 20f, K.Navy, FontStyles.Bold);

            K.ProgressBar(card.transform, "Progress", 216f, 15f, progress, progressColor);

            Image pin = K.Image(card.transform, "Pin", K.RoundedOutlined, EditorAssetUtil.Hex("#C9A27A"));
            pin.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            K.Place(pin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(21f, 36f));
        }

        private static void FreeSlot(Transform parent, float rotation)
        {
            RectTransform holder = K.Rect(parent, "Ticket_Free");
            K.Size(holder.gameObject, 252f, 168f);
            Image card = K.Image(holder, "Card", K.RoundedOutlined, new Color(1f, 0.99f, 0.97f, 0.55f));
            K.Stretch(card.rectTransform);
            card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            K.VLayout(card.gameObject, new RectOffset(18, 18, 30, 24), 6f, TextAnchor.MiddleCenter, fitWidth: false, fitHeight: false);
            K.Text(card.transform, "Title", "Free slot", 21f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Text(card.transform, "Hint", "Take more jobs at the counter", 18f, K.Navy, FontStyles.Normal, TextAlignmentOptions.Center, wrap: true);
        }

        private static void BuildShopNotice(Transform root)
        {
            RectTransform column = K.Rect(root, "Notices");
            K.VLayout(column.gameObject, new RectOffset(), 15f, TextAnchor.LowerRight, fitWidth: true, fitHeight: true);
            K.Corner(column, new Vector2(1f, 0f), new Vector2(-30f, 33f));
            column.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;

            Image hint = K.Image(column, "Controls", K.PillSolid, new Color(1f, 0.99f, 0.97f, 0.85f));
            K.HLayout(hint.gameObject, new RectOffset(18, 18, 6, 8), 18f, TextAnchor.MiddleCenter);
            K.Text(hint.transform, "Click", "Click a station", 18f, K.Navy, FontStyles.Bold);
            K.KeyHint(hint.transform, "I", "parts", K.Navy, 18f);
            K.KeyHint(hint.transform, "U", "upgrades", K.Navy, 18f);
            K.KeyHint(hint.transform, "Esc", "menu", K.Navy, 18f);

            Image notice = K.Image(column, "CounterNotice", K.PanelTealDrop, Color.white);
            K.HLayout(notice.gameObject, new RectOffset(21, 24, 15, 21), 15f, TextAnchor.MiddleLeft);
            K.Icon(notice.transform, "Bell", K.CircleOutlined, 30f, K.Brass);
            K.Text(notice.transform, "Text", "Mrs. Tanabe is waiting at the counter", 23f, K.Navy, FontStyles.Bold);
        }

        private static void BuildStationLabel(Transform root, SelectionController selection, Camera worldCamera)
        {
            Image pill = K.Image(root, "StationLabel", K.PillSolid, K.Navy);
            K.HLayout(pill.gameObject, new RectOffset(24, 9, 9, 9), 12f, TextAnchor.MiddleCenter);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pill.rectTransform.pivot = new Vector2(0.5f, 0f);
            TextMeshProUGUI name = K.Text(pill.transform, "Name", "Workbench", 23f, K.Paper, FontStyles.Bold);
            Image verbChip = K.Image(pill.transform, "Verb", K.PillSolid, K.Orange);
            K.HLayout(verbChip.gameObject, new RectOffset(15, 15, 4, 6), 0f, TextAnchor.MiddleCenter);
            TextMeshProUGUI verb = K.Text(verbChip.transform, "Text", "Repair", 20f, K.NavyDeep, FontStyles.Bold);
            var group = pill.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            var label = pill.gameObject.AddComponent<StationLabel>();
            EditorAssetUtil.Wire(label,
                ("selection", selection),
                ("worldCamera", worldCamera),
                ("pill", pill.rectTransform),
                ("group", group),
                ("nameLabel", name),
                ("verbLabel", verb),
                ("screenOffset", new Vector2(0f, 18f)));
        }

        // ---- counter dialogue (UI03) ------------------------------------------------------------------

        private static UiPanel BuildDialogue(Transform canvas, out DialogueBox box)
        {
            UiPanel panel = Modal(canvas, "Dialogue", 0.3f, out RectTransform root);

            Image where = K.Image(root, "Where", K.Pill, K.Paper);
            K.HLayout(where.gameObject, new RectOffset(21, 21, 9, 11), 15f);
            K.Corner(where.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -30f));
            K.Text(where.transform, "Place", "At the counter", 21f, K.Navy, FontStyles.Bold);
            K.Text(where.transform, "Time", "16:31", 21f, K.Navy);

            // Who: big portrait stamp and name plate.
            RectTransform who = K.Rect(root, "Customer");
            K.VLayout(who.gameObject, new RectOffset(), 15f, TextAnchor.UpperCenter, fitWidth: true, fitHeight: true);
            K.Corner(who, new Vector2(0f, 0f), new Vector2(60f, 75f));
            who.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            K.PortraitStamp(who, "Fumi", 330f, EditorAssetUtil.Hex("#E9C2B4"));
            Image plate = K.Image(who, "NamePlate", K.Rounded, K.Navy);
            K.VLayout(plate.gameObject, new RectOffset(27, 27, 12, 14), 0f, TextAnchor.MiddleCenter, fitWidth: true, fitHeight: true);
            K.Text(plate.transform, "Name", "Mrs. Tanabe", 27f, K.Paper, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Text(plate.transform, "Visits", "Regular  ·  6th visit", 18f, K.Mist, FontStyles.Bold, TextAlignmentOptions.Center);

            // What she says.
            RectTransform bubble = K.Card(root, "Speech", K.PanelHard, Color.white, new RectOffset(39, 39, 33, 36), 15f, 840f);
            K.Corner(bubble, new Vector2(0f, 0f), new Vector2(447f, 156f));
            TextMeshProUGUI speech = K.Text(bubble, "Speech", "Good afternoon, dear!", 30f, K.Navy, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            K.Text(bubble, "Hint", "Answer with 1 - 3, or click", 18f, K.Slate, FontStyles.Bold);
            box = bubble.gameObject.AddComponent<DialogueBox>();
            var lines = new[]
            {
                "Good afternoon, dear! My phone gets <b>so warm</b>, and then it just... goes to sleep at forty percent. Could you have a look before Thursday?",
                "Wet? Oh no, no. Well... it did have a little swim in the bath once. Only a <b>little</b> swim!",
                "Oh, that's quite alright, dear. I'll bring you some mikan tomorrow and try again.",
            };
            var so = new UnityEditor.SerializedObject(box);
            UnityEditor.SerializedProperty linesProperty = so.FindProperty("lines");
            linesProperty.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                linesProperty.GetArrayElementAtIndex(i).stringValue = lines[i];
            }

            so.FindProperty("speech").objectReferenceValue = speech;
            so.ApplyModifiedPropertiesWithoutUndo();
            speech.text = lines[0];

            BuildTicketCard(root);

            // Answers.
            RectTransform answers = K.Rect(root, "Answers");
            K.VLayout(answers.gameObject, new RectOffset(), 18f, TextAnchor.LowerRight, fitWidth: true, fitHeight: true);
            K.Corner(answers, new Vector2(1f, 0f), new Vector2(-42f, 51f));
            K.Key take = K.Keycap(answers, "Answer_Take", "Leave it with me", null, K.KeyStyle.Teal, new Vector2(510f, 84f), 25f,
                TextAlignmentOptions.Left, "1");
            K.Key ask = K.Keycap(answers, "Answer_Ask", "Does it ever get wet?", null, K.KeyStyle.Paper, new Vector2(510f, 84f), 25f,
                TextAlignmentOptions.Left, "2");
            K.Key decline = K.Keycap(answers, "Answer_Decline", "Sorry, we're fully booked", null, K.KeyStyle.Paper, new Vector2(510f, 84f), 25f,
                TextAlignmentOptions.Left, "3");
            K.OnClick(take.Button, panel.Hide);
            K.OnClick(ask.Button, box.Say, 1);
            K.OnClick(decline.Button, box.Say, 2);
            Shortcut(root.gameObject, Key.Digit1, take.Button, panel);
            Shortcut(root.gameObject, Key.Digit2, ask.Button, panel);
            Shortcut(root.gameObject, Key.Digit3, decline.Button, panel);

            K.On(panel.Opened, box.Say, 0);
            EditorAssetUtil.Wire(panel, ("closeOnEscape", true), ("popTarget", bubble));
            return panel;
        }

        private static void BuildTicketCard(RectTransform root)
        {
            RectTransform ticket = K.Card(root, "Ticket", K.PanelHard, Color.white, new RectOffset(33, 30, 27, 33), 15f, 510f);
            K.Corner(ticket, new Vector2(1f, 1f), new Vector2(-42f, -96f));

            RectTransform head = K.Row(ticket, "Head", 12f);
            K.Text(head, "Title", "Repair ticket", 27f, K.Navy, FontStyles.Bold);
            K.Spacer(head);
            K.Text(head, "Number", "No. 0041", 22f, K.Navy);

            RectTransform phone = K.Row(ticket, "Phone", 18f);
            Image swatch = K.Image(phone, "Swatch", K.RoundedOutlined, EditorAssetUtil.Hex("#F4C6D2"));
            K.Size(swatch.gameObject, 60f, 105f);
            RectTransform phoneText = K.Column(phone, "Text", 3f);
            K.Text(phoneText, "Model", "Mochi One", 25f, K.Navy, FontStyles.Bold);
            K.Text(phoneText, "Notes", "Sakura, flip case, 4 years old", 19f, K.Slate);

            RectTransform chips = K.Row(ticket, "Symptoms", 12f);
            K.Chip(chips, "Gets hot", K.Coral);
            K.Chip(chips, "Dies at 40%", K.Coral);
            K.Chip(chips, "? hidden", K.Disabled);

            K.Divider(ticket, vertical: false, 440f);
            ValueRow(ticket, "Offer", "¥3,200", 21f, K.Navy);
            ValueRow(ticket, "Deadline", "Thu 18:00", 21f, K.Navy);
            ValueRow(ticket, "Tip if early", "3 mikan", 21f, K.Navy);

            RectTransform mood = K.Row(ticket, "Mood", 12f);
            K.Icon(mood, "Smile", K.Sun, 30f);
            K.Text(mood, "Text", "Very patient today", 20f, K.Navy, FontStyles.Bold);
        }

        // ---- parts drawers (UI07) --------------------------------------------------------------------

        private static UiPanel BuildInventory(Transform canvas)
        {
            UiPanel panel = Modal(canvas, "Inventory", 0.55f, out RectTransform root);
            Image cabinet = K.Image(root, "Cabinet", K.PanelWalnut, Color.white, raycast: true);
            K.Place(cabinet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1812f, 990f));
            K.VLayout(cabinet.gameObject, new RectOffset(30, 30, 27, 39), 21f, TextAnchor.UpperLeft, fitWidth: false, fitHeight: false);

            RectTransform header = K.Row(cabinet.transform, "Header", 12f);
            K.Text(header, "Title", "Parts drawers", 45f, K.Paper, FontStyles.Bold);
            K.Spacer(header);
            string[] tabs = { "All", "Screens", "Batteries", "Ports", "Boards", "Cameras", "Small bits" };
            foreach (string tab in tabs)
            {
                K.Chip(header, tab, tab == "All" ? K.Orange : K.Paper, 20f);
            }

            K.Key close = K.Keycap(header, "Close", "Esc", null, K.KeyStyle.Paper, new Vector2(84f, 66f), 22f);
            K.OnClick(close.Button, panel.Hide);

            RectTransform body = K.Row(cabinet.transform, "Body", 27f, TextAnchor.UpperLeft);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Image drawers = K.Image(body, "Drawers", K.Rounded, K.WalnutDark);
            var grid = drawers.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.spacing = new Vector2(18f, 18f);
            grid.cellSize = new Vector2(280f, 222f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            (string name, string grade, string count, Color swatch)[] stock =
            {
                ("Screen · Mochi One", "A", "×2", EditorAssetUtil.Hex("#2F3B4A")),
                ("Screen · Dango", "B", "×1", EditorAssetUtil.Hex("#2F3B4A")),
                ("Battery · Mochi One", "A · B", "×3", EditorAssetUtil.Hex("#8FC1B5")),
                ("Battery · Dango", "A", "×1", EditorAssetUtil.Hex("#8FC1B5")),
                ("Charging port", "A", "×4", EditorAssetUtil.Hex("#B8BEC6")),
                ("Display flex", "A", "×2", EditorAssetUtil.Hex("#E8B04B")),
                ("Camera · Dango", null, null, Color.clear),
                ("Logicboard · One", "C · refurb", "×1", EditorAssetUtil.Hex("#7FA36B")),
                ("Screws P2", "bulk", "×48", EditorAssetUtil.Hex("#C9CDD2")),
                ("Speaker", "B", "×2", EditorAssetUtil.Hex("#4A5463")),
                ("Back cover · Sakura", "A", "×1", EditorAssetUtil.Hex("#F4C6D2")),
                ("Adhesive strips", "bulk", "×10", EditorAssetUtil.Hex("#F5E8D3")),
            };
            foreach (var item in stock)
            {
                DrawerCard(drawers.transform, item.name, item.grade, item.count, item.swatch, selected: item.name == "Battery · Dango");
            }

            BuildPartDetail(body);

            RectTransform footer = K.Row(cabinet.transform, "Footer", 24f);
            K.Text(footer, "Drawers", "Drawers 11 / 12", 22f, K.Paper, FontStyles.Bold);
            K.Spacer(footer);
            K.Icon(footer, "Coin", K.Coin, 30f);
            K.Text(footer, "Wallet", "Wallet  ¥12,480", 22f, K.Paper, FontStyles.Bold);
            K.KeyHint(footer, "Esc", "close", K.Paper, 20f);

            EditorAssetUtil.Wire(panel, ("toggleKey", Key.I), ("closeOnEscape", true), ("popTarget", cabinet.rectTransform));
            return panel;
        }

        private static void DrawerCard(Transform grid, string name, string grade, string count, Color swatch, bool selected)
        {
            bool empty = grade == null;
            RectTransform holder = K.Rect(grid, "Drawer_" + name);
            if (selected)
            {
                Image ring = K.Image(holder, "SelectedRing", K.Rounded, K.Teal);
                K.Stretch(ring.rectTransform, -6f, -6f, -6f, -6f);
            }

            Image card = K.Image(holder, "Card", empty ? K.RoundedOutlined : K.PanelWhite,
                empty ? new Color(K.Kraft.r, K.Kraft.g, K.Kraft.b, 0.35f) : selected ? K.KraftLight : K.Kraft);
            K.Stretch(card.rectTransform);
            K.VLayout(card.gameObject, new RectOffset(15, 15, 12, 15), 6f, empty ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft, fitWidth: false, fitHeight: false);
            if (empty)
            {
                K.Text(card.transform, "Name", name, 20f, K.Paper, FontStyles.Bold, TextAlignmentOptions.Center);
                K.Text(card.transform, "State", "Empty  ·  order", 18f, K.Paper, FontStyles.Bold, TextAlignmentOptions.Center);
                return;
            }

            RectTransform handleRow = K.Row(card.transform, "HandleRow", 0f, TextAnchor.MiddleCenter);
            Image handle = K.Image(handleRow, "Handle", K.RoundedOutlined, K.Brass);
            K.Size(handle.gameObject, 51f, 12f);

            RectTransform art = K.Row(card.transform, "Art", 0f, TextAnchor.MiddleCenter);
            K.Size(art.gameObject, -1f, 84f);
            Image part = K.Image(art, "Part", K.RoundedOutlined, swatch);
            K.Size(part.gameObject, name.StartsWith("Screen") || name.StartsWith("Back") ? 54f : 96f,
                name.StartsWith("Screen") || name.StartsWith("Back") ? 84f : 51f);

            K.Text(card.transform, "Name", name, 20f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Left, wrap: true);
            RectTransform info = K.Row(card.transform, "Info", 9f);
            K.Chip(info, grade, K.Paper, 17f);
            K.Spacer(info);
            K.Text(info, "Count", count, 25f, K.Navy, FontStyles.Bold);
        }

        private static void BuildPartDetail(Transform body)
        {
            RectTransform detail = K.Card(body, "Detail", K.PanelHard, Color.white, new RectOffset(33, 33, 30, 33), 15f, 510f);
            Image battery = K.Image(detail, "Battery", K.RoundedOutlined, EditorAssetUtil.Hex("#8FC1B5"));
            K.Size(battery.gameObject, -1f, 150f);
            TextMeshProUGUI label = K.Text(battery.transform, "Label", "MOCHI  ·  4100 mAh", 27f, K.NavyDeep, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Stretch(label.rectTransform);

            K.Text(detail, "Title", "Mochi Dango battery", 33f, K.Navy, FontStyles.Bold);
            K.Text(detail, "Fits", "4100 mAh  ·  fits Mochi Dango only", 20f, K.Slate);
            RectTransform chips = K.Row(detail, "Chips", 12f);
            K.Chip(chips, "Grade A · new", K.Leaf);
            K.Chip(chips, "In drawer ×1", K.Paper);
            K.Divider(detail, vertical: false, 440f);
            ValueRow(detail, "Supplier price", "¥1,400", 22f, K.Navy);
            ValueRow(detail, "Arrives", "tomorrow 9:00", 22f, K.Navy);

            RectTransform qty = K.Row(detail, "Quantity", 15f, TextAnchor.MiddleCenter);
            K.Keycap(qty, "Less", "-", null, K.KeyStyle.Paper, new Vector2(66f, 66f), 30f);
            K.Text(qty, "Count", "2", 36f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Keycap(qty, "More", "+", null, K.KeyStyle.Paper, new Vector2(66f, 66f), 30f);
            K.Keycap(detail, "Order", "Order  ·  ¥2,800", null, K.KeyStyle.Teal, new Vector2(444f, 84f), 27f);
        }

        // ---- upgrade catalog (UI08) ------------------------------------------------------------------

        private static UiPanel BuildUpgrades(Transform canvas)
        {
            UiPanel panel = Modal(canvas, "Upgrades", 0.55f, out RectTransform root);
            Image catalog = K.Image(root, "Catalog", K.PanelHard, Color.white, raycast: true);
            K.Place(catalog.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1740f, 960f));
            K.VLayout(catalog.gameObject, new RectOffset(42, 42, 33, 42), 24f, TextAnchor.UpperLeft, fitWidth: false, fitHeight: false);

            RectTransform header = K.Row(catalog.transform, "Header", 18f);
            RectTransform brand = K.Column(header, "Brand", 0f);
            K.Text(brand, "Name", "FIXER SUPPLY CO.", 45f, K.Navy, FontStyles.Bold);
            K.Text(brand, "Issue", "Autumn catalog  ·  vol. 3", 20f, K.Slate, FontStyles.Bold);
            K.Chip(header, "NEW!", K.Orange, 20f);
            K.Spacer(header);
            foreach (string tab in new[] { "Tools", "Bench", "Shop", "Decor" })
            {
                K.Chip(header, tab, tab == "Tools" ? K.Teal : K.Paper, 21f);
            }

            K.Key close = K.Keycap(header, "Close", "Esc", null, K.KeyStyle.Paper, new Vector2(84f, 66f), 22f);
            K.OnClick(close.Button, panel.Hide);

            RectTransform body = K.Row(catalog.transform, "Body", 30f, TextAnchor.UpperLeft);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // Featured item.
            RectTransform featured = K.Card(body, "Featured", K.PanelWhite, K.Butter, new RectOffset(33, 33, 30, 33), 15f, 540f);
            K.Chip(featured, "FEATURED", K.Paper, 18f);
            Image art = K.Image(featured, "Art", K.RoundedOutlined, K.Cream);
            K.Size(art.gameObject, -1f, 210f);
            DrawElectricScrewdriver(art.transform);
            K.Text(featured, "Name", "Electric screwdriver", 36f, K.Navy, FontStyles.Bold);
            K.Text(featured, "Pitch", "Hold to auto-turn. Screws come out twice as fast, and it hums a little tune.", 22f, K.Slate,
                FontStyles.Normal, TextAlignmentOptions.Left, wrap: true);
            K.Text(featured, "Price", "¥18,000", 42f, K.Navy, FontStyles.Bold);
            K.Chip(featured, "Need ¥5,520 more", K.Coral, 20f);

            Image items = K.Image(body, "Items", null, Color.clear);
            var grid = items.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(24f, 24f);
            grid.cellSize = new Vector2(339f, 318f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            UpgradeCard(items.transform, "Magnifier lamp", "Hidden faults glow coral", "¥12,500", "¥20 short", K.Coral, null, DrawMagnifier);
            UpgradeCard(items.transform, "Battery tester Pro", "Reads health and heat", "¥9,800", "Owned", K.Leaf, null, DrawTester);
            UpgradeCard(items.transform, "Ultrasonic cleaner", "Corrosion off in one go", "¥42,000", "needs 40 stars", K.Disabled, null, DrawCleaner);
            UpgradeCard(items.transform, "Second workbench", "Hire a helper for easy jobs", "¥150,000", "needs Tier 1", K.Disabled, null, DrawWorkbench);
            UpgradeCard(items.transform, "Shop radio", "+5% customer patience", "¥4,500", null, Color.clear, "Buy", DrawRadio);
            UpgradeCard(items.transform, "Cushioned bench", "Waiting customers stay calm", "¥8,000", null, Color.clear, "Buy", DrawCushionBench);

            RectTransform footer = K.Row(catalog.transform, "Footer", 18f);
            K.Text(footer, "Small print", "Delivered next morning  ·  prices include tax  ·  no refunds on cats", 19f, K.Slate, FontStyles.Bold);
            K.Spacer(footer);
            K.Icon(footer, "Coin", K.Coin, 30f);
            K.Text(footer, "Wallet", "¥12,480", 26f, K.Navy, FontStyles.Bold);
            K.Icon(footer, "Star", K.Star, 30f);
            K.Text(footer, "Stars", "34", 26f, K.Navy, FontStyles.Bold);

            EditorAssetUtil.Wire(panel, ("toggleKey", Key.U), ("closeOnEscape", true), ("popTarget", catalog.rectTransform));
            return panel;
        }

        private static void UpgradeCard(Transform grid, string name, string pitch, string price, string state, Color stateColor, string buy,
            System.Action<Transform> drawIcon)
        {
            Image card = K.Image(grid, "Upgrade_" + name, K.Panel, Color.white);
            K.VLayout(card.gameObject, new RectOffset(24, 24, 21, 27), 9f, TextAnchor.UpperLeft, fitWidth: false, fitHeight: false);
            Image art = K.Image(card.transform, "Art", K.RoundedOutlined, K.Cream);
            K.Size(art.gameObject, -1f, 96f);
            drawIcon?.Invoke(art.transform);
            K.Text(card.transform, "Name", name, 25f, K.Navy, FontStyles.Bold);
            K.Text(card.transform, "Pitch", pitch, 19f, K.Slate, FontStyles.Normal, TextAlignmentOptions.Left, wrap: true);
            RectTransform bottom = K.Row(card.transform, "Bottom", 9f);
            K.Text(bottom, "Price", price, 27f, K.Navy, FontStyles.Bold);
            K.Spacer(bottom);
            if (buy != null)
            {
                K.Keycap(bottom, "Buy", buy, null, K.KeyStyle.Teal, new Vector2(108f, 60f), 22f);
            }
            else
            {
                K.Chip(bottom, state, stateColor, 17f, stateColor == K.Disabled ? K.DisabledInk : K.NavyDeep);
            }
        }

        // Little flat illustrations for the catalog, built from kit shapes (navy outlines, palette fills).

        private static void DrawElectricScrewdriver(Transform art)
        {
            K.Glyph(art, K.Rounded, K.Slate, new Vector2(118f, 0f), new Vector2(96f, 12f));                 // shaft
            K.Glyph(art, K.Rounded, K.Navy, new Vector2(170f, 0f), new Vector2(18f, 7f));                    // bit
            K.Glyph(art, K.RoundedOutlined, K.Brass, new Vector2(62f, 0f), new Vector2(30f, 48f));           // collar
            K.Glyph(art, K.RoundedOutlined, K.Teal, new Vector2(-48f, 0f), new Vector2(190f, 66f), 0f, 0.8f); // grip
            K.Glyph(art, K.RoundedOutlined, K.Orange, new Vector2(-30f, 22f), new Vector2(42f, 18f));        // trigger
            K.Glyph(art, K.Rounded, new Color(1f, 1f, 1f, 0.35f), new Vector2(-70f, -14f), new Vector2(110f, 8f)); // shine
        }

        private static void DrawMagnifier(Transform art)
        {
            K.Glyph(art, K.RoundedOutlined, K.Walnut, new Vector2(26f, -22f), new Vector2(18f, 50f), 40f);  // handle
            K.Glyph(art, K.CircleOutlined, K.Brass, new Vector2(-12f, 8f), new Vector2(66f, 66f));           // ring
            K.Glyph(art, K.Circle, EditorAssetUtil.Hex("#F6D4CC"), new Vector2(-12f, 8f), new Vector2(46f, 46f)); // lens, coral glow
            K.Glyph(art, K.Circle, K.Coral, new Vector2(-12f, 8f), new Vector2(14f, 14f));
        }

        private static void DrawTester(Transform art)
        {
            K.Glyph(art, K.Rounded, K.Slate, new Vector2(78f, 0f), new Vector2(44f, 7f));                   // probe
            K.Glyph(art, K.RoundedOutlined, K.Butter, new Vector2(0f, 0f), new Vector2(120f, 48f));          // body
            K.Glyph(art, K.Rounded, K.LcdBack, new Vector2(-22f, 0f), new Vector2(46f, 24f));                // screen
            K.Glyph(art, K.Rounded, K.LcdTeal, new Vector2(-26f, 4f), new Vector2(28f, 5f));
            K.Glyph(art, K.Rounded, K.LcdTeal, new Vector2(-30f, -5f), new Vector2(20f, 5f));
            K.Glyph(art, K.Circle, K.Navy, new Vector2(30f, 0f), new Vector2(14f, 14f));                     // button
        }

        private static void DrawCleaner(Transform art)
        {
            K.Glyph(art, K.RoundedOutlined, K.Mist, new Vector2(0f, -12f), new Vector2(130f, 54f));          // tub
            K.Glyph(art, K.Rounded, new Color(K.LcdTeal.r, K.LcdTeal.g, K.LcdTeal.b, 0.8f), new Vector2(0f, -4f), new Vector2(110f, 14f)); // water
            for (int i = -1; i <= 1; i++)
            {
                K.Glyph(art, K.Rounded, K.Teal, new Vector2(i * 32f, 26f), new Vector2(22f, 5f));            // waves
            }
        }

        private static void DrawWorkbench(Transform art)
        {
            K.Glyph(art, K.Rounded, K.Walnut, new Vector2(-58f, -16f), new Vector2(10f, 44f));               // legs
            K.Glyph(art, K.Rounded, K.Walnut, new Vector2(58f, -16f), new Vector2(10f, 44f));
            K.Glyph(art, K.RoundedOutlined, K.KraftLight, new Vector2(0f, 10f), new Vector2(150f, 20f));     // top
            K.Glyph(art, K.Rounded, K.Navy, new Vector2(-8f, 24f), new Vector2(56f, 7f));                    // mat
            K.Glyph(art, K.Rounded, K.Brass, new Vector2(46f, 34f), new Vector2(8f, 22f), -20f);             // lamp
        }

        private static void DrawRadio(Transform art)
        {
            K.Glyph(art, K.Rounded, K.Navy, new Vector2(38f, 30f), new Vector2(5f, 44f), -28f);              // antenna
            K.Glyph(art, K.RoundedOutlined, K.Coral, new Vector2(0f, -6f), new Vector2(118f, 62f));          // body
            K.Glyph(art, K.CircleOutlined, K.Paper, new Vector2(-24f, -6f), new Vector2(40f, 40f));          // speaker
            K.Glyph(art, K.Circle, K.Butter, new Vector2(30f, 2f), new Vector2(18f, 18f));                   // dial
            K.Glyph(art, K.Rounded, K.Navy, new Vector2(30f, -18f), new Vector2(26f, 5f));
        }

        private static void DrawCushionBench(Transform art)
        {
            K.Glyph(art, K.Rounded, K.Walnut, new Vector2(-62f, -22f), new Vector2(10f, 30f));               // legs
            K.Glyph(art, K.Rounded, K.Walnut, new Vector2(62f, -22f), new Vector2(10f, 30f));
            K.Glyph(art, K.RoundedOutlined, K.Walnut, new Vector2(0f, -6f), new Vector2(156f, 14f));         // frame
            K.Glyph(art, K.RoundedOutlined, K.Teal, new Vector2(0f, 12f), new Vector2(146f, 30f));           // cushion
        }

        // ---- pause (UI11) ----------------------------------------------------------------------------

        private static UiPanel BuildPause(Transform canvas, UiActions actions)
        {
            UiPanel panel = Modal(canvas, "Pause", 0.6f, out RectTransform root);

            RectTransform sign = K.Card(root, "Sign", K.PanelHard, Color.white, new RectOffset(51, 51, 36, 39), 9f);
            K.Place(sign, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-330f, -126f), Vector2.zero);
            sign.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
            K.Text(sign, "Kicker", "PAUSED", 21f, K.Slate, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Text(sign, "Title", "BE RIGHT BACK", 63f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Center);
            K.Text(sign, "Hours", "Shop closes at 19:00  ·  autosaved 16:30", 20f, K.Slate, FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform keys = K.Rect(root, "Keys");
            K.VLayout(keys.gameObject, new RectOffset(), 18f, TextAnchor.UpperLeft, fitWidth: true, fitHeight: true);
            K.Place(keys, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, -90f), Vector2.zero);
            K.Key resume = K.Keycap(keys, "Resume", "Resume", "Esc", K.KeyStyle.Teal, new Vector2(510f, 90f), 30f, TextAlignmentOptions.Left);
            K.Keycap(keys, "Settings", "Settings", null, K.KeyStyle.Disabled, new Vector2(510f, 78f), 26f, TextAlignmentOptions.Left);
            K.Keycap(keys, "Controls", "Controls", null, K.KeyStyle.Disabled, new Vector2(510f, 78f), 26f, TextAlignmentOptions.Left);
            K.Key quit = K.Keycap(keys, "QuitToMenu", "Save & quit to menu", null, K.KeyStyle.Paper, new Vector2(510f, 78f), 26f, TextAlignmentOptions.Left);
            K.OnClick(resume.Button, panel.Hide);
            K.OnClick(quit.Button, actions.LoadScene, PhoneFixerPaths.MenuSceneName);

            RectTransform today = K.Card(root, "Today", K.PanelHard, Color.white, new RectOffset(36, 36, 30, 33), 12f, 450f);
            K.Place(today, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -90f), new Vector2(450f, 0f));
            RectTransform head = K.Row(today, "Head", 12f);
            K.Text(head, "Title", "Today so far", 30f, K.Navy, FontStyles.Bold);
            K.Spacer(head);
            K.Chip(head, "DAY 3", K.Butter, 18f);
            K.Divider(today, vertical: false, 378f);
            ValueRow(today, "Jobs done", "3", 23f, K.Navy);
            ValueRow(today, "Earned", "¥9,800", 23f, K.Navy);
            ValueRow(today, "Parts used", "4", 23f, K.Navy);
            ValueRow(today, "Reputation", "+5", 23f, K.Navy);
            ValueRow(today, "Cat petted", "7×", 23f, K.Navy);

            Image tip = K.Image(root, "Tip", K.PillSolid, K.Paper);
            K.HLayout(tip.gameObject, new RectOffset(27, 27, 10, 12), 0f, TextAnchor.MiddleCenter);
            K.Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), Vector2.zero);
            K.Text(tip.transform, "Text", "Tip: hidden faults glow coral under the magnifier lamp.", 21f, K.Navy, FontStyles.Bold);

            EditorAssetUtil.Wire(panel, ("toggleKey", Key.Escape), ("closeOnEscape", true), ("pauseTime", true), ("popTarget", sign));
            return panel;
        }

        // ---- repair HUD (UI04-UI06) + receipt (UI09/UI10) -----------------------------------------------

        public static void BuildBench(SelectionController selection, GameInput input, ToolBelt toolBelt, RepairInteractor repair,
            RepairFeedback feedback, UiActions actions)
        {
            CreateEventSystem();
            Canvas hud = K.CreateOverlayCanvas("RepairHUD", 0);
            Canvas modal = K.CreateOverlayCanvas("RepairScreens", 10);
            hud.gameObject.AddComponent<HudFader>();
            Transform root = hud.transform;
            UiPanel receipt = BuildReceipt(modal.transform, actions);

            BuildJobTicket(root);
            BuildObjective(root);
            BuildDiagnosis(root);
            BuildSelectionCard(root, selection);
            BuildToolBar(root, toolBelt);
            BuildBenchLegend(root);
            BuildBenchKeys(root, receipt, actions);
            BuildToast(root, feedback);
            BuildProgressRing(root, repair, input);
            BuildTooltip(root, selection, input); // last = on top
        }

        private static void BuildJobTicket(Transform root)
        {
            RectTransform ticket = K.Card(root, "JobTicket", K.Panel, Color.white, new RectOffset(24, 27, 18, 27), 12f, 570f);
            K.Corner(ticket, new Vector2(0f, 1f), new Vector2(30f, -27f));
            RectTransform head = K.Row(ticket, "Head", 15f);
            K.PortraitStamp(head, "Ren", 69f, EditorAssetUtil.Hex("#D9D2EA"));
            RectTransform names = K.Column(head, "Names", 0f);
            K.Text(names, "Who", "Ren  ·  Mochi Dango", 25f, K.Navy, FontStyles.Bold);
            K.Text(names, "Ticket", "Ticket 0042  ·  won't charge", 19f, K.Slate);
            K.Spacer(head);
            RectTransform numbers = K.Column(head, "Numbers", 0f, TextAnchor.UpperRight);
            K.Text(numbers, "Timer", "38:20", 27f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Right);
            K.Text(numbers, "Pay", "¥5,600", 23f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Right);
            RectTransform chips = K.Row(ticket, "Faults", 12f);
            K.Chip(chips, "Port", K.Coral);
            K.Chip(chips, "? 1 hidden", K.Disabled, 19f, K.DisabledInk);
        }

        private static void BuildObjective(Transform root)
        {
            Image pill = K.Image(root, "Objective", K.Panel, Color.white);
            K.HLayout(pill.gameObject, new RectOffset(24, 30, 15, 21), 15f, TextAnchor.MiddleLeft);
            K.Corner(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -27f));
            K.Icon(pill.transform, "Step", K.CircleOutlined, 27f, K.Orange);
            K.Text(pill.transform, "Text", "Open it up: remove the 4 back-cover screws", 24f, K.Navy, FontStyles.Bold);
        }

        private static void BuildDiagnosis(Transform root)
        {
            RectTransform lcd = K.Card(root, "Diagnosis", K.LcdPanel, Color.white, new RectOffset(24, 24, 18, 27), 9f, 405f);
            K.Corner(lcd, new Vector2(1f, 1f), new Vector2(-30f, -27f));
            K.Text(lcd, "Title", "DIAGNOSIS", 18f, K.LcdMuted, FontStyles.Bold);
            ValueRow(lcd, "BATTERY", "88% OK", 21f, K.LcdTeal);
            ValueRow(lcd, "PORT", "× NO CHARGE", 21f, K.LcdCoral);
            ValueRow(lcd, "SCREEN", "OK", 21f, K.LcdTeal);
            ValueRow(lcd, "BOARD", "? OPEN IT", 21f, K.LcdMuted);
        }

        private static void BuildSelectionCard(Transform root, SelectionController selection)
        {
            RectTransform card = K.Card(root, "SelectionCard", K.Panel, Color.white, new RectOffset(30, 30, 24, 30), 8f, 465f);
            K.Corner(card, new Vector2(1f, 1f), new Vector2(-30f, -300f));
            var group = card.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            TextMeshProUGUI subtitle = K.Text(card, "Subtitle", "Battery  ·  Mochi Dango", 19f, EditorAssetUtil.Hex("#3F7F79"), FontStyles.Bold);
            TextMeshProUGUI title = K.Text(card, "Title", "Battery Pack", 36f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Left, wrap: true);
            TextMeshProUGUI status = K.Text(card, "Status", "Installed", 21f, EditorAssetUtil.Hex("#3F7F79"), FontStyles.Bold);
            K.Divider(card, vertical: false, 400f);
            TextMeshProUGUI body = K.Text(card, "Body", "Description", 21f, K.Slate, FontStyles.Normal, TextAlignmentOptions.Left, wrap: true);
            K.Text(card, "Hint", "F focus  ·  Esc deselect", 17f, K.DisabledInk, FontStyles.Italic);
            var panel = card.gameObject.AddComponent<SelectionInfoPanel>();
            EditorAssetUtil.Wire(panel,
                ("selection", selection),
                ("canvasGroup", group),
                ("card", card),
                ("titleText", title),
                ("subtitleText", subtitle),
                ("bodyText", body),
                ("statusText", status));
        }

        private static void BuildToolBar(Transform root, ToolBelt toolBelt)
        {
            RectTransform column = K.Rect(root, "Tools");
            K.VLayout(column.gameObject, new RectOffset(), 12f, TextAnchor.LowerCenter, fitWidth: true, fitHeight: true);
            K.Corner(column, new Vector2(0.5f, 0f), new Vector2(0f, 30f));
            column.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;

            Image hint = K.Image(column, "ToolHint", K.PillSolid, K.Navy);
            K.HLayout(hint.gameObject, new RectOffset(21, 21, 6, 8), 0f, TextAnchor.MiddleCenter);
            K.Text(hint.transform, "Text", "Hold left to use the tool  ·  1-4 switch tools", 19f, K.Paper, FontStyles.Bold);

            RectTransform bar = K.Rect(column, "ToolBar");
            K.HLayout(bar.gameObject, new RectOffset(), 15f, TextAnchor.LowerCenter);

            // Template keycap; ToolBarView clones it once per tool at runtime.
            Image keyBase = K.Image(bar, "ToolButton_Template", K.KeyBase, Color.white, raycast: true);
            K.Size(keyBase.gameObject, 228f, 87f);
            Image face = K.Image(keyBase.transform, "Face", K.KeyPaper, Color.white);
            K.Stretch(face.rectTransform, 0f, 0f, 0f, 6f);
            K.HLayout(face.gameObject, new RectOffset(18, 18, 6, 8), 12f, TextAnchor.MiddleCenter, fit: false);
            TextMeshProUGUI hotkey = K.Text(face.transform, "Hotkey", "1", 27f, K.Navy, FontStyles.Bold);
            TextMeshProUGUI name = K.Text(face.transform, "Name", "Tool", 24f, K.Navy, FontStyles.Bold);
            var button = keyBase.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var press = keyBase.gameObject.AddComponent<KeycapPress>();
            EditorAssetUtil.Wire(press, ("content", face.rectTransform));
            var view = keyBase.gameObject.AddComponent<ToolButtonView>();
            EditorAssetUtil.Wire(view, ("button", button), ("background", face), ("hotkeyLabel", hotkey), ("nameLabel", name));

            var barView = bar.gameObject.AddComponent<ToolBarView>();
            EditorAssetUtil.Wire(barView,
                ("toolBelt", toolBelt),
                ("template", view),
                ("container", bar),
                ("normalBackground", Color.white),
                ("selectedBackground", K.Teal),
                ("normalText", K.Navy),
                ("selectedText", K.NavyDeep),
                ("selectedScale", 1.06f));
        }

        private static void BuildBenchLegend(Transform root)
        {
            Image legend = K.Image(root, "Controls", K.RoundedOutlined, new Color(1f, 0.99f, 0.97f, 0.92f));
            K.VLayout(legend.gameObject, new RectOffset(21, 24, 18, 18), 9f, TextAnchor.UpperLeft, fitWidth: true, fitHeight: true);
            K.Corner(legend.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 30f));
            K.KeyHint(legend.transform, "Right drag", "turn the phone", K.Navy);
            K.KeyHint(legend.transform, "Wheel", "zoom to cursor", K.Navy);
            K.KeyHint(legend.transform, "Space", "flip", K.Navy);
            K.KeyHint(legend.transform, "F", "frame selection", K.Navy);
            K.KeyHint(legend.transform, "R", "reset view", K.Navy);
        }

        private static void BuildBenchKeys(Transform root, UiPanel receipt, UiActions actions)
        {
            RectTransform keys = K.Rect(root, "BenchKeys");
            K.VLayout(keys.gameObject, new RectOffset(), 15f, TextAnchor.LowerRight, fitWidth: true, fitHeight: true);
            K.Corner(keys, new Vector2(1f, 0f), new Vector2(-30f, 30f));
            K.Key test = K.Keycap(keys, "PowerTest", "Power test", "Enter", K.KeyStyle.Teal, new Vector2(330f, 84f), 26f, TextAlignmentOptions.Left);
            K.Key back = K.Keycap(keys, "BackToShop", "Back to shop", "B", K.KeyStyle.Paper, new Vector2(330f, 78f), 24f, TextAlignmentOptions.Left);
            K.OnClick(test.Button, receipt.Show);
            K.OnClick(back.Button, actions.LoadScene, PhoneFixerPaths.ShopSceneName);
            Shortcut(keys.gameObject, Key.Enter, test.Button, null);
        }

        private static void BuildToast(Transform root, RepairFeedback feedback)
        {
            Image pill = K.Image(root, "Toast", K.PillSolid, K.Navy);
            K.HLayout(pill.gameObject, new RectOffset(30, 30, 12, 14), 0f, TextAnchor.MiddleCenter);
            K.Corner(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f));
            var group = pill.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            TextMeshProUGUI label = K.Text(pill.transform, "Message", "Message", 24f, K.Paper, FontStyles.Bold);
            var toast = pill.gameObject.AddComponent<ToastView>();
            EditorAssetUtil.Wire(toast,
                ("feedback", feedback),
                ("canvasGroup", group),
                ("pill", pill.rectTransform),
                ("background", pill),
                ("label", label),
                ("infoColor", new Color(K.Navy.r, K.Navy.g, K.Navy.b, 0.96f)),
                ("warningColor", K.CoralDeep));
        }

        private static void BuildProgressRing(Transform root, RepairInteractor repair, GameInput input)
        {
            RectTransform rect = K.Rect(root, "ActionProgress");
            K.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            Image track = K.Image(rect, "Track", K.Ring, new Color(K.NavyDeep.r, K.NavyDeep.g, K.NavyDeep.b, 0.6f));
            K.Stretch(track.rectTransform);
            Image fill = K.Image(rect, "Fill", K.Ring, K.Orange);
            K.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            var view = rect.gameObject.AddComponent<ActionProgressView>();
            EditorAssetUtil.Wire(view, ("repair", repair), ("input", input), ("canvasGroup", group), ("ring", fill));
        }

        private static void BuildTooltip(Transform root, SelectionController selection, GameInput input)
        {
            Image tip = K.Image(root, "HoverTooltip", K.PillSolid, K.Navy);
            tip.rectTransform.anchorMin = tip.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tip.rectTransform.pivot = new Vector2(0f, 1f);
            K.VLayout(tip.gameObject, new RectOffset(21, 21, 9, 12), 0f, TextAnchor.UpperLeft, fitWidth: true, fitHeight: true);
            var group = tip.gameObject.AddComponent<CanvasGroup>();
            Hide(group);
            TextMeshProUGUI label = K.Text(tip.transform, "Label", "Screw 4", 22f, K.Paper, FontStyles.Bold);
            TextMeshProUGUI hint = K.Text(tip.transform, "Hint", "Hold · Unscrew", 19f, K.Orange, FontStyles.Bold);
            var tooltip = tip.gameObject.AddComponent<HoverTooltip>();
            EditorAssetUtil.Wire(tooltip,
                ("selection", selection),
                ("input", input),
                ("canvasGroup", group),
                ("label", label),
                ("hintLabel", hint),
                ("readyColor", K.Orange),
                ("neutralColor", K.Mist),
                ("blockedColor", K.LcdCoral));
        }

        private static UiPanel BuildReceipt(Transform canvas, UiActions actions)
        {
            UiPanel panel = Modal(canvas, "Receipt", 0.5f, out RectTransform root);
            RectTransform card = K.Card(root, "Card", K.PanelHard, Color.white, new RectOffset(54, 54, 42, 45), 18f, 930f);
            K.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(930f, 0f));

            RectTransform head = K.Row(card, "Head", 18f);
            K.Text(head, "Title", "Fixed!", 78f, K.Navy, FontStyles.Bold);
            K.Spacer(head);
            RectTransform stamp = K.Chip(head, "REPAIRED", K.Leaf, 24f);
            stamp.localRotation = Quaternion.Euler(0f, 0f, -6f);
            K.Text(card, "Ticket", "TICKET 0042  ·  REN  ·  MOCHI DANGO  ·  16:58", 21f, K.Slate, FontStyles.Bold);

            foreach (string done in new[] { "Charging port  ·  cleaned", "Hidden: battery plug reseated", "Power test  ·  passed" })
            {
                RectTransform row = K.Row(card, "Done", 15f);
                K.Icon(row, "Tick", K.CircleOutlined, 27f, K.Leaf);
                K.Text(row, "Text", done, 25f, K.Navy, FontStyles.Bold);
            }

            RectTransform stats = K.Row(card, "Stats", 18f);
            foreach ((string label, string value) in new[] { ("Quality", "94%"), ("Speed", "6:48"), ("Care", "0 scuffs") })
            {
                Image box = K.Image(stats, "Stat_" + label, K.RoundedOutlined, K.Cream);
                K.VLayout(box.gameObject, new RectOffset(24, 24, 15, 18), 0f, TextAnchor.MiddleCenter, fitWidth: false, fitHeight: true);
                K.Size(box.gameObject, 258f);
                K.Text(box.transform, "Label", label, 19f, K.Slate, FontStyles.Bold, TextAlignmentOptions.Center);
                K.Text(box.transform, "Value", value, 39f, K.Navy, FontStyles.Bold, TextAlignmentOptions.Center);
            }

            K.Divider(card, vertical: false, 820f);
            ValueRow(card, "Charging port repair", "¥5,600", 24f, K.Navy);
            ValueRow(card, "Early bonus", "¥500", 24f, K.Navy);
            ValueRow(card, "Total", "¥6,100", 30f, K.Navy);
            K.Text(card, "Note", "Ren is going to be so relieved. Probably asleep, but relieved.", 22f, K.Slate, FontStyles.Italic,
                TextAlignmentOptions.Left, wrap: true);

            RectTransform buttons = K.Row(card, "Buttons", 18f);
            K.Key collect = K.Keycap(buttons, "HandBack", "Hand it back  ·  collect ¥6,100", "Enter", K.KeyStyle.Teal, new Vector2(516f, 90f), 26f);
            K.Keycap(buttons, "Photo", "Photo for the wall", null, K.KeyStyle.Disabled, new Vector2(282f, 90f), 22f);
            K.OnClick(collect.Button, actions.LoadScene, PhoneFixerPaths.ShopSceneName);
            Shortcut(root.gameObject, Key.Enter, collect.Button, panel);

            EditorAssetUtil.Wire(panel, ("closeOnEscape", true), ("popTarget", card));
            return panel;
        }
    }
}

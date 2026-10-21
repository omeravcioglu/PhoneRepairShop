using System.IO;
using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using PhoneRepair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds the bench HUD (uGUI + TMP): header + controls (top-left), selection card (top-right),
    /// toast (top-centre), tool bar (bottom-centre), hover tooltip and progress ring (at the cursor).
    /// </summary>
    internal static class WorkbenchUiBuilder
    {
        public const string RoundedSpritePath = "Assets/_Project/Art/UI/UI_RoundedRect.png";
        public const string RingSpritePath = "Assets/_Project/Art/UI/UI_Ring.png";
        private const int RoundedSpriteSize = 96;
        private const int RoundedSpriteRadius = 36;

        private static readonly Color CardColor = EditorAssetUtil.Hex("#FFF8EF", 0.96f);
        private static readonly Color InkColor = EditorAssetUtil.Hex("#3B3346");
        private static readonly Color AccentColor = EditorAssetUtil.Hex("#D9668A");
        private static readonly Color StatusColor = EditorAssetUtil.Hex("#2E8C7E");
        private static readonly Color MutedColor = EditorAssetUtil.Hex("#6E6479");
        private static readonly Color HintColor = EditorAssetUtil.Hex("#A69CB2");
        private static readonly Color PillColor = EditorAssetUtil.Hex("#2B2433", 0.8f);
        private static readonly Color PillTextColor = EditorAssetUtil.Hex("#FFF8EF");
        private static readonly Color PillAccentColor = EditorAssetUtil.Hex("#F6B9C9");
        private static readonly Color RingColor = EditorAssetUtil.Hex("#F5B65C"); // amber: reads on the mint mat and the pink cover

        private static int UiLayer => LayerMask.NameToLayer("UI");

        public static void Build(SelectionController selection, GameInput input, ToolBelt toolBelt, RepairInteractor repair, RepairFeedback feedback)
        {
            Sprite rounded = EnsureRoundedSprite();
            Sprite ring = EnsureRingSprite();
            CreateEventSystem();
            var root = (RectTransform)CreateCanvas().transform;

            BuildHeader(root, rounded);
            BuildControlsHint(root, rounded);
            BuildSelectionCard(root, rounded, selection);
            BuildToolBar(root, rounded, toolBelt);
            BuildToast(root, rounded, feedback);
            BuildProgressRing(root, ring, repair, input);
            BuildTooltip(root, rounded, selection, input); // last child = drawn on top
        }

        private static void CreateEventSystem()
        {
            // InputSystemUIInputModule assigns its default UI actions at runtime when none are set.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UiLayer
            };
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildHeader(RectTransform root, Sprite rounded)
        {
            RectTransform header = CreatePanel(root, "Header", rounded, PillColor, raycastTarget: false, cornerScale: 1.6f);
            Anchor(header, new Vector2(0f, 1f), new Vector2(32f, -32f));
            AddVerticalLayout(header, new RectOffset(24, 24, 14, 16), 2f, fitWidth: true);
            AddText(header, "Title", "Repair Bench", 28f, PillTextColor, FontStyles.Bold, wrap: false);
            AddText(header, "Subtitle", "Milestone 2  ·  Take the Mochi One apart (and back together)", 18f, PillAccentColor, FontStyles.Normal, wrap: false);
        }

        private static void BuildControlsHint(RectTransform root, Sprite rounded)
        {
            RectTransform hint = CreatePanel(root, "ControlsHint", rounded, PillColor, raycastTarget: false, cornerScale: 1.6f);
            Anchor(hint, new Vector2(0f, 1f), new Vector2(32f, -126f));
            AddVerticalLayout(hint, new RectOffset(20, 20, 10, 12), 3f, fitWidth: true);
            AddText(hint, "Line1", "<b>1-4</b> tools    <b>Hold left</b> use tool    <b>Click</b> inspect    <b>F</b> focus",
                17f, PillTextColor, FontStyles.Normal, wrap: false);
            AddText(hint, "Line2", "<b>Right drag</b> rotate    <b>Space</b> flip    <b>Wheel</b> zoom    <b>R</b> reset",
                17f, PillTextColor, FontStyles.Normal, wrap: false);
            AddText(hint, "Line3", "Screws: hold, and circle the cursor <i>counter-clockwise</i> to go faster",
                15f, PillAccentColor, FontStyles.Normal, wrap: false);
        }

        private static void BuildSelectionCard(RectTransform root, Sprite rounded, SelectionController selection)
        {
            RectTransform card = CreatePanel(root, "SelectionCard", rounded, CardColor, raycastTarget: true, cornerScale: 1f);
            Anchor(card, new Vector2(1f, 1f), new Vector2(-32f, -32f));
            card.sizeDelta = new Vector2(440f, 200f);
            AddVerticalLayout(card, new RectOffset(28, 28, 22, 24), 6f, fitWidth: false);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.17f, 0.12f, 0.2f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -6f);

            TMP_Text subtitle = AddText(card, "Subtitle", "Category  ·  Model", 19f, AccentColor, FontStyles.Bold);
            TMP_Text title = AddText(card, "Title", "Part name", 34f, InkColor, FontStyles.Bold);
            TMP_Text status = AddText(card, "Status", "Installed", 19f, StatusColor, FontStyles.Bold);
            AddDivider(card);
            TMP_Text body = AddText(card, "Body", "Description", 21f, MutedColor, FontStyles.Normal);
            AddText(card, "Hint", "Double-click or F to focus  ·  Esc to deselect", 16f, HintColor, FontStyles.Italic);

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

        private static void BuildToolBar(RectTransform root, Sprite rounded, ToolBelt toolBelt)
        {
            RectTransform bar = CreateRect(root, "ToolBar");
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0f, 28f));
            bar.pivot = new Vector2(0.5f, 0f);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerCenter;
            var fitter = bar.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Template button (inactive); ToolBarView clones it per tool at runtime.
            RectTransform button = CreatePanel(bar, "ToolButton_Template", rounded, PillColor, raycastTarget: true, cornerScale: 1.4f);
            var buttonLayout = button.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.padding = new RectOffset(16, 22, 12, 12);
            buttonLayout.spacing = 12f;
            buttonLayout.childControlWidth = buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = buttonLayout.childForceExpandHeight = false;
            buttonLayout.childAlignment = TextAnchor.MiddleLeft;
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 150f;
            element.minHeight = 58f;
            var uiButton = button.gameObject.AddComponent<Button>();
            uiButton.transition = Selectable.Transition.None;
            TMP_Text hotkey = AddText(button, "Hotkey", "1", 22f, PillTextColor, FontStyles.Bold, wrap: false);
            TMP_Text label = AddText(button, "Name", "Tool", 20f, PillTextColor, FontStyles.Normal, wrap: false);
            var view = button.gameObject.AddComponent<ToolButtonView>();
            EditorAssetUtil.Wire(view,
                ("button", uiButton),
                ("background", button.GetComponent<Image>()),
                ("hotkeyLabel", hotkey),
                ("nameLabel", label));

            var barView = bar.gameObject.AddComponent<ToolBarView>();
            EditorAssetUtil.Wire(barView, ("toolBelt", toolBelt), ("template", view), ("container", bar));
        }

        private static void BuildToast(RectTransform root, Sprite rounded, RepairFeedback feedback)
        {
            RectTransform pill = CreatePanel(root, "Toast", rounded, PillColor, raycastTarget: false, cornerScale: 1.6f);
            Anchor(pill, new Vector2(0.5f, 1f), new Vector2(0f, -36f));
            pill.pivot = new Vector2(0.5f, 1f);
            AddVerticalLayout(pill, new RectOffset(26, 26, 12, 14), 0f, fitWidth: true);
            var group = pill.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            TMP_Text label = AddText(pill, "Message", "Message", 22f, PillTextColor, FontStyles.Bold, wrap: false);

            var toast = pill.gameObject.AddComponent<ToastView>();
            EditorAssetUtil.Wire(toast,
                ("feedback", feedback),
                ("canvasGroup", group),
                ("pill", pill),
                ("background", pill.GetComponent<Image>()),
                ("label", label));
        }

        private static void BuildProgressRing(RectTransform root, Sprite ringSprite, RepairInteractor repair, GameInput input)
        {
            RectTransform rect = CreateRect(root, "ActionProgress");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(58f, 58f);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var track = CreateRect(rect, "Track").gameObject.AddComponent<Image>();
            Stretch((RectTransform)track.transform);
            track.sprite = ringSprite;
            track.color = new Color(0.17f, 0.14f, 0.2f, 0.62f);
            track.raycastTarget = false;

            var fill = CreateRect(rect, "Fill").gameObject.AddComponent<Image>();
            Stretch((RectTransform)fill.transform);
            fill.sprite = ringSprite;
            fill.color = RingColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            var view = rect.gameObject.AddComponent<ActionProgressView>();
            EditorAssetUtil.Wire(view, ("repair", repair), ("input", input), ("canvasGroup", group), ("ring", fill));
        }

        private static void BuildTooltip(RectTransform root, Sprite rounded, SelectionController selection, GameInput input)
        {
            RectTransform tip = CreatePanel(root, "HoverTooltip", rounded, PillColor, raycastTarget: false, cornerScale: 2f);
            tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0f, 1f);
            tip.anchoredPosition = Vector2.zero;
            AddVerticalLayout(tip, new RectOffset(14, 14, 7, 9), 1f, fitWidth: true);

            var group = tip.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_Text label = AddText(tip, "Label", "Part", 20f, PillTextColor, FontStyles.Bold, wrap: false);
            TMP_Text hint = AddText(tip, "Hint", "Hold · Action", 16f, PillAccentColor, FontStyles.Normal, wrap: false);
            var tooltip = tip.gameObject.AddComponent<HoverTooltip>();
            EditorAssetUtil.Wire(tooltip,
                ("selection", selection),
                ("input", input),
                ("canvasGroup", group),
                ("label", label),
                ("hintLabel", hint));
        }

        // --- helpers ---------------------------------------------------------------------------

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget, float cornerScale)
        {
            RectTransform rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale; // >1 = smaller corner radius
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 corner, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AddVerticalLayout(RectTransform rect, RectOffset padding, float spacing, bool fitWidth)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = fitWidth ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text, float size, Color color, FontStyles style, bool wrap = true)
        {
            RectTransform rect = CreateRect(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.richText = true;
            label.raycastTarget = false;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return label;
        }

        private static void AddDivider(Transform parent)
        {
            RectTransform rect = CreateRect(parent, "Divider");
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.3f);
            image.raycastTarget = false;
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 3f;
        }

        /// <summary>White rounded-rectangle sprite (9-sliced) shared by all panels.</summary>
        private static Sprite EnsureRoundedSprite()
        {
            return EnsureGeneratedSprite(RoundedSpritePath, RoundedSpriteSize, Vector4.one * (RoundedSpriteRadius + 2), (p, size) =>
            {
                const float radius = RoundedSpriteRadius;
                var nearest = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                return Mathf.Clamp01(radius - Vector2.Distance(p, nearest) + 0.5f);
            });
        }

        /// <summary>White ring for the radial progress indicator.</summary>
        private static Sprite EnsureRingSprite()
        {
            return EnsureGeneratedSprite(RingSpritePath, 128, Vector4.zero, (p, size) =>
            {
                float d = Vector2.Distance(p, new Vector2(size * 0.5f, size * 0.5f));
                float outer = Mathf.Clamp01(61f - d + 0.5f);
                float inner = Mathf.Clamp01(d - 45f + 0.5f);
                return Mathf.Min(outer, inner);
            });
        }

        private static Sprite EnsureGeneratedSprite(string path, int size, Vector4 border, System.Func<Vector2, int, float> alpha)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(path));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = alpha(new Vector2(x + 0.5f, y + 0.5f), size);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}

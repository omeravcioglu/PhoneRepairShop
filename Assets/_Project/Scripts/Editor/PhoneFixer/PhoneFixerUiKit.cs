using System;
using System.Collections.Generic;
using System.IO;
using PhoneRepair.PhoneFixer;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// The "Paper &amp; Keys" UI kit from the art direction canvas (UI kit board): generated 9-slice
    /// sprites (paper cards with a hard navy drop, two-layer keycaps, pills, LCD panels, a progress ring,
    /// small icons) and the uGUI/TMP builders every Phone Fixer screen uses. Sizes are for the
    /// 1920x1080 canvas reference (the 1280x720 mockups x 1.5). Sprites are generated once, when missing.
    /// </summary>
    internal static class PhoneFixerUiKit
    {
        public static readonly Color Navy = EditorAssetUtil.Hex("#283747");
        public static readonly Color NavyDeep = EditorAssetUtil.Hex("#1C2733");
        public static readonly Color Slate = EditorAssetUtil.Hex("#4E5B69");
        public static readonly Color Mist = EditorAssetUtil.Hex("#C9D3DC");
        public static readonly Color Paper = EditorAssetUtil.Hex("#FFFDF8");
        public static readonly Color Cream = EditorAssetUtil.Hex("#F5E8D3");
        public static readonly Color Kraft = EditorAssetUtil.Hex("#E6CBA0");
        public static readonly Color KraftLight = EditorAssetUtil.Hex("#EFD7AE");
        public static readonly Color Line = EditorAssetUtil.Hex("#C9B9A2");
        public static readonly Color Butter = EditorAssetUtil.Hex("#EFD9A0");
        public static readonly Color Peach = EditorAssetUtil.Hex("#F7C899");
        public static readonly Color Brass = EditorAssetUtil.Hex("#C9A25E");
        public static readonly Color Walnut = EditorAssetUtil.Hex("#8A6244");
        public static readonly Color WalnutDark = EditorAssetUtil.Hex("#6E4B36");
        public static readonly Color Teal = EditorAssetUtil.Hex("#6FA9A3");
        public static readonly Color Orange = EditorAssetUtil.Hex("#F2A65A");
        public static readonly Color Coral = EditorAssetUtil.Hex("#DE8175");
        public static readonly Color CoralDeep = EditorAssetUtil.Hex("#C4605A");
        public static readonly Color Leaf = EditorAssetUtil.Hex("#8CCB6E");
        public static readonly Color LcdBack = EditorAssetUtil.Hex("#1E2E3B");
        public static readonly Color LcdTeal = EditorAssetUtil.Hex("#9DF0E4");
        public static readonly Color LcdCoral = EditorAssetUtil.Hex("#F0A99E");
        public static readonly Color LcdMuted = EditorAssetUtil.Hex("#9FB0BF");
        public static readonly Color Disabled = EditorAssetUtil.Hex("#EFE6D6");
        public static readonly Color DisabledLine = EditorAssetUtil.Hex("#8C97A3");
        public static readonly Color DisabledInk = EditorAssetUtil.Hex("#6B7682");

        private static readonly Color DropSoft = new(0.157f, 0.216f, 0.278f, 0.25f);

        public static int UiLayer => LayerMask.NameToLayer("UI");

        public enum KeyStyle
        {
            Paper,
            Teal,
            Orange,
            Coral,
            Disabled,
        }

        // ---- sprites -----------------------------------------------------------------------------

        public static Sprite Panel => Shape("UI_PF_Panel", 96, 104, 27f, 3f, 8f, Paper, Navy, DropSoft);
        public static Sprite PanelHard => Shape("UI_PF_PanelHard", 96, 105, 27f, 4f, 9f, Paper, Navy, NavyDeep);
        public static Sprite PanelTealDrop => Shape("UI_PF_PanelTealDrop", 96, 102, 21f, 3f, 6f, Paper, Navy, Teal);
        public static Sprite PanelWhite => Shape("UI_PF_PanelWhite", 96, 96, 21f, 3f, 0f, Color.white, Navy, Color.clear);
        public static Sprite PanelWalnut => Shape("UI_PF_PanelWalnut", 96, 108, 36f, 4f, 12f, Walnut, Navy, NavyDeep);
        public static Sprite LcdPanel => Shape("UI_PF_Lcd", 96, 102, 21f, 3f, 6f, LcdBack, Navy, DropSoft);
        public static Sprite KeyBase => Shape("UI_PF_KeyBase", 96, 96, 21f, 0f, 0f, Navy, Navy, Color.clear);
        public static Sprite KeyPaper => Shape("UI_PF_KeyPaper", 96, 96, 21f, 3f, 0f, Paper, Navy, Color.clear);
        public static Sprite KeyTeal => Shape("UI_PF_KeyTeal", 96, 96, 21f, 3f, 0f, Teal, Navy, Color.clear);
        public static Sprite KeyOrange => Shape("UI_PF_KeyOrange", 96, 96, 21f, 3f, 0f, Orange, Navy, Color.clear);
        public static Sprite KeyCoral => Shape("UI_PF_KeyCoral", 96, 96, 21f, 3f, 0f, Coral, Navy, Color.clear);
        public static Sprite KeyDisabled => Shape("UI_PF_KeyDisabled", 96, 96, 21f, 3f, 0f, Disabled, DisabledLine, Color.clear);
        public static Sprite Pill => Shape("UI_PF_Pill", 96, 48, 24f, 3f, 0f, Color.white, Navy, Color.clear);
        public static Sprite PillSolid => Shape("UI_PF_PillSolid", 96, 48, 24f, 0f, 0f, Color.white, Color.white, Color.clear);
        public static Sprite Rounded => Shape("UI_PF_Rounded", 64, 64, 12f, 0f, 0f, Color.white, Color.white, Color.clear);
        public static Sprite RoundedOutlined => Shape("UI_PF_RoundedOutlined", 64, 64, 18f, 3f, 0f, Color.white, Navy, Color.clear);
        public static Sprite Circle => Shape("UI_PF_Circle", 64, 64, 32f, 0f, 0f, Color.white, Color.white, Color.clear);
        public static Sprite CircleOutlined => Shape("UI_PF_CircleOutlined", 64, 64, 32f, 4f, 0f, Color.white, Navy, Color.clear);
        public static Sprite Ring => RingSprite("UI_PF_Ring", 128, 61f, 46f, null);
        public static Sprite PortraitRing => RingSprite("UI_PF_PortraitRing", 256, 127f, 119f, Navy);
        public static Sprite Star => StarSprite("UI_PF_Star", 64);
        public static Sprite Coin => Disc("UI_PF_Coin", 64, Brass, Navy, 4f, true);
        public static Sprite Sun => Disc("UI_PF_Sun", 64, Butter, Navy, 4f, false);

        /// <summary>Generates every sprite up front (the first access of a property also generates it).</summary>
        public static void EnsureSprites()
        {
            Sprite[] all =
            {
                Panel, PanelHard, PanelTealDrop, PanelWhite, PanelWalnut, LcdPanel, KeyBase, KeyPaper, KeyTeal, KeyOrange, KeyCoral,
                KeyDisabled, Pill, PillSolid, Rounded, RoundedOutlined, Circle, CircleOutlined, Ring, PortraitRing, Star, Coin, Sun,
            };
            foreach (Sprite s in all)
            {
                if (s == null)
                {
                    throw new InvalidOperationException("A Phone Fixer UI sprite failed to generate.");
                }
            }
        }

        /// <summary>Dialogue portrait rendered by build_pf_customers.py, imported as a sprite.</summary>
        public static Sprite Portrait(string customer)
        {
            string path = PhoneFixerPaths.Portrait(customer);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[PhoneFixer] Portrait missing: {path} (run build_pf_customers.py).");
                return null;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- basic builders ----------------------------------------------------------------------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Left, bool wrap = false)
        {
            RectTransform rect = Rect(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = align;
            label.richText = true;
            label.raycastTarget = false;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.lineSpacing = wrap ? 8f : 0f;
            return label;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Anchors and pivots to the same corner/edge point (e.g. (0,1) = top-left) at an offset.</summary>
        public static void Corner(RectTransform rect, Vector2 corner, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, RectOffset padding, float spacing, TextAnchor align = TextAnchor.MiddleLeft, bool fit = true)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (fit)
            {
                var fitter = go.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return layout;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, RectOffset padding, float spacing, TextAnchor align = TextAnchor.UpperLeft, bool fitWidth = true, bool fitHeight = true)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (fitWidth || fitHeight)
            {
                var fitter = go.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = fitWidth ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = fitHeight ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            }

            return layout;
        }

        public static LayoutElement Size(GameObject go, float width = -1f, float height = -1f)
        {
            var element = EditorAssetUtil.GetOrAdd<LayoutElement>(go);
            if (width >= 0f)
            {
                element.minWidth = element.preferredWidth = width;
            }

            if (height >= 0f)
            {
                element.minHeight = element.preferredHeight = height;
            }

            return element;
        }

        /// <summary>Flexible empty space inside a layout group.</summary>
        public static void Spacer(Transform parent, float flexible = 1f)
        {
            RectTransform rect = Rect(parent, "Spacer");
            rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = flexible;
        }

        public static Image Divider(Transform parent, bool vertical, float length, float thickness = 3f, Color? color = null)
        {
            Image line = Image(parent, "Divider", Rounded, color ?? Line);
            Size(line.gameObject, vertical ? thickness : length, vertical ? length : thickness);
            line.rectTransform.sizeDelta = vertical ? new Vector2(thickness, length) : new Vector2(length, thickness);
            return line;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, float size, Color? color = null)
        {
            Image icon = Image(parent, name, sprite, color ?? Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            Size(icon.gameObject, size, size);
            return icon;
        }

        /// <summary>A paper card (or any sliced sprite) at a fixed width that grows with its content.</summary>
        public static RectTransform Card(Transform parent, string name, Sprite sprite, Color tint, RectOffset padding, float spacing, float width = -1f)
        {
            Image card = Image(parent, name, sprite, tint, raycast: true);
            VLayout(card.gameObject, padding, spacing, TextAnchor.UpperLeft, fitWidth: width < 0f, fitHeight: true);
            if (width >= 0f)
            {
                card.rectTransform.sizeDelta = new Vector2(width, card.rectTransform.sizeDelta.y);
                Size(card.gameObject, width);
            }

            return card.rectTransform;
        }

        public static RectTransform Row(Transform parent, string name, float spacing, TextAnchor align = TextAnchor.MiddleLeft, RectOffset padding = null)
        {
            RectTransform row = Rect(parent, name);
            HLayout(row.gameObject, padding ?? new RectOffset(), spacing, align, fit: false);
            return row;
        }

        public static RectTransform Column(Transform parent, string name, float spacing, TextAnchor align = TextAnchor.UpperLeft, RectOffset padding = null)
        {
            RectTransform column = Rect(parent, name);
            VLayout(column.gameObject, padding ?? new RectOffset(), spacing, align, fitWidth: false, fitHeight: false);
            return column;
        }

        // ---- composite builders -----------------------------------------------------------------

        public readonly struct Key
        {
            public readonly Button Button;
            public readonly Image Face;
            public readonly TextMeshProUGUI Label;

            public Key(Button button, Image face, TextMeshProUGUI label)
            {
                Button = button;
                Face = face;
                Label = label;
            }
        }

        public static Sprite KeyFace(KeyStyle style) => style switch
        {
            KeyStyle.Teal => KeyTeal,
            KeyStyle.Orange => KeyOrange,
            KeyStyle.Coral => KeyCoral,
            KeyStyle.Disabled => KeyDisabled,
            _ => KeyPaper,
        };

        public static Color KeyInk(KeyStyle style) => style switch
        {
            KeyStyle.Paper => Navy,
            KeyStyle.Disabled => DisabledInk,
            _ => NavyDeep,
        };

        /// <summary>
        /// Two-layer keycap: a navy base and a face that sinks onto it when pressed (KeycapPress). The
        /// hotkey badge rides on the face. Disabled keys are flat and not interactable.
        /// </summary>
        public static Key Keycap(Transform parent, string name, string label, string hotkey, KeyStyle style, Vector2 size, float fontSize = 25f,
            TextAlignmentOptions align = TextAlignmentOptions.Center, string leadingKey = null)
        {
            bool disabled = style == KeyStyle.Disabled;
            Image root = Image(parent, name, disabled ? null : KeyBase, disabled ? Color.clear : Color.white, raycast: true);
            root.rectTransform.sizeDelta = size;
            Size(root.gameObject, size.x, size.y);

            const float drop = 6f;
            Image face = Image(root.transform, "Face", KeyFace(style), Color.white);
            Stretch(face.rectTransform, 0f, 0f, 0f, disabled ? 0f : drop);

            float left = align == TextAlignmentOptions.Center ? 12f : 24f;
            if (!string.IsNullOrEmpty(leadingKey))
            {
                // Inline navy key at the left edge ("1  Leave it with me").
                Image key = Image(face.transform, "LeadingKey", Rounded, disabled ? DisabledLine : Navy);
                Place(key.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(39f, 39f));
                TextMeshProUGUI keyText = Text(key.transform, "Text", leadingKey, 20f, Paper, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(keyText.rectTransform);
                left = 72f;
            }

            TextMeshProUGUI text = Text(face.transform, "Label", label, fontSize, KeyInk(style), FontStyles.Bold, align);
            Stretch(text.rectTransform, left, 2f, 12f, 2f);

            if (!string.IsNullOrEmpty(hotkey))
            {
                Badge(face.transform, hotkey, disabled);
            }

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.92f);
            colors.pressedColor = new Color(0.9f, 0.88f, 0.84f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.interactable = !disabled;

            if (!disabled)
            {
                var press = root.gameObject.AddComponent<KeycapPress>();
                EditorAssetUtil.Wire(press, ("content", face.rectTransform));
            }

            return new Key(button, face, text);
        }

        /// <summary>Little navy key label stuck on a corner of a keycap or button ("Esc", "1").</summary>
        public static RectTransform Badge(Transform parent, string text, bool muted = false)
        {
            Image badge = Image(parent, "Hotkey", Rounded, muted ? DisabledLine : Navy);
            Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -6f),
                new Vector2(Mathf.Max(32f, 13f * text.Length + 16f), 30f));
            TextMeshProUGUI label = Text(badge.transform, "Text", text, 17f, Paper, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return badge.rectTransform;
        }

        /// <summary>Inline key label for hint lines: [Esc] close.</summary>
        public static RectTransform KeyHint(Transform parent, string key, string what, Color ink, float fontSize = 19f)
        {
            RectTransform row = Row(parent, "Hint_" + key, 9f);
            Image cap = Image(row, "Key", Rounded, Navy);
            HLayout(cap.gameObject, new RectOffset(10, 10, 3, 4), 0f, TextAnchor.MiddleCenter);
            Text(cap.transform, "Text", key, fontSize - 2f, Paper, FontStyles.Bold, TextAlignmentOptions.Center);
            Text(row, "What", what, fontSize, ink, FontStyles.Bold);
            return row;
        }

        /// <summary>Status chip: colour + word (always paired with a word, never colour alone).</summary>
        public static RectTransform Chip(Transform parent, string text, Color color, float fontSize = 19f, Color? ink = null)
        {
            Image chip = Image(parent, "Chip_" + text, Pill, color);
            chip.pixelsPerUnitMultiplier = 1.6f; // corner radius 15 so a ~34 px chip stays a pill, not an oval
            HLayout(chip.gameObject, new RectOffset(16, 16, 5, 7), 0f, TextAnchor.MiddleCenter);
            Text(chip.transform, "Text", text, fontSize, ink ?? NavyDeep, FontStyles.Bold);
            return chip.rectTransform;
        }

        /// <summary>A flat shape for small UI illustrations (icons drawn from kit sprites), centred in its parent.</summary>
        public static Image Glyph(Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size, float rotation = 0f, float cornerScale = 1f)
        {
            Image glyph = Image(parent, "Glyph", sprite, color);
            glyph.pixelsPerUnitMultiplier = cornerScale;
            Place(glyph.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            glyph.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return glyph;
        }

        /// <summary>Thin rounded progress bar with a navy outline.</summary>
        public static RectTransform ProgressBar(Transform parent, string name, float width, float height, float value, Color fill)
        {
            Image track = Image(parent, name, Pill, Disabled);
            Size(track.gameObject, width, height);
            track.rectTransform.sizeDelta = new Vector2(width, height);
            Image bar = Image(track.transform, "Fill", PillSolid, fill);
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            bar.rectTransform.offsetMin = new Vector2(3f, 3f);
            bar.rectTransform.offsetMax = new Vector2(0f, -3f);
            return track.rectTransform;
        }

        /// <summary>Circular portrait stamp: coloured disc, masked portrait render, navy ring.</summary>
        public static RectTransform PortraitStamp(Transform parent, string customer, float diameter, Color backing)
        {
            Image disc = Image(parent, "Portrait_" + customer, Circle, backing);
            disc.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            Size(disc.gameObject, diameter, diameter);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Sprite portrait = Portrait(customer);
            if (portrait != null)
            {
                // The renders frame head + shoulders with the face near the centre.
                Image face = Image(disc.transform, "Face", portrait, Color.white);
                face.preserveAspect = true;
                Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -diameter * 0.04f),
                    new Vector2(diameter * 1.12f, diameter * 1.12f));
            }

            Image ring = Image(disc.transform, "Ring", PortraitRing, Color.white);
            Stretch(ring.rectTransform);
            return disc.rectTransform;
        }

        /// <summary>
        /// Full-screen modal layer: navy dim that swallows clicks + a UiPanel (fade, pop, Esc). Content
        /// goes under the returned root. The UiPanel pops <paramref name="popTarget"/> once it is set.
        /// </summary>
        public static UiPanel Modal(Transform canvas, string name, float dim, out RectTransform root)
        {
            root = Rect(canvas, name);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            Image shade = Image(root, "Dim", null, new Color(NavyDeep.r, NavyDeep.g, NavyDeep.b, dim), raycast: true);
            Stretch(shade.rectTransform);
            return root.gameObject.AddComponent<UiPanel>();
        }

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = UiLayer };
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        // ---- persistent listeners (saved with the scene) ----------------------------------------

        public static void OnClick(Button button, UnityAction action) => UnityEventTools.AddPersistentListener(button.onClick, action);

        public static void OnClick(Button button, UnityAction<string> action, string argument) =>
            UnityEventTools.AddStringPersistentListener(button.onClick, action, argument);

        public static void OnClick(Button button, UnityAction<int> action, int argument) =>
            UnityEventTools.AddIntPersistentListener(button.onClick, action, argument);

        public static void On(UnityEvent unityEvent, UnityAction action) => UnityEventTools.AddPersistentListener(unityEvent, action);

        public static void On<T>(UnityEvent unityEvent, UnityAction<T> action, T argument) where T : Object =>
            UnityEventTools.AddObjectPersistentListener(unityEvent, action, argument);

        public static void On(UnityEvent unityEvent, UnityAction<int> action, int argument) =>
            UnityEventTools.AddIntPersistentListener(unityEvent, action, argument);

        // ---- sprite generation -------------------------------------------------------------------

        private static string SpritePath(string name) => $"{PhoneFixerPaths.UiFolder}/{name}.png";

        /// <summary>Rounded rectangle (optional border) with an optional hard drop below, as a 9-slice sprite.</summary>
        private static Sprite Shape(string name, int width, int height, float radius, float border, float drop, Color fill, Color borderColor, Color dropColor)
        {
            string path = SpritePath(name);
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            var pixels = new Color[width * height];
            var top = new Rect(0f, drop, width, height - drop);
            var under = new Rect(0f, 0f, width, height - drop);
            float r = Mathf.Min(radius, (height - drop) * 0.5f, width * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SdRoundRect(p, top, r);
                    float aMain = Mathf.Clamp01(0.5f - d);
                    Color main = Color.clear;
                    if (aMain > 0f)
                    {
                        float aFill = border > 0f ? Mathf.Min(aMain, Mathf.Clamp01(0.5f - (d + border))) : aMain;
                        float aBorder = aMain - aFill;
                        float alpha = fill.a * aFill + borderColor.a * aBorder;
                        Color rgb = alpha > 0f ? (fill * (fill.a * aFill) + borderColor * (borderColor.a * aBorder)) / alpha : fill;
                        main = new Color(rgb.r, rgb.g, rgb.b, alpha);
                    }

                    if (drop > 0f && dropColor.a > 0f)
                    {
                        float aDrop = Mathf.Clamp01(0.5f - SdRoundRect(p, under, r)) * dropColor.a;
                        main = Over(main, new Color(dropColor.r, dropColor.g, dropColor.b, aDrop));
                    }

                    pixels[y * width + x] = main;
                }
            }

            float side = r + border + 2f;
            return SaveSprite(path, width, height, pixels, new Vector4(side, drop + side, side, side));
        }

        private static Sprite Disc(string name, int size, Color fill, Color border, float borderWidth, bool innerRing)
        {
            string path = SpritePath(name);
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            var pixels = new Color[size * size];
            float c = size * 0.5f;
            float outer = c - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = Mathf.Clamp01(outer - d + 0.5f);
                    float inFill = Mathf.Clamp01(outer - borderWidth - d + 0.5f);
                    Color col = Color.Lerp(border, fill, inFill);
                    if (innerRing)
                    {
                        float ring = Mathf.Clamp01(1.2f - Mathf.Abs(d - outer * 0.62f));
                        col = Color.Lerp(col, border, ring * 0.8f);
                    }

                    pixels[y * size + x] = new Color(col.r, col.g, col.b, a);
                }
            }

            return SaveSprite(path, size, size, pixels, Vector4.zero);
        }

        private static Sprite RingSprite(string name, int size, float outer, float inner, Color? color)
        {
            string path = SpritePath(name);
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            Color tint = color ?? Color.white;
            var pixels = new Color[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = Mathf.Min(Mathf.Clamp01(outer - d + 0.5f), Mathf.Clamp01(d - inner + 0.5f));
                    pixels[y * size + x] = new Color(tint.r, tint.g, tint.b, a);
                }
            }

            return SaveSprite(path, size, size, pixels, Vector4.zero);
        }

        private static Sprite StarSprite(string name, int size)
        {
            string path = SpritePath(name);
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            Vector2[] outerStar = StarPolygon(size * 0.5f, size * 0.47f, size * 0.48f, size * 0.21f);
            Vector2[] innerStar = StarPolygon(size * 0.5f, size * 0.47f, size * 0.37f, size * 0.155f);
            var pixels = new Color[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hitOuter = 0, hitInner = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var p = new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss);
                            if (Inside(p, outerStar))
                            {
                                hitOuter++;
                            }

                            if (Inside(p, innerStar))
                            {
                                hitInner++;
                            }
                        }
                    }

                    float a = hitOuter / (float)(ss * ss);
                    float f = hitInner / (float)(ss * ss);
                    Color col = Color.Lerp(Navy, Butter, a > 0f ? f / a : 0f);
                    pixels[y * size + x] = new Color(col.r, col.g, col.b, a);
                }
            }

            return SaveSprite(path, size, size, pixels, Vector4.zero);
        }

        private static Vector2[] StarPolygon(float cx, float cy, float rOuter, float rInner)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? rOuter : rInner;
                points[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }

            return points;
        }

        private static bool Inside(Vector2 p, IReadOnlyList<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float SdRoundRect(Vector2 p, Rect rect, float radius)
        {
            Vector2 half = rect.size * 0.5f - new Vector2(radius, radius);
            var q = new Vector2(Mathf.Abs(p.x - rect.center.x), Mathf.Abs(p.y - rect.center.y)) - half;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static Color Over(Color src, Color dst)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0f)
            {
                return Color.clear;
            }

            Color rgb = (src * src.a + dst * (dst.a * (1f - src.a))) / a;
            return new Color(rgb.r, rgb.g, rgb.b, a);
        }

        private static Sprite SaveSprite(string path, int width, int height, Color[] pixels, Vector4 border)
        {
            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(path));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
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
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}

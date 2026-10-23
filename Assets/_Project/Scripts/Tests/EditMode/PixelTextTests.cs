using NUnit.Framework;
using PhoneRepair.Pixel;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.Tests
{
    /// <summary>The pixel font and the text layout every label, bubble and ticket relies on.</summary>
    public class PixelTextTests
    {
        private const string FontPath = "Assets/_Project/Art/Pixel/Fonts/FNT_Pixel.asset";

        private static PixelFont LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<PixelFont>(FontPath);
            if (font == null)
            {
                Assert.Ignore("Run Tools > Phone Repair > Run Setup first.");
            }

            return font;
        }

        private static PixelText MakeText(PixelFont font, string text, int wrap = 0)
        {
            var go = new GameObject("Text", typeof(MeshFilter), typeof(MeshRenderer));
            var label = go.AddComponent<PixelText>();
            label.Font = font;
            label.WrapWidth = wrap;
            label.Text = text;
            return label;
        }

        [Test]
        public void Font_HasEveryCharacterTheGameWrites()
        {
            PixelFont font = LoadFont();
            Assert.That(font.Atlas, Is.Not.Null);
            Assert.That(font.Material, Is.Not.Null);
            for (char c = '!'; c <= '~'; c++)
            {
                Assert.That(font.HasGlyph(c), Is.True, $"'{c}'");
            }

            foreach (char c in "¥×·…—")
            {
                Assert.That(font.HasGlyph(c), Is.True, $"U+{(int)c:X4}");
            }
        }

        [Test]
        public void Text_IsMeasuredInWholePixels()
        {
            PixelText label = MakeText(LoadFont(), "Hello");
            try
            {
                // H(5) e(5) l(2) l(2) o(5) with 1 px between letters
                Assert.That(label.Size, Is.EqualTo(new Vector2Int(23, 9)));
                Assert.That(label.CharacterCount, Is.EqualTo(5));
                Assert.That(label.LineCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(label.gameObject);
            }
        }

        [Test]
        public void LongLines_WrapAtTheWrapWidth()
        {
            PixelText label = MakeText(LoadFont(), "My phone smells like ramen and it is very puffy", wrap: 80);
            try
            {
                Assert.That(label.LineCount, Is.GreaterThan(1));
                Assert.That(label.ContentWidth, Is.LessThanOrEqualTo(80));
                Assert.That(label.Size.y, Is.EqualTo((label.LineCount - 1) * 11 + 9));
            }
            finally
            {
                Object.DestroyImmediate(label.gameObject);
            }
        }

        [Test]
        public void Typewriter_ShowsOnlyTheFirstCharacters()
        {
            PixelText label = MakeText(LoadFont(), "ab cd");
            try
            {
                Assert.That(label.CharacterCount, Is.EqualTo(5), "the space counts");
                label.VisibleCharacters = 2;
                Mesh mesh = label.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.vertexCount, Is.EqualTo(8), "two glyph quads");
                label.VisibleCharacters = -1;
                Assert.That(mesh.vertexCount, Is.EqualTo(16), "four glyph quads (spaces draw nothing)");
            }
            finally
            {
                Object.DestroyImmediate(label.gameObject);
            }
        }
    }
}

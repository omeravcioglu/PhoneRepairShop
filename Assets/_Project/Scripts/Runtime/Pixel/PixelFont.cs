using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>
    /// A bitmap font for crisp pixel text: one atlas texture (white glyphs on transparent) plus the
    /// rectangle and advance of every character. All glyph rects share the cell height; the cell's
    /// top row is the top of a capital letter. Tools > Phone Repair > Run Setup builds the placeholder
    /// font; a designed font only needs a new atlas and glyph table.
    /// </summary>
    [CreateAssetMenu(fileName = "FNT_New", menuName = "Phone Repair/Pixel Font", order = 40)]
    public sealed class PixelFont : ScriptableObject
    {
        [Serializable]
        public struct Glyph
        {
            [Tooltip("Unicode code point.")]
            public int code;
            [Tooltip("Pixels in the atlas, origin bottom-left. Height = the font's cell height.")]
            public RectInt rect;
            [Tooltip("Pen movement after the glyph (its width + letter spacing).")]
            public int advance;
        }

        [SerializeField] private Texture2D atlas;
        [Tooltip("Material that draws this atlas (shader PhoneRepair/Pixel Text).")]
        [SerializeField] private Material material;
        [SerializeField, Min(1)] private int cellHeight = 9;
        [Tooltip("Distance from one line's top to the next line's top.")]
        [SerializeField, Min(1)] private int lineHeight = 11;
        [SerializeField, Min(0)] private int spaceAdvance = 4;
        [Tooltip("Shown for characters the font doesn't have.")]
        [SerializeField] private int fallbackCode = '?';
        [SerializeField] private List<Glyph> glyphs = new();

        private Dictionary<int, Glyph> lookup;

        public Texture2D Atlas => atlas;
        public Material Material => material;
        public int CellHeight => cellHeight;
        public int LineHeight => lineHeight;
        public int SpaceAdvance => spaceAdvance;
        public IReadOnlyList<Glyph> Glyphs => glyphs;

        public bool HasGlyph(int code)
        {
            EnsureLookup();
            return lookup.ContainsKey(code);
        }

        /// <summary>The glyph for a character, or the fallback glyph. False only if neither exists.</summary>
        public bool TryGetGlyph(int code, out Glyph glyph)
        {
            EnsureLookup();
            return lookup.TryGetValue(code, out glyph) || lookup.TryGetValue(fallbackCode, out glyph);
        }

        /// <summary>Pen movement for one character (spaces have no glyph).</summary>
        public int AdvanceOf(int code)
        {
            if (code == ' ')
            {
                return spaceAdvance;
            }

            return TryGetGlyph(code, out Glyph glyph) ? glyph.advance : spaceAdvance;
        }

        /// <summary>Used by the editor font builder.</summary>
        public void Configure(Texture2D atlasTexture, Material atlasMaterial, int cell, int line, int space, IEnumerable<Glyph> table)
        {
            atlas = atlasTexture;
            material = atlasMaterial;
            cellHeight = Mathf.Max(1, cell);
            lineHeight = Mathf.Max(1, line);
            spaceAdvance = Mathf.Max(0, space);
            glyphs = new List<Glyph>(table);
            lookup = null;
        }

        private void OnValidate() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null && lookup.Count == glyphs.Count)
            {
                return;
            }

            lookup = new Dictionary<int, Glyph>(glyphs.Count);
            foreach (Glyph glyph in glyphs)
            {
                lookup[glyph.code] = glyph;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>
    /// Crisp pixel text drawn with a <see cref="PixelFont"/>: a mesh of glyph quads on whole pixels.
    /// The transform is the top of the first line: its left end (left aligned), its middle (centred)
    /// or its right end (right aligned). With a wrap width, lines wrap at that width and alignment
    /// happens inside it, measured from the transform. Optional 1 px drop shadow, integer scale and a
    /// typewriter count of visible characters.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PixelText : MonoBehaviour
    {
        public enum Align
        {
            Left = 0,
            Center = 1,
            Right = 2,
        }

        [SerializeField] private PixelFont font;
        [SerializeField, TextArea(2, 6)] private string text = string.Empty;
        [SerializeField] private Color color = Color.white;
        [Tooltip("Drawn one pixel below the text when its alpha is above zero.")]
        [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0f);
        [SerializeField] private Align alignment = Align.Left;
        [Tooltip("Wrap width in pixels before scaling (0 = never wrap).")]
        [SerializeField, Min(0)] private int wrapWidth;
        [SerializeField, Range(1, 8)] private int scale = 1;
        [SerializeField] private int sortingOrder = SortOrder.UiText;
        [Tooltip("How many characters to show (-1 = all). Spaces count, line breaks don't.")]
        [SerializeField] private int visibleCharacters = -1;

        private sealed class Line
        {
            public readonly List<(int code, int x)> Glyphs = new();
            public int Width;
        }

        private readonly List<Line> lines = new();
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<Color> colors = new();
        private readonly List<int> indices = new();
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private bool dirty = true;

        public PixelFont Font
        {
            get => font;
            set => Set(ref font, value);
        }

        public string Text
        {
            get => text;
            set => Set(ref text, value ?? string.Empty);
        }

        public Color Color
        {
            get => color;
            set => Set(ref color, value);
        }

        public Color ShadowColor
        {
            get => shadowColor;
            set => Set(ref shadowColor, value);
        }

        public Align Alignment
        {
            get => alignment;
            set => Set(ref alignment, value);
        }

        public int WrapWidth
        {
            get => wrapWidth;
            set => Set(ref wrapWidth, Mathf.Max(0, value));
        }

        public int Scale
        {
            get => scale;
            set => Set(ref scale, Mathf.Clamp(value, 1, 8));
        }

        public int SortingOrder
        {
            get => sortingOrder;
            set => Set(ref sortingOrder, value);
        }

        /// <summary>-1 shows everything; typewriters count up from 0 to <see cref="CharacterCount"/>.</summary>
        public int VisibleCharacters
        {
            get => visibleCharacters;
            set => Set(ref visibleCharacters, value);
        }

        /// <summary>Characters the typewriter steps through (line breaks excluded).</summary>
        public int CharacterCount { get; private set; }

        public int LineCount => lines.Count;

        /// <summary>Width of the longest line, in pixels after scaling.</summary>
        public int ContentWidth { get; private set; }

        /// <summary>Size of the text block in pixels after scaling.</summary>
        public Vector2Int Size { get; private set; }

        public void Rebuild()
        {
            dirty = false;
            EnsureMesh();
            Layout();

            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            indices.Clear();
            if (font != null && font.Atlas != null)
            {
                int limit = visibleCharacters < 0 ? int.MaxValue : visibleCharacters;
                if (shadowColor.a > 0f)
                {
                    Emit(limit, new Vector2Int(0, -1), new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * color.a));
                }

                Emit(limit, Vector2Int.zero, color);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();

            meshRenderer.sortingOrder = sortingOrder;
            if (font != null && font.Material != null && meshRenderer.sharedMaterial != font.Material)
            {
                meshRenderer.sharedMaterial = font.Material;
            }
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            dirty = true; // rebuilt in LateUpdate: renderers must not be touched from OnValidate
        }

        private void LateUpdate()
        {
            if (dirty)
            {
                Rebuild();
            }
        }

        private void OnDestroy()
        {
            if (mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
        }

        private void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            Rebuild(); // immediately, so Size and CharacterCount are right after setting
        }

        private void EnsureMesh()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (mesh != null)
            {
                return;
            }

            mesh = new Mesh { name = "PixelText", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void Layout()
        {
            lines.Clear();
            CharacterCount = 0;
            ContentWidth = 0;
            Size = Vector2Int.zero;
            if (font == null)
            {
                return;
            }

            string[] paragraphs = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            foreach (string paragraph in paragraphs)
            {
                var line = new Line();
                int pen = 0;
                bool hasContent = false;
                string[] words = paragraph.Split(' ');
                for (int w = 0; w < words.Length; w++)
                {
                    string word = words[w];
                    if (wrapWidth > 0 && hasContent && word.Length > 0 &&
                        pen + font.SpaceAdvance + MeasureInk(word) > wrapWidth)
                    {
                        Finish(line);
                        line = new Line();
                        pen = 0;
                        hasContent = false;
                    }

                    if (w > 0 && hasContent)
                    {
                        line.Glyphs.Add((' ', pen));
                        pen += font.SpaceAdvance;
                        CharacterCount++;
                    }

                    foreach (char c in word)
                    {
                        line.Glyphs.Add((c, pen));
                        pen += font.AdvanceOf(c);
                        CharacterCount++;
                    }

                    hasContent |= word.Length > 0;
                }

                Finish(line);
            }

            int height = (lines.Count - 1) * font.LineHeight + font.CellHeight;
            Size = new Vector2Int(ContentWidth, Mathf.Max(0, height) * scale);
        }

        private void Finish(Line line)
        {
            int width = 0;
            foreach ((int code, int x) in line.Glyphs)
            {
                if (code != ' ' && font.TryGetGlyph(code, out PixelFont.Glyph glyph))
                {
                    width = Mathf.Max(width, x + glyph.rect.width);
                }
            }

            line.Width = width;
            ContentWidth = Mathf.Max(ContentWidth, width * scale);
            lines.Add(line);
        }

        /// <summary>Width of a word's ink (no spacing after the last letter).</summary>
        private int MeasureInk(string word)
        {
            int pen = 0;
            int ink = 0;
            foreach (char c in word)
            {
                if (font.TryGetGlyph(c, out PixelFont.Glyph glyph))
                {
                    ink = pen + glyph.rect.width;
                }

                pen += font.AdvanceOf(c);
            }

            return ink;
        }

        private int AlignOffset(int lineWidth)
        {
            if (wrapWidth > 0)
            {
                return alignment switch
                {
                    Align.Center => (wrapWidth - lineWidth) / 2,
                    Align.Right => wrapWidth - lineWidth,
                    _ => 0,
                };
            }

            return alignment switch
            {
                Align.Center => Mathf.FloorToInt(-lineWidth / 2f),
                Align.Right => -lineWidth,
                _ => 0,
            };
        }

        private void Emit(int limit, Vector2Int offset, Color tint)
        {
            Texture2D atlas = font.Atlas;
            float invWidth = 1f / atlas.width;
            float invHeight = 1f / atlas.height;
            int shown = 0;
            for (int l = 0; l < lines.Count; l++)
            {
                Line line = lines[l];
                int top = -l * font.LineHeight + offset.y;
                int start = AlignOffset(line.Width) + offset.x;
                foreach ((int code, int x) in line.Glyphs)
                {
                    if (shown >= limit)
                    {
                        return;
                    }

                    shown++;
                    if (code == ' ' || !font.TryGetGlyph(code, out PixelFont.Glyph glyph))
                    {
                        continue;
                    }

                    int left = (start + x) * scale;
                    int right = (start + x + glyph.rect.width) * scale;
                    int glyphTop = top * scale;
                    int bottom = (top - glyph.rect.height) * scale;

                    int index = vertices.Count;
                    vertices.Add(new Vector3(left, bottom));
                    vertices.Add(new Vector3(left, glyphTop));
                    vertices.Add(new Vector3(right, glyphTop));
                    vertices.Add(new Vector3(right, bottom));

                    float u0 = glyph.rect.xMin * invWidth;
                    float u1 = glyph.rect.xMax * invWidth;
                    float v0 = glyph.rect.yMin * invHeight;
                    float v1 = glyph.rect.yMax * invHeight;
                    uvs.Add(new Vector2(u0, v0));
                    uvs.Add(new Vector2(u0, v1));
                    uvs.Add(new Vector2(u1, v1));
                    uvs.Add(new Vector2(u1, v0));

                    colors.Add(tint);
                    colors.Add(tint);
                    colors.Add(tint);
                    colors.Add(tint);

                    indices.Add(index);
                    indices.Add(index + 1);
                    indices.Add(index + 2);
                    indices.Add(index);
                    indices.Add(index + 2);
                    indices.Add(index + 3);
                }
            }
        }
    }
}

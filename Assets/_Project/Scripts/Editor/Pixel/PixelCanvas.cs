using System;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// A tiny raster for painting placeholder sprites in code: boxes, rounded boxes, ellipses, lines,
    /// pixel text and silhouette outlines. Origin bottom-left, like Unity textures and the sprites.
    /// </summary>
    internal sealed class PixelCanvas
    {
        private readonly Color32[] pixels;

        public PixelCanvas(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            pixels = new Color32[Width * Height];
        }

        public int Width { get; }
        public int Height { get; }

        public Color32 Get(int x, int y) => Inside(x, y) ? pixels[y * Width + x] : default;

        public bool IsInk(int x, int y) => Get(x, y).a > 127;

        /// <summary>Draws one pixel; semi-transparent colours blend over what is there.</summary>
        public void Plot(int x, int y, Color32 c)
        {
            if (!Inside(x, y) || c.a == 0)
            {
                return;
            }

            int i = y * Width + x;
            if (c.a == 255 || pixels[i].a == 0)
            {
                pixels[i] = c;
                return;
            }

            Color under = pixels[i];
            Color over = c;
            float a = over.a + under.a * (1f - over.a);
            Color mixed = (over * over.a + under * under.a * (1f - over.a)) / Mathf.Max(a, 1e-5f);
            mixed.a = a;
            pixels[i] = mixed;
        }

        public void Clear(int x, int y)
        {
            if (Inside(x, y))
            {
                pixels[y * Width + x] = default;
            }
        }

        public void Fill(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
            {
                for (int i = x; i < x + w; i++)
                {
                    Plot(i, j, c);
                }
            }
        }

        public void Rect(int x, int y, int w, int h, Color32 line)
        {
            for (int i = x; i < x + w; i++)
            {
                Plot(i, y, line);
                Plot(i, y + h - 1, line);
            }

            for (int j = y + 1; j < y + h - 1; j++)
            {
                Plot(x, j, line);
                Plot(x + w - 1, j, line);
            }
        }

        public void Box(int x, int y, int w, int h, Color32 fill, Color32 line)
        {
            Fill(x, y, w, h, fill);
            Rect(x, y, w, h, line);
        }

        /// <summary>Rounded rectangle; corners cut along a circle of radius r. Line = the 1 px edge.</summary>
        public void RoundBox(int x, int y, int w, int h, int r, Color32 fill, Color32? line = null)
        {
            r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
            Shape(x, y, x + w - 1, y + h - 1, (px, py) =>
            {
                float cx = Mathf.Clamp(px, x + r, x + w - 1 - r);
                float cy = Mathf.Clamp(py, y + r, y + h - 1 - r);
                float dx = px - cx;
                float dy = py - cy;
                return dx * dx + dy * dy <= r * r + r * 0.6f;
            }, fill, line);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 fill, Color32? line = null)
        {
            int x0 = Mathf.FloorToInt(cx - rx - 1);
            int x1 = Mathf.CeilToInt(cx + rx + 1);
            int y0 = Mathf.FloorToInt(cy - ry - 1);
            int y1 = Mathf.CeilToInt(cy + ry + 1);
            Shape(x0, y0, x1, y1, (px, py) =>
            {
                float dx = (px + 0.5f - cx) / Mathf.Max(0.5f, rx);
                float dy = (py + 0.5f - cy) / Mathf.Max(0.5f, ry);
                return dx * dx + dy * dy <= 1f;
            }, fill, line);
        }

        /// <summary>Fills every pixel where inside() holds; pixels on the shape's edge get the line colour.</summary>
        public void Shape(int x0, int y0, int x1, int y1, Func<int, int, bool> inside, Color32 fill, Color32? line = null)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!inside(x, y))
                    {
                        continue;
                    }

                    bool edge = line.HasValue && (!inside(x - 1, y) || !inside(x + 1, y) || !inside(x, y - 1) || !inside(x, y + 1));
                    Plot(x, y, edge ? line.Value : fill);
                }
            }
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Plot(x0, y0, c);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>Pixel text with the placeholder font. (x, top) is the top-left of the first line.</summary>
        public int Text(string text, int x, int top, Color32 c, int scale = 1)
        {
            int pen = x;
            int lineTop = top;
            foreach (char ch in text)
            {
                if (ch == '\n')
                {
                    pen = x;
                    lineTop -= PixelGlyphs.LineHeight * scale;
                    continue;
                }

                if (ch != ' ')
                {
                    string[] rows = PixelGlyphs.Rows(ch) ?? PixelGlyphs.Rows('?');
                    for (int r = 0; r < rows.Length; r++)
                    {
                        for (int col = 0; col < rows[r].Length; col++)
                        {
                            if (rows[r][col] == '#')
                            {
                                Fill(pen + col * scale, lineTop - (r + 1) * scale + 1, scale, scale, c);
                            }
                        }
                    }
                }

                pen += PixelGlyphs.Advance(ch) * scale;
            }

            return pen - x;
        }

        /// <summary>Centred pixel text (single line), top at <paramref name="top"/>.</summary>
        public void TextCentered(string text, int centreX, int top, Color32 c, int scale = 1)
        {
            int width = PixelGlyphs.MeasureText(text) * scale;
            Text(text, centreX - width / 2, top, c, scale);
        }

        /// <summary>A 1 px outline around everything painted so far (stickers, cut-outs).</summary>
        public void OutlineSilhouette(Color32 c)
        {
            var ink = new bool[Width * Height];
            for (int i = 0; i < ink.Length; i++)
            {
                ink[i] = pixels[i].a > 127;
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (ink[y * Width + x])
                    {
                        continue;
                    }

                    bool near = (x > 0 && ink[y * Width + x - 1]) || (x < Width - 1 && ink[y * Width + x + 1]) ||
                                (y > 0 && ink[(y - 1) * Width + x]) || (y < Height - 1 && ink[(y + 1) * Width + x]);
                    if (near)
                    {
                        pixels[y * Width + x] = c;
                    }
                }
            }
        }

        public byte[] EncodeToPng()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    }
}

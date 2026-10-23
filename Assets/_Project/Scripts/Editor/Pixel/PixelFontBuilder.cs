using System.Collections.Generic;
using System.IO;
using PhoneRepair.Pixel;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds the placeholder pixel font from <see cref="PixelGlyphs"/>: a 128×128 atlas of white glyphs,
    /// the PixelFont asset that maps characters to rectangles, and the text material.
    /// </summary>
    internal static class PixelFontBuilder
    {
        public const string Folder = "Assets/_Project/Art/Pixel/Fonts";
        public const string AtlasPath = Folder + "/font_pixel.png";
        public const string FontPath = Folder + "/FNT_Pixel.asset";
        public const string MaterialPath = Folder + "/M_PixelText.mat";
        public const string TextShaderPath = "Assets/_Project/Art/Shaders/PixelText.shader";

        private const int CellWidth = 8;
        private const int CellHeight = PixelGlyphs.CellHeight + 1;
        private const int Columns = 16;
        private const int AtlasSize = 128;

        public static PixelFont Build()
        {
            var canvas = new PixelCanvas(AtlasSize, AtlasSize);
            var white = new Color32(255, 255, 255, 255);
            var table = new List<PixelFont.Glyph>();
            int index = 0;
            foreach ((char code, string[] rows) in PixelGlyphs.All)
            {
                int column = index % Columns;
                int row = index / Columns;
                int x = column * CellWidth;
                int y = AtlasSize - (row + 1) * CellHeight;          // glyph rect bottom
                int width = rows[0].Length;
                for (int r = 0; r < rows.Length; r++)
                {
                    for (int c = 0; c < width; c++)
                    {
                        if (rows[r][c] == '#')
                        {
                            canvas.Plot(x + c, y + PixelGlyphs.CellHeight - 1 - r, white);
                        }
                    }
                }

                table.Add(new PixelFont.Glyph
                {
                    code = code,
                    rect = new RectInt(x, y, width, PixelGlyphs.CellHeight),
                    advance = width + PixelGlyphs.LetterSpacing,
                });
                index++;
            }

            EditorAssetUtil.EnsureFolder(Folder);
            File.WriteAllBytes(Path.GetFullPath(AtlasPath), canvas.EncodeToPng());
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TextShaderPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_PixelText" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.mainTexture = atlas;
            EditorUtility.SetDirty(material);

            var font = EditorAssetUtil.LoadOrCreate<PixelFont>(FontPath, out _);
            font.Configure(atlas, material, PixelGlyphs.CellHeight, PixelGlyphs.LineHeight, PixelGlyphs.SpaceAdvance, table);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Import rules for everything under Assets/_Project/Art/Pixel: crisp pixel art (point filter,
    /// no compression, no mipmaps), 1 pixel per unit, Full Rect meshes (needed for 9-slicing and the
    /// hover outline), physics shapes from alpha, and the pivot and 9-slice border from the art
    /// contract. Dropping a new PNG over a placeholder re-applies all of this automatically.
    /// </summary>
    internal sealed class PixelArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Art/Pixel/";

        public override uint GetVersion() => 3;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            bool isFontAtlas = assetPath.IndexOf("/Fonts/", StringComparison.OrdinalIgnoreCase) >= 0;

            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.sRGBTexture = true;

            if (isFontAtlas)
            {
                importer.textureType = TextureImporterType.Default;
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            Vector2Int pivot = new(width / 2, height / 2);
            Vector4 border = Vector4.zero;
            try
            {
                if (ArtContract.TryFind(assetPath, out SpriteSpec spec, out _))
                {
                    pivot = spec.PivotPixels(width, height);
                    border = spec.Border;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhoneRepair] Art contract unreadable while importing {assetPath}: {e.Message}");
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = 1;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = true;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(pivot.x / (float)width, pivot.y / (float)height);
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
        }
    }
}

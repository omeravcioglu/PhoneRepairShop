using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// The pixel-art contract holds: every sprite exists, at the agreed size, imported as crisp pixel
    /// art with the agreed pivot and 9-slice border. Reads art_contract.json on its own (no editor code),
    /// so it also catches a setup that drifted from the contract.
    /// </summary>
    public class ArtContractTests
    {
        private const string JsonPath = "Assets/_Project/Art/Pixel/art_contract.json";

        [Serializable]
        private sealed class Contract
        {
            public string root;
            public List<Spec> sprites;
            public List<Anchor> anchors;
        }

        [Serializable]
        private sealed class Spec
        {
            public string id;
            public string file;
            public int w;
            public int h;
            public string pivot;
            public int px;
            public int py;
            public int frames;
            public int[] border;
            public bool place;
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class Anchor
        {
            public string id;
        }

        private static Contract Load()
        {
            string full = Path.GetFullPath(JsonPath);
            Assert.That(File.Exists(full), Is.True, JsonPath);
            return JsonUtility.FromJson<Contract>(File.ReadAllText(full));
        }

        private static IEnumerable<(Spec spec, string path)> Files(Contract contract)
        {
            foreach (Spec spec in contract.sprites)
            {
                int frames = Mathf.Max(1, spec.frames);
                for (int f = 0; f < frames; f++)
                {
                    yield return (spec, $"{contract.root}/{spec.file}{(frames > 1 ? "_" + f : string.Empty)}.png");
                }
            }
        }

        private static Vector2 ExpectedPivot(Spec spec) => (spec.pivot ?? "Center") switch
        {
            "BottomLeft" => Vector2.zero,
            "BottomCenter" => new Vector2(spec.w / 2, 0),
            "TopLeft" => new Vector2(0, spec.h),
            "Pixel" => new Vector2(spec.px, spec.py),
            _ => new Vector2(spec.w / 2, spec.h / 2),
        };

        [Test]
        public void Ids_AndAnchors_AreUnique()
        {
            Contract contract = Load();
            var ids = new HashSet<string>();
            foreach (Spec spec in contract.sprites)
            {
                Assert.That(ids.Add(spec.id), Is.True, $"duplicate sprite id {spec.id}");
                Assert.That(spec.w, Is.GreaterThan(0), spec.id);
                Assert.That(spec.h, Is.GreaterThan(0), spec.id);
            }

            var anchors = new HashSet<string>();
            foreach (Anchor anchor in contract.anchors)
            {
                Assert.That(anchors.Add(anchor.id), Is.True, $"duplicate anchor {anchor.id}");
            }
        }

        [Test]
        public void EveryFile_IsImportedAsCrispPixelArt()
        {
            foreach ((Spec spec, string path) in Files(Load()))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"{path} is missing - run Tools > Phone Repair > Run Setup");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(1f), path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), $"{path}: 9-slicing and outlines need Full Rect");
            }
        }

        [Test]
        public void EverySprite_HasTheAgreedSize_PivotAndBorder()
        {
            foreach ((Spec spec, string path) in Files(Load()))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(spec.w, spec.h)), $"{path} size");
                Assert.That(Vector2.Distance(sprite.pivot, ExpectedPivot(spec)), Is.LessThan(0.01f), $"{path} pivot");
                Vector4 border = spec.border != null && spec.border.Length == 4
                    ? new Vector4(spec.border[0], spec.border[1], spec.border[2], spec.border[3])
                    : Vector4.zero;
                Assert.That(sprite.border, Is.EqualTo(border), $"{path} 9-slice border");
            }
        }

        [Test]
        public void PlacedSprites_SitInsideTheirView()
        {
            foreach (Spec spec in Load().sprites)
            {
                if (!spec.place)
                {
                    continue;
                }

                Assert.That(spec.x, Is.InRange(0, 640), spec.id);
                Assert.That(spec.y, Is.InRange(-360, 360), spec.id);
            }
        }
    }
}

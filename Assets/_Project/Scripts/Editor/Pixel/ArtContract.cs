using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>One sprite in the art contract (see art_contract.json).</summary>
    [Serializable]
    internal sealed class SpriteSpec
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
        public bool outline;
        public bool physics;
        public string group;
        public bool place;
        public int x;
        public int y;
        public string notes;

        public int FrameCount => Mathf.Max(1, frames);

        /// <summary>Path of one frame: Root/file.png, or Root/file_N.png for animations.</summary>
        public string FramePath(string root, int frame) =>
            $"{root}/{file}{(FrameCount > 1 ? "_" + frame : string.Empty)}.png";

        /// <summary>The pivot in whole pixels from the bottom-left, for an image of the given size.</summary>
        public Vector2Int PivotPixels(int width, int height)
        {
            switch (string.IsNullOrEmpty(pivot) ? "Center" : pivot)
            {
                case "BottomLeft":
                    return Vector2Int.zero;
                case "BottomCenter":
                    return new Vector2Int(width / 2, 0);
                case "TopLeft":
                    return new Vector2Int(0, height);
                case "Pixel":
                    return new Vector2Int(px, py);
                default:
                    return new Vector2Int(width / 2, height / 2);
            }
        }

        /// <summary>9-slice border (left, bottom, right, top) or zero.</summary>
        public Vector4 Border => border != null && border.Length == 4
            ? new Vector4(border[0], border[1], border[2], border[3])
            : Vector4.zero;

        public Vector3 Position => new(x, y, 0f);
    }

    [Serializable]
    internal sealed class AnchorSpec
    {
        public string id;
        public int x;
        public int y;
        public string notes;
    }

    [Serializable]
    internal sealed class ArtContractData
    {
        public int version;
        public string title;
        public string root;
        public int width;
        public int height;
        public string[] rules;
        public List<SpriteSpec> sprites = new();
        public List<AnchorSpec> anchors = new();
    }

    /// <summary>
    /// The pixel-art contract: every sprite the game needs (file, size, pivot, frames, 9-slice
    /// border, where it sits on screen) and the named anchor points of the layout. One JSON file
    /// drives the placeholder generator, the import settings, the scene builder and the art doc.
    /// </summary>
    internal static class ArtContract
    {
        public const string JsonPath = "Assets/_Project/Art/Pixel/art_contract.json";

        private static ArtContractData cached;
        private static DateTime cachedStamp;

        public static ArtContractData Data => Load();
        public static string Root => Load().root;

        public static ArtContractData Load()
        {
            string full = Path.GetFullPath(JsonPath);
            DateTime stamp = File.Exists(full) ? File.GetLastWriteTimeUtc(full) : DateTime.MinValue;
            if (cached != null && stamp == cachedStamp)
            {
                return cached;
            }

            if (!File.Exists(full))
            {
                throw new FileNotFoundException($"Art contract missing: {JsonPath}");
            }

            var data = JsonUtility.FromJson<ArtContractData>(File.ReadAllText(full));
            if (data == null || data.sprites == null || data.sprites.Count == 0)
            {
                throw new InvalidDataException($"{JsonPath} has no sprites.");
            }

            data.root = string.IsNullOrWhiteSpace(data.root) ? "Assets/_Project/Art/Pixel" : data.root.TrimEnd('/');
            data.anchors ??= new List<AnchorSpec>();
            cached = data;
            cachedStamp = stamp;
            return data;
        }

        public static SpriteSpec Sprite(string id)
        {
            foreach (SpriteSpec spec in Load().sprites)
            {
                if (spec.id == id)
                {
                    return spec;
                }
            }

            throw new KeyNotFoundException($"No sprite '{id}' in {JsonPath}.");
        }

        public static bool HasSprite(string id) => Load().sprites.Exists(s => s.id == id);

        public static Vector3 Anchor(string id)
        {
            foreach (AnchorSpec anchor in Load().anchors)
            {
                if (anchor.id == id)
                {
                    return new Vector3(anchor.x, anchor.y, 0f);
                }
            }

            throw new KeyNotFoundException($"No anchor '{id}' in {JsonPath}.");
        }

        public static string FramePath(SpriteSpec spec, int frame) => spec.FramePath(Root, frame);

        /// <summary>Every file the contract asks for.</summary>
        public static IEnumerable<(SpriteSpec spec, int frame, string path)> Files()
        {
            ArtContractData data = Load();
            foreach (SpriteSpec spec in data.sprites)
            {
                for (int f = 0; f < spec.FrameCount; f++)
                {
                    yield return (spec, f, spec.FramePath(data.root, f));
                }
            }
        }

        public static bool TryFind(string assetPath, out SpriteSpec spec, out int frame)
        {
            foreach ((SpriteSpec s, int f, string path) in Files())
            {
                if (string.Equals(path, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    spec = s;
                    frame = f;
                    return true;
                }
            }

            spec = null;
            frame = 0;
            return false;
        }
    }
}

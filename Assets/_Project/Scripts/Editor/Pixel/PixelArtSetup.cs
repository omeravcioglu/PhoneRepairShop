using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Makes sure every sprite in the art contract exists. Missing files get a generated placeholder;
    /// files that are still an untouched placeholder are regenerated (so placeholder fixes land);
    /// anything else is real art and is never touched. A manifest of placeholder hashes
    /// (ArtSource/Pixel/placeholders.json, outside Assets) tells the two apart.
    /// </summary>
    internal static class PixelArtSetup
    {
        public const string ManifestPath = "ArtSource/Pixel/placeholders.json";

        [Serializable]
        private sealed class Manifest
        {
            public List<Entry> files = new();
        }

        [Serializable]
        private sealed class Entry
        {
            public string path;
            public string md5;
        }

        public readonly struct Status
        {
            public Status(int real, int placeholders, int missing)
            {
                Real = real;
                Placeholders = placeholders;
                Missing = missing;
            }

            public int Real { get; }
            public int Placeholders { get; }
            public int Missing { get; }
        }

        /// <summary>Generates/refreshes placeholders and re-imports every contract file with the contract's settings.</summary>
        public static Status EnsurePlaceholders()
        {
            Dictionary<string, string> known = LoadManifest();
            var written = new List<string>();
            int real = 0;
            int placeholders = 0;

            foreach ((SpriteSpec spec, int frame, string path) in ArtContract.Files())
            {
                string full = Path.GetFullPath(path);
                bool exists = File.Exists(full);
                string current = exists ? Md5(File.ReadAllBytes(full)) : null;
                bool isPlaceholder = !exists || (known.TryGetValue(path, out string recorded) && recorded == current);
                if (!isPlaceholder)
                {
                    real++;
                    continue;
                }

                byte[] png = PlaceholderArt.Paint(spec, frame).EncodeToPng();
                string md5 = Md5(png);
                if (md5 != current)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    File.WriteAllBytes(full, png);
                    written.Add(path);
                }

                known[path] = md5;
                placeholders++;
            }

            SaveManifest(known);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // pivots and borders come from the contract, which can change without the PNG changing
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach ((SpriteSpec _, int _, string path) in ArtContract.Files())
                {
                    if (File.Exists(Path.GetFullPath(path)))
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[PhoneRepair] Pixel art: {real} real, {placeholders} placeholders ({written.Count} written this run).");
            return new Status(real, placeholders, 0);
        }

        /// <summary>How much of the contract is real art, still placeholder, or missing.</summary>
        public static Status Check(out List<string> placeholderPaths, out List<string> missingPaths)
        {
            Dictionary<string, string> known = LoadManifest();
            placeholderPaths = new List<string>();
            missingPaths = new List<string>();
            int real = 0;
            foreach ((SpriteSpec _, int _, string path) in ArtContract.Files())
            {
                string full = Path.GetFullPath(path);
                if (!File.Exists(full))
                {
                    missingPaths.Add(path);
                    continue;
                }

                if (known.TryGetValue(path, out string recorded) && recorded == Md5(File.ReadAllBytes(full)))
                {
                    placeholderPaths.Add(path);
                }
                else
                {
                    real++;
                }
            }

            return new Status(real, placeholderPaths.Count, missingPaths.Count);
        }

        public static Sprite LoadSprite(string id, int frame = 0)
        {
            SpriteSpec spec = ArtContract.Sprite(id);
            string path = ArtContract.FramePath(spec, Mathf.Clamp(frame, 0, spec.FrameCount - 1));
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Sprite '{id}' frame {frame} not imported at {path}.");
            }

            return sprite;
        }

        public static Sprite[] LoadFrames(string id)
        {
            SpriteSpec spec = ArtContract.Sprite(id);
            var frames = new Sprite[spec.FrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = LoadSprite(id, i);
            }

            return frames;
        }

        private static Dictionary<string, string> LoadManifest()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string full = Path.GetFullPath(ManifestPath);
            if (!File.Exists(full))
            {
                return result;
            }

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(full));
            if (manifest?.files != null)
            {
                foreach (Entry entry in manifest.files)
                {
                    result[entry.path] = entry.md5;
                }
            }

            return result;
        }

        private static void SaveManifest(Dictionary<string, string> known)
        {
            var manifest = new Manifest();
            var paths = new List<string>(known.Keys);
            paths.Sort(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                manifest.files.Add(new Entry { path = path, md5 = known[path] });
            }

            string full = Path.GetFullPath(ManifestPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, JsonUtility.ToJson(manifest, true));
        }

        private static string Md5(byte[] bytes)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}

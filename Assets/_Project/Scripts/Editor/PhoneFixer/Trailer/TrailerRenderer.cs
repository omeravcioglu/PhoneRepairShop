using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools.Trailer
{
    /// <summary>One shot of the trailer: a scene, a length in frames and a pose function of time.</summary>
    internal sealed class Shot
    {
        public string Name;
        public string Scene;
        public int Frames;
        /// <summary>Motion-blur samples per frame (1 = off). Only the main sample records cues and tracks.</summary>
        public int Subframes = 1;
        /// <summary>Open shutter as a fraction of the frame interval (0.5 = 180 degrees).</summary>
        public float Shutter = 0.5f;
        public Action<Stage> Setup;
        public Action<Stage, float> Pose;

        public float Seconds => Frames / (float)TrailerRenderer.Fps;
    }

    /// <summary>
    /// Renders the Phone Fixer trailer in edit mode, frame by frame (works headless, nothing is saved):
    /// the 3D frame at 2x supersampling with its post stack (s_####.png), plus the game UI as a separate
    /// layer over black and over white (u_####_b/w.png) for an exact composite, plus a manifest of sound
    /// cues and tracked screen points. ArtSource/Trailer/edit_trailer.py cuts it to the soundtrack.
    /// Batch:
    ///   -executeMethod PhoneRepair.EditorTools.Trailer.TrailerRenderer.RenderBatch -trailerOut &lt;dir&gt;
    ///   [-shots a,b,c] [-preview 0,0.5,1] [-animatic 6]
    /// </summary>
    public static class TrailerRenderer
    {
        public const int Fps = 60;
        public const float Bpm = 100f;
        public const int Width = 1920;
        public const int Height = 1080;
        private const int Supersample = 2;
        private const int MaxPendingWrites = 16;

        private static RenderPipelineAsset projectPipeline;
        private static readonly List<Task> pending = new();
        private static readonly SemaphoreSlim writeSlots = new(MaxPendingWrites);

        public static int Beats(float beats) => Mathf.RoundToInt(beats * 60f / Bpm * Fps);
        public static int Bars(float bars) => Beats(bars * 4f);

        [MenuItem("Tools/Phone Fixer/Trailer/Render Previews", priority = 60)]
        public static void PreviewFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Run(Path.GetFullPath("Marketing/Trailer/Frames"), null, new[] { 0f, 0.5f, 1f }, 0);
            }
        }

        [MenuItem("Tools/Phone Fixer/Trailer/Render All Frames", priority = 61)]
        public static void RenderFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Run(Path.GetFullPath("Marketing/Trailer/Frames"), null, null, 0);
            }
        }

        public static void RenderBatch()
        {
            try
            {
                string output = GetArgument("-trailerOut") ?? Path.GetFullPath("Marketing/Trailer/Frames");
                string[] only = GetArgument("-shots")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                float[] samples = GetArgument("-preview")?.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                int animatic = int.TryParse(GetArgument("-animatic"), out int every) ? every : 0;
                Run(output, only, samples, animatic);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <param name="previewSamples">Shot fractions to render as quick composited stills (null = full render).</param>
        /// <param name="animaticEvery">If &gt; 0, every Nth frame as a quick composited low-res image.</param>
        private static void Run(string output, string[] only, float[] previewSamples, int animaticEvery)
        {
            List<Shot> shots = TrailerShots.All();
            Directory.CreateDirectory(output);
            WriteTimeline(output, shots);
            projectPipeline = QualitySettings.renderPipeline;
            var clock = Stopwatch.StartNew();
            try
            {
                foreach (Shot shot in shots)
                {
                    if (only != null && !only.Contains(shot.Name))
                    {
                        continue;
                    }

                    var shotClock = Stopwatch.StartNew();
                    if (previewSamples != null)
                    {
                        RenderPreview(shot, Path.Combine(output, "preview"), previewSamples);
                    }
                    else if (animaticEvery > 0)
                    {
                        RenderAnimatic(shot, Path.Combine(output, "animatic", shot.Name), animaticEvery);
                    }
                    else
                    {
                        RenderShot(shot, Path.Combine(output, shot.Name));
                    }

                    Debug.Log($"[Trailer] {shot.Name}: {shotClock.Elapsed.TotalSeconds:F1} s");
                }
            }
            finally
            {
                Task.WaitAll(pending.ToArray());
                pending.Clear();
                QualitySettings.renderPipeline = projectPipeline;
            }

            Debug.Log($"[Trailer] Done in {clock.Elapsed.TotalMinutes:F1} min -> {output}");
        }

        // ---- full render ------------------------------------------------------------------------------------

        private static void RenderShot(Shot shot, string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            using Stage stage = Begin(shot);
            var uiFrames = new List<int>();
            for (int frame = 0; frame < shot.Frames; frame++)
            {
                byte[] pixels = RenderFrame(shot, stage, frame, Width, Height, Supersample);
                Write(Path.Combine(folder, $"s_{frame:D4}.png"), pixels, Width, Height);
                if (stage.UiVisible)
                {
                    (byte[] overBlack, byte[] overWhite) = RenderUi(stage, Width, Height);
                    Write(Path.Combine(folder, $"u_{frame:D4}_b.png"), overBlack, Width, Height);
                    Write(Path.Combine(folder, $"u_{frame:D4}_w.png"), overWhite, Width, Height);
                    uiFrames.Add(frame);
                }
            }

            WriteManifest(Path.Combine(folder, "manifest.json"), shot, stage, uiFrames);
        }

        private static Stage Begin(Shot shot)
        {
            Stage stage = Stage.Open(shot.Scene, projectPipeline);
            shot.Setup?.Invoke(stage);
            // Warm-up: the first render after loading a scene has no colour-grading LUT yet.
            stage.Time = 0f;
            stage.MainPass = false;
            shot.Pose?.Invoke(stage, 0f);
            RenderScene(stage.Camera, 320, 180, 1);
            stage.MainPass = true;
            stage.Cues.Clear();
            stage.Tracks.Clear();
            return stage;
        }

        /// <summary>Poses the shot at this frame (with motion-blur subframes) and returns RGB24 pixels, bottom row first.</summary>
        private static byte[] RenderFrame(Shot shot, Stage stage, int frame, int width, int height, int supersample)
        {
            float t = frame / (float)Fps;
            int samples = Mathf.Max(1, shot.Subframes);
            if (samples == 1)
            {
                Pose(shot, stage, frame, t, true);
                return RenderScene(stage.Camera, width, height, supersample);
            }

            // Subframes spread over the open shutter, centred on the frame time; the main pose goes last so
            // the scene is left posed at t for the UI layer.
            var sum = new int[width * height * 3];
            for (int s = 0; s < samples; s++)
            {
                float offset = ((s + 0.5f) / samples - 0.5f) * shot.Shutter / Fps;
                Pose(shot, stage, frame, Mathf.Max(0f, t + offset), false);
                byte[] image = RenderScene(stage.Camera, width, height, supersample);
                for (int i = 0; i < sum.Length; i++)
                {
                    sum[i] += image[i];
                }
            }

            var result = new byte[sum.Length];
            for (int i = 0; i < sum.Length; i++)
            {
                result[i] = (byte)((sum[i] + samples / 2) / samples);
            }

            Pose(shot, stage, frame, t, true);
            return result;
        }

        private static void Pose(Shot shot, Stage stage, int frame, float t, bool main)
        {
            stage.Frame = frame;
            stage.Time = t;
            stage.MainPass = main;
            shot.Pose?.Invoke(stage, t);
        }

        // ---- previews ----------------------------------------------------------------------------------------

        private static void RenderPreview(Shot shot, string folder, float[] fractions)
        {
            Directory.CreateDirectory(folder);
            using Stage stage = Begin(shot);
            foreach (float fraction in fractions)
            {
                int frame = Mathf.Clamp(Mathf.RoundToInt(fraction * (shot.Frames - 1)), 0, shot.Frames - 1);
                byte[] image = Composited(shot, stage, frame, Width / 2, Height / 2);
                Write(Path.Combine(folder, $"{shot.Name}_{frame:D4}.png"), image, Width / 2, Height / 2);
            }
        }

        private static void RenderAnimatic(Shot shot, string folder, int every)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            using Stage stage = Begin(shot);
            for (int frame = 0; frame < shot.Frames; frame += every)
            {
                byte[] image = Composited(shot, stage, frame, Width / 3, Height / 3);
                Write(Path.Combine(folder, $"a_{frame:D4}.png"), image, Width / 3, Height / 3);
            }

            WriteManifest(Path.Combine(folder, "manifest.json"), shot, stage, new List<int>());
        }

        /// <summary>Scene + UI in one image (previews only; the final edit composites in Python).</summary>
        private static byte[] Composited(Shot shot, Stage stage, int frame, int width, int height)
        {
            byte[] scene = RenderFrame(shot, stage, frame, width, height, 1);
            if (!stage.UiVisible)
            {
                return scene;
            }

            (byte[] black, byte[] white) = RenderUi(stage, width, height);
            var toLinear = new float[256];
            for (int i = 0; i < 256; i++)
            {
                toLinear[i] = Mathf.GammaToLinearSpace(i / 255f);
            }

            for (int i = 0; i < scene.Length; i += 3)
            {
                float keep = Mathf.Clamp01((toLinear[white[i]] - toLinear[black[i]] + toLinear[white[i + 1]] - toLinear[black[i + 1]] +
                                            toLinear[white[i + 2]] - toLinear[black[i + 2]]) / 3f);
                for (int c = 0; c < 3; c++)
                {
                    float v = toLinear[black[i + c]] + toLinear[scene[i + c]] * keep;
                    scene[i + c] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(v)) * 255f), 0, 255);
                }
            }

            return scene;
        }

        // ---- rendering -----------------------------------------------------------------------------------------

        private static byte[] RenderScene(Camera camera, int width, int height, int supersample)
        {
            var descriptor = new RenderTextureDescriptor(width * supersample, height * supersample, RenderTextureFormat.ARGB32, 24)
            {
                sRGB = true,
                msaaSamples = 1,
            };
            RenderTexture big = RenderTexture.GetTemporary(descriptor);
            RenderTexture small = null;
            try
            {
                Submit(camera, big);
                RenderTexture source = big;
                if (supersample > 1)
                {
                    // Exactly half size: bilinear sampling at the new pixel centres averages each 2x2 block.
                    small = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0) { sRGB = true });
                    big.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(big, small);
                    source = small;
                }

                return Read(source, width, height);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(big);
                if (small != null)
                {
                    RenderTexture.ReleaseTemporary(small);
                }
            }
        }

        private static (byte[] overBlack, byte[] overWhite) RenderUi(Stage stage, int width, int height)
        {
            stage.RefreshUi();
            RenderTexture target = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { sRGB = true });
            try
            {
                stage.UiCamera.backgroundColor = Color.black;
                Submit(stage.UiCamera, target);
                byte[] black = Read(target, width, height);
                stage.UiCamera.backgroundColor = Color.white;
                Submit(stage.UiCamera, target);
                byte[] white = Read(target, width, height);
                return (black, white);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void Submit(Camera camera, RenderTexture target)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = null;
            }
        }

        private static byte[] Read(RenderTexture source, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                return texture.GetRawTextureData<byte>().ToArray();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>PNG encoding and writing on worker threads (EncodeArrayToPNG is thread safe).</summary>
        private static void Write(string path, byte[] rgb, int width, int height)
        {
            writeSlots.Wait();
            pending.RemoveAll(t => t.IsCompleted);
            pending.Add(Task.Run(() =>
            {
                try
                {
                    byte[] png = ImageConversion.EncodeArrayToPNG(rgb, GraphicsFormat.R8G8B8_SRGB, (uint)width, (uint)height);
                    File.WriteAllBytes(path, png);
                }
                finally
                {
                    writeSlots.Release();
                }
            }));
        }

        // ---- manifests -------------------------------------------------------------------------------------------

        private static void WriteTimeline(string output, List<Shot> shots)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"fps\": ").Append(Fps).Append(",\n  \"bpm\": ").Append(F(Bpm)).Append(",\n  \"shots\": [\n");
            int start = 0;
            for (int i = 0; i < shots.Count; i++)
            {
                Shot s = shots[i];
                sb.Append("    {\"name\": \"").Append(s.Name).Append("\", \"start\": ").Append(start).Append(", \"frames\": ").Append(s.Frames).Append('}');
                sb.Append(i < shots.Count - 1 ? ",\n" : "\n");
                start += s.Frames;
            }

            sb.Append("  ],\n  \"frames\": ").Append(start).Append("\n}\n");
            File.WriteAllText(Path.Combine(output, "timeline.json"), sb.ToString());
        }

        private static void WriteManifest(string path, Shot shot, Stage stage, List<int> uiFrames)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"name\": \"").Append(shot.Name).Append("\",\n  \"frames\": ").Append(shot.Frames).Append(",\n  \"fps\": ").Append(Fps);
            sb.Append(",\n  \"ui\": [").Append(string.Join(",", uiFrames)).Append("],\n  \"cues\": [");
            for (int i = 0; i < stage.Cues.Count; i++)
            {
                Stage.CueRecord c = stage.Cues[i];
                sb.Append(i == 0 ? "\n    " : ",\n    ");
                sb.Append("{\"frame\": ").Append(c.Frame).Append(", \"offset\": ").Append(F(c.Offset)).Append(", \"sound\": \"").Append(c.Sound)
                    .Append("\", \"gain\": ").Append(F(c.Gain)).Append(", \"pitch\": ").Append(F(c.Pitch)).Append(", \"pan\": ").Append(F(c.Pan)).Append('}');
            }

            sb.Append("\n  ],\n  \"tracks\": {");
            int n = 0;
            foreach (KeyValuePair<string, List<Vector4>> track in stage.Tracks)
            {
                sb.Append(n++ == 0 ? "\n    \"" : ",\n    \"").Append(track.Key).Append("\": [");
                sb.Append(string.Join(", ", track.Value.Select(v => $"[{(int)v.x}, {F(v.y)}, {F(v.z)}, {F(v.w)}]")));
                sb.Append(']');
            }

            sb.Append("\n  }\n}\n");
            File.WriteAllText(path, sb.ToString());
        }

        private static string F(float value) => value.ToString("0.#####", CultureInfo.InvariantCulture);

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}

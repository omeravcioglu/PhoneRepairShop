using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools.Trailer
{
    /// <summary>Easing curves (t in 0..1).</summary>
    internal static class Ease
    {
        public static float Clamp(float t) => Mathf.Clamp01(t);
        public static float Window(float t, float from, float to) => Mathf.Clamp01((t - from) / Mathf.Max(1e-5f, to - from));
        public static float Smooth(float t) => (t = Clamp(t)) * t * (3f - 2f * t);
        public static float Smoother(float t) => (t = Clamp(t)) * t * t * (t * (t * 6f - 15f) + 10f);
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * Clamp(t)) - 1f) * 0.5f;
        public static float InCubic(float t) => (t = Clamp(t)) * t * t;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - Clamp(t), 3f);
        public static float InOutCubic(float t) => (t = Clamp(t)) < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        public static float OutQuint(float t) => 1f - Mathf.Pow(1f - Clamp(t), 5f);
        public static float InQuint(float t) => Mathf.Pow(Clamp(t), 5f);
        public static float InOutQuint(float t) => (t = Clamp(t)) < 0.5f ? 16f * Mathf.Pow(t, 5f) : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f;
        public static float InExpo(float t) => (t = Clamp(t)) <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Clamp(t) - 1f;
            return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
        }

        /// <summary>Damped spring settling from 0 to 1 (bouncy landing).</summary>
        public static float OutSpring(float t, float bounces = 2.5f, float damping = 5f)
        {
            t = Clamp(t);
            return 1f - Mathf.Exp(-damping * t) * Mathf.Cos(bounces * Mathf.PI * 2f * t * 0.5f);
        }

        /// <summary>0 -> 1 -> 0 hump.</summary>
        public static float Hump(float t) => Mathf.Sin(Mathf.PI * Clamp(t));
    }

    /// <summary>A camera keyframe: where it is, what it looks at, its vertical field of view and roll.</summary>
    internal readonly struct CamKey
    {
        public readonly Vector3 Position;
        public readonly Vector3 Target;
        public readonly float Fov;
        public readonly float Roll;

        public CamKey(Vector3 position, Vector3 target, float fov, float roll = 0f)
        {
            Position = position;
            Target = target;
            Fov = fov;
            Roll = roll;
        }

        public static CamKey Orbit(Vector3 pivot, float yawDegrees, float pitchDegrees, float distance, float fov, float roll = 0f)
        {
            Quaternion q = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            return new CamKey(pivot - q * Vector3.forward * distance, pivot, fov, roll);
        }

        public static CamKey Lerp(CamKey a, CamKey b, float t) =>
            new(Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.Target, b.Target, t), Mathf.Lerp(a.Fov, b.Fov, t), Mathf.Lerp(a.Roll, b.Roll, t));

        /// <summary>Lerp around the pivot: positions travel on an arc (yaw/pitch/distance interpolated) instead of a chord.</summary>
        public static CamKey Arc(CamKey a, CamKey b, Vector3 pivot, float t)
        {
            Vector3 da = a.Position - pivot, db = b.Position - pivot;
            float ra = da.magnitude, rb = db.magnitude;
            Vector3 dir = Vector3.Slerp(da / ra, db / rb, t);
            return new CamKey(pivot + dir * Mathf.Lerp(ra, rb, t), Vector3.Lerp(a.Target, b.Target, t), Mathf.Lerp(a.Fov, b.Fov, t),
                Mathf.Lerp(a.Roll, b.Roll, t));
        }

        /// <summary>Centripetal-free uniform Catmull-Rom through the keys, u in 0..1 across all segments.</summary>
        public static CamKey Path(float u, params CamKey[] keys)
        {
            if (keys.Length == 1)
            {
                return keys[0];
            }

            float x = Mathf.Clamp01(u) * (keys.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(x), keys.Length - 2);
            float t = x - i;
            CamKey k0 = keys[Mathf.Max(0, i - 1)], k1 = keys[i], k2 = keys[i + 1], k3 = keys[Mathf.Min(keys.Length - 1, i + 2)];
            return new CamKey(CatmullRom(k0.Position, k1.Position, k2.Position, k3.Position, t),
                CatmullRom(k0.Target, k1.Target, k2.Target, k3.Target, t),
                Mathf.Lerp(k1.Fov, k2.Fov, Ease.Smooth(t)), Mathf.Lerp(k1.Roll, k2.Roll, Ease.Smooth(t)));
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        public void Apply(Stage s) => s.Aim(Position, Target, Fov, Roll);
    }

    internal static class Arc3
    {
        /// <summary>Cubic Bezier flight like the game's PartMover: leave along <paramref name="exit"/>, arrive from above.</summary>
        public static Vector3 Flight(Vector3 from, Vector3 to, Vector3 exit, Vector3 arriveFrom, float height, float t)
        {
            height = Mathf.Max(height, Vector3.Distance(from, to) * 0.3f);
            Vector3 c1 = from + exit.normalized * height;
            Vector3 c2 = to + arriveFrom.normalized * (height * 0.6f);
            float u = 1f - t;
            return u * u * u * from + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * to;
        }
    }

    /// <summary>The repair bench scene: phone, parts, landing spots and tool props.</summary>
    internal sealed class BenchRig
    {
        public readonly Stage S;
        public readonly PhoneDevice Device;
        public readonly Transform Pivot;
        public readonly PartsArea Area;
        public readonly Vector3 PivotRestPosition;
        public readonly Quaternion PivotRestRotation;
        private readonly Dictionary<string, Pose> landings = new();
        private readonly Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> rest = new();

        public BenchRig(Stage s)
        {
            S = s;
            Device = s.Phone();
            Pivot = Device.transform.parent;
            Area = s.PartsArea();
            PivotRestPosition = Pivot.position;
            PivotRestRotation = Pivot.rotation;
            _ = Device.Parts; // index parts and remember their installed poses before anything moves
            foreach (PhonePart part in Device.Parts)
            {
                rest[part.SlotId] = (part.transform.localPosition, part.transform.localRotation, part.transform.localScale);
            }
        }

        public PhonePart P(string id) => Stage.Part(Device, id);

        public Transform T(string id) => P(id).transform;

        public static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        /// <summary>Flat on the mat, screen up (0) or flipped onto its screen (1), turned over the long axis.</summary>
        public void Flip(float amount, float lift = 0f)
        {
            Pivot.SetPositionAndRotation(PivotRestPosition + Vector3.up * lift,
                Quaternion.AngleAxis(180f * amount, Vector3.forward) * PivotRestRotation);
        }

        /// <summary>Puts every part back where it was installed (local pose), scale 1.</summary>
        public void Reassemble()
        {
            foreach (PhonePart part in Device.Parts)
            {
                (Vector3 p, Quaternion r, Vector3 sc) = rest[part.SlotId];
                part.transform.SetLocalPositionAndRotation(p, r);
                part.transform.localScale = sc;
            }
        }

        public void Installed(string id)
        {
            (Vector3 p, Quaternion r, Vector3 sc) = rest[id];
            T(id).SetLocalPositionAndRotation(p, r);
            T(id).localScale = sc;
        }

        public Pose InstalledWorld(string id) => Stage.InstalledWorld(P(id));

        /// <summary>Where the game would lay this part down on the mat (reserved once, for the phone's pose at that moment).</summary>
        public Pose Landing(string id)
        {
            if (landings.TryGetValue(id, out Pose pose))
            {
                return pose;
            }

            PhonePart part = P(id);
            if (!Area.TryReserve(part, out PartsArea.Slot slot, out Pose local))
            {
                throw new InvalidOperationException($"No room on the mat for {id}.");
            }

            pose = new Pose(slot.Anchor.TransformPoint(local.position), slot.Anchor.rotation * local.rotation);
            landings[id] = pose;
            return pose;
        }

        public void Place(string id, Pose world) => T(id).SetPositionAndRotation(world.position, world.rotation);

        /// <summary>A part flying from its installed pose to its landing spot (u 0..1), spinning about its up axis.</summary>
        public void FlyOut(string id, float u, float spinTurns = 0f, float arc = 0.06f)
        {
            Pose from = InstalledWorld(id);
            Pose to = Landing(id);
            float e = Ease.InOutCubic(u);
            Vector3 exit = from.rotation * (P(id).Definition != null ? P(id).Definition.RemovalDirection : Vector3.up);
            if (Vector3.Dot(exit, Vector3.up) < 0.2f)
            {
                exit = Vector3.up;
            }

            Vector3 position = Arc3.Flight(from.position, to.position, exit, Vector3.up, arc, e);
            Quaternion rotation = Quaternion.Slerp(from.rotation, to.rotation, e) * Quaternion.AngleAxis(360f * spinTurns * e, Vector3.up);
            T(id).SetPositionAndRotation(position, rotation);
        }

        /// <summary>Screw loosened by <paramref name="looseness"/> (0..1): turned counter-clockwise and raised along its axis.</summary>
        public void Unscrew(string id, float looseness, float turns, float rise)
        {
            (Vector3 p, Quaternion r, _) = rest[id];
            Vector3 axis = r * Vector3.up;
            T(id).SetLocalPositionAndRotation(p + axis * (rise * looseness), r * Quaternion.AngleAxis(-360f * turns * looseness, Vector3.up));
        }

        /// <summary>World point on top of a screw head (where a screwdriver tip sits).</summary>
        public Vector3 ScrewHead(string id)
        {
            Transform t = T(id);
            float head = t.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null ? filter.sharedMesh.bounds.max.y : 0f;
            return t.TransformPoint(new Vector3(0f, head, 0f));
        }

        public void Faults(params string[] states)
        {
            var visuals = Device.GetComponent<PhoneFaultVisuals>();
            foreach (string state in PhoneFaultVisuals.KnownStates)
            {
                visuals.SetState(state, states.Contains(state));
            }
        }

        public Transform Fault(string state) => EditorAssetUtil.FindDeep(Device.transform, state);

        /// <summary>A tool model standing on its tip at the origin, pointing up; the caddy copy hides.</summary>
        public Transform Tool(string name, float scale)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.ToolFbx(name))
                        ?? throw new InvalidOperationException($"No tool model {name}.");
            GameObject go = S.Spawn(model, $"Trailer_{name}");
            go.transform.localScale = Vector3.one * scale;
            if (S.TryFind($"Caddy_{name}", out Transform caddy))
            {
                caddy.gameObject.SetActive(false);
            }

            return go.transform;
        }

        /// <summary>Tool tip at <paramref name="tip"/>, its long axis along <paramref name="axis"/>, facing the camera side.</summary>
        public void Hold(Transform tool, Vector3 tip, Vector3 axis, float spinDegrees = 0f)
        {
            Vector3 forward = Vector3.ProjectOnPlane(S.Camera.transform.position - tip, axis);
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, axis);
            }

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, axis.normalized) * Quaternion.AngleAxis(spinDegrees, Vector3.up);
            tool.SetPositionAndRotation(tip, rotation);
        }

        /// <summary>World bounds of a part's renderers.</summary>
        public Bounds BoundsOf(string id) => EditorAssetUtil.RendererBounds(T(id).gameObject);

        /// <summary>A glowing quad lying on the display (boot screen etc.), fading with <see cref="SetScreen"/>.</summary>
        public Renderer CreateScreen(string texturePath, float boost)
        {
            Transform display = EditorAssetUtil.FindDeep(T("Screen"), "Screen_Display")
                                ?? throw new InvalidOperationException("No Screen_Display.");
            Texture2D texture = TrailerAssets.Texture(texturePath);
            GameObject quad = S.Primitive(PrimitiveType.Quad, "Trailer_ScreenImage", S.Unlit(texture, boost));
            quad.transform.SetParent(display, false);
            Mesh mesh = display.GetComponent<MeshFilter>().sharedMesh;
            Bounds b = mesh.bounds; // local: the display lies in its own XZ plane, facing +Y
            quad.transform.localPosition = new Vector3(b.center.x, b.max.y + 0.00025f, b.center.z);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(b.size.x, b.size.z, 1f);
            return quad.GetComponent<Renderer>();
        }

        public static void SetScreen(Renderer screen, Texture2D texture, float brightness)
        {
            Material m = screen.sharedMaterial;
            if (texture != null)
            {
                m.SetTexture("_BaseMap", texture);
            }

            m.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1f));
            screen.enabled = brightness > 0.001f;
        }
    }

    /// <summary>Trailer-only assets that live outside the Unity project (ArtSource/Trailer/textures).</summary>
    internal static class TrailerAssets
    {
        private static readonly Dictionary<string, Texture2D> textures = new();

        public static string Folder => Path.GetFullPath("ArtSource/Trailer/textures");

        public static Texture2D Texture(string fileName)
        {
            if (textures.TryGetValue(fileName, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            string path = Path.Combine(Folder, fileName);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false) { hideFlags = HideFlags.DontSave, name = fileName };
            if (!File.Exists(path) || !texture.LoadImage(File.ReadAllBytes(path)))
            {
                throw new FileNotFoundException($"Trailer texture missing: {path} (run ArtSource/Trailer/make_textures.py).");
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 8;
            textures[fileName] = texture;
            return texture;
        }
    }

    /// <summary>The shop scene: customers, door, stations.</summary>
    internal sealed class ShopRig
    {
        public readonly Stage S;
        public readonly GameObject Fumi, Ren, Mio, Kanemochi, Sasaki;
        private readonly Transform door;
        private readonly Vector3 doorRest;
        private readonly Transform cat;
        private readonly Vector3 catRest;

        public static readonly Vector3 CounterSpot = new(1.82f, 0f, 2.3f);
        public static readonly Vector3 QueueSpot = new(1.5f, 0f, 3.05f);
        public static readonly Vector3 DioramaTarget = PhoneFixerScenes.DioramaTarget;
        public static readonly Vector3 DioramaPosition = PhoneFixerScenes.DioramaPosition;

        public ShopRig(Stage s)
        {
            S = s;
            Fumi = s.Customer("Fumi");
            Ren = s.Customer("Ren");
            Mio = s.Customer("Mio");
            Kanemochi = s.Customer("Kanemochi");
            Sasaki = s.Customer("Sasaki");
            door = s.Find("Shop_DoorPanel_2");
            doorRest = door.position;
            cat = s.Find("Deco_ShopCat");
            catRest = cat.localScale;
        }

        public IEnumerable<GameObject> Everyone => new[] { Fumi, Ren, Mio, Kanemochi, Sasaki };

        public static void Place(GameObject actor, Vector3 position, float yaw) =>
            actor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        /// <summary>Everyone in their default mood loop, phase-shifted so nobody bobs in sync.</summary>
        public void Life(float t, params GameObject[] skip)
        {
            void Loop(GameObject actor, string clip, float offset)
            {
                if (!skip.Contains(actor) && actor.activeInHierarchy)
                {
                    S.Animate(actor, clip, t + offset);
                }
            }

            Loop(Fumi, "Talk", 0.31f);
            Loop(Ren, "Impatient", 0.13f);
            Loop(Mio, "Idle", 0.77f);
            Loop(Kanemochi, "Idle", 1.29f);
            Loop(Sasaki, "Impatient", 0.52f);
        }

        /// <summary>Sliding door: 0 closed, 1 open (the outer panel slides over the inner one).</summary>
        public void Door(float open) => door.position = doorRest + new Vector3(0f, 0f, -0.48f * Ease.InOutSine(open));

        /// <summary>The cat's squash-and-stretch (same curve as SquashBounce) plus a slow sleepy breath.</summary>
        public void Cat(float bounceElapsed, float breathe, float t)
        {
            float s = 0f;
            if (bounceElapsed >= 0f && bounceElapsed < 0.5f)
            {
                float k = bounceElapsed / 0.5f;
                s = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 0.2f;
            }

            float b = Mathf.Sin(t * Mathf.PI * 2f / 2.4f) * 0.018f * breathe;
            cat.localScale = new Vector3(catRest.x * (1f + s * 0.5f - b * 0.4f), catRest.y * (1f - s + b), catRest.z * (1f + s * 0.5f - b * 0.4f));
        }

        public Vector3 CatHead => S.BoundsOf("Deco_ShopCat").center + new Vector3(0.12f, 0.06f, 0f);

        /// <summary>Middle of the eyes, after posing.</summary>
        public Vector3 Eyes(GameObject actor) => (S.Bone(actor, "Eye.L").position + S.Bone(actor, "Eye.R").position) * 0.5f;

        public Vector3 HeadTop(GameObject actor) => Eyes(actor) + Vector3.up * (0.34f * actor.transform.lossyScale.y);

        /// <summary>
        /// Walks a polyline at <paramref name="speed"/> m/s for <paramref name="time"/> seconds, facing a point a little
        /// ahead so corners turn smoothly. Returns the metres walked (drives the walk cycle).
        /// </summary>
        public static float WalkAlong(GameObject actor, Vector3[] path, float speed, float time, out bool arrived)
        {
            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                total += Vector3.Distance(path[i], path[i + 1]);
            }

            float walked = Mathf.Clamp(time * speed, 0f, total);
            arrived = time * speed >= total;
            Vector3 position = PointAt(path, walked);
            Vector3 ahead = PointAt(path, Mathf.Min(total, walked + 0.3f)) - PointAt(path, Mathf.Max(0f, walked - 0.05f));
            ahead.y = 0f;
            Quaternion facing = ahead.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(ahead) : actor.transform.rotation;
            actor.transform.SetPositionAndRotation(position, facing);
            return walked;
        }

        private static Vector3 PointAt(Vector3[] path, float distance)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                float length = Vector3.Distance(path[i], path[i + 1]);
                if (distance <= length || i == path.Length - 2)
                {
                    return Vector3.Lerp(path[i], path[i + 1], length > 0f ? Mathf.Clamp01(distance / length) : 1f);
                }

                distance -= length;
            }

            return path[^1];
        }

        public ShopStation Station(string sceneToLoad) =>
            Object.FindObjectsByType<ShopStation>(FindObjectsSortMode.None).FirstOrDefault(st => st.SceneToLoad == sceneToLoad);
    }

    internal static class Reflect
    {
        public static void Call(Object target, string method, params object[] args)
        {
            if (target == null)
            {
                return;
            }

            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                              ?? throw new MissingMethodException(target.GetType().Name, method);
            info.Invoke(target, args);
        }

        public static T Field<T>(Object target, string field) where T : Object =>
            (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    }
}

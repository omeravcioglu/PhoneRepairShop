using System;
using System.Collections.Generic;
using System.Linq;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools.Trailer
{
    /// <summary>
    /// One opened scene, staged for one trailer shot. Shots pose everything for a time t (seconds since
    /// the shot started) through this: camera, depth of field, customers (clip sampling and blending),
    /// game UI, plus sound cues and tracked screen points that the editing script (ArtSource/Trailer)
    /// turns into sound effects and overlays. Nothing is ever saved: the next shot reopens its scene.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        public const float CanvasWidth = 1920f;
        public const float CanvasHeight = 1080f;
        private static readonly Vector3 UiRigOrigin = new(5000f, 5000f, 5000f);

        private readonly Dictionary<string, AnimationClip> clips = new();
        private readonly Dictionary<GameObject, ActorRig> rigs = new();
        private readonly Dictionary<string, Transform> found = new();
        private readonly List<GameObject> spawned = new();
        private Volume volume;
        private DepthOfField depthOfField;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private GameObject uiCameraObject;
        private Graphic[] uiGraphics = Array.Empty<Graphic>();
        private UniversalRenderPipelineAsset pipelineCopy;
        private RenderPipelineAsset pipelineBefore;

        public Camera Camera { get; }
        public Camera UiCamera { get; private set; }
        public Scene Scene { get; }

        /// <summary>Seconds since the shot started.</summary>
        public float Time { get; internal set; }

        public int Frame { get; internal set; }
        public float FrameSeconds => 1f / TrailerRenderer.Fps;

        /// <summary>False while rendering motion-blur subframes: cues and tracks are only recorded on the main pass.</summary>
        internal bool MainPass { get; set; } = true;

        /// <summary>When true after a frame is posed, the game UI is rendered for that frame as a separate layer.</summary>
        public bool UiVisible { get; set; }

        internal readonly List<CueRecord> Cues = new();
        internal readonly SortedDictionary<string, List<Vector4>> Tracks = new(StringComparer.Ordinal);

        internal struct CueRecord
        {
            public int Frame;
            public float Offset;
            public string Sound;
            public float Gain;
            public float Pitch;
            public float Pan;
        }

        private Stage(Scene scene, Camera camera)
        {
            Scene = scene;
            Camera = camera;
        }

        public static Stage Open(string scenePath, RenderPipelineAsset fallbackPipeline)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var pipeline = Object.FindFirstObjectByType<PipelineOverride>();
            QualitySettings.renderPipeline = pipeline != null && pipeline.Pipeline != null ? pipeline.Pipeline : fallbackPipeline;
            DynamicGI.UpdateEnvironment();
            Camera camera = Camera.main ?? throw new InvalidOperationException($"No MainCamera in {scenePath}.");
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            camera.aspect = CanvasWidth / CanvasHeight;
            if (camera.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None; // MSAA from the pipeline asset, plus 2x supersampling
            }

            var stage = new Stage(scene, camera);
            stage.LoadClips();
            stage.CreateVolume();
            return stage;
        }

        public void Dispose()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
            if (volume != null && volume.sharedProfile != null)
            {
                Object.DestroyImmediate(volume.sharedProfile);
            }

            if (pipelineCopy != null)
            {
                QualitySettings.renderPipeline = pipelineBefore;
                Object.DestroyImmediate(pipelineCopy);
                pipelineCopy = null;
            }
        }

        // ---- camera -------------------------------------------------------------------------------------

        /// <summary>Puts the camera at <paramref name="position"/> looking at <paramref name="target"/>.</summary>
        public void Aim(Vector3 position, Vector3 target, float fieldOfView, float rollDegrees = 0f)
        {
            Vector3 forward = target - position;
            if (forward.sqrMagnitude < 1e-10f)
            {
                forward = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.AngleAxis(rollDegrees, Vector3.forward);
            Camera.transform.SetPositionAndRotation(position, rotation);
            Camera.fieldOfView = fieldOfView;
            Camera.aspect = CanvasWidth / CanvasHeight;
        }

        /// <summary>Tiny organic camera sway (degrees), so locked-off shots feel hand-held but steady.</summary>
        public void Sway(float degrees, float speed = 0.35f, float seed = 0f)
        {
            float t = Time * speed;
            float yaw = (Mathf.PerlinNoise(t, seed + 1.7f) - 0.5f) * 2f * degrees;
            float pitch = (Mathf.PerlinNoise(seed + 5.3f, t) - 0.5f) * 2f * degrees;
            Camera.transform.rotation *= Quaternion.Euler(pitch, yaw, 0f);
        }

        /// <summary>Bokeh depth of field focused on a world point. Focal length follows the field of view (full-frame sensor).</summary>
        public void Focus(Vector3 worldPoint, float fNumber, float focalScale = 1f)
        {
            float distance = Vector3.Dot(worldPoint - Camera.transform.position, Camera.transform.forward);
            FocusDistance(Mathf.Max(0.05f, distance), fNumber, focalScale);
        }

        public void FocusDistance(float distance, float fNumber, float focalScale = 1f)
        {
            float focal = 12f / Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * focalScale; // 24 mm sensor height
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            depthOfField.focusDistance.Override(distance);
            depthOfField.aperture.Override(Mathf.Clamp(fNumber, 1f, 32f));
            depthOfField.focalLength.Override(Mathf.Clamp(focal, 1f, 300f));
            depthOfField.bladeCount.Override(6);
            depthOfField.bladeCurvature.Override(1f);
        }

        public void NoFocus()
        {
            depthOfField.mode.Override(DepthOfFieldMode.Off);
            depthOfField.active = false;
        }

        /// <summary>Shot-level grade on top of the scene's own volume: exposure (stops) and saturation offsets.</summary>
        public void Grade(float exposure = 0f, float saturation = 0f, float contrast = 0f)
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(exposure);
            colorAdjustments.saturation.Override(saturation);
            colorAdjustments.contrast.Override(contrast);
        }

        /// <summary>
        /// Shorter shadow distance for close-ups (sharper shadows): switches to an in-memory copy of the
        /// active URP asset for the rest of this shot.
        /// </summary>
        public void ShadowDistance(float metres)
        {
            if (pipelineCopy == null)
            {
                if (QualitySettings.renderPipeline is not UniversalRenderPipelineAsset active)
                {
                    return;
                }

                pipelineBefore = active;
                pipelineCopy = Object.Instantiate(active);
                pipelineCopy.hideFlags = HideFlags.DontSave;
                QualitySettings.renderPipeline = pipelineCopy;
            }

            pipelineCopy.shadowDistance = metres;
        }

        public void ExtraVignette(float intensity)
        {
            vignette.active = intensity > 0f;
            vignette.intensity.Override(intensity);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(EditorAssetUtil.Hex("#1C2733"));
        }

        private void CreateVolume()
        {
            var go = new GameObject("TrailerVolume") { hideFlags = HideFlags.DontSave };
            spawned.Add(go);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            depthOfField = profile.Add<DepthOfField>(true);
            depthOfField.mode.Override(DepthOfFieldMode.Off);
            depthOfField.active = false;
            colorAdjustments = profile.Add<ColorAdjustments>(false);
            colorAdjustments.active = false;
            vignette = profile.Add<Vignette>(false);
            vignette.active = false;
            volume.sharedProfile = profile;
        }

        // ---- finding things --------------------------------------------------------------------------------

        /// <summary>First transform with this name anywhere in the scene (inactive included). Cached.</summary>
        public Transform Find(string name)
        {
            if (found.TryGetValue(name, out Transform cached) && cached != null)
            {
                return cached;
            }

            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                Transform t = EditorAssetUtil.FindDeep(root.transform, name);
                if (t != null)
                {
                    found[name] = t;
                    return t;
                }
            }

            throw new InvalidOperationException($"'{name}' not found in {Scene.name}.");
        }

        public bool TryFind(string name, out Transform t)
        {
            try
            {
                t = Find(name);
                return true;
            }
            catch (InvalidOperationException)
            {
                t = null;
                return false;
            }
        }

        public Bounds BoundsOf(string name) => EditorAssetUtil.RendererBounds(Find(name).gameObject);

        public GameObject Customer(string name)
        {
            foreach (CustomerActor actor in Object.FindObjectsByType<CustomerActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (actor.name.Contains($"_{name}", StringComparison.Ordinal))
                {
                    return actor.gameObject;
                }
            }

            throw new InvalidOperationException($"No customer '{name}' in {Scene.name}.");
        }

        public void Hide(params string[] names)
        {
            foreach (string name in names)
            {
                Find(name).gameObject.SetActive(false);
            }
        }

        /// <summary>Remembers a spawned object so it is destroyed with the stage.</summary>
        public T Own<T>(T obj) where T : Object
        {
            GameObject go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go != null)
            {
                spawned.Add(go);
            }

            return obj;
        }

        // ---- animation -------------------------------------------------------------------------------------

        private void LoadClips()
        {
            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(PhoneFixerPaths.BaseCharacterFbx).OfType<AnimationClip>())
            {
                if (!clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    clips.TryAdd(clip.name, clip);
                }
            }
        }

        public float ClipLength(string clip) => Clip(clip).length;

        private AnimationClip Clip(string name) =>
            clips.TryGetValue(name, out AnimationClip clip) ? clip : throw new InvalidOperationException($"No clip '{name}'.");

        private static bool Loops(string clip) => Array.IndexOf(PhoneFixerPaths.LoopingClips, clip) >= 0;

        /// <summary>Poses a customer with one clip. Looping clips wrap, one-shots hold their last pose.</summary>
        public void Animate(GameObject actor, string clip, float time)
        {
            ActorRig rig = Rig(actor);
            Sample(rig, actor, clip, time);
            rig.Bake();
        }

        /// <summary>Cross-fade: pose from clip A blended toward clip B by <paramref name="weight"/> (0 = all A).</summary>
        public void Blend(GameObject actor, string clipA, float timeA, string clipB, float timeB, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (weight <= 0.0001f)
            {
                Animate(actor, clipA, timeA);
                return;
            }

            if (weight >= 0.9999f)
            {
                Animate(actor, clipB, timeB);
                return;
            }

            ActorRig rig = Rig(actor);
            Transform[] list = rig.Bones;
            Sample(rig, actor, clipA, timeA);
            var positions = new Vector3[list.Length];
            var rotations = new Quaternion[list.Length];
            var scales = new Vector3[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                positions[i] = list[i].localPosition;
                rotations[i] = list[i].localRotation;
                scales[i] = list[i].localScale;
            }

            Sample(rig, actor, clipB, timeB);
            for (int i = 0; i < list.Length; i++)
            {
                Transform b = list[i];
                b.SetLocalPositionAndRotation(Vector3.Lerp(positions[i], b.localPosition, weight),
                    Quaternion.Slerp(rotations[i], b.localRotation, weight));
                b.localScale = Vector3.Lerp(scales[i], b.localScale, weight);
            }

            rig.Bake();
        }

        /// <summary>Extra rotation on one bone after a clip was sampled (degrees, bone-local), then re-bakes.</summary>
        public void Nudge(GameObject actor, string bone, Vector3 localEuler)
        {
            ActorRig rig = Rig(actor);
            Transform b = rig.Bones.First(t => t.name == bone);
            b.localRotation *= Quaternion.Euler(localEuler);
            rig.Bake();
        }

        public Transform Bone(GameObject actor, string bone) => Rig(actor).Bones.First(t => t.name == bone);

        private void Sample(ActorRig rig, GameObject actor, string clip, float time)
        {
            AnimationClip c = Clip(clip);
            float t = Loops(clip) ? Mathf.Repeat(time, c.length) : Mathf.Clamp(time, 0f, c.length);
            rig.ResetPose(); // clips only write what they animate: start every sample from the rest pose
            c.SampleAnimation(actor, t);
        }

        private ActorRig Rig(GameObject actor)
        {
            if (!rigs.TryGetValue(actor, out ActorRig rig))
            {
                rig = new ActorRig(actor);
                rigs[actor] = rig;
                Own(rig.BakedObject);
            }

            return rig;
        }

        /// <summary>
        /// Skinned meshes are not re-skinned between renders inside one editor tick, so a posed customer is
        /// baked into a plain mesh every time it is posed (CPU skinning), and the skinned renderer stays off.
        /// </summary>
        private sealed class ActorRig
        {
            public readonly Transform[] Bones;
            public readonly GameObject BakedObject;
            private readonly SkinnedMeshRenderer skin;
            private readonly Mesh baked;
            private readonly Vector3[] restPositions;
            private readonly Quaternion[] restRotations;
            private readonly Vector3[] restScales;

            public ActorRig(GameObject actor)
            {
                skin = actor.GetComponentInChildren<SkinnedMeshRenderer>(true)
                       ?? throw new InvalidOperationException($"{actor.name} has no SkinnedMeshRenderer.");
                Bones = actor.GetComponentsInChildren<Transform>(true).Where(t => t != actor.transform && t != skin.transform).ToArray();
                restPositions = Bones.Select(b => b.localPosition).ToArray();
                restRotations = Bones.Select(b => b.localRotation).ToArray();
                restScales = Bones.Select(b => b.localScale).ToArray();

                baked = new Mesh { name = $"{actor.name}_Baked", hideFlags = HideFlags.DontSave };
                BakedObject = new GameObject($"{actor.name}_Baked") { hideFlags = HideFlags.DontSave, layer = skin.gameObject.layer };
                BakedObject.transform.SetParent(skin.transform, false);
                BakedObject.AddComponent<MeshFilter>().sharedMesh = baked;
                var renderer = BakedObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = skin.sharedMaterials;
                renderer.shadowCastingMode = skin.shadowCastingMode;
                renderer.receiveShadows = skin.receiveShadows;
                renderer.probeAnchor = skin.probeAnchor;
                skin.enabled = false;
                Bake();
            }

            public void ResetPose()
            {
                for (int i = 0; i < Bones.Length; i++)
                {
                    Bones[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
                    Bones[i].localScale = restScales[i];
                }
            }

            public void Bake()
            {
                // useScale false: vertices in the renderer's unscaled local space; the child inherits its scale.
                skin.BakeMesh(baked, false);
                baked.RecalculateBounds();
            }
        }

        // ---- cues and tracking ----------------------------------------------------------------------------

        /// <summary>
        /// A sound effect at shot time <paramref name="at"/> (recorded once, on the frame that contains it).
        /// Pan is -1 (left) .. 1 (right).
        /// </summary>
        public void Cue(string sound, float at, float gain = 1f, float pitch = 1f, float pan = 0f)
        {
            if (!MainPass || at < Time - 1e-5f || at >= Time + FrameSeconds - 1e-5f)
            {
                return;
            }

            Cues.Add(new CueRecord { Frame = Frame, Offset = Mathf.Max(0f, at - Time), Sound = sound, Gain = gain, Pitch = pitch, Pan = pan });
        }

        /// <summary>Cue panned by where a world point is on screen.</summary>
        public void CueAt(string sound, float at, Vector3 world, float gain = 1f, float pitch = 1f)
        {
            Vector3 v = Camera.WorldToViewportPoint(world);
            Cue(sound, at, gain, pitch, Mathf.Clamp((v.x - 0.5f) * 1.2f, -0.8f, 0.8f));
        }

        /// <summary>Records where a world point is on screen this frame (viewport x, y, depth).</summary>
        public void Track(string id, Vector3 world)
        {
            if (!MainPass)
            {
                return;
            }

            Vector3 v = Camera.WorldToViewportPoint(world);
            if (!Tracks.TryGetValue(id, out List<Vector4> list))
            {
                list = new List<Vector4>();
                Tracks[id] = list;
            }

            list.Add(new Vector4(Frame, v.x, v.y, v.z));
        }

        /// <summary>World point -> anchored position in the 1920x1080 reference canvas (anchors at its centre).</summary>
        public Vector2 CanvasPoint(Vector3 world)
        {
            Vector3 viewport = Camera.WorldToViewportPoint(world);
            return new Vector2((viewport.x - 0.5f) * CanvasWidth, (viewport.y - 0.5f) * CanvasHeight);
        }

        // ---- game UI ----------------------------------------------------------------------------------------

        /// <summary>
        /// Moves every root canvas into world space in front of an orthographic UI camera (as the screenshot
        /// tool does), so the UI renders headless and can be layered over the 3D frame.
        /// </summary>
        public void PrepareUi()
        {
            if (uiCameraObject != null)
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            uiCameraObject = new GameObject("TrailerUiCamera") { hideFlags = HideFlags.DontSave };
            spawned.Add(uiCameraObject);
            uiCameraObject.transform.SetPositionAndRotation(UiRigOrigin + new Vector3(0f, 0f, -10f), Quaternion.identity);
            UiCamera = uiCameraObject.AddComponent<Camera>();
            UiCamera.enabled = false;
            UiCamera.orthographic = true;
            UiCamera.orthographicSize = CanvasHeight * 0.5f;
            UiCamera.aspect = CanvasWidth / CanvasHeight;
            UiCamera.nearClipPlane = 0.1f;
            UiCamera.farClipPlane = 100f;
            UiCamera.clearFlags = CameraClearFlags.SolidColor;
            UiCamera.backgroundColor = Color.black;
            UiCamera.cullingMask = 1 << uiLayer;
            UiCamera.allowHDR = false;
            UiCamera.allowMSAA = false;
            var data = uiCameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = false;

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                if (canvas.TryGetComponent(out CanvasScaler scaler))
                {
                    scaler.enabled = false; // world-space canvas: 1 unit = 1 reference pixel
                }

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = UiCamera;
                var rect = (RectTransform)canvas.transform;
                rect.SetPositionAndRotation(UiRigOrigin, Quaternion.identity);
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            }

            uiGraphics = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RefreshUi();
        }

        /// <summary>
        /// Shows only the named UI elements (and everything inside them); every other graphic is switched
        /// off without touching layout. Their parent canvas groups are made fully visible.
        /// </summary>
        public void UiOnly(params string[] names)
        {
            PrepareUi();
            var targets = names.Select(n => (Transform)UiElement(n)).ToList();

            foreach (Graphic g in uiGraphics)
            {
                if (g != null)
                {
                    g.enabled = targets.Any(t => g.transform == t || g.transform.IsChildOf(t));
                }
            }

            foreach (Transform t in targets)
            {
                for (Transform p = t; p != null; p = p.parent)
                {
                    if (p.TryGetComponent(out CanvasGroup group))
                    {
                        group.alpha = 1f;
                    }

                    if (!p.gameObject.activeSelf)
                    {
                        p.gameObject.SetActive(true);
                    }
                }
            }

            UiVisible = targets.Count > 0;
        }

        /// <summary>A UI element by name; "CanvasName/Element" picks the root canvas when names repeat.</summary>
        public RectTransform UiElement(string name)
        {
            PrepareUi();
            string canvasName = null;
            int slash = name.IndexOf('/');
            if (slash > 0)
            {
                canvasName = name[..slash];
                name = name[(slash + 1)..];
            }

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && (canvasName == null || canvas.name == canvasName) &&
                    EditorAssetUtil.FindDeep(canvas.transform, name) is RectTransform rect)
                {
                    return rect;
                }
            }

            throw new InvalidOperationException($"No UI element '{name}' in {Scene.name}.");
        }

        public void UiAlpha(string name, float alpha)
        {
            RectTransform rect = UiElement(name);
            if (!rect.TryGetComponent(out CanvasGroup group))
            {
                group = rect.gameObject.AddComponent<CanvasGroup>(); // (GetComponent's editor fake-null defeats ??)
            }

            group.alpha = Mathf.Clamp01(alpha);
        }

        public void UiText(string elementName, string labelName, string text)
        {
            Transform label = EditorAssetUtil.FindDeep(UiElement(elementName), labelName)
                              ?? throw new InvalidOperationException($"No '{labelName}' under '{elementName}'.");
            label.GetComponent<TMP_Text>().text = text;
        }

        public void RefreshUi()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.isRootCanvas)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                    }
                }

                foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                {
                    text.ForceMeshUpdate(true);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        // ---- props and highlights -------------------------------------------------------------------------

        /// <summary>
        /// A lasting copy of the game's hover glow (InteractionHighlighter.DrawOverlay queues draws for the
        /// whole editor tick, so the trailer uses real overlay objects instead). Returns the overlay renderers.
        /// </summary>
        public List<Renderer> AddGlow(Interactable target, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialLibrary.HighlightMaterialPath);
            var result = new List<Renderer>();
            IReadOnlyList<Renderer> renderers = target.HighlightRenderers;
            IReadOnlyList<MeshFilter> filters = target.HighlightMeshFilters;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null || filters[i] == null || filters[i].sharedMesh == null || !renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                var go = new GameObject($"{renderers[i].name}_Glow") { hideFlags = HideFlags.DontSave };
                Own(go);
                go.transform.SetParent(renderers[i].transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = filters[i].sharedMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = Enumerable.Repeat(material, filters[i].sharedMesh.subMeshCount).ToArray();
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                result.Add(r);
            }

            SetGlow(result, color);
            return result;
        }

        public static void SetGlow(List<Renderer> overlays, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            foreach (Renderer r in overlays)
            {
                r.SetPropertyBlock(block);
                r.enabled = color.maxColorComponent > 0.001f;
            }
        }

        /// <summary>URP Lit material made in memory (never saved).</summary>
        public Material Lit(string hex, float smoothness = 0.4f, float metallic = 0f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", EditorAssetUtil.Hex(hex));
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        /// <summary>Unlit textured material (screens), optionally HDR-boosted so it glows a little.</summary>
        public Material Unlit(Texture2D texture, float boost = 1f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.DontSave };
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", new Color(boost, boost, boost, 1f));
            return m;
        }

        public GameObject Primitive(PrimitiveType type, string name, Material material, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return Own(go);
        }

        public GameObject Spawn(GameObject prefabOrModel, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabOrModel, Scene);
            go.name = name;
            return Own(go);
        }

        // ---- phone helpers ------------------------------------------------------------------------------------

        public PhoneDevice Phone() => Object.FindFirstObjectByType<PhoneDevice>() ?? throw new InvalidOperationException("No PhoneDevice.");

        public PartsArea PartsArea() => Object.FindFirstObjectByType<PartsArea>();

        public static PhonePart Part(PhoneDevice device, string slotId) =>
            device.TryGetPart(slotId, out PhonePart part) ? part : throw new InvalidOperationException($"No part '{slotId}'.");

        /// <summary>World pose a part has when installed, for the phone's current pose.</summary>
        public static Pose InstalledWorld(PhonePart part)
        {
            Transform parent = part.InstalledParent;
            Vector3 position = parent != null ? parent.TransformPoint(part.InstalledLocalPosition) : part.InstalledLocalPosition;
            Quaternion rotation = parent != null ? parent.rotation * part.InstalledLocalRotation : part.InstalledLocalRotation;
            return new Pose(position, rotation);
        }

        public static void SetWorld(PhonePart part, Pose pose) => part.transform.SetPositionAndRotation(pose.position, pose.rotation);

        public static void Unpack(GameObject member)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(member);
            if (root != null)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }
    }
}

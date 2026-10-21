using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhoneRepair.Audio;
using PhoneRepair.Cameras;
using PhoneRepair.Controls;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using PhoneRepair.Workbench;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/Sandbox/M1_Workbench.unity (the repair bench sandbox, now with
    /// Milestone 2 tools) from the prefabs. The scene is REGENERATED every run, so make a copy
    /// before hand-editing it for experiments.
    /// </summary>
    internal static class WorkbenchSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/M1_Workbench.unity";

        // Must match ArtSource/Blender/Scripts/build_env_workbench.py (desk top 0.75 m + 4 mm mat).
        private const float MatTopY = 0.754f;
        private static readonly Vector3 MatCenter = new(0f, MatTopY, -0.05f);
        private static readonly Vector3 IntroCameraPosition = new(0f, 1.32f, -0.98f);
        private static readonly Vector3 IntroLookAt = new(0f, 0.84f, 0.02f);

        public static void Build(GameObject phonePrefab, GameObject environmentPrefab, VolumeProfile volumeProfile,
            Material highlightMaterial, IReadOnlyList<ToolDefinition> tools)
        {
            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Lighting & environment ------------------------------------------------------
            Transform lighting = new GameObject("Lighting").transform;
            Light sun = CreateSun(lighting);
            ConfigureRenderSettings(sun);

            Transform environmentRoot = new GameObject("Environment").transform;
            var environment = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab, scene);
            environment.transform.SetParent(environmentRoot, false);
            CreateLampLight(lighting, environment.transform);
            CreateVolume(lighting, volumeProfile);

            // --- Systems -----------------------------------------------------------------------
            Transform systems = new GameObject("Systems").transform;
            var input = CreateChild<GameInput>(systems, "GameInput");
            var selection = CreateChild<SelectionController>(systems, "SelectionController");
            var raycaster = CreateChild<InteractionRaycaster>(systems, "InteractionRaycaster");
            var highlighter = CreateChild<InteractionHighlighter>(systems, "InteractionHighlighter");
            var sfx = CreateChild<SfxPlayer>(systems, "SfxPlayer");
            var toolBelt = CreateChild<ToolBelt>(systems, "ToolBelt");
            var mover = CreateChild<PartMover>(systems, "PartMover");
            var feedback = CreateChild<RepairFeedback>(systems, "RepairFeedback");
            var repair = CreateChild<RepairInteractor>(systems, "RepairInteractor");

            // --- Workbench: the phone sits on a turntable pivot resting on the mat -------------
            Transform workbench = new GameObject("Workbench").transform;
            var pivot = new GameObject("PhonePivot");
            pivot.transform.SetParent(workbench, false);
            var phone = (GameObject)PrefabUtility.InstantiatePrefab(phonePrefab, scene);
            phone.transform.SetParent(pivot.transform, false);
            float bottomBelowPivot = -EditorAssetUtil.RendererBounds(phone).min.y; // pivot is at the origin here
            pivot.transform.position = new Vector3(MatCenter.x, MatTopY + bottomBelowPivot + 0.0003f, MatCenter.z);

            // Kinematic body: the turntable moves many child colliders every frame.
            var body = pivot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var turntable = pivot.AddComponent<InspectionTurntable>();

            PartsArea partsArea = CreatePartsArea(workbench, environment.transform);
            var presenter = CreateChild<ToolPresenter>(workbench, "ToolPresenter");

            // --- Cameras -----------------------------------------------------------------------
            Transform cameras = new GameObject("Cameras").transform;
            Camera camera = CreateCamera(cameras);
            var director = camera.gameObject.AddComponent<CameraDirector>();
            var inspectionView = CreateChild<InspectionCameraView>(cameras, "View_PhoneInspection");
            EditorAssetUtil.Wire(inspectionView, ("anchor", pivot.transform));
            EditorAssetUtil.Wire(director, ("initialView", inspectionView));

            // --- Wiring ------------------------------------------------------------------------
            var controller = CreateChild<WorkbenchController>(workbench, "WorkbenchController");
            EditorAssetUtil.Wire(controller,
                ("input", input),
                ("raycaster", raycaster),
                ("selection", selection),
                ("turntable", turntable),
                ("inspectionView", inspectionView),
                ("cameraDirector", director),
                ("repair", repair),
                ("toolBelt", toolBelt));
            EditorAssetUtil.Wire(raycaster, ("targetCamera", camera));
            EditorAssetUtil.Wire(highlighter, ("selection", selection), ("highlightMaterial", highlightMaterial));
            EditorAssetUtil.WireArray(toolBelt, "tools", tools.Cast<Object>().ToList());
            EditorAssetUtil.Wire(feedback, ("sfx", sfx), ("highlighter", highlighter), ("mover", mover));
            EditorAssetUtil.Wire(repair,
                ("phone", phone.GetComponent<PhoneDevice>()),
                ("toolBelt", toolBelt),
                ("mover", mover),
                ("partsArea", partsArea),
                ("feedback", feedback),
                ("viewCamera", camera));
            EditorAssetUtil.Wire(presenter,
                ("toolBelt", toolBelt),
                ("repair", repair),
                ("raycaster", raycaster),
                ("input", input),
                ("viewCamera", camera));

            WorkbenchUiBuilder.Build(selection, input, toolBelt, repair, feedback);
            CreateReflectionProbe(lighting);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"Could not save {ScenePath}.");
            }

            AddToBuildSettings(ScenePath);
            BakeEnvironmentLighting(scene);
        }

        /// <summary>
        /// Landing spots for removed parts: big spots left of the phone (cover, screen), medium and
        /// small spots right/front of it, and the six screw-tray compartments marked in the Blender model.
        /// </summary>
        private static PartsArea CreatePartsArea(Transform parent, Transform environment)
        {
            var area = CreateChild<PartsArea>(parent, "PartsArea");

            void AddMatSpot(string name, float x, float z, Vector2 size)
            {
                var anchor = new GameObject(name).transform;
                anchor.SetParent(area.transform, false);
                anchor.position = new Vector3(x, MatTopY, z);
                area.AddSlot(anchor, size, false);
            }

            var big = new Vector2(0.16f, 0.08f);
            var medium = new Vector2(0.085f, 0.085f);
            var small = new Vector2(0.07f, 0.045f);
            AddMatSpot("Spot_Big_1", -0.155f, 0.065f, big);
            AddMatSpot("Spot_Big_2", -0.155f, -0.02f, big);
            AddMatSpot("Spot_Big_3", -0.155f, -0.105f, big);
            AddMatSpot("Spot_Medium_1", 0.125f, -0.07f, medium);
            AddMatSpot("Spot_Medium_2", 0.205f, -0.07f, medium);
            AddMatSpot("Spot_Medium_3", 0.125f, -0.16f, medium);
            AddMatSpot("Spot_Medium_4", 0.205f, -0.16f, medium);
            AddMatSpot("Spot_Small_1", -0.2f, -0.185f, small);
            AddMatSpot("Spot_Small_2", -0.115f, -0.185f, small);
            AddMatSpot("Spot_Small_3", 0f, -0.185f, small);

            int traySlots = 0;
            for (int i = 0; ; i++)
            {
                Transform slot = EditorAssetUtil.FindDeep(environment, $"ScrewTray_Slot_{i}");
                if (slot == null)
                {
                    break;
                }

                area.AddSlot(slot, new Vector2(0.04f, 0.04f), true);
                traySlots++;
            }

            if (traySlots == 0)
            {
                Debug.LogWarning("[PhoneRepair] No ScrewTray_Slot_* empties in the environment model; screws have nowhere to go. Re-run build_env_workbench.py.");
            }

            return area;
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static Light CreateSun(Transform parent)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(50f, 30f, 0f); // from front-left, shadows fall back-right
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = EditorAssetUtil.Hex("#FFF0DC");
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
            return light;
        }

        private static void CreateLampLight(Transform parent, Transform environment)
        {
            Transform bulb = EditorAssetUtil.FindDeep(environment, "DeskLamp_Bulb");
            Vector3 position = bulb != null ? bulb.position : new Vector3(-0.21f, 0.99f, 0.03f);
            Vector3 direction = (MatCenter - position).normalized;
            position += direction * 0.01f; // just outside the shade so it doesn't shadow itself

            var go = new GameObject("LampLight");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = EditorAssetUtil.Hex("#FFD9A8");
            light.intensity = 0.05f; // ~35 cm from the mat with inverse-square falloff: small values go a long way
            light.range = 2f;
            light.spotAngle = 85f;
            light.innerSpotAngle = 40f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
        }

        private static void ConfigureRenderSettings(Light sun)
        {
            RenderSettings.skybox = MaterialLibrary.EnsureWorkbenchSkybox();
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = EditorAssetUtil.Hex("#DCE6F0");
            RenderSettings.ambientEquatorColor = EditorAssetUtil.Hex("#EAD6C4");
            RenderSettings.ambientGroundColor = EditorAssetUtil.Hex("#7A6658");
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;
            RenderSettings.fog = false;
        }

        private static void CreateVolume(Transform parent, VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static Camera CreateCamera(Transform parent)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(parent, false);
            // Placed wide on purpose: the director blends from here into the inspection view (intro shot).
            go.transform.SetPositionAndRotation(IntroCameraPosition, Quaternion.LookRotation(IntroLookAt - IntroCameraPosition));
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f; // millimetre close-ups
            camera.farClipPlane = 30f;
            camera.fieldOfView = 40f;
            camera.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
            var data = EditorAssetUtil.GetOrAdd<UniversalAdditionalCameraData>(go);
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA comes from the pipeline asset
            data.renderShadows = true;
            return camera;
        }

        /// <summary>
        /// Baked probe around the bench: metal screws and glossy glass reflect the warm room instead of
        /// black (no reflection data) or a blue sky. Re-bake via Window > Rendering > Lighting after
        /// changing the environment.
        /// </summary>
        private static void CreateReflectionProbe(Transform parent)
        {
            var go = new GameObject("WorkbenchReflectionProbe");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 1.0f, 0f);
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(2.6f, 2.0f, 1.9f);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.intensity = 1f;
        }

        /// <summary>
        /// Environment-only bake (no lightmaps, no realtime GI): stores the ambient probe and the
        /// reflection probe in LightingData so the editor, play mode and builds all look the same.
        /// </summary>
        private static void BakeEnvironmentLighting(Scene scene)
        {
            string settingsPath = Path.Combine(Path.GetDirectoryName(ScenePath) ?? "Assets", "M1_Workbench_LightingSettings.lighting").Replace('\\', '/');
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "M1_Workbench_LightingSettings", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            Lightmapping.lightingSettings = settings;
            if (!Lightmapping.Bake())
            {
                Debug.LogWarning("[PhoneRepair] Environment lighting bake failed; generate it from Window > Rendering > Lighting.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

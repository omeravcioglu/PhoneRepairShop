using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhoneRepair.Audio;
using PhoneRepair.Cameras;
using PhoneRepair.Controls;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using PhoneRepair.Workbench;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using K = PhoneRepair.EditorTools.PhoneFixerUiKit;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds the Phone Fixer scenes from the prefabs: PF_MainMenu (title over the diorama), PF_Shop
    /// (fixed diorama camera, stations, customers, HUD) and PF_RepairBench (the close-up repair view on
    /// the same bench inside the same room, wired like M1_Workbench). All three are REGENERATED on
    /// every run, so copy one before hand-editing it.
    /// </summary>
    internal static class PhoneFixerScenes
    {
        /// <summary>Asset paths of everything the scenes use (reloaded after each new scene).</summary>
        public sealed class Content
        {
            public string ShopPrefab = PhoneFixerPrefabs.ShopPrefabPath;
            public string BenchPrefab = PhoneFixerPrefabs.BenchPrefabPath;
            public string PhonePrefab;
            public string HighlightMaterial = MaterialLibrary.HighlightMaterialPath;
            public string ShopPipeline = PhoneFixerPaths.ShopPipelinePath;
            public string BenchVolume = PhoneFixerPaths.BenchVolumePath;
            public string Skybox = PhoneFixerPaths.MaterialsFolder + "/M_PF_Sky.mat";
            public readonly List<string> ToolPaths = new();
        }

        private sealed class Loaded
        {
            public GameObject Shop, Bench, Phone;
            public readonly Dictionary<string, GameObject> Customers = new();
            public readonly List<ToolDefinition> Tools = new();
            public Material Highlight, Skybox;
            public RenderPipelineAsset ShopPipeline;
            public VolumeProfile BenchVolume;
        }

        // Diorama camera: the framing of the Blender preview (58 mm lens = ~20 degrees vertical at 16:9).
        public static readonly Vector3 DioramaPosition = new(13.2f, 10.4f, -10.6f);
        public static readonly Vector3 DioramaTarget = new(2.45f, 0.55f, 2.2f);
        public const float DioramaFov = 20f;
        // Counter close-up: from behind the counter, high and toward the front so the sightline clears the PC
        // (z 1.3-1.7) and the register (z 2.05-2.45); ~2.5 m away, Mrs. Tanabe's face above frame centre.
        public static readonly Vector3 CounterViewPosition = new(3.8f, 2.2f, 1.0f);
        public static readonly Vector3 CounterViewTarget = new(1.85f, 0.78f, 2.3f);
        public const float CounterViewFov = 34f;
        private static readonly Color Backdrop = EditorAssetUtil.Hex("#F1DFC6");
        private const float BenchTop = 0.80f; // build_pf_repair_bench.py TOP

        // Golden hour: warm sun low through the storefront (from -X), cool sky fill from the open front-right.
        private static readonly Vector3 ShopSunDirection = new(7.5f, -3.4f, 1.2f); // ~24 degrees up: low golden-hour sun
        private static readonly Vector3 FillDirection = new(-0.55f, -0.5f, 0.68f);

        // ---- main menu -----------------------------------------------------------------------------

        public static void BuildMenu(Content content)
        {
            Scene scene = NewScene();
            Loaded a = Load(content);
            Transform environment = new GameObject("Environment").transform;
            GameObject shop = Instantiate(a.Shop, environment, Vector3.zero);
            GameObject bench = Instantiate(a.Bench, environment, Spot(shop, "Spot_Bench"));
            DecorateBench(bench, a, null, null);
            Transform lighting = new GameObject("Lighting").transform;
            ShopLighting(lighting, a);
            CreatePendants(lighting, shop);
            PopulateShop(a, shop);

            Transform cameras = new GameObject("Cameras").transform;
            Camera camera = CreateCamera(cameras, 0.3f, 80f);
            // Wider and offset so the shop sits right of the menu keys.
            var view = CreateFixedView(cameras, "View_Menu", new Vector3(12.4f, 8.6f, -11.8f), new Vector3(0.6f, 0.9f, 2.5f), 24f, 0.6f);
            var director = camera.gameObject.AddComponent<CameraDirector>();
            EditorAssetUtil.Wire(director, ("initialView", view), ("blendIntoInitialView", false));
            SnapCamera(camera, view);

            Transform systems = new GameObject("Systems").transform;
            var actions = CreateChild<UiActions>(systems, "UiActions");
            PhoneFixerHud.BuildMainMenu(actions);
            PhoneFixerHud.CreateFader();
            CreateProbe(lighting, new Vector3(3f, 1.3f, 2.25f), new Vector3(6.6f, 2.8f, 5.0f));
            Save(scene, PhoneFixerPaths.MenuScene);
        }

        // ---- shop --------------------------------------------------------------------------------------

        public static void BuildShop(Content content)
        {
            Scene scene = NewScene();
            Loaded a = Load(content);
            Transform environment = new GameObject("Environment").transform;
            GameObject shop = Instantiate(a.Shop, environment, Vector3.zero);
            GameObject bench = Instantiate(a.Bench, environment, Spot(shop, "Spot_Bench"));
            DecorateBench(bench, a, null, null);
            Transform lighting = new GameObject("Lighting").transform;
            ShopLighting(lighting, a);
            CreatePendants(lighting, shop);
            PopulateShop(a, shop);

            // Systems
            Transform systems = new GameObject("Systems").transform;
            var input = CreateChild<GameInput>(systems, "GameInput");
            var selection = CreateChild<SelectionController>(systems, "SelectionController");
            var raycaster = CreateChild<InteractionRaycaster>(systems, "InteractionRaycaster");
            var highlighter = CreateChild<InteractionHighlighter>(systems, "InteractionHighlighter");
            var controller = CreateChild<ShopController>(systems, "ShopController");
            var actions = CreateChild<UiActions>(systems, "UiActions");

            // Cameras: the fixed diorama view, and a close-up at the counter for conversations.
            Transform cameras = new GameObject("Cameras").transform;
            Camera camera = CreateCamera(cameras, 0.3f, 80f);
            var diorama = CreateFixedView(cameras, "View_Diorama", DioramaPosition, DioramaTarget, DioramaFov, 0.4f);
            var counter = CreateFixedView(cameras, "View_Counter", CounterViewPosition, CounterViewTarget, CounterViewFov, 0.25f);
            Vector3 introPosition = DioramaPosition + new Vector3(3.5f, 4.5f, -3.5f);
            camera.transform.SetPositionAndRotation(introPosition, Quaternion.LookRotation(DioramaTarget - introPosition));
            var director = camera.gameObject.AddComponent<CameraDirector>();
            EditorAssetUtil.Wire(director, ("initialView", diorama), ("introBlendDuration", 2.2f));

            EditorAssetUtil.Wire(raycaster, ("targetCamera", camera), ("maxDistance", 60f));
            WireHighlighter(highlighter, selection, a.Highlight);
            // Stations are seen from ~20 m: a stronger orange so "you can pick it" still reads.
            EditorAssetUtil.Wire(highlighter, ("readyColor", new Color(1.5f, 0.85f, 0.32f)));
            EditorAssetUtil.Wire(controller, ("input", input), ("raycaster", raycaster), ("selection", selection));

            PhoneFixerHud.ShopScreens screens = PhoneFixerHud.BuildShop(selection, camera, director, diorama, counter, actions);
            PhoneFixerHud.CreateFader();

            // Stations: what the player can click in the diorama.
            AddStation(bench, "Workbench", "Repair", PhoneFixerPaths.BenchSceneName, bench.transform);
            ShopStation desk = AddStation(Find(shop, "Shop_Counter").gameObject, "Counter", "Talk", null,
                Find(shop, "Shop_Counter"), Find(shop, "Counter_Register"), Find(shop, "Counter_PC"), Find(shop, "Counter_Keyboard"),
                Find(shop, "Counter_Bell"), Find(shop, "Counter_TicketLine"));
            ShopStation shelf = AddStation(Find(shop, "Shop_Shelf").gameObject, "Parts shelf", "Browse", null, Find(shop, "Shop_Shelf"));
            ShopStation upgrade = AddStation(Find(shop, "Shop_UpgradeSlot").gameObject, "Upgrade spot", "Upgrades", null,
                Find(shop, "Shop_UpgradeSlot"));
            Transform catNode = Find(shop, "Deco_ShopCat");
            ShopStation cat = AddStation(catNode.gameObject, "Denchi the cat", "Pet", null, catNode);
            var bounce = catNode.gameObject.AddComponent<SquashBounce>();
            K.On(desk.Used, screens.Dialogue.Show);
            K.On(shelf.Used, screens.Inventory.Show);
            K.On(upgrade.Used, screens.Upgrades.Show);
            K.On(cat.Used, bounce.Play);

            CreateProbe(lighting, new Vector3(3f, 1.3f, 2.25f), new Vector3(6.6f, 2.8f, 5.0f));
            Save(scene, PhoneFixerPaths.ShopScene);
        }

        // ---- repair bench -----------------------------------------------------------------------------

        public static void BuildBench(Content content)
        {
            Scene scene = NewScene();
            Loaded a = Load(content);
            if (a.Tools.Count == 0)
            {
                throw new ArgumentException("The bench needs the PF tool set.");
            }

            // The bench at the origin, inside the same shop (Spot_Bench), so the close-up has the real room behind it.
            Transform environment = new GameObject("Environment").transform;
            GameObject shop = Instantiate(a.Shop, environment, Vector3.zero);
            shop.transform.position = -Spot(shop, "Spot_Bench");
            GameObject bench = Instantiate(a.Bench, environment, Vector3.zero);
            foreach (string proxy in new[] { "ShadowProxy_Ceiling", "ShadowProxy_Front", "ShadowProxy_Right" })
            {
                Find(shop, proxy).gameObject.SetActive(false); // the close-up wants direct, readable light
            }

            Transform lighting = new GameObject("Lighting").transform;
            Light sun = CreateDirectional(lighting, "Sun", Quaternion.Euler(46f, 32f, 0f) * Vector3.forward, EditorAssetUtil.Hex("#FFE7CC"), 1.05f, true);
            sun.shadowStrength = 0.8f;
            CreateDirectional(lighting, "SkyFill", FillDirection, EditorAssetUtil.Hex("#A9C9CE"), 0.18f, false);
            Ambient(sun, a.Skybox, EditorAssetUtil.Hex("#D3DEE3"), EditorAssetUtil.Hex("#EBD5BF"), EditorAssetUtil.Hex("#7A6656"));
            CreateLampLight(lighting, bench);
            CreateMonitorGlow(lighting, bench);
            CreateVolume(lighting, a.BenchVolume);

            // Systems (same set as M1_Workbench)
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
            var actions = CreateChild<UiActions>(systems, "UiActions");
            var back = CreateChild<SceneHotkey>(systems, "BackToShop");
            EditorAssetUtil.Wire(back, ("sceneName", PhoneFixerPaths.ShopSceneName));

            // Phone on a turntable pivot resting on the mat's PhoneSpot.
            Transform workbench = new GameObject("Workbench").transform;
            var pivot = new GameObject("PhonePivot");
            pivot.transform.SetParent(workbench, false);
            var phone = (GameObject)PrefabUtility.InstantiatePrefab(a.Phone, scene);
            phone.transform.SetParent(pivot.transform, false);
            var faults = phone.GetComponent<PhoneFaultVisuals>();
            if (faults != null)
            {
                faults.SetState("ChargingPort_Lint", true); // Ren's job: "won't charge" (crumbs in the port)
                EditorUtility.SetDirty(faults);
            }

            float bottomBelowPivot = -EditorAssetUtil.RendererBounds(phone).min.y; // pivot is at the origin here
            Vector3 phoneSpot = Find(bench, "PhoneSpot").position;
            pivot.transform.position = new Vector3(phoneSpot.x, phoneSpot.y + bottomBelowPivot + 0.0003f, phoneSpot.z);
            var body = pivot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var turntable = pivot.AddComponent<InspectionTurntable>();
            PartsArea partsArea = CreatePartsArea(workbench, bench);
            var presenter = CreateChild<ToolPresenter>(workbench, "ToolPresenter");
            DecorateBench(bench, a, toolBelt, a.Tools);

            // Cameras: wide intro shot that settles into the inspection view.
            Transform cameras = new GameObject("Cameras").transform;
            Camera camera = CreateCamera(cameras, 0.01f, 40f);
            var introPosition = new Vector3(0f, 1.55f, -1.15f);
            camera.transform.SetPositionAndRotation(introPosition, Quaternion.LookRotation(new Vector3(0f, 0.86f, 0.02f) - introPosition));
            camera.fieldOfView = 40f;
            var director = camera.gameObject.AddComponent<CameraDirector>();
            var inspectionView = CreateChild<InspectionCameraView>(cameras, "View_PhoneInspection");
            // A little wider than M1 (0.36 m) so the mat, screw map and tray read around the phone, as in UI04.
            EditorAssetUtil.Wire(inspectionView, ("anchor", pivot.transform), ("defaultDistance", 0.44f));
            EditorAssetUtil.Wire(director, ("initialView", inspectionView));

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
            WireHighlighter(highlighter, selection, a.Highlight);
            EditorAssetUtil.WireArray(toolBelt, "tools", a.Tools.Cast<Object>().ToList());
            EditorAssetUtil.Wire(feedback, ("sfx", sfx), ("highlighter", highlighter), ("mover", mover));
            EditorAssetUtil.Wire(repair,
                ("phone", phone.GetComponent<PhoneDevice>()),
                ("toolBelt", toolBelt),
                ("mover", mover),
                ("partsArea", partsArea),
                ("feedback", feedback));
            EditorAssetUtil.Wire(presenter,
                ("toolBelt", toolBelt),
                ("repair", repair),
                ("raycaster", raycaster),
                ("input", input),
                ("viewCamera", camera));

            PhoneFixerHud.BuildBench(selection, input, toolBelt, repair, feedback, actions);
            PhoneFixerHud.CreateFader();
            CreateProbe(lighting, new Vector3(0f, 1.0f, 0f), new Vector3(2.6f, 2.0f, 1.9f));
            Save(scene, PhoneFixerPaths.BenchScene);
        }

        /// <summary>Landing spots from the bench model: mat spots for big flat parts, bento compartments, screw map.</summary>
        private static PartsArea CreatePartsArea(Transform parent, GameObject bench)
        {
            var area = CreateChild<PartsArea>(parent, "PartsArea");

            void Add(string name, Vector2 size, bool fasteners)
            {
                Transform anchor = EditorAssetUtil.FindDeep(bench.transform, name);
                if (anchor == null)
                {
                    Debug.LogWarning($"[PhoneFixer] Bench model has no '{name}' empty.");
                    return;
                }

                area.AddSlot(anchor, size, fasteners);
            }

            for (int i = 1; i <= 4; i++)
            {
                Add($"PartSpot_Medium_{i}", new Vector2(0.17f, 0.095f), false); // screen, back cover (turned 90 degrees)
                Add($"PartSpot_Big_{i}", new Vector2(0.14f, 0.11f), false);     // bento: battery, board, camera...
            }

            for (int i = 1; i <= 3; i++)
            {
                Add($"PartSpot_Small_{i}", new Vector2(0.075f, 0.05f), false);
            }

            for (int i = 0; i < 8; i++)
            {
                Add($"ScrewTray_Slot_{i}", new Vector2(0.025f, 0.025f), true);
            }

            return area;
        }

        // ---- shared pieces -----------------------------------------------------------------------------

        private static Loaded Load(Content content)
        {
            T Need<T>(string path) where T : Object =>
                AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"{typeof(T).Name} missing at {path}; run the Phone Fixer setup.");

            var loaded = new Loaded
            {
                Shop = Need<GameObject>(content.ShopPrefab),
                Bench = Need<GameObject>(content.BenchPrefab),
                Phone = Need<GameObject>(content.PhonePrefab),
                Highlight = Need<Material>(content.HighlightMaterial),
                ShopPipeline = Need<RenderPipelineAsset>(content.ShopPipeline),
                BenchVolume = Need<VolumeProfile>(content.BenchVolume),
                Skybox = Need<Material>(content.Skybox),
            };
            foreach (string customer in PhoneFixerPaths.CustomerNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.CharacterPrefab(customer));
                if (prefab != null)
                {
                    loaded.Customers[customer] = prefab;
                }
            }

            loaded.Tools.AddRange(content.ToolPaths.Select(Need<ToolDefinition>));
            return loaded;
        }

        private static Scene NewScene()
        {
            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.ScenesFolder);
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void Save(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"Could not save {path}.");
            }

            AddToBuildSettings(path);
            BakeEnvironment(scene, path);
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }

        private static Transform Find(GameObject root, string name) =>
            EditorAssetUtil.FindDeep(root.transform, name) ?? throw new InvalidOperationException($"'{name}' not found under {root.name}.");

        private static Vector3 Spot(GameObject shop, string name) => Find(shop, name).position;

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        /// <summary>The neighbours: Mrs. Tanabe at the counter, Ren queueing, Mio waiting, two passers-by outside.</summary>
        private static void PopulateShop(Loaded a, GameObject shop)
        {
            Transform people = new GameObject("Customers").transform;
            PlaceCustomer(a, people, "Fumi", Spot(shop, "Spot_Customer_Counter"), 90f, "Talk", null);
            PlaceCustomer(a, people, "Ren", Spot(shop, "Spot_Queue_1"), 90f, "Impatient", new[] { "Confused" });
            PlaceCustomer(a, people, "Mio", new Vector3(1.0f, 0f, 1.05f), 125f, "Idle", new[] { "Celebrate" });
            PlaceCustomer(a, people, "Kanemochi", Spot(shop, "Spot_Door_Outside"), 90f, "Idle", new[] { "Impatient" });
            PlaceCustomer(a, people, "Sasaki", new Vector3(-0.95f, 0.025f, 1.45f), 120f, "Impatient", null);
        }

        private static void ShopLighting(Transform lighting, Loaded a)
        {
            Light sun = CreateDirectional(lighting, "Sun", ShopSunDirection, EditorAssetUtil.Hex("#FFC98B"), 2.8f, true);
            sun.shadowStrength = 0.95f;
            CreateDirectional(lighting, "SkyFill", FillDirection, EditorAssetUtil.Hex("#9CC3C9"), 0.26f, false);
            Ambient(sun, a.Skybox, EditorAssetUtil.Hex("#AFC1CB"), EditorAssetUtil.Hex("#D6BB9C"), EditorAssetUtil.Hex("#5A483C"));
            RenderSettings.ambientIntensity = 0.9f;
            CreateVolume(lighting, PhoneFixerRendering.EnsureShopVolume());
            var pipeline = lighting.gameObject.AddComponent<PipelineOverride>();
            EditorAssetUtil.Wire(pipeline, ("pipeline", a.ShopPipeline));
        }

        private static Light CreateDirectional(Transform parent, string name, Vector3 direction, Color color, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.LookRotation(direction.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
            return light;
        }

        private static void CreatePendants(Transform parent, GameObject shop)
        {
            foreach (string name in new[] { "Counter", "Waiting" })
            {
                Transform anchor = EditorAssetUtil.FindDeep(shop.transform, $"Light_Pendant_{name}");
                if (anchor == null)
                {
                    Debug.LogWarning($"[PhoneFixer] Shop model has no Light_Pendant_{name} empty.");
                    continue;
                }

                // Light budget (style guide): only the sun and the bench lamp cast shadows.
                var go = new GameObject($"Pendant_{name}");
                go.transform.SetParent(parent, false);
                go.transform.position = anchor.position;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = EditorAssetUtil.Hex("#FFD9A1");
                light.intensity = 1.6f;
                light.range = 4.5f;
                light.shadows = LightShadows.None;
                EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
            }
        }

        /// <summary>Faint teal glow from the CRT diagnostic screen onto the mat (tiny range, no shadows).</summary>
        private static void CreateMonitorGlow(Transform parent, GameObject bench)
        {
            Transform screen = EditorAssetUtil.FindDeep(bench.transform, "Monitor_Screen");
            if (screen == null)
            {
                return;
            }

            Renderer r = screen.GetComponent<Renderer>();
            Vector3 position = r != null ? r.bounds.center : screen.position;
            var go = new GameObject("MonitorGlow");
            go.transform.SetParent(parent, false);
            go.transform.position = position + bench.transform.forward * -0.08f; // a little in front of the glass
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = EditorAssetUtil.Hex("#CDEFF5");
            light.intensity = 0.03f;
            light.range = 0.7f;
            light.shadows = LightShadows.None;
            EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
        }

        private static void CreateLampLight(Transform parent, GameObject bench)
        {
            Transform bulb = EditorAssetUtil.FindDeep(bench.transform, "DeskLamp_Bulb");
            Transform spot = EditorAssetUtil.FindDeep(bench.transform, "PhoneSpot");
            Vector3 target = spot != null ? spot.position : new Vector3(0f, 0.8f, -0.03f);
            Vector3 position = bulb != null ? bulb.position : new Vector3(-0.2f, 1.15f, 0.1f);
            Vector3 direction = (target - position).normalized;
            position += direction * 0.012f; // just outside the bulb so the shade does not shadow its own light

            var go = new GameObject("DeskLamp");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = EditorAssetUtil.Hex("#FFE2B0");
            light.intensity = 0.06f;
            light.range = 2f;
            light.spotAngle = 80f;
            light.innerSpotAngle = 38f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            EditorAssetUtil.GetOrAdd<UniversalAdditionalLightData>(go);
        }

        private static void Ambient(Light sun, Material skybox, Color sky, Color equator, Color ground)
        {
            RenderSettings.skybox = skybox; // reflections only; cameras clear to the paper backdrop
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
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

        private static void CreateProbe(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = size;
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.intensity = 1f;
        }

        private static Camera CreateCamera(Transform parent, float near, float far)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(parent, false);
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = near;
            camera.farClipPlane = far;
            camera.fieldOfView = DioramaFov;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Backdrop;
            go.AddComponent<AudioListener>();
            var data = EditorAssetUtil.GetOrAdd<UniversalAdditionalCameraData>(go);
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA comes from the pipeline asset
            data.renderShadows = true;
            return camera;
        }

        private static FixedCameraView CreateFixedView(Transform parent, string name, Vector3 position, Vector3 target, float fov, float drift)
        {
            var view = CreateChild<FixedCameraView>(parent, name);
            view.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            EditorAssetUtil.Wire(view, ("fieldOfView", fov), ("driftDegrees", drift));
            return view;
        }

        private static void SnapCamera(Camera camera, CameraView view)
        {
            CameraPose pose = view.GetPose();
            camera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            camera.fieldOfView = pose.FieldOfView;
        }

        private static void WireHighlighter(InteractionHighlighter highlighter, SelectionController selection, Material material)
        {
            // Phone Fixer colour code: orange = you can pick it, teal = selected/in use, coral = blocked.
            EditorAssetUtil.Wire(highlighter,
                ("selection", selection),
                ("highlightMaterial", material),
                ("readyColor", new Color(1.0f, 0.6f, 0.28f)),
                ("neutralColor", new Color(0.62f, 0.66f, 0.78f)),
                ("blockedColor", new Color(1.05f, 0.45f, 0.4f)),
                ("selectedColor", new Color(0.42f, 0.95f, 0.86f)));
        }

        /// <summary>
        /// Interactable + ShopStation on <paramref name="host"/>, glowing over the given parts, with one box
        /// collider around all of them (siblings like the register become part of the counter click).
        /// </summary>
        private static ShopStation AddStation(GameObject host, string displayName, string verb, string scene, params Transform[] parts)
        {
            var renderers = new List<Renderer>();
            foreach (Transform part in parts)
            {
                foreach (Renderer r in part.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.name.StartsWith("ShadowProxy_", StringComparison.Ordinal) && !renderers.Contains(r))
                    {
                        renderers.Add(r);
                    }
                }
            }

            var interactable = host.AddComponent<Interactable>();
            EditorAssetUtil.Wire(interactable, ("displayName", displayName));
            EditorAssetUtil.WireArray(interactable, "highlightRenderers", renderers.Cast<Object>().ToList());

            // Box around everything, in the host's space; at least 5 cm tall so floor tape is easy to click.
            Matrix4x4 toLocal = host.transform.worldToLocalMatrix;
            bool any = false;
            var local = new Bounds();
            foreach (Renderer r in renderers)
            {
                Bounds b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = toLocal.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        local = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        local.Encapsulate(p);
                    }
                }
            }

            var box = host.AddComponent<BoxCollider>();
            box.center = local.center + new Vector3(0f, Mathf.Max(0f, 0.05f - local.size.y) * 0.5f, 0f);
            box.size = new Vector3(local.size.x, Mathf.Max(local.size.y, 0.05f), local.size.z);

            var station = host.AddComponent<ShopStation>();
            EditorAssetUtil.Wire(station, ("verb", verb), ("sceneToLoad", scene ?? string.Empty));
            return station;
        }

        private static void PlaceCustomer(Loaded a, Transform parent, string name, Vector3 position, float yaw, string loop, string[] gestures)
        {
            if (!a.Customers.TryGetValue(name, out GameObject prefab))
            {
                Debug.LogWarning($"[PhoneFixer] No prefab for customer {name}.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var actor = go.GetComponent<CustomerActor>();
            if (actor == null)
            {
                return;
            }

            var so = new SerializedObject(actor);
            so.FindProperty("loopState").stringValue = loop;
            SerializedProperty list = so.FindProperty("gestures");
            gestures ??= Array.Empty<string>();
            list.arraySize = gestures.Length;
            for (int i = 0; i < gestures.Length; i++)
            {
                list.GetArrayElementAtIndex(i).stringValue = gestures[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Tool props standing in the caddy (with a tool belt, the equipped one leaves its slot), a
        /// magnifier on the bench and, in the shop, Ren's Mochi Dango opened up on the mat.
        /// </summary>
        private static void DecorateBench(GameObject bench, Loaded a, ToolBelt toolBelt, IReadOnlyList<ToolDefinition> tools)
        {
            var props = new GameObject("BenchProps").transform;
            props.SetParent(bench.transform.parent, false);
            var shown = new List<GameObject>();
            var owners = new List<ToolDefinition>();
            foreach (string name in new[] { "Screwdriver", "Spudger", "Tweezers", "SuctionCup", "Brush", "Tester" })
            {
                Transform slot = EditorAssetUtil.FindDeep(bench.transform, $"Caddy_Slot_{name}");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.ToolFbx(name));
                if (slot == null || model == null)
                {
                    continue;
                }

                var prop = (GameObject)PrefabUtility.InstantiatePrefab(model, bench.scene);
                prop.name = $"Caddy_{name}";
                prop.transform.SetParent(props, false);
                prop.transform.SetPositionAndRotation(slot.position, slot.rotation * Quaternion.Euler(0f, 25f * shown.Count, 0f));
                shown.Add(prop);
                owners.Add(tools?.FirstOrDefault(t => t != null && t.Model == model));
            }

            var magnifier = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.ToolFbx("Magnifier"));
            if (magnifier != null)
            {
                // Lying flat on the front-right corner of the bench top: lens normal (local Z) up, handle toward the front-left.
                var lens = (GameObject)PrefabUtility.InstantiatePrefab(magnifier, bench.scene);
                lens.name = "Bench_Magnifier";
                lens.transform.SetParent(props, false);
                lens.transform.SetPositionAndRotation(bench.transform.TransformPoint(new Vector3(0.72f, BenchTop, -0.22f)),
                    bench.transform.rotation * Quaternion.Euler(-90f, 30f, 0f));
                RestOn(lens, bench.transform.TransformPoint(new Vector3(0f, BenchTop, 0f)).y);
            }

            if (toolBelt != null)
            {
                var display = props.gameObject.AddComponent<ToolCaddyDisplay>();
                EditorAssetUtil.Wire(display, ("toolBelt", toolBelt));
                EditorAssetUtil.WireArray(display, "tools", owners.Cast<Object>().ToList());
                EditorAssetUtil.WireArray(display, "props", shown.Cast<Object>().ToList());
                return;
            }

            // Shop / menu: Ren's phone opened up on the mat, waiting for its repair (no interaction).
            Transform spot = EditorAssetUtil.FindDeep(bench.transform, "PhoneSpot");
            var phoneModel = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.PhoneFbx);
            if (spot == null || phoneModel == null)
            {
                return;
            }

            var phone = (GameObject)PrefabUtility.InstantiatePrefab(phoneModel, bench.scene);
            phone.name = "Ren_MochiDango";
            phone.transform.SetParent(props, false);
            // Face down with the back cover and screws off: the open phone reads from the diorama camera.
            phone.transform.SetPositionAndRotation(spot.position, Quaternion.Euler(0f, 20f, 0f) * Quaternion.Euler(0f, 0f, 180f));
            foreach (string hidden in new[] { "BackCover", "Screw_01", "Screw_02", "Screw_03", "Screw_04" })
            {
                Transform node = EditorAssetUtil.FindDeep(phone.transform, hidden);
                if (node != null)
                {
                    node.gameObject.SetActive(false);
                }
            }

            foreach (string state in PhoneFaultVisuals.KnownStates)
            {
                Transform node = EditorAssetUtil.FindDeep(phone.transform, state);
                if (node != null)
                {
                    node.gameObject.SetActive(state == "ChargingPort_Lint");
                }
            }

            RestOn(phone, spot.position.y + 0.0003f);
        }

        /// <summary>Moves an object vertically so the bottom of its active renderers sits at <paramref name="y"/>.</summary>
        private static void RestOn(GameObject go, float y)
        {
            Bounds bounds = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (any)
            {
                go.transform.position += Vector3.up * (y - bounds.min.y);
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Environment-only bake (ambient probe + reflection probe), like M1_Workbench.</summary>
        private static void BakeEnvironment(Scene scene, string scenePath)
        {
            string name = Path.GetFileNameWithoutExtension(scenePath);
            string settingsPath = $"{PhoneFixerPaths.ScenesFolder}/{name}_LightingSettings.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = $"{name}_LightingSettings", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            Lightmapping.lightingSettings = settings;
            if (!Lightmapping.Bake())
            {
                Debug.LogWarning($"[PhoneFixer] Environment lighting bake failed for {name}; bake it from Window > Rendering > Lighting.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}

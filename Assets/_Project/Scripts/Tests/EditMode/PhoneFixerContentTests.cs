using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// Guard rails for the Phone Fixer art pass (Blender generators -> Tools > Phone Fixer > Run Art Setup):
    /// the Mochi Dango contract, damage meshes, palette remaps, the shared customer rig, the diorama's
    /// shadow proxies and the generated scenes. Ignored until the setup has run.
    /// </summary>
    public class PhoneFixerContentTests
    {
        private const string DangoDefinitionPath = "Assets/_Project/Data/Phones/MochiDango/PHN_MochiDango_Model.asset";
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/PhoneFixer";
        private const string ShopPrefabPath = "Assets/_Project/Prefabs/PhoneFixer/ENV_PF_Shop.prefab";
        private const string BenchPrefabPath = "Assets/_Project/Prefabs/PhoneFixer/ENV_PF_RepairBench.prefab";
        private const string BaseCharacterFbx = "Assets/_Project/Art/Models/Characters/PF/CHR_PF_Base.fbx";
        private const string ControllerPath = "Assets/_Project/Data/PhoneFixer/Characters/AC_PF_Customer.controller";
        private const string ShopScenePath = "Assets/_Project/Scenes/PhoneFixer/PF_Shop.unity";

        private static readonly string[] Customers = { "Fumi", "Ren", "Sasaki", "Mio", "Kanemochi" };
        private static readonly string[] Clips = { "Idle", "Walk", "Talk", "Celebrate", "Disappointed", "Confused", "Impatient" };
        private static readonly string[] LoopingClips = { "Idle", "Walk", "Talk", "Impatient" };
        private static readonly string[] PfScenes =
        {
            "Assets/_Project/Scenes/PhoneFixer/PF_MainMenu.unity",
            ShopScenePath,
            "Assets/_Project/Scenes/PhoneFixer/PF_RepairBench.unity",
        };

        private static PhoneModelDefinition LoadDango()
        {
            var definition = AssetDatabase.LoadAssetAtPath<PhoneModelDefinition>(DangoDefinitionPath);
            if (definition == null || definition.Prefab == null)
            {
                Assert.Ignore("Run Tools > Phone Fixer > Run Art Setup first.");
            }

            return definition;
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Assert.Ignore($"{path} missing. Run Tools > Phone Fixer > Run Art Setup first.");
            }

            return prefab;
        }

        // ---- Mochi Dango ---------------------------------------------------------------------------

        [Test]
        public void Dango_EverySlotHasAPartAnInteractableAndACollider()
        {
            PhoneModelDefinition definition = LoadDango();
            Transform root = definition.Prefab.transform;
            foreach (PartSlot slot in definition.Slots)
            {
                Transform node = FindDeep(root, slot.slotId);
                Assert.That(node, Is.Not.Null, $"model has no object named '{slot.slotId}'");
                var part = node.GetComponent<PhonePart>();
                Assert.That(part, Is.Not.Null, $"{slot.slotId}: missing PhonePart");
                Assert.That(part.Definition, Is.SameAs(slot.part), $"{slot.slotId}: wrong PartDefinition");
                Assert.That(part.Definition.HasRepairData, Is.True, $"{slot.slotId}: no repair data");
                Assert.That(node.GetComponent<Interactable>(), Is.Not.Null, $"{slot.slotId}: missing Interactable");
                Assert.That(node.GetComponentsInChildren<Collider>(true), Is.Not.Empty, $"{slot.slotId}: no collider");
            }
        }

        [Test]
        public void Dango_ImportsAtRealWorldScaleWithScreenUpAndTopForward()
        {
            Transform root = LoadDango().Prefab.transform;
            Bounds bounds = RendererBounds(root.gameObject, includeInactive: false);
            Assert.That(bounds.size.x, Is.EqualTo(0.076f).Within(0.003f), "width (m)");
            Assert.That(bounds.size.z, Is.EqualTo(0.156f).Within(0.003f), "length (m)");
            Assert.That(bounds.size.y, Is.InRange(0.010f, 0.016f), "thickness (m), camera island included");

            float screenY = RendererBounds(FindDeep(root, "Screen").gameObject, false).center.y;
            float coverY = RendererBounds(FindDeep(root, "BackCover").gameObject, false).center.y;
            Assert.That(screenY, Is.GreaterThan(coverY), "screen must face +Y");
            float portZ = RendererBounds(FindDeep(root, "ChargingPort").gameObject, false).center.z;
            float cameraZ = RendererBounds(FindDeep(root, "RearCamera").gameObject, false).center.z;
            Assert.That(cameraZ, Is.GreaterThan(portZ), "top edge must point +Z");
        }

        [Test]
        public void Dango_ScrewsPointOutOfTheBackAlongLocalUp()
        {
            Transform root = LoadDango().Prefab.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Screw_")))
            {
                Assert.That(Vector3.Dot(t.up, Vector3.down), Is.GreaterThan(0.99f), $"{t.name}: local up should point out of the back (-Y)");
            }
        }

        [Test]
        public void Dango_HasEveryDamageStateHiddenByDefault()
        {
            Transform root = LoadDango().Prefab.transform;
            Assert.That(root.GetComponent<PhoneFaultVisuals>(), Is.Not.Null, "PhoneFaultVisuals missing on the phone prefab");
            Bounds phone = RendererBounds(root.gameObject, includeInactive: false);
            phone.Expand(0.002f);
            foreach (string state in PhoneFaultVisuals.KnownStates)
            {
                Transform node = FindDeep(root, state);
                Assert.That(node, Is.Not.Null, $"no damage mesh '{state}' (build_phone_mochi_dango.py)");
                Assert.That(node.gameObject.activeSelf, Is.False, $"{state} should be hidden until a fault shows it");
                string owner = state.Substring(0, state.IndexOf('_'));
                Assert.That(node.GetComponentInParent<PhonePart>(true)?.name, Is.EqualTo(owner), $"{state} must be a child of {owner}");
                Bounds b = RendererBounds(node.gameObject, includeInactive: true);
                Assert.That(phone.Contains(b.min) && phone.Contains(b.max), Is.True, $"{state} sticks out of the phone");
            }
        }

        // ---- materials ----------------------------------------------------------------------------------

        [Test]
        public void PfModels_UseOnlyPaletteMaterials()
        {
            var models = new List<string>
            {
                "Assets/_Project/Art/Models/Environment/PF/ENV_PF_Shop.fbx",
                "Assets/_Project/Art/Models/Environment/PF/ENV_PF_RepairBench.fbx",
                "Assets/_Project/Art/Models/Phones/MochiDango/PHN_MochiDango.fbx",
                BaseCharacterFbx,
            };
            models.AddRange(Customers.Select(c => $"Assets/_Project/Art/Models/Characters/PF/CHR_PF_{c}.fbx"));
            models.AddRange(new[] { "Screwdriver", "Spudger", "SuctionCup", "Tweezers", "Brush", "Magnifier", "Tester" }
                .Select(t => $"Assets/_Project/Art/Models/Tools/PF/TOOL_PF_{t}.fbx"));

            foreach (string path in models)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Assert.Ignore($"{path} missing; run the Blender generators.");
                }

                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.That(m, Is.Not.Null, $"{path}/{r.name}: empty material slot");
                        string materialPath = AssetDatabase.GetAssetPath(m);
                        Assert.That(materialPath, Does.StartWith(MaterialsFolder),
                            $"{path}/{r.name}: '{m.name}' is not a palette material (add it to pf_bpy.PALETTE)");
                    }
                }
            }
        }

        // ---- customers ----------------------------------------------------------------------------------

        [Test]
        public void Customers_ShareOneControllerWithEveryClip()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Assert.Ignore("Run Tools > Phone Fixer > Run Art Setup first.");
            }

            string[] states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            Assert.That(states, Is.EquivalentTo(Clips));
            Assert.That(controller.layers[0].stateMachine.defaultState.name, Is.EqualTo("Idle"));

            foreach (string customer in Customers)
            {
                GameObject prefab = LoadPrefab($"Assets/_Project/Prefabs/PhoneFixer/Characters/CHR_PF_{customer}.prefab");
                var animator = prefab.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null, $"{customer}: no Animator");
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller), $"{customer}: wrong controller");
                Assert.That(prefab.GetComponent<CustomerActor>(), Is.Not.Null, $"{customer}: no CustomerActor");
                Assert.That(prefab.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null, $"{customer}: no skinned body");
            }
        }

        [Test]
        public void Customers_BaseClipsBindToEveryVariant()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(BaseCharacterFbx).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (clips.Length == 0)
            {
                Assert.Ignore("CHR_PF_Base has no clips yet.");
            }

            Assert.That(clips.Select(c => c.name), Is.SupersetOf(Clips));
            foreach (AnimationClip clip in clips)
            {
                Assert.That(clip.isLooping, Is.EqualTo(LoopingClips.Contains(clip.name)), $"{clip.name}: loop setting");
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.That(bindings, Is.Not.Empty, $"{clip.name}: no curves");
                foreach (string customer in Customers)
                {
                    Transform root = LoadPrefab($"Assets/_Project/Prefabs/PhoneFixer/Characters/CHR_PF_{customer}.prefab").transform;
                    foreach (EditorCurveBinding binding in bindings)
                    {
                        Assert.That(root.Find(binding.path), Is.Not.Null,
                            $"{clip.name} animates '{binding.path}', which {customer} does not have (rig names must match CHR_PF_Base)");
                    }
                }
            }
        }

        [Test]
        public void Customers_StandOnTheFloorAtTheirHeight()
        {
            var expected = new Dictionary<string, float> { { "Fumi", 0.94f }, { "Ren", 1f }, { "Sasaki", 1.04f }, { "Mio", 0.9f }, { "Kanemochi", 1f } };
            foreach (string customer in Customers)
            {
                GameObject prefab = LoadPrefab($"Assets/_Project/Prefabs/PhoneFixer/Characters/CHR_PF_{customer}.prefab");
                Assert.That(prefab.transform.localScale.y, Is.EqualTo(expected[customer]).Within(0.001f), $"{customer}: height scale");
                var body = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Bounds local = body.sharedMesh.bounds;
                Assert.That(local.min.y, Is.InRange(-0.03f, 0.05f), $"{customer}: feet must be at the origin (bind pose)");
                // Built at the base height (~1.38 m); tall hair (Madame K's beehive) may reach ~1.75 m.
                Assert.That(local.size.y, Is.InRange(1.2f, 1.8f), $"{customer}: unexpected height before scaling");
            }
        }

        // ---- environment ----------------------------------------------------------------------------

        [Test]
        public void Shop_ShadowProxiesAreShadowOnlyAndUnpickable()
        {
            GameObject shop = LoadPrefab(ShopPrefabPath);
            var proxies = shop.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("ShadowProxy_")).ToArray();
            Assert.That(proxies.Select(p => p.name), Is.EquivalentTo(new[] { "ShadowProxy_Ceiling", "ShadowProxy_Front", "ShadowProxy_Right" }));
            foreach (MeshRenderer proxy in proxies)
            {
                Assert.That(proxy.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly), proxy.name);
                Assert.That(proxy.GetComponent<Collider>(), Is.Null, $"{proxy.name} would block clicks from the diorama camera");
            }

            foreach (string spot in new[] { "Spot_Bench", "Spot_Customer_Counter", "Spot_Door_Inside", "Spot_Door_Outside", "Light_Pendant_Counter" })
            {
                Assert.That(FindDeep(shop.transform, spot), Is.Not.Null, $"shop model has no '{spot}' empty");
            }
        }

        [Test]
        public void Bench_HasTheSpotsTheSceneBuilderNeeds()
        {
            Transform bench = LoadPrefab(BenchPrefabPath).transform;
            Assert.That(FindDeep(bench, "PhoneSpot"), Is.Not.Null);
            Assert.That(FindDeep(bench, "DeskLamp_Bulb"), Is.Not.Null);
            for (int i = 0; i < 8; i++)
            {
                Assert.That(FindDeep(bench, $"ScrewTray_Slot_{i}"), Is.Not.Null, $"ScrewTray_Slot_{i}");
            }

            for (int i = 1; i <= 4; i++)
            {
                Assert.That(FindDeep(bench, $"PartSpot_Big_{i}"), Is.Not.Null, $"PartSpot_Big_{i}");
                Assert.That(FindDeep(bench, $"PartSpot_Medium_{i}"), Is.Not.Null, $"PartSpot_Medium_{i}");
            }

            Transform spot = FindDeep(bench, "PhoneSpot");
            Assert.That(spot.position.y, Is.EqualTo(0.805f).Within(0.002f), "phone spot = bench top (0.80) + mat (5 mm)");
        }

        // ---- scenes --------------------------------------------------------------------------------------

        [Test]
        public void PfScenes_ExistAndAreInBuildSettings()
        {
            foreach (string path in PfScenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Assert.Ignore($"{path} missing. Run Tools > Phone Fixer > Run Art Setup first.");
                }

                Assert.That(EditorBuildSettings.scenes.Any(s => s.path == path && s.enabled), Is.True, $"{path} is not in Build Settings (scene loading by name fails)");
            }
        }

        [Test]
        public void Shop_StationsArePickableAndWired()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopScenePath) == null)
            {
                Assert.Ignore("PF_Shop missing. Run Tools > Phone Fixer > Run Art Setup first.");
            }

            Scene scene = EditorSceneManager.OpenScene(ShopScenePath, OpenSceneMode.Additive);
            try
            {
                ShopStation[] stations = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShopStation>(true)).ToArray();
                Assert.That(stations.Length, Is.EqualTo(5), "workbench, counter, shelf, upgrade spot, cat");
                foreach (ShopStation station in stations)
                {
                    Assert.That(station.GetComponent<Interactable>(), Is.Not.Null, $"{station.name}: no Interactable");
                    Assert.That(station.GetComponent<Collider>(), Is.Not.Null, $"{station.name}: no collider to click");
                    Assert.That(station.Verb, Is.Not.Empty, $"{station.name}: no verb for the hover label");
                    bool loadsScene = !string.IsNullOrEmpty(station.SceneToLoad);
                    Assert.That(loadsScene || station.Used.GetPersistentEventCount() > 0, Is.True, $"{station.name}: clicking it does nothing");
                }

                Assert.That(stations.Count(s => s.SceneToLoad == "PF_RepairBench"), Is.EqualTo(1), "exactly one station opens the repair bench");
                Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PipelineOverride>(true)).Single().Pipeline,
                    Is.Not.Null, "the shop must switch to the long-shadow pipeline asset");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- helpers -------------------------------------------------------------------------------------

        private static Transform FindDeep(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static Bounds RendererBounds(GameObject go, bool includeInactive)
        {
            // Prefab assets are not in a scene, so compute bounds from meshes + transforms.
            bool has = false;
            Bounds result = default;
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || (!includeInactive && !IsActiveUnder(filter.transform, go.transform)))
                {
                    continue;
                }

                Bounds local = filter.sharedMesh.bounds;
                Matrix4x4 m = filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (has)
                    {
                        result.Encapsulate(p);
                    }
                    else
                    {
                        result = new Bounds(p, Vector3.zero);
                        has = true;
                    }
                }
            }

            return result;
        }

        private static bool IsActiveUnder(Transform t, Transform root)
        {
            for (Transform c = t; c != null; c = c.parent)
            {
                if (!c.gameObject.activeSelf)
                {
                    return false;
                }

                if (c == root)
                {
                    break;
                }
            }

            return true;
        }
    }
}

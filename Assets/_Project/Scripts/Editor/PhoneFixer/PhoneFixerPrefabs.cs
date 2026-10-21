using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhoneRepair.Devices;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Phone Fixer models -> Unity: import settings + material remap, environment prefabs (mesh
    /// colliders, shadow-only proxies), the Mochi Dango prefab with fault visuals, the PF tool set, and
    /// the customers (generic rig, shared clips from CHR_PF_Base, one Animator Controller).
    /// Prefabs are regenerated from the models on every run and saved over the same paths (same GUIDs).
    /// </summary>
    internal static class PhoneFixerPrefabs
    {
        public const string ShopPrefabPath = PhoneFixerPaths.PrefabsFolder + "/ENV_PF_Shop.prefab";
        public const string BenchPrefabPath = PhoneFixerPaths.PrefabsFolder + "/ENV_PF_RepairBench.prefab";

        /// <summary>No BatchingStatic: the hover highlight redraws the original meshes.</summary>
        private const StaticEditorFlags EnvironmentFlags =
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;

        // ---- import ----------------------------------------------------------------------------------

        public static IEnumerable<string> StaticModelPaths()
        {
            yield return PhoneFixerPaths.ShopFbx;
            yield return PhoneFixerPaths.BenchFbx;
            yield return PhoneFixerPaths.PhoneFbx;
            foreach (string tool in AllToolModels())
            {
                yield return PhoneFixerPaths.ToolFbx(tool);
            }
        }

        public static IEnumerable<string> AllToolModels() =>
            new[] { "Screwdriver", "Spudger", "SuctionCup" }.Concat(PhoneFixerPaths.PropTools).Append("Magnifier");

        /// <summary>Applies the standard import settings and maps every M_PF_* slot to the generated materials.</summary>
        public static void ConfigureModels()
        {
            foreach (string path in StaticModelPaths())
            {
                RequireFile(path);
                ModelImportSetup.ConfigureAndRemap(path);
            }

            ConfigureCharacter(PhoneFixerPaths.BaseCharacterFbx, withClips: true);
            foreach (string customer in PhoneFixerPaths.CustomerNames)
            {
                ConfigureCharacter(PhoneFixerPaths.CharacterFbx(customer), withClips: false);
            }
        }

        private static void ConfigureCharacter(string path, bool withClips)
        {
            RequireFile(path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            ModelImportSetup.ApplyStandardSettings(importer);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importAnimation = withClips;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            importer.SaveAndReimport();

            if (!withClips)
            {
                return;
            }

            // Blender names the takes "Armature|Idle"; keep the clip names the controller and code use.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            var renamed = new List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                if (!PhoneFixerPaths.AnimationClips.Contains(name))
                {
                    continue;
                }

                clip.name = name;
                clip.loopTime = PhoneFixerPaths.LoopingClips.Contains(name);
                clip.loopPose = false;
                clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = false;
                renamed.Add(clip);
            }

            if (renamed.Count != PhoneFixerPaths.AnimationClips.Length)
            {
                Debug.LogWarning($"[PhoneFixer] {path}: expected clips {string.Join(", ", PhoneFixerPaths.AnimationClips)}, " +
                                 $"found takes {string.Join(", ", clips.Select(c => c.name))}.");
            }

            importer.clipAnimations = renamed.ToArray();
            importer.SaveAndReimport();
        }

        // ---- environment -----------------------------------------------------------------------------

        public static GameObject BuildShop() => BuildEnvironment(PhoneFixerPaths.ShopFbx, ShopPrefabPath);

        public static GameObject BuildBench() => BuildEnvironment(PhoneFixerPaths.BenchFbx, BenchPrefabPath);

        private static GameObject BuildEnvironment(string fbxPath, string prefabPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath)
                        ?? throw new InvalidOperationException($"Model not found at {fbxPath}.");
            EditorAssetUtil.EnsureFolder(Path.GetDirectoryName(prefabPath));
            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObject go = renderer.gameObject;
                    string name = go.name;
                    if (name.StartsWith("ShadowProxy_", StringComparison.Ordinal))
                    {
                        // Closes the cut-away box for the sun only: invisible, unpickable.
                        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                        renderer.receiveShadows = false;
                        continue;
                    }

                    if (name.EndsWith("_Bulb", StringComparison.Ordinal))
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.Off; // the light sits inside it
                    }

                    if (go.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null && !go.TryGetComponent(out Collider _))
                    {
                        go.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    }
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, EnvironmentFlags);
                }

                return Save(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---- phone -----------------------------------------------------------------------------------

        /// <summary>Interactive Mochi Dango prefab (PhonePrefabBuilder) plus its damage meshes, all hidden by default.</summary>
        public static GameObject BuildPhone(PhoneModelDefinition definition)
        {
            GameObject saved = PhonePrefabBuilder.Build(definition, PhoneFixerPaths.PhoneFbx);
            string path = AssetDatabase.GetAssetPath(saved);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var faults = EditorAssetUtil.GetOrAdd<PhoneFaultVisuals>(contents);
                foreach (string state in PhoneFaultVisuals.KnownStates)
                {
                    if (EditorAssetUtil.FindDeep(contents.transform, state) == null)
                    {
                        Debug.LogWarning($"[PhoneFixer] {PhoneFixerPaths.PhoneFbx} has no damage mesh '{state}'.");
                    }
                }

                faults.Apply(); // nothing listed -> every damage mesh hidden in the prefab
                PrefabUtility.SaveAsPrefabAsset(contents, path, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException($"Could not save {path}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ---- tools -----------------------------------------------------------------------------------

        private static readonly (string asset, string id, string name, ToolKind kind, string model, string description)[] Tools =
        {
            ("TOOL_PF_Hands", "tool.pf.hands", "Hands", ToolKind.Hands, null,
                "Ten fingers, two thumbs, one steady breath. Lifts parts out and seats them again."),
            ("TOOL_PF_Screwdriver", "tool.pf.screwdriver", "Screwdriver", ToolKind.Screwdriver, "Screwdriver",
                "P2 driver with a teal grip and a brass ferrule. Lefty-loosey, righty-tighty."),
            ("TOOL_PF_Spudger", "tool.pf.spudger", "Spudger", ToolKind.Spudger, "Spudger",
                "Sakura-pink nylon pry stick. Pops covers and unplugs connectors without a scratch."),
            ("TOOL_PF_SuctionCup", "tool.pf.suction_cup", "Suction Cup", ToolKind.SuctionCup, "SuctionCup",
                "Grabs a screen by its face. The 'thwop' is free."),
        };

        /// <summary>Creates any missing PF tool assets and returns them in hotkey order (fresh from disk).</summary>
        public static List<ToolDefinition> EnsureTools()
        {
            foreach (var spec in Tools)
            {
                var tool = EditorAssetUtil.LoadOrCreate<ToolDefinition>($"{PhoneFixerPaths.ToolDataFolder}/{spec.asset}.asset", out bool created);
                if (created)
                {
                    EditorAssetUtil.Wire(tool,
                        ("toolId", spec.id),
                        ("displayName", spec.name),
                        ("kind", spec.kind),
                        ("description", spec.description));
                }

                if (spec.model != null && tool.Model == null)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.ToolFbx(spec.model));
                    if (model == null)
                    {
                        Debug.LogWarning($"[PhoneFixer] {PhoneFixerPaths.ToolFbx(spec.model)} not found; run build_pf_tools.py.");
                    }
                    else
                    {
                        EditorAssetUtil.Wire(tool, ("model", model));
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return LoadTools();
        }

        public static List<ToolDefinition> LoadTools() =>
            Tools.Select(spec =>
            {
                string path = $"{PhoneFixerPaths.ToolDataFolder}/{spec.asset}.asset";
                return AssetDatabase.LoadAssetAtPath<ToolDefinition>(path)
                       ?? throw new InvalidOperationException($"Tool asset missing at {path}; run the Phone Fixer setup again.");
            }).ToList();

        // ---- characters ------------------------------------------------------------------------------

        /// <summary>One controller for every customer: a state per clip, Idle by default.</summary>
        public static AnimatorController EnsureCustomerController()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(PhoneFixerPaths.BaseCharacterFbx)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length == 0)
            {
                throw new InvalidOperationException($"{PhoneFixerPaths.BaseCharacterFbx} has no animation clips; re-run build_pf_customers.py.");
            }

            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.CharacterDataFolder);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PhoneFixerPaths.CustomerControllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(PhoneFixerPaths.CustomerControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            AnimatorState idle = null;
            for (int i = 0; i < PhoneFixerPaths.AnimationClips.Length; i++)
            {
                string clipName = PhoneFixerPaths.AnimationClips[i];
                AnimationClip clip = clips.FirstOrDefault(c => c.name == clipName);
                if (clip == null)
                {
                    Debug.LogWarning($"[PhoneFixer] Clip '{clipName}' missing in {PhoneFixerPaths.BaseCharacterFbx}.");
                    continue;
                }

                AnimatorState state = machine.AddState(clipName, new Vector3(300f, 60f * i, 0f));
                state.motion = clip;
                state.writeDefaultValues = true;
                if (clipName == "Idle")
                {
                    idle = state;
                }
            }

            if (idle != null)
            {
                machine.defaultState = idle;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>CHR_PF_&lt;Name&gt;.prefab per customer: Animator (shared controller), CustomerActor, height scale.</summary>
        public static Dictionary<string, GameObject> BuildCharacters(RuntimeAnimatorController controller)
        {
            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.CharacterPrefabsFolder);
            var result = new Dictionary<string, GameObject>();
            foreach (string customer in PhoneFixerPaths.CustomerNames)
            {
                string fbx = PhoneFixerPaths.CharacterFbx(customer);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx)
                            ?? throw new InvalidOperationException($"Model not found at {fbx}.");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
                try
                {
                    root.transform.localScale = Vector3.one * PhoneFixerPaths.HeightScale(customer);
                    var animator = EditorAssetUtil.GetOrAdd<Animator>(root);
                    animator.runtimeAnimatorController = controller;
                    animator.avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    EditorAssetUtil.GetOrAdd<CustomerActor>(root);
                    foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        skin.updateWhenOffscreen = false;
                        skin.shadowCastingMode = ShadowCastingMode.On;
                    }

                    result[customer] = Save(root, PhoneFixerPaths.CharacterPrefab(customer));
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            return result;
        }

        // ---- helpers ---------------------------------------------------------------------------------

        private static GameObject Save(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                throw new InvalidOperationException($"Could not save {path}.");
            }

            return saved;
        }

        private static void RequireFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"{path} is missing. Run the Blender generators in ArtSource/Blender/Scripts (build_pf_*.py, build_phone_mochi_dango.py).");
            }
        }
    }
}

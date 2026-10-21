using System;
using System.Collections.Generic;
using System.Linq;
using PhoneRepair.Devices;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// One-click (or one-command) integration of the Phone Fixer art pass:
    /// palette materials -> model import -> UI sprites -> Mochi Dango data + PF tools -> prefabs
    /// (phone, shop, bench, customers) -> rendering assets -> scenes (PF_MainMenu, PF_Shop, PF_RepairBench).
    /// Leaves the M1/M2 assets and M1_Workbench alone. Materials, data and rendering assets are created
    /// once and then kept; prefabs and the PF scenes are regenerated on every run.
    ///
    /// Menu:  Tools > Phone Fixer > Run Art Setup
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod PhoneRepair.EditorTools.PhoneFixerSetup.RunBatch
    /// </summary>
    public static class PhoneFixerSetup
    {
        private const int StepCount = 8;

        [MenuItem("Tools/Phone Fixer/Run Art Setup", priority = 40)]
        public static void RunFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Phone Fixer: art setup",
                    "Imports the Phone Fixer models, creates the PF materials, data and prefabs, and REGENERATES " +
                    "PF_MainMenu, PF_Shop and PF_RepairBench.\n\nM1/M2 content is not touched. Existing PF materials and data are kept.",
                    "Run setup", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                Run();
                EditorSceneManager.OpenScene(PhoneFixerPaths.ShopScene);
                EditorUtility.DisplayDialog("Phone Fixer", "Done. Open PF_MainMenu or PF_Shop and press Play.", "Nice");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Phone Fixer: setup failed", e.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Entry point for -executeMethod. Exits with code 1 on failure.</summary>
        public static void RunBatch()
        {
            try
            {
                Run();
                Debug.Log("[PhoneFixer] Art setup complete.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            Step(1, "Project basics (URP, TMP, highlight material)", () =>
            {
                RenderPipelineSetup.EnsureUrp();
                TmpSetup.EnsureEssentials();
                MaterialLibrary.EnsureHighlightMaterial();
            });

            Step(2, "Palette materials", () =>
            {
                int created = PhoneFixerMaterials.EnsureAll();
                PhoneFixerRendering.EnsureSkybox();
                Debug.Log($"[PhoneFixer] Palette materials: {created} created.");
            });

            Step(3, "Model import + material remap", PhoneFixerPrefabs.ConfigureModels);

            Step(4, "UI sprites + portraits", () =>
            {
                PhoneFixerUiKit.EnsureSprites();
                foreach (string customer in PhoneFixerPaths.CustomerNames)
                {
                    PhoneFixerUiKit.Portrait(customer);
                }
            });

            PhoneModelDefinition dango = null;
            Step(5, "Mochi Dango data + PF tools", () =>
            {
                dango = MochiDangoDataSetup.EnsureData();
                PhoneFixerPrefabs.EnsureTools();
            });

            var content = new PhoneFixerScenes.Content();
            Step(6, "Prefabs (phone, shop, bench, customers)", () =>
            {
                GameObject phone = PhoneFixerPrefabs.BuildPhone(dango);
                content.PhonePrefab = AssetDatabase.GetAssetPath(phone);
                PhoneFixerPrefabs.BuildShop();
                PhoneFixerPrefabs.BuildBench();
                var controller = PhoneFixerPrefabs.EnsureCustomerController();
                Dictionary<string, GameObject> customers = PhoneFixerPrefabs.BuildCharacters(controller);
                Debug.Log($"[PhoneFixer] Customers: {string.Join(", ", customers.Keys)}");
            });

            Step(7, "Rendering (shop pipeline, volumes)", () =>
            {
                PhoneFixerRendering.EnsureShopPipeline();
                PhoneFixerRendering.EnsureBenchVolume();
                List<ToolDefinition> tools = PhoneFixerPrefabs.LoadTools();
                content.ToolPaths.AddRange(tools.Select(AssetDatabase.GetAssetPath));
            });

            Step(8, "Scenes (PF_MainMenu, PF_Shop, PF_RepairBench)", () =>
            {
                PhoneFixerScenes.BuildMenu(content);
                PhoneFixerScenes.BuildShop(content);
                PhoneFixerScenes.BuildBench(content);
            });

            AssetDatabase.SaveAssets();
        }

        private static void Step(int index, string title, Action action)
        {
            Debug.Log($"[PhoneFixer] Setup {index}/{StepCount}: {title}");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Phone Fixer setup", title, index / (float)StepCount);
            }

            action();
        }
    }
}

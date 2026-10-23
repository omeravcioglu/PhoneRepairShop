using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using PhoneRepair.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// One-click (or one-command) setup of the 2D game: URP 2D renderer -> sprite material -> pixel font
    /// -> pixel art (placeholders for anything missing, import settings from the art contract) ->
    /// Mochi One data -> tools, customers, calendar -> 2D phone prefab -> Shop scene -> art contract doc.
    /// Idempotent. Real art and edited data assets are never overwritten; the prefab and the scene are
    /// regenerated.
    ///
    /// Menu:  Tools > Phone Repair > Run Setup
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod PhoneRepair.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        private const int StepCount = 9;

        [MenuItem("Tools/Phone Repair/Run Setup", priority = 0)]
        public static void RunFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Phone Repair: setup",
                    "Sets up the 2D renderer, fills in any missing art with placeholders, refreshes the data, rebuilds the phone prefab and REGENERATES " +
                    ShopSceneBuilder.ScenePath + ".\n\nYour art and edited data assets are kept.",
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
                EditorSceneManager.OpenScene(ShopSceneBuilder.ScenePath);
                EditorUtility.DisplayDialog("Phone Repair", "Setup complete. Press Play in the Shop scene.", "Nice");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Phone Repair: setup failed", e.Message, "OK");
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
                Debug.Log("[PhoneRepair] Setup complete.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            Step(1, "2D render pipeline", () => Pipeline2DSetup.EnsurePipeline());

            Material spriteMaterial = null;
            Step(2, "Sprite material", () => spriteMaterial = Pipeline2DSetup.EnsureSpriteMaterial());

            PixelFont font = null;
            Step(3, "Pixel font", () => font = PixelFontBuilder.Build());

            Step(4, "Pixel art: placeholders and import settings", () => PixelArtSetup.EnsurePlaceholders());

            PhoneModelDefinition model = null;
            Step(5, "Mochi One data (parts, repair data, assembly rules)", () => model = MochiOneDataSetup.EnsureData());

            List<ToolDefinition> tools = null;
            ShopCalendar calendar = null;
            Step(6, "Tools, customers, calendar", () =>
            {
                tools = ShopDataSetup.EnsureTools();
                List<CustomerDefinition> customers = ShopDataSetup.EnsureCustomers(model);
                calendar = ShopDataSetup.EnsureCalendar(customers);
            });

            Step(7, "2D phone prefab", () => Phone2DPrefabBuilder.Build(model, spriteMaterial));
            Step(8, "Shop scene", () => ShopSceneBuilder.Build(spriteMaterial, font, tools, calendar));
            Step(9, "Art contract doc and contact sheet", ArtContractReport.Write);

            AssetDatabase.SaveAssets();
        }

        private static void Step(int index, string title, Action action)
        {
            Debug.Log($"[PhoneRepair] Setup {index}/{StepCount}: {title}");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Phone Repair setup", title, index / (float)StepCount);
            }

            action();
        }
    }
}

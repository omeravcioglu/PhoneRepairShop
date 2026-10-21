using System;
using System.Collections.Generic;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// One-click (or one-command) setup for the repair bench (Milestones 1-2):
    /// URP -> TMP resources -> materials -> model import -> Mochi One data -> tools -> prefabs -> scene.
    /// Idempotent: data assets and materials are created once and then left alone; prefabs are
    /// updated in place; the sandbox scene is regenerated.
    ///
    /// Menu:  Tools > Phone Repair > Run Setup
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod PhoneRepair.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Phone Repair/Run Setup", priority = 0)]
        public static void RunFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Phone Repair: setup",
                    "Configures URP, creates materials and the Mochi One data, (re)builds the prefabs and REGENERATES " +
                    WorkbenchSceneBuilder.ScenePath + ".\n\nExisting materials and data assets are kept.",
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
                EditorSceneManager.OpenScene(WorkbenchSceneBuilder.ScenePath);
                EditorUtility.DisplayDialog("Phone Repair", "Setup complete. Press Play in M1_Workbench.", "Nice");
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

        private const int StepCount = 9;

        public static void Run()
        {
            Step(1, "Render pipeline (URP)", () => RenderPipelineSetup.EnsureUrp());
            Step(2, "TextMeshPro resources", TmpSetup.EnsureEssentials);
            Step(3, "Materials", () => MaterialLibrary.EnsureAll());

            GameObject phoneModel = null;
            Step(4, "Model import + material remap", () =>
            {
                phoneModel = ModelImportSetup.ConfigureAndRemap(MochiOneDataSetup.FbxPath);
                ModelImportSetup.ConfigureAndRemap(EnvironmentPrefabBuilder.FbxPath);
                foreach (string toolModel in ToolDataSetup.ModelPaths())
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(toolModel) != null)
                    {
                        ModelImportSetup.ConfigureAndRemap(toolModel);
                    }
                }
            });

            Devices.PhoneModelDefinition definition = null;
            Step(5, "Mochi One data (parts, repair data, assembly rules)", () => definition = MochiOneDataSetup.EnsureData());

            List<ToolDefinition> tools = null;
            Step(6, "Tools", () => tools = ToolDataSetup.EnsureTools());

            GameObject phonePrefab = null;
            GameObject environmentPrefab = null;
            Step(7, "Prefabs", () =>
            {
                phonePrefab = PhonePrefabBuilder.Build(definition, MochiOneDataSetup.FbxPath);
                environmentPrefab = EnvironmentPrefabBuilder.Build();
            });

            VolumeProfile volume = null;
            Step(8, "Post-processing profile", () => volume = RenderPipelineSetup.EnsureWorkbenchVolumeProfile());
            Step(9, "Workbench scene", () =>
                WorkbenchSceneBuilder.Build(phonePrefab, environmentPrefab, volume, MaterialLibrary.EnsureHighlightMaterial(), tools));

            AssetDatabase.SaveAssets();
            Debug.Log($"[PhoneRepair] Phone model imported: {phoneModel.name} ({phoneModel.transform.childCount} parts at root), {tools.Count} tools.");
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

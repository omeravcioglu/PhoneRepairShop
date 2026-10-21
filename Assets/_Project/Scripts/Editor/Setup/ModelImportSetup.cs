using System;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Import rules for every model under Assets/_Project/Art/Models (Blender FBX exported with
    /// ArtSource/Blender/Scripts/prs_bpy.py: metres, -Z forward, Y up).
    /// Applied on first import only, so later manual tweaks in the inspector are respected.
    /// </summary>
    internal sealed class ModelImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!ModelImportSetup.IsProjectModel(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            if (importer.importSettingsMissing)
            {
                ModelImportSetup.ApplyStandardSettings(importer);
            }
        }
    }

    internal static class ModelImportSetup
    {
        public const string ModelsRoot = "Assets/_Project/Art/Models/";

        public static bool IsProjectModel(string assetPath) =>
            assetPath.StartsWith(ModelsRoot, StringComparison.Ordinal) &&
            assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        public static void ApplyStandardSettings(ModelImporter importer)
        {
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;          // Blender Z-up -> Unity Y-up baked into the whole hierarchy
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;   // keeps Blender's hardened bevel normals
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.addCollider = false;                // gameplay colliders are added by the prefab builders
            importer.preserveHierarchy = false;
            importer.sortHierarchyByName = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        /// <summary>Applies standard settings and maps the model's material slots to project materials by name.</summary>
        public static GameObject ConfigureAndRemap(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter
                           ?? throw new InvalidOperationException($"No model found at {fbxPath}. Run the Blender export script first (see ArtSource/Blender/README.md).");
            ApplyStandardSettings(importer);
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            ReportUnmappedMaterials(fbxPath, model);
            return model;
        }

        private static void ReportUnmappedMaterials(string fbxPath, GameObject model)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && AssetDatabase.GetAssetPath(m) == fbxPath)
                    {
                        Debug.LogWarning($"[PhoneRepair] {fbxPath}: material slot '{m.name}' on '{r.name}' has no project material with that name (using the embedded fallback).");
                    }
                }
            }
        }
    }
}

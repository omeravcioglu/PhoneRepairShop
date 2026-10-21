using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Creates and assigns the project's URP asset (+ renderer with SSAO) and the workbench
    /// post-processing profile. Safe to run repeatedly: existing assets are reused.
    /// </summary>
    internal static class RenderPipelineSetup
    {
        public const string Folder = "Assets/_Project/Settings/Rendering";
        public const string PipelineAssetPath = Folder + "/PRS_URP.asset";
        public const string RendererPath = Folder + "/PRS_URP_Renderer.asset";
        public const string WorkbenchVolumePath = Folder + "/PRS_Volume_Workbench.asset";
        private const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        public static UniversalRenderPipelineAsset EnsureUrp()
        {
            EditorAssetUtil.EnsureFolder(Folder);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (asset == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                ResourceReloader.TryReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
                var ssao = AddRendererFeature<ScreenSpaceAmbientOcclusion>(rendererData, "SSAO");
                var ssaoSettings = new SerializedObject(ssao);
                ssaoSettings.FindProperty("m_Settings.Intensity").floatValue = 1.4f; // default 3 is heavy at tabletop scale
                ssaoSettings.FindProperty("m_Settings.Radius").floatValue = 0.03f;
                ssaoSettings.ApplyModifiedPropertiesWithoutUndo();

                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, PipelineAssetPath);
                ConfigurePipelineAsset(asset);
                Debug.Log($"[PhoneRepair] Created URP asset at {PipelineAssetPath}");
            }

            if (GraphicsSettings.defaultRenderPipeline != asset)
            {
                GraphicsSettings.defaultRenderPipeline = asset;
            }

            for (int i = 0; i < QualitySettings.count; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) != null && QualitySettings.GetRenderPipelineAssetAt(i) != asset)
                {
                    Debug.LogWarning($"[PhoneRepair] Quality level {QualitySettings.names[i]} overrides the render pipeline asset; it will not use {PipelineAssetPath}.");
                }
            }

            // URP drops its global settings at the Assets root the first time it runs; keep the root tidy.
            // MoveAsset keeps GUIDs, so Graphics settings still find them.
            MoveIfPresent("Assets/UniversalRenderPipelineGlobalSettings.asset");
            MoveIfPresent("Assets/DefaultVolumeProfile.asset");

            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void MoveIfPresent(string assetPath)
        {
            string destination = $"{Folder}/{System.IO.Path.GetFileName(assetPath)}";
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null || AssetDatabase.LoadMainAssetAtPath(destination) != null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(assetPath, destination);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[PhoneRepair] Could not move {assetPath}: {error}");
            }
        }

        public static VolumeProfile EnsureWorkbenchVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(WorkbenchVolumePath);
            if (profile != null)
            {
                return profile;
            }

            EditorAssetUtil.EnsureFolder(Folder);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, WorkbenchVolumePath);

            // Neutral tonemapping keeps pastel colours honest (ACES would push them warmer/darker).
            AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

            var bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.6f);

            var colour = AddOverride<ColorAdjustments>(profile);
            colour.postExposure.Override(-0.1f);
            colour.contrast.Override(8f);
            colour.saturation.Override(10f);

            AddOverride<WhiteBalance>(profile).temperature.Override(6f);

            var vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void ConfigurePipelineAsset(UniversalRenderPipelineAsset asset)
        {
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            asset.renderScale = 1f;
            // Tabletop scale: short shadow distance = crisp contact shadows for millimetre parts.
            asset.shadowDistance = 4f;
            asset.shadowCascadeCount = 2;
            asset.mainLightShadowmapResolution = 4096;
            asset.shadowDepthBias = 0.5f;
            asset.shadowNormalBias = 0.4f;

            var so = new SerializedObject(asset);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = (int)SoftShadowQuality.High;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static T AddRendererFeature<T>(UniversalRendererData rendererData, string featureName) where T : ScriptableRendererFeature
        {
            if (rendererData.rendererFeatures.Find(f => f is T) is T existing)
            {
                return existing;
            }

            // Mirrors what the renderer inspector's "Add Renderer Feature" button does.
            var feature = ScriptableObject.CreateInstance<T>();
            feature.name = featureName;
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var so = new SerializedObject(rendererData);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            return feature;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}

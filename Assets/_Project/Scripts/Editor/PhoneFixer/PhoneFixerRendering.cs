using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Rendering for the Phone Fixer scenes. The shop diorama is seen from ~20 m and needs ~30 m of
    /// shadows plus room-scale SSAO, so it gets its own URP asset (PF_URP_Shop, applied by the scene's
    /// PipelineOverride); the bench keeps the project asset (4 m, crisp millimetre shadows).
    /// Both get a "golden hour, forever" post stack. Existing assets are reused, never overwritten.
    /// </summary>
    internal static class PhoneFixerRendering
    {
        private const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        public static UniversalRenderPipelineAsset EnsureShopPipeline()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PhoneFixerPaths.ShopPipelinePath);
            if (asset != null)
            {
                return asset;
            }

            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.RenderingFolder);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
            AssetDatabase.CreateAsset(rendererData, PhoneFixerPaths.ShopRendererPath);
            ResourceReloader.TryReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            var ssao = AddRendererFeature<ScreenSpaceAmbientOcclusion>(rendererData, "SSAO");
            var ssaoSettings = new SerializedObject(ssao);
            ssaoSettings.FindProperty("m_Settings.Intensity").floatValue = 1.1f;
            ssaoSettings.FindProperty("m_Settings.Radius").floatValue = 0.35f; // room scale (the bench uses 3 cm)
            ssaoSettings.ApplyModifiedPropertiesWithoutUndo();

            asset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(asset, PhoneFixerPaths.ShopPipelinePath);
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            asset.renderScale = 1f;
            asset.shadowDistance = 30f;     // camera ~20 m away, back wall ~24 m
            asset.shadowCascadeCount = 1;   // the whole diorama fits one 4k map (~8 mm texels)
            asset.mainLightShadowmapResolution = 4096;
            asset.shadowDepthBias = 1f;
            asset.shadowNormalBias = 0.8f;

            var so = new SerializedObject(asset);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = (int)SoftShadowQuality.High;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            SerializedProperty additionalResolution = so.FindProperty("m_AdditionalLightsShadowmapResolution");
            if (additionalResolution != null)
            {
                additionalResolution.intValue = 2048; // two pendant point lights, 6 faces each
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PhoneFixer] Created {PhoneFixerPaths.ShopPipelinePath}");
            return asset;
        }

        /// <summary>
        /// Warm, slightly lifted diorama look. No depth of field: the same profile serves the 20 m diorama
        /// view and the 1.8 m counter close-up, and one focus distance cannot suit both.
        /// </summary>
        public static VolumeProfile EnsureShopVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PhoneFixerPaths.ShopVolumePath);
            if (profile != null && profile.TryGet(out DepthOfField oldDepth))
            {
                // Early builds of this profile had a tilt-shift DoF; it blurred the counter close-up.
                profile.Remove<DepthOfField>();
                Object.DestroyImmediate(oldDepth, true);
                EditorUtility.SetDirty(profile);
            }

            if (profile == null)
            {
                profile = CreateProfile(PhoneFixerPaths.ShopVolumePath);
                AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

                // Style guide post stack: neutral tonemap, bloom 0.35 @ 1.1, contrast +8 (+4 for the
                // far diorama), saturation +6, warmth +6, navy vignette 0.18.
                var bloom = AddOverride<Bloom>(profile);
                bloom.threshold.Override(1.1f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.65f);
                bloom.tint.Override(EditorAssetUtil.Hex("#FFE2C4"));

                var colour = AddOverride<ColorAdjustments>(profile);
                colour.postExposure.Override(0f);
                colour.contrast.Override(12f);
                colour.saturation.Override(6f);

                AddOverride<WhiteBalance>(profile).temperature.Override(6f);

                var vignette = AddOverride<Vignette>(profile);
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);
                vignette.color.Override(EditorAssetUtil.Hex("#283747"));
                EditorUtility.SetDirty(profile);
            }

            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>The close-up: same warmth, crisper (no depth of field, gentler vignette).</summary>
        public static VolumeProfile EnsureBenchVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PhoneFixerPaths.BenchVolumePath);
            if (profile != null)
            {
                return profile;
            }

            profile = CreateProfile(PhoneFixerPaths.BenchVolumePath);
            AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

            var bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(EditorAssetUtil.Hex("#FFE2C4"));

            var colour = AddOverride<ColorAdjustments>(profile);
            colour.postExposure.Override(0f);
            colour.contrast.Override(8f);
            colour.saturation.Override(6f);

            AddOverride<WhiteBalance>(profile).temperature.Override(6f);

            var vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(EditorAssetUtil.Hex("#3A2E2A"));

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>
        /// Warm procedural sky. Never seen (the cameras clear to the paper backdrop) but it feeds
        /// reflections, so glass and brass outside the reflection probes pick up a golden-hour tint.
        /// </summary>
        public static Material EnsureSkybox()
        {
            string path = PhoneFixerPaths.MaterialsFolder + "/M_PF_Sky.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Skybox/Procedural") ?? throw new System.InvalidOperationException("Shader 'Skybox/Procedural' not found.");
            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.MaterialsFolder);
            material = new Material(shader) { name = "M_PF_Sky" };
            material.SetColor("_SkyTint", new Color(0.66f, 0.6f, 0.52f));
            material.SetColor("_GroundColor", new Color(0.45f, 0.37f, 0.3f));
            material.SetFloat("_AtmosphereThickness", 0.6f);
            material.SetFloat("_Exposure", 0.95f);
            material.SetFloat("_SunSize", 0.03f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static VolumeProfile CreateProfile(string path)
        {
            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.RenderingFolder);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static T AddRendererFeature<T>(UniversalRendererData rendererData, string featureName) where T : ScriptableRendererFeature
        {
            if (rendererData.rendererFeatures.Find(f => f is T) is T existing)
            {
                return existing;
            }

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
    }
}

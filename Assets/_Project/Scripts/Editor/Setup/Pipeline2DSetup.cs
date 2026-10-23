using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// The 2D render setup: a URP asset with the 2D Renderer (no HDR, no MSAA: pixel art wants hard
    /// edges), assigned as the project pipeline, plus the shared sprite material.
    /// Safe to run repeatedly; existing assets are reused.
    /// </summary>
    internal static class Pipeline2DSetup
    {
        public const string Folder = "Assets/_Project/Settings/Rendering";
        public const string PipelinePath = Folder + "/PRS_URP_2D.asset";
        public const string RendererPath = Folder + "/PRS_URP_2D_Renderer.asset";
        public const string MaterialFolder = "Assets/_Project/Art/Materials";
        public const string SpriteMaterialPath = MaterialFolder + "/M_PixelSprite.mat";
        public const string SpriteShaderPath = "Assets/_Project/Art/Shaders/PixelSprite.shader";

        public static UniversalRenderPipelineAsset EnsurePipeline()
        {
            EditorAssetUtil.EnsureFolder(Folder);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (asset == null)
            {
                var rendererData = ScriptableObject.CreateInstance<Renderer2DData>(); // no post-processing: pixel art stays exact
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                ResourceReloader.TryReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);

                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, PipelinePath);
                Debug.Log($"[PhoneRepair] Created the 2D URP asset at {PipelinePath}");
            }

            asset.supportsHDR = false;
            asset.msaaSampleCount = 1;
            asset.renderScale = 1f;
            asset.useSRPBatcher = true;
            EditorUtility.SetDirty(asset);

            if (GraphicsSettings.defaultRenderPipeline != asset)
            {
                GraphicsSettings.defaultRenderPipeline = asset;
            }

            for (int i = 0; i < QualitySettings.count; i++)
            {
                RenderPipelineAsset levelAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (levelAsset != null && levelAsset != asset)
                {
                    Debug.LogWarning($"[PhoneRepair] Quality level {QualitySettings.names[i]} overrides the render pipeline; it will not use {PipelinePath}.");
                }
            }

            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>The one material every sprite uses (unlit, with hover outline and flash).</summary>
        public static Material EnsureSpriteMaterial()
        {
            EditorAssetUtil.EnsureFolder(MaterialFolder);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SpriteShaderPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_PixelSprite" };
                AssetDatabase.CreateAsset(material, SpriteMaterialPath);
            }

            material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}

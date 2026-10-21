using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Creates the project's URP materials. Names match the material slot names exported from
    /// Blender, so models pick them up through ModelImporter's "Search and Remap" by name.
    /// Existing materials are left alone (artists own them once created) unless overwrite is set.
    /// </summary>
    internal static class MaterialLibrary
    {
        public const string PhoneFolder = "Assets/_Project/Art/Materials/Phone";
        public const string EnvironmentFolder = "Assets/_Project/Art/Materials/Environment";
        public const string ToolFolder = "Assets/_Project/Art/Materials/Tools";
        public const string FxFolder = "Assets/_Project/Art/Materials/FX";
        public const string HighlightMaterialPath = FxFolder + "/M_InteractableHighlight.mat";
        public const string HighlightShaderName = "PhoneRepair/Interactable Highlight";
        public const string WorkbenchSkyboxPath = EnvironmentFolder + "/M_Sky_Workbench.mat";

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";

        private readonly struct Spec
        {
            public readonly string Name;
            public readonly string Hex;
            public readonly float Smoothness;
            public readonly float Metallic;
            public readonly float Emission;
            public readonly bool Unlit;

            public Spec(string name, string hex, float smoothness, float metallic = 0f, float emission = 0f, bool unlit = false)
            {
                Name = name;
                Hex = hex;
                Smoothness = smoothness;
                Metallic = metallic;
                Emission = emission;
                Unlit = unlit;
            }
        }

        // Cozy pastel palette. Keep in sync with the colours in ArtSource/Blender/Scripts/*.py
        // (Blender colours only affect previews; these materials are what the game renders).
        private static readonly Spec[] PhoneMaterials =
        {
            new("M_Phone_Frame", "#F3E9DA", 0.55f),
            new("M_Phone_Midplate", "#B9C0C9", 0.65f, 0.6f),
            new("M_Phone_Accent", "#EF8FA8", 0.55f),
            new("M_Phone_BackCover", "#F4A7BE", 0.62f),
            new("M_Phone_Logo", "#FFF9F2", 0.5f),
            new("M_Phone_Blush", "#F27E9B", 0.5f),
            new("M_Phone_Ink", "#2B2433", 0.4f),
            new("M_Phone_Glass", "#14172A", 0.93f),
            new("M_Phone_Display", "#232B4D", 0.86f),
            new("M_Phone_Crack", "#EAF6FF", 0f, unlit: true),
            new("M_Phone_Screw", "#C7CDD6", 0.72f, 1f),
            new("M_Phone_ScrewRecess", "#3A3F4A", 0.4f, 0.5f),
            new("M_Phone_Battery", "#4B5A78", 0.45f),
            new("M_Phone_BatteryLabel", "#FFD36E", 0.4f),
            new("M_Phone_PullTab", "#F25C54", 0.4f),
            new("M_Phone_PCB", "#2E8C7E", 0.55f),
            new("M_Phone_Chip", "#2A2D3A", 0.6f),
            new("M_Phone_Metal", "#AEB6C1", 0.7f, 1f),
            new("M_Phone_Gold", "#E7B95B", 0.7f, 1f),
            new("M_Phone_Flex", "#E9A23B", 0.5f),
            new("M_Phone_CameraBody", "#30343F", 0.5f),
            new("M_Phone_Lens", "#0D1630", 0.95f),
            new("M_Phone_LensInner", "#5B7BD6", 0.95f),
            new("M_Phone_Speaker", "#4A505C", 0.4f),
        };

        private static readonly Spec[] EnvironmentMaterials =
        {
            new("M_Env_Floor", "#B98B66", 0.3f),
            new("M_Env_Wall", "#F2E6D6", 0.1f),
            new("M_Env_WoodDark", "#8C5E43", 0.4f),
            new("M_Env_Wood", "#D4A577", 0.45f),
            new("M_Env_Mat", "#8ECDBD", 0.15f),
            new("M_Env_MatLines", "#E8F7F1", 0.15f),
            new("M_Env_Tray", "#9CC9E8", 0.55f),
            new("M_Env_Lamp", "#F7D58B", 0.6f),
            new("M_Env_Metal", "#C7CDD6", 0.7f, 1f),
            new("M_Env_Bulb", "#FFF2D6", 0.3f, emission: 2.5f),
            new("M_Env_Pegboard", "#E8D2B0", 0.25f),
            new("M_Env_PegHole", "#5B4636", 0.1f),
            new("M_Env_Mug", "#FFF4E8", 0.65f),
            new("M_Env_MugStripe", "#F4A6B8", 0.65f),
            new("M_Env_Coffee", "#6B4A3A", 0.85f),
            new("M_Env_Pot", "#D98E6A", 0.2f),
            new("M_Env_Soil", "#5A4636", 0.05f),
            new("M_Env_Plant", "#7FBF8E", 0.4f),
            new("M_Env_PlantLight", "#A8DDB0", 0.4f),
        };

        private static readonly Spec[] ToolMaterials =
        {
            new("M_Tool_Steel", "#C7CDD6", 0.78f, 1f),
            new("M_Tool_Handle", "#F28C82", 0.55f),
            new("M_Tool_Grip", "#2B2433", 0.3f),
            new("M_Tool_Spudger", "#B9A6E8", 0.5f),
            new("M_Tool_Cup", "#9CC9E8", 0.65f),
        };

        public static void EnsureAll(bool overwrite = false)
        {
            Ensure(PhoneFolder, PhoneMaterials, overwrite);
            Ensure(EnvironmentFolder, EnvironmentMaterials, overwrite);
            Ensure(ToolFolder, ToolMaterials, overwrite);
            EnsureHighlightMaterial();
            AssetDatabase.SaveAssets();
        }

        public static Material EnsureHighlightMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(HighlightShaderName)
                            ?? throw new InvalidOperationException($"Shader '{HighlightShaderName}' not found. Is Assets/_Project/Art/Shaders/InteractableHighlight.shader compiling?");
            EditorAssetUtil.EnsureFolder(FxFolder);
            material = new Material(shader) { name = "M_InteractableHighlight" };
            AssetDatabase.CreateAsset(material, HighlightMaterialPath);
            return material;
        }

        /// <summary>
        /// Warm, neutral procedural sky. Never seen directly (the room has walls) but it feeds
        /// reflections, so glossy glass and metal reflect "indoors" instead of a blue sky.
        /// </summary>
        public static Material EnsureWorkbenchSkybox()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchSkyboxPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Skybox/Procedural") ?? throw new InvalidOperationException("Shader 'Skybox/Procedural' not found.");
            EditorAssetUtil.EnsureFolder(EnvironmentFolder);
            material = new Material(shader) { name = "M_Sky_Workbench" };
            material.SetColor("_SkyTint", new Color(0.62f, 0.58f, 0.54f));
            material.SetColor("_GroundColor", new Color(0.42f, 0.34f, 0.29f));
            material.SetFloat("_AtmosphereThickness", 0.55f);
            material.SetFloat("_Exposure", 0.85f);
            material.SetFloat("_SunSize", 0.02f);
            AssetDatabase.CreateAsset(material, WorkbenchSkyboxPath);
            return material;
        }

        public static IEnumerable<string> AllMaterialNames()
        {
            foreach (Spec spec in PhoneMaterials)
            {
                yield return spec.Name;
            }

            foreach (Spec spec in EnvironmentMaterials)
            {
                yield return spec.Name;
            }

            foreach (Spec spec in ToolMaterials)
            {
                yield return spec.Name;
            }
        }

        private static void Ensure(string folder, IEnumerable<Spec> specs, bool overwrite)
        {
            EditorAssetUtil.EnsureFolder(folder);
            Shader lit = Shader.Find(LitShader) ?? throw new InvalidOperationException($"Shader '{LitShader}' not found. Is URP installed?");
            Shader unlit = Shader.Find(UnlitShader) ?? throw new InvalidOperationException($"Shader '{UnlitShader}' not found.");

            foreach (Spec spec in specs)
            {
                string path = $"{folder}/{spec.Name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = material == null;
                if (!isNew && !overwrite)
                {
                    continue;
                }

                if (isNew)
                {
                    material = new Material(spec.Unlit ? unlit : lit) { name = spec.Name };
                }
                else
                {
                    material.shader = spec.Unlit ? unlit : lit;
                }

                Color color = EditorAssetUtil.Hex(spec.Hex);
                material.SetColor("_BaseColor", color);
                if (!spec.Unlit)
                {
                    material.SetFloat("_Smoothness", spec.Smoothness);
                    material.SetFloat("_Metallic", spec.Metallic);
                    if (spec.Emission > 0f)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", color * spec.Emission);
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    EditorUtility.SetDirty(material);
                }
            }
        }
    }
}

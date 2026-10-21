using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Creates the art-direction palette materials (M_PF_*) from pf_palette.json. The Blender generators
    /// write that file from pf_bpy.PALETTE, which is the single source of truth for colour, smoothness,
    /// metallic and emission. Kinds: "lit" (URP Lit), "unlit" (URP Unlit), "glass" (URP Lit, transparent,
    /// no shadow casting). Existing materials are kept unless overwrite is set, so editor tweaks survive.
    /// </summary>
    internal static class PhoneFixerMaterials
    {
        [Serializable]
        private sealed class PaletteFile
        {
            public PaletteRow[] materials;
        }

        [Serializable]
        private sealed class PaletteRow
        {
            public string name;
            public string hex;
            public float smoothness;
            public float metallic;
            public float emission;
            public string kind;
        }

        /// <summary>Returns the number of materials created.</summary>
        public static int EnsureAll(bool overwrite = false)
        {
            if (!File.Exists(PhoneFixerPaths.PaletteJson))
            {
                throw new InvalidOperationException($"{PhoneFixerPaths.PaletteJson} is missing. Run any ArtSource/Blender/Scripts/build_pf_*.py first.");
            }

            var file = JsonUtility.FromJson<PaletteFile>(File.ReadAllText(PhoneFixerPaths.PaletteJson));
            if (file?.materials == null || file.materials.Length == 0)
            {
                throw new InvalidOperationException($"{PhoneFixerPaths.PaletteJson} has no materials.");
            }

            EditorAssetUtil.EnsureFolder(PhoneFixerPaths.MaterialsFolder);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP Lit shader not found.");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? throw new InvalidOperationException("URP Unlit shader not found.");

            int created = 0;
            foreach (PaletteRow row in file.materials)
            {
                string path = $"{PhoneFixerPaths.MaterialsFolder}/{row.name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = material == null;
                if (!isNew && !overwrite)
                {
                    continue;
                }

                bool isUnlit = row.kind == "unlit";
                if (isNew)
                {
                    material = new Material(isUnlit ? unlit : lit) { name = row.name };
                }
                else
                {
                    material.shader = isUnlit ? unlit : lit;
                }

                Color color = EditorAssetUtil.Hex(row.hex);
                if (row.kind == "glass")
                {
                    color.a = 0.18f;
                }

                material.SetColor("_BaseColor", color);
                if (!isUnlit)
                {
                    material.SetFloat("_Smoothness", row.smoothness);
                    material.SetFloat("_Metallic", row.metallic);
                    if (row.emission > 0f)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * row.emission);
                        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }
                }

                if (row.kind == "glass")
                {
                    MakeTransparent(material);
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(material, path);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(material);
                }
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        public static Material Get(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{PhoneFixerPaths.MaterialsFolder}/{name}.mat");

        private static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
    }
}

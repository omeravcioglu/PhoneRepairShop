using System.Collections.Generic;
using PhoneRepair.Devices;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>Creates the starting tools (Hands, Screwdriver, Spudger, Suction Cup). Existing assets are kept.</summary>
    internal static class ToolDataSetup
    {
        public const string Folder = "Assets/_Project/Data/Tools";
        public const string ModelFolder = "Assets/_Project/Art/Models/Tools";

        private readonly struct ToolSpec
        {
            public readonly string Asset, Id, Name, Description, Model;
            public readonly ToolKind Kind;

            public ToolSpec(string asset, string id, string name, ToolKind kind, string model, string description)
            {
                Asset = asset;
                Id = id;
                Name = name;
                Kind = kind;
                Model = model;
                Description = description;
            }
        }

        private static readonly ToolSpec[] Tools =
        {
            new("TOOL_Hands", "tool.hands", "Hands", ToolKind.Hands, null,
                "Your own two hands. Gentle, warm, slightly greasy. Lifts parts out and puts them back."),
            new("TOOL_Screwdriver", "tool.screwdriver.basic", "Screwdriver", ToolKind.Screwdriver, "TOOL_Screwdriver",
                "P2 precision driver with a coral grip. Lefty-loosey, righty-tighty."),
            new("TOOL_Spudger", "tool.spudger", "Spudger", ToolKind.Spudger, "TOOL_Spudger",
                "Nylon pry stick. Pops covers and unplugs connectors without scratching anything (much)."),
            new("TOOL_SuctionCup", "tool.suction_cup", "Suction Cup", ToolKind.SuctionCup, "TOOL_SuctionCup",
                "Grabs a screen by its face and lifts it out. The 'thwop' is free."),
        };

        /// <summary>Starting tool belt, in hotkey order.</summary>
        public static List<ToolDefinition> EnsureTools()
        {
            var result = new List<ToolDefinition>();
            foreach (ToolSpec spec in Tools)
            {
                var tool = EditorAssetUtil.LoadOrCreate<ToolDefinition>($"{Folder}/{spec.Asset}.asset", out bool created);
                if (created)
                {
                    EditorAssetUtil.Wire(tool,
                        ("toolId", spec.Id),
                        ("displayName", spec.Name),
                        ("kind", spec.Kind),
                        ("description", spec.Description));
                }

                if (spec.Model != null && tool.Model == null)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{spec.Model}.fbx");
                    if (model == null)
                    {
                        Debug.LogWarning($"[PhoneRepair] Tool model {spec.Model}.fbx not found; run ArtSource/Blender/Scripts/build_tools.py.");
                    }
                    else
                    {
                        EditorAssetUtil.Wire(tool, ("model", model));
                    }
                }

                result.Add(tool);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        public static IEnumerable<string> ModelPaths()
        {
            foreach (ToolSpec spec in Tools)
            {
                if (spec.Model != null)
                {
                    yield return $"{ModelFolder}/{spec.Model}.fbx";
                }
            }
        }
    }
}

using System.Collections.Generic;
using PhoneRepair.Devices;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Data for the Phone Fixer hero phone, the Mochi Dango (ArtSource/Blender/Scripts/build_phone_mochi_dango.py).
    /// Same slot contract and assembly rules as the Mochi One, its own part assets and descriptions; the
    /// screws reuse the shared P2 Mochi Screw part. Existing assets are only filled in where data is
    /// missing, so designer edits survive.
    /// </summary>
    internal static class MochiDangoDataSetup
    {
        public const string DataFolder = "Assets/_Project/Data/Phones/MochiDango";
        public const string PartsFolder = DataFolder + "/Parts";
        public const string ModelDefinitionPath = DataFolder + "/PHN_MochiDango_Model.asset";
        public const string SharedScrewPath = MochiOneDataSetup.PartsFolder + "/PRT_Screw_P2.asset";

        private static readonly Vector3 OutOfTheBack = Vector3.down; // phone local -Y
        private static readonly Vector3 OutOfTheFront = Vector3.up;

        private sealed class PartSpec
        {
            public string Asset, Id, Name, Description;
            public PartCategory Category;
            public DetachStyle Style = DetachStyle.Remove;
            public PartInteraction Interaction = PartInteraction.Hold;
            public ToolKind DetachTool = ToolKind.Hands, AttachTool = ToolKind.Hands;
            public string DetachVerb = "Lift out", AttachVerb = "Seat";
            public float DetachSeconds = 0.5f, AttachSeconds = 0.3f, ScrewTurns = 3f;
            public Vector3 RemovalDirection = OutOfTheBack;
            public Vector3 ReleaseAxis = Vector3.right;
            public float ReleaseAngle = 70f;
        }

        private static readonly PartSpec[] Parts =
        {
            new()
            {
                Asset = "PRT_MochiDango_Frame", Id = "mochi_dango.frame", Name = "Mochi Dango Frame", Category = PartCategory.Chassis,
                Description = "Cream aluminium band with soft corners. Survives drops, pockets and the occasional bowl of ramen.",
                Style = DetachStyle.Fixed, DetachVerb = "Remove", AttachVerb = "Install",
            },
            new()
            {
                Asset = "PRT_MochiDango_Screen", Id = "mochi_dango.screen", Name = "Display Assembly", Category = PartCategory.Display,
                Description = "6.3\" MochiVision panel with rounded corners. Cracks travel fast; so do the customer's feelings.",
                DetachTool = ToolKind.SuctionCup, DetachVerb = "Lift off", AttachVerb = "Press into place",
                DetachSeconds = 1.0f, RemovalDirection = OutOfTheFront,
            },
            new()
            {
                Asset = "PRT_MochiDango_BackCover", Id = "mochi_dango.back_cover", Name = "Back Cover", Category = PartCategory.Cover,
                Description = "Sakura-pink shell with the three-dango camera skewer on a cream island. Four screws, zero glue. Mochi loves you.",
                DetachTool = ToolKind.Spudger, DetachVerb = "Pry off", AttachVerb = "Snap on", DetachSeconds = 0.8f,
            },
            new()
            {
                Asset = "PRT_MochiDango_Battery", Id = "mochi_dango.battery", Name = "Battery Pack", Category = PartCategory.Battery,
                Description = "4,100 mAh with a smiling dango on the label. If it starts to look puffy, it is not happy to see you.",
                DetachSeconds = 0.6f,
            },
            new()
            {
                Asset = "PRT_MochiDango_BatteryConnector", Id = "mochi_dango.battery_connector", Name = "Battery Connector", Category = PartCategory.Connector,
                Description = "Kapton flex from the battery to the board. Unplug it before anything else. Always.",
                Style = DetachStyle.ReleaseInPlace, DetachTool = ToolKind.Spudger, DetachVerb = "Unplug", AttachVerb = "Plug in",
                DetachSeconds = 0.35f, AttachSeconds = 0.2f, ReleaseAxis = Vector3.right,
            },
            new()
            {
                Asset = "PRT_MochiDango_DisplayConnector", Id = "mochi_dango.display_connector", Name = "Display Connector", Category = PartCategory.Connector,
                Description = "The screen's ribbon cable. Thin, golden and easily torn by impatient fingers.",
                Style = DetachStyle.ReleaseInPlace, DetachTool = ToolKind.Spudger, DetachVerb = "Unplug", AttachVerb = "Plug in",
                DetachSeconds = 0.35f, AttachSeconds = 0.2f, ReleaseAxis = Vector3.forward,
            },
            new()
            {
                Asset = "PRT_MochiDango_Logicboard", Id = "mochi_dango.logic_board", Name = "Logic Board", Category = PartCategory.LogicBoard,
                Description = "Indigo board with gold pads under a brass shield. A chalky green-white crust means it went swimming.",
                DetachSeconds = 0.6f,
            },
            new()
            {
                Asset = "PRT_MochiDango_ChargingPort", Id = "mochi_dango.charging_port", Name = "Charging Port", Category = PartCategory.Port,
                Description = "USB-C port. Pocket lint's favourite holiday destination.",
            },
            new()
            {
                Asset = "PRT_MochiDango_RearCamera", Id = "mochi_dango.rear_camera", Name = "Rear Camera Module", Category = PartCategory.Camera,
                Description = "Three lenses on a brass skewer: wide, main and the one nobody knows the purpose of.",
            },
            new()
            {
                Asset = "PRT_MochiDango_Speaker", Id = "mochi_dango.speaker", Name = "Loudspeaker", Category = PartCategory.Audio,
                Description = "Plays the Mochi jingle at a volume the neighbours have opinions about.",
            },
        };

        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };
        private const string ScrewAsset = "PRT_Screw_P2";

        // Model node (Blender object name) -> part asset + assembly rules (same graph as the Mochi One).
        private static readonly (string slot, string asset, string[] blockedBy, string[] mountedOn)[] Slots =
        {
            ("Frame", "PRT_MochiDango_Frame", null, null),
            ("Screen", "PRT_MochiDango_Screen", new[] { "DisplayConnector" }, null),
            ("BackCover", "PRT_MochiDango_BackCover", Screws, null),
            ("Screw_01", ScrewAsset, null, new[] { "BackCover" }),
            ("Screw_02", ScrewAsset, null, new[] { "BackCover" }),
            ("Screw_03", ScrewAsset, null, new[] { "BackCover" }),
            ("Screw_04", ScrewAsset, null, new[] { "BackCover" }),
            ("Battery", "PRT_MochiDango_Battery", new[] { "BackCover", "BatteryConnector" }, null),
            ("BatteryConnector", "PRT_MochiDango_BatteryConnector", new[] { "BackCover" }, new[] { "Battery", "Logicboard" }),
            ("DisplayConnector", "PRT_MochiDango_DisplayConnector", new[] { "BackCover" }, new[] { "Screen", "Logicboard" }),
            ("Logicboard", "PRT_MochiDango_Logicboard", new[] { "BackCover", "BatteryConnector", "DisplayConnector", "RearCamera" }, null),
            ("ChargingPort", "PRT_MochiDango_ChargingPort", new[] { "BackCover" }, null),
            ("RearCamera", "PRT_MochiDango_RearCamera", new[] { "BackCover" }, new[] { "Logicboard" }),
            ("Speaker", "PRT_MochiDango_Speaker", new[] { "BackCover" }, null),
        };

        public static PhoneModelDefinition EnsureData()
        {
            var partAssets = new Dictionary<string, PartDefinition>();
            foreach (PartSpec spec in Parts)
            {
                var part = EditorAssetUtil.LoadOrCreate<PartDefinition>($"{PartsFolder}/{spec.Asset}.asset", out bool created);
                if (created)
                {
                    EditorAssetUtil.Wire(part,
                        ("partId", spec.Id),
                        ("displayName", spec.Name),
                        ("category", spec.Category),
                        ("description", spec.Description));
                }

                if (!part.HasRepairData)
                {
                    EditorAssetUtil.Wire(part,
                        ("detachStyle", spec.Style),
                        ("interaction", spec.Interaction),
                        ("detachTool", spec.DetachTool),
                        ("attachTool", spec.AttachTool),
                        ("detachVerb", spec.DetachVerb),
                        ("attachVerb", spec.AttachVerb),
                        ("detachSeconds", spec.DetachSeconds),
                        ("attachSeconds", spec.AttachSeconds),
                        ("screwTurns", spec.ScrewTurns),
                        ("removalDirection", spec.RemovalDirection),
                        ("releaseAxis", spec.ReleaseAxis),
                        ("releaseAngle", spec.ReleaseAngle));
                    EditorUtility.SetDirty(part);
                }

                partAssets[spec.Asset] = part;
            }

            // The P2 screw is shared with the Mochi One; its setup creates it when missing.
            var screw = AssetDatabase.LoadAssetAtPath<PartDefinition>(SharedScrewPath);
            if (screw == null)
            {
                MochiOneDataSetup.EnsureData();
                screw = AssetDatabase.LoadAssetAtPath<PartDefinition>(SharedScrewPath)
                        ?? throw new System.InvalidOperationException($"Shared screw part missing at {SharedScrewPath}.");
            }

            partAssets[ScrewAsset] = screw;

            var model = EditorAssetUtil.LoadOrCreate<PhoneModelDefinition>(ModelDefinitionPath, out bool modelCreated);
            if (modelCreated)
            {
                EditorAssetUtil.Wire(model,
                    ("modelId", "mochi_dango"),
                    ("brand", "Mochi Mobile"),
                    ("modelName", "Mochi Dango"),
                    ("description", "Mochi's chunky crowd-pleaser: three lenses on a brass skewer, four honest screws and a battery you can actually reach."));
            }

            FillSlots(model, partAssets, modelCreated);
            AssetDatabase.SaveAssets();
            return model;
        }

        private static void FillSlots(PhoneModelDefinition model, Dictionary<string, PartDefinition> partAssets, bool created)
        {
            var so = new SerializedObject(model);
            SerializedProperty slots = so.FindProperty("slots");
            if (created || slots.arraySize == 0)
            {
                slots.arraySize = Slots.Length;
                for (int i = 0; i < Slots.Length; i++)
                {
                    SerializedProperty element = slots.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("slotId").stringValue = Slots[i].slot;
                    element.FindPropertyRelative("part").objectReferenceValue = partAssets[Slots[i].asset];
                }
            }

            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                string slotId = element.FindPropertyRelative("slotId").stringValue;
                int index = System.Array.FindIndex(Slots, s => s.slot == slotId);
                if (index < 0)
                {
                    continue;
                }

                SerializedProperty blockedBy = element.FindPropertyRelative("blockedBy");
                SerializedProperty mountedOn = element.FindPropertyRelative("mountedOn");
                if (blockedBy.arraySize == 0 && mountedOn.arraySize == 0)
                {
                    SetStrings(blockedBy, Slots[index].blockedBy);
                    SetStrings(mountedOn, Slots[index].mountedOn);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStrings(SerializedProperty array, string[] values)
        {
            values ??= System.Array.Empty<string>();
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}

using System.Collections.Generic;
using PhoneRepair.Devices;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Creates the data assets for the first phone, the Mochi One. Existing assets are only ever
    /// filled in where data is missing (e.g. repair data added in Milestone 2); anything a designer
    /// already edited is left alone.
    /// </summary>
    internal static class MochiOneDataSetup
    {
        public const string FbxPath = "Assets/_Project/Art/Models/Phones/MochiOne/PHN_MochiOne.fbx";
        public const string DataFolder = "Assets/_Project/Data/Phones/MochiOne";
        public const string PartsFolder = DataFolder + "/Parts";
        public const string ModelDefinitionPath = DataFolder + "/PHN_MochiOne_Model.asset";

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
                Asset = "PRT_MochiOne_Frame", Id = "mochi_one.frame", Name = "Mochi One Frame", Category = PartCategory.Chassis,
                Description = "Cream aluminium frame. Holds everything together, physically and emotionally.",
                Style = DetachStyle.Fixed, DetachVerb = "Remove", AttachVerb = "Install",
            },
            new()
            {
                Asset = "PRT_MochiOne_Screen", Id = "mochi_one.screen", Name = "Display Assembly", Category = PartCategory.Display,
                Description = "6.1\" MochiVision panel. The glass is the part that breaks; the pixels are the part that's expensive.",
                DetachTool = ToolKind.SuctionCup, DetachVerb = "Lift off", AttachVerb = "Press into place",
                DetachSeconds = 1.0f, RemovalDirection = OutOfTheFront,
            },
            new()
            {
                Asset = "PRT_MochiOne_BackCover", Id = "mochi_one.back_cover", Name = "Back Cover", Category = PartCategory.Cover,
                Description = "Sakura-pink shell, held on by four screws and the owner's optimism.",
                DetachTool = ToolKind.Spudger, DetachVerb = "Pry off", AttachVerb = "Snap on", DetachSeconds = 0.8f,
            },
            new()
            {
                Asset = "PRT_Screw_P2", Id = "screw.p2", Name = "P2 Mochi Screw", Category = PartCategory.Fastener,
                Description = "Tiny, shiny, and desperate to roll under the workbench.",
                Interaction = PartInteraction.Screw, DetachTool = ToolKind.Screwdriver, AttachTool = ToolKind.Screwdriver,
                DetachVerb = "Unscrew", AttachVerb = "Screw in", ScrewTurns = 3f, RemovalDirection = Vector3.up,
            },
            new()
            {
                Asset = "PRT_MochiOne_Battery", Id = "mochi_one.battery", Name = "Battery Pack", Category = PartCategory.Battery,
                Description = "3,000 mAh of snack-sized energy. Do not puncture, bend, or tell it your secrets.",
                DetachSeconds = 0.6f,
            },
            new()
            {
                Asset = "PRT_MochiOne_BatteryConnector", Id = "mochi_one.battery_connector", Name = "Battery Connector", Category = PartCategory.Connector,
                Description = "Links the battery to the logic board. Unplug it first: sparks are not a repair technique.",
                Style = DetachStyle.ReleaseInPlace, DetachTool = ToolKind.Spudger, DetachVerb = "Unplug", AttachVerb = "Plug in",
                DetachSeconds = 0.35f, AttachSeconds = 0.2f, ReleaseAxis = Vector3.right,
            },
            new()
            {
                Asset = "PRT_MochiOne_DisplayConnector", Id = "mochi_one.display_connector", Name = "Display Connector", Category = PartCategory.Connector,
                Description = "Carries every pixel from the logic board to the screen. Mostly cat pictures.",
                Style = DetachStyle.ReleaseInPlace, DetachTool = ToolKind.Spudger, DetachVerb = "Unplug", AttachVerb = "Plug in",
                DetachSeconds = 0.35f, AttachSeconds = 0.2f, ReleaseAxis = Vector3.forward,
            },
            new()
            {
                Asset = "PRT_MochiOne_Logicboard", Id = "mochi_one.logic_board", Name = "Logic Board", Category = PartCategory.LogicBoard,
                Description = "The phone's brain: processor, memory, and one suspicious smiley sticker.",
                DetachSeconds = 0.6f,
            },
            new()
            {
                Asset = "PRT_MochiOne_ChargingPort", Id = "mochi_one.charging_port", Name = "Charging Port", Category = PartCategory.Port,
                Description = "USB-C port. Common contaminants: lint, sand, and once, a gummy bear.",
            },
            new()
            {
                Asset = "PRT_MochiOne_RearCamera", Id = "mochi_one.rear_camera", Name = "Rear Camera", Category = PartCategory.Camera,
                Description = "12 MP MochiCam. Takes lovely photos whenever somebody wipes the lens.",
            },
            new()
            {
                Asset = "PRT_MochiOne_Speaker", Id = "mochi_one.speaker", Name = "Loudspeaker", Category = PartCategory.Audio,
                Description = "Ringtones, alarms, and videos played at full volume on the bus.",
            },
        };

        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        // Model node (Blender object name) -> part asset + assembly rules. Screws share one part type.
        private static readonly (string slot, string asset, string[] blockedBy, string[] mountedOn)[] Slots =
        {
            ("Frame", "PRT_MochiOne_Frame", null, null),
            ("Screen", "PRT_MochiOne_Screen", new[] { "DisplayConnector" }, null),
            ("BackCover", "PRT_MochiOne_BackCover", Screws, null),
            ("Screw_01", "PRT_Screw_P2", null, new[] { "BackCover" }),
            ("Screw_02", "PRT_Screw_P2", null, new[] { "BackCover" }),
            ("Screw_03", "PRT_Screw_P2", null, new[] { "BackCover" }),
            ("Screw_04", "PRT_Screw_P2", null, new[] { "BackCover" }),
            ("Battery", "PRT_MochiOne_Battery", new[] { "BackCover", "BatteryConnector" }, null),
            ("BatteryConnector", "PRT_MochiOne_BatteryConnector", new[] { "BackCover" }, new[] { "Battery", "Logicboard" }),
            ("DisplayConnector", "PRT_MochiOne_DisplayConnector", new[] { "BackCover" }, new[] { "Screen", "Logicboard" }),
            ("Logicboard", "PRT_MochiOne_Logicboard", new[] { "BackCover", "BatteryConnector", "DisplayConnector", "RearCamera" }, null),
            ("ChargingPort", "PRT_MochiOne_ChargingPort", new[] { "BackCover" }, null),
            ("RearCamera", "PRT_MochiOne_RearCamera", new[] { "BackCover" }, new[] { "Logicboard" }),
            ("Speaker", "PRT_MochiOne_Speaker", new[] { "BackCover" }, null),
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

            var model = EditorAssetUtil.LoadOrCreate<PhoneModelDefinition>(ModelDefinitionPath, out bool modelCreated);
            if (modelCreated)
            {
                EditorAssetUtil.Wire(model,
                    ("modelId", "mochi_one"),
                    ("brand", "Mochi Mobile"),
                    ("modelName", "Mochi One"),
                    ("description", "Chubby, friendly, and screwed together like it wants to be repaired. Every technician's first love."));
            }

            FillSlots(model, partAssets, modelCreated);
            AssetDatabase.SaveAssets();
            return model;
        }

        /// <summary>Creates the slot list on a new asset; on an existing one only fills rules that are still empty.</summary>
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

using System.Collections.Generic;
using NUnit.Framework;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.Tests
{
    /// <summary>
    /// Guard rails for the art pipeline: catches renamed Blender objects, wrong export axes or
    /// scale, and missing colliders before anyone presses Play.
    /// </summary>
    public class PhoneContentValidationTests
    {
        private const string MochiOneDefinitionPath = "Assets/_Project/Data/Phones/MochiOne/PHN_MochiOne_Model.asset";

        private static PhoneModelDefinition LoadMochiOne()
        {
            var definition = AssetDatabase.LoadAssetAtPath<PhoneModelDefinition>(MochiOneDefinitionPath);
            if (definition == null || definition.Prefab == null)
            {
                Assert.Ignore("Run Tools > Phone Repair > Run Setup first.");
            }

            return definition;
        }

        [Test]
        public void EverySlot_HasAPartAnInteractableAndAPickableCollider()
        {
            PhoneModelDefinition definition = LoadMochiOne();
            Transform root = definition.Prefab.transform;
            foreach (PartSlot slot in definition.Slots)
            {
                Transform node = FindDeep(root, slot.slotId);
                Assert.That(node, Is.Not.Null, $"model has no object named '{slot.slotId}'");
                var part = node.GetComponent<PhonePart>();
                Assert.That(part, Is.Not.Null, $"{slot.slotId}: missing PhonePart");
                Assert.That(part.SlotId, Is.EqualTo(slot.slotId));
                Assert.That(part.Definition, Is.SameAs(slot.part), $"{slot.slotId}: wrong PartDefinition");
                Assert.That(node.GetComponent<Interactable>(), Is.Not.Null, $"{slot.slotId}: missing Interactable");
                Assert.That(node.GetComponentsInChildren<Collider>(true), Is.Not.Empty, $"{slot.slotId}: no collider, it can't be clicked");
            }
        }

        [Test]
        public void Model_ImportsAtRealWorldScaleWithScreenUpAndTopForward()
        {
            Transform root = LoadMochiOne().Prefab.transform;
            Bounds bounds = RendererBounds(root.gameObject);
            Assert.That(bounds.size.x, Is.EqualTo(0.074f).Within(0.002f), "width (m) - check FBX unit scale");
            Assert.That(bounds.size.z, Is.EqualTo(0.152f).Within(0.002f), "length (m) - check FBX axes");
            Assert.That(bounds.size.y, Is.InRange(0.010f, 0.014f), "thickness (m)");

            float screenY = RendererBounds(FindDeep(root, "Screen").gameObject).center.y;
            float coverY = RendererBounds(FindDeep(root, "BackCover").gameObject).center.y;
            Assert.That(screenY, Is.GreaterThan(coverY), "screen must face +Y");

            float portZ = RendererBounds(FindDeep(root, "ChargingPort").gameObject).center.z;
            float cameraZ = RendererBounds(FindDeep(root, "RearCamera").gameObject).center.z;
            Assert.That(cameraZ, Is.GreaterThan(portZ), "top edge must point +Z");
        }

        [Test]
        public void Decorations_StayInsideTheirPart()
        {
            // Catches broken FBX hierarchies (e.g. Blender's "Apply Transform" rotating grandchildren 90 degrees).
            Transform root = LoadMochiOne().Prefab.transform;
            Bounds phone = RendererBounds(root.gameObject);
            phone.Expand(0.001f);
            foreach (PhonePart part in root.GetComponentsInChildren<PhonePart>(true))
            {
                foreach (Transform child in part.transform)
                {
                    if (child.GetComponent<PhonePart>() != null)
                    {
                        continue; // a part riding on another part (the battery's connector) is checked as a part
                    }

                    Bounds b = RendererBounds(child.gameObject);
                    Assert.That(phone.Contains(b.min) && phone.Contains(b.max), Is.True,
                        $"{part.name}/{child.name} sticks out of the phone ({b.min} .. {b.max}); check the FBX export axes");
                    Assert.That(b.size.y, Is.LessThan(0.012f), $"{part.name}/{child.name} is not lying flat");
                }
            }
        }

        [Test]
        public void Screws_PointOutOfTheBackAlongLocalUp()
        {
            Transform root = LoadMochiOne().Prefab.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Screw_"))
                {
                    continue;
                }

                Assert.That(Vector3.Dot(t.up, Vector3.down), Is.GreaterThan(0.99f), $"{t.name}: local up should point out of the back (-Y)");
            }
        }

        [Test]
        public void EveryPart_HasRepairData_AndTheToolsItAsksForExist()
        {
            var kinds = new HashSet<ToolKind>();
            foreach (string guid in AssetDatabase.FindAssets("t:ToolDefinition"))
            {
                kinds.Add(AssetDatabase.LoadAssetAtPath<ToolDefinition>(AssetDatabase.GUIDToAssetPath(guid)).Kind);
            }

            foreach (PartSlot slot in LoadMochiOne().Slots)
            {
                PartDefinition part = slot.part;
                Assert.That(part.HasRepairData, Is.True, $"{slot.slotId}: no repair data");
                if (part.DetachStyle == DetachStyle.Fixed)
                {
                    continue;
                }

                Assert.That(kinds, Does.Contain(part.DetachTool), $"{slot.slotId}: needs a {part.DetachTool} but no tool asset has that kind");
                Assert.That(kinds, Does.Contain(part.AttachTool), $"{slot.slotId}: needs a {part.AttachTool} but no tool asset has that kind");
                if (part.Interaction == PartInteraction.Screw)
                {
                    Assert.That(part.Category, Is.EqualTo(PartCategory.Fastener), $"{slot.slotId}: screws go to the screw tray");
                }
                else
                {
                    Assert.That(part.DetachSeconds, Is.GreaterThan(0f), slot.slotId);
                    Assert.That(part.AttachSeconds, Is.GreaterThan(0f), slot.slotId);
                }
            }
        }

        [Test]
        public void PartIds_AreUnique()
        {
            var seen = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:PartDefinition"))
            {
                var part = AssetDatabase.LoadAssetAtPath<PartDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(seen.ContainsKey(part.PartId), Is.False, $"duplicate part id '{part.PartId}' ({part.name} and {(seen.TryGetValue(part.PartId, out string other) ? other : "?")})");
                seen[part.PartId] = part.name;
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            // Prefab assets are not in a scene, so compute bounds from meshes + transforms.
            bool has = false;
            Bounds result = default;
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = filter.sharedMesh.bounds;
                Matrix4x4 m = filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (has)
                    {
                        result.Encapsulate(p);
                    }
                    else
                    {
                        result = new Bounds(p, Vector3.zero);
                        has = true;
                    }
                }
            }

            return result;
        }
    }
}

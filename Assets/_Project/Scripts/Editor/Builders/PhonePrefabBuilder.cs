using System;
using System.Collections.Generic;
using System.IO;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Turns an imported phone model into an interactive prefab, driven purely by data:
    /// every slot in the PhoneModelDefinition gets a PhonePart, an Interactable and a collider that
    /// suits its category. The result is a Prefab VARIANT of the FBX. It is regenerated from the
    /// model on every run and saved over the same path (same GUID, so references survive), which
    /// keeps it in sync with Blender even when the model's hierarchy changes.
    /// </summary>
    internal static class PhonePrefabBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Phones";

        public static GameObject Build(PhoneModelDefinition model, string fbxPath)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath)
                             ?? throw new InvalidOperationException($"Model not found at {fbxPath}.");
            EditorAssetUtil.EnsureFolder(PrefabFolder);
            string prefabPath = $"{PrefabFolder}/{Path.GetFileNameWithoutExtension(fbxPath)}.prefab";

            var root = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            try
            {
                Configure(root, model);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException($"Could not save {prefabPath}.");
                }

                EditorAssetUtil.Wire(model, ("prefab", saved));
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Configure(GameObject root, PhoneModelDefinition model)
        {
            EditorAssetUtil.Wire(EditorAssetUtil.GetOrAdd<PhoneDevice>(root), ("model", model));

            var configured = new List<(GameObject go, PartDefinition definition)>();
            foreach (PartSlot slot in model.Slots)
            {
                Transform node = EditorAssetUtil.FindDeep(root.transform, slot.slotId)
                                 ?? throw new InvalidOperationException(
                                     $"Slot '{slot.slotId}' of {model.name} was not found in the model. The Blender object must be named exactly '{slot.slotId}'.");
                if (slot.part == null)
                {
                    throw new InvalidOperationException($"Slot '{slot.slotId}' of {model.name} has no PartDefinition.");
                }

                EditorAssetUtil.Wire(EditorAssetUtil.GetOrAdd<PhonePart>(node.gameObject),
                    ("slotId", slot.slotId), ("definition", slot.part));
                EditorAssetUtil.Wire(EditorAssetUtil.GetOrAdd<Interactable>(node.gameObject),
                    ("displayName", slot.part.DisplayName));
                configured.Add((node.gameObject, slot.part));
            }

            // Colliders after all parts exist, so nested-part ownership is known.
            foreach ((GameObject go, PartDefinition definition) in configured)
            {
                EnsureColliders(go, definition.Category);
            }
        }

        private static void EnsureColliders(GameObject part, PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Fastener:
                    EnsureScrewCollider(part);
                    break;
                case PartCategory.Connector:
                    EnsurePaddedBox(part, 0.001f);
                    break;
                default:
                    EnsureMeshColliders(part);
                    break;
            }
        }

        /// <summary>
        /// Screws are tiny, so they get a generous sphere around the head. Convention (Blender script):
        /// local +Y is the screw axis and the head sits between y = 0 and the top of the mesh.
        /// Size is recomputed on every build so it follows model changes.
        /// </summary>
        private static void EnsureScrewCollider(GameObject screw)
        {
            if (!screw.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                EnsureMeshColliders(screw);
                return;
            }

            Bounds b = filter.sharedMesh.bounds;
            float headRadius = Mathf.Max(b.extents.x, b.extents.z);
            var sphere = EditorAssetUtil.GetOrAdd<SphereCollider>(screw);
            sphere.radius = Mathf.Max(headRadius * 1.45f, 0.0025f);
            sphere.center = new Vector3(b.center.x, Mathf.Max(0f, b.max.y) * 0.5f, b.center.z);
        }

        /// <summary>Box around the mesh plus padding, recomputed on every build.</summary>
        private static void EnsurePaddedBox(GameObject part, float padding)
        {
            if (!part.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                EnsureMeshColliders(part);
                return;
            }

            Bounds b = filter.sharedMesh.bounds;
            var box = EditorAssetUtil.GetOrAdd<BoxCollider>(part);
            box.center = b.center;
            box.size = b.size + Vector3.one * (padding * 2f);
        }

        private static void EnsureMeshColliders(GameObject part)
        {
            foreach (MeshFilter filter in part.GetComponentsInChildren<MeshFilter>(true))
            {
                PhonePart owner = filter.GetComponentInParent<PhonePart>(true);
                if (owner == null || owner.gameObject != part || filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                {
                    continue;
                }

                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }
    }
}

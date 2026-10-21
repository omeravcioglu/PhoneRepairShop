using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Makes a static environment prefab (variant of the FBX) with mesh colliders, so the bench and
    /// props block picking rays and give zoom-to-cursor something to aim at. Regenerated from the
    /// model on every run and saved over the same path (same GUID).
    /// </summary>
    internal static class EnvironmentPrefabBuilder
    {
        public const string FbxPath = "Assets/_Project/Art/Models/Environment/ENV_Workbench.fbx";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Environment";

        private const StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;

        public static GameObject Build(string fbxPath = FbxPath)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath)
                             ?? throw new InvalidOperationException($"Model not found at {fbxPath}.");
            EditorAssetUtil.EnsureFolder(PrefabFolder);
            string prefabPath = $"{PrefabFolder}/{Path.GetFileNameWithoutExtension(fbxPath)}.prefab";

            var root = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            try
            {
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh != null && filter.GetComponent<Collider>() == null)
                    {
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    }
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticFlags);
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException($"Could not save {prefabPath}.");
                }

                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

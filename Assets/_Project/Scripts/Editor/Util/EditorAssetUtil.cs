using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>Small helpers shared by the setup/build tools.</summary>
    internal static class EditorAssetUtil
    {
        public static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }

        /// <summary>Loads the asset at path, or creates (and saves) a new instance there.</summary>
        public static T LoadOrCreate<T>(string assetPath, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            created = asset == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(assetPath));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            return asset;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent(out T existing) ? existing : go.AddComponent<T>();
        }

        public static Transform FindDeep(Transform root, string name)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                Transform t = queue.Dequeue();
                if (t.name == name)
                {
                    return t;
                }

                foreach (Transform child in t)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Assigns serialized (often private) fields by name. Throws on a wrong property name so a
        /// renamed field breaks the setup loudly instead of producing a silently unwired scene.
        /// </summary>
        public static void Wire(Object target, params (string property, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach ((string property, object value) in values)
            {
                SerializedProperty p = so.FindProperty(property)
                                       ?? throw new InvalidOperationException($"'{target.GetType().Name}' has no serialized field '{property}'.");
                switch (value)
                {
                    case null:
                        p.objectReferenceValue = null;
                        break;
                    case Object reference:
                        p.objectReferenceValue = reference;
                        break;
                    case bool b:
                        p.boolValue = b;
                        break;
                    case int i:
                        p.intValue = i;
                        break;
                    case float f:
                        p.floatValue = f;
                        break;
                    case string s:
                        p.stringValue = s;
                        break;
                    case Vector2 v2:
                        p.vector2Value = v2;
                        break;
                    case Vector3 v3:
                        p.vector3Value = v3;
                        break;
                    case Color c:
                        p.colorValue = c;
                        break;
                    case Enum e:
                        p.intValue = Convert.ToInt32(e);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported value type {value.GetType().Name} for '{property}'.");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a serialized array/list of object references.</summary>
        public static void WireArray(Object target, string property, IReadOnlyList<Object> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property)
                                   ?? throw new InvalidOperationException($"'{target.GetType().Name}' has no serialized field '{property}'.");
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Color Hex(string hex, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException($"Bad colour '{hex}'.");
            }

            color.a = alpha;
            return color;
        }

        public static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}

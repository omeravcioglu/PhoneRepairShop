using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PhoneRepair.Devices;
using PhoneRepair.PhoneFixer;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools.Trailer
{
    /// <summary>
    /// Location scouting for the trailer: writes the world bounds of the props, spots, customers and phone
    /// parts of the Phone Fixer scenes to a text file, so shots can be framed from real numbers.
    /// Batch: -executeMethod PhoneRepair.EditorTools.Trailer.TrailerScout.ScoutBatch -trailerOut &lt;dir&gt;
    /// </summary>
    public static class TrailerScout
    {
        public static void ScoutBatch()
        {
            try
            {
                string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-trailerOut").Skip(1).FirstOrDefault()
                                ?? Path.GetFullPath("Marketing/Trailer/Frames");
                Directory.CreateDirectory(output);
                var sb = new StringBuilder();
                Scout(sb, PhoneFixerPaths.ShopScene);
                Scout(sb, PhoneFixerPaths.BenchScene);
                File.WriteAllText(Path.Combine(output, "scout.txt"), sb.ToString());
                Debug.Log($"[Trailer] Scout written to {output}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Scout(StringBuilder sb, string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            sb.Append("## ").Append(scene.name).Append('\n');
            Camera camera = Camera.main;
            if (camera != null)
            {
                sb.Append($"camera pos {V(camera.transform.position)} fwd {V(camera.transform.forward)} fov {camera.fieldOfView:F1} near {camera.nearClipPlane} far {camera.farClipPlane}\n");
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Walk(sb, root.transform, 0, 3);
            }

            foreach (CustomerActor actor in Object.FindObjectsByType<CustomerActor>(FindObjectsSortMode.None))
            {
                Bounds b = EditorAssetUtil.RendererBounds(actor.gameObject);
                sb.Append($"customer {actor.name} pos {V(actor.transform.position)} yaw {actor.transform.eulerAngles.y:F0} scale {actor.transform.localScale.x:F2} bounds {V(b.min)}..{V(b.max)} loop {actor.LoopState}\n");
                foreach (Transform t in actor.GetComponentsInChildren<Transform>(true).Take(60))
                {
                    sb.Append($"   bone {t.name} {V(t.position)}\n");
                }
            }

            var device = Object.FindFirstObjectByType<PhoneDevice>();
            if (device != null)
            {
                sb.Append($"phone {device.name} pos {V(device.transform.position)} rot {V(device.transform.eulerAngles)} up {V(device.transform.up)}\n");
                foreach (PhonePart part in device.Parts)
                {
                    Bounds b = EditorAssetUtil.RendererBounds(part.gameObject);
                    sb.Append($"  part {part.SlotId} pos {V(part.transform.position)} up {V(part.transform.up)} bounds {V(b.min)}..{V(b.max)}\n");
                }
            }

            var area = Object.FindFirstObjectByType<PartsArea>();
            if (area != null)
            {
                foreach (PartsArea.Slot slot in area.Slots)
                {
                    if (slot.Anchor != null)
                    {
                        sb.Append($"  slot {slot.Anchor.name} pos {V(slot.Anchor.position)} size {slot.size.x:F3}x{slot.size.y:F3} fasteners {slot.fastenersOnly}\n");
                    }
                }
            }

            foreach (string tool in new[] { "Screwdriver", "Spudger", "SuctionCup", "Brush", "Tweezers", "Tester", "Magnifier" })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(PhoneFixerPaths.ToolFbx(tool));
                if (model != null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    Bounds b = EditorAssetUtil.RendererBounds(go);
                    sb.Append($"tool {tool} bounds at origin {V(b.min)}..{V(b.max)}\n");
                    Object.DestroyImmediate(go);
                }
            }

            sb.Append('\n');
        }

        private static void Walk(StringBuilder sb, Transform t, int depth, int maxDepth)
        {
            Renderer r = t.GetComponent<Renderer>();
            string bounds = "";
            if (r != null)
            {
                bounds = $" bounds {V(r.bounds.min)}..{V(r.bounds.max)}";
            }
            else if (t.childCount > 0 && depth >= 1)
            {
                Bounds b = EditorAssetUtil.RendererBounds(t.gameObject);
                if (b.size != Vector3.zero)
                {
                    bounds = $" groupbounds {V(b.min)}..{V(b.max)}";
                }
            }

            sb.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeInHierarchy ? "" : " (inactive)")
                .Append(" pos ").Append(V(t.position)).Append(bounds).Append('\n');
            if (depth >= maxDepth)
            {
                return;
            }

            foreach (Transform child in t)
            {
                if (child.GetComponent<RectTransform>() != null)
                {
                    continue; // UI
                }

                Walk(sb, child, depth + 1, maxDepth);
            }
        }

        private static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:F3}, {1:F3}, {2:F3})", v.x, v.y, v.z);
    }
}

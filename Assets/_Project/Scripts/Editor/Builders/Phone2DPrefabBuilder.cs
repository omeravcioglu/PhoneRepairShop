using System;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Builds the 2D Mochi One from its art-contract layers: a Front side (body + screen) and a Back
    /// side (insides, battery with its connector, display connector, cover, four screws). Every part
    /// is a PhonePart with the same slot id as in the PhoneModelDefinition, so the Milestone 2 rules
    /// apply unchanged. Saved over the same prefab path every run (GUID kept) and linked from the model.
    /// </summary>
    internal static class Phone2DPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Phones/PHN_MochiOne_2D.prefab";

        private sealed class Layer
        {
            public string Slot;        // PhonePart slot id, or null for decoration
            public string Name;
            public string Sprite;
            public string Broken;
            public string Loose;
            public int Order;          // added to SortOrder.Phone
            public string Parent;      // another layer's name (the battery connector rides on the battery)
        }

        private static readonly Layer[] FrontLayers =
        {
            new() { Name = "Body", Sprite = "phone_front_body", Order = 0 },
            new() { Slot = "Screen", Name = "Screen", Sprite = "phone_screen", Broken = "phone_screen_cracked", Order = 10 },
        };

        private static readonly Layer[] BackLayers =
        {
            new() { Name = "Body", Sprite = "phone_back_body", Order = 0 },
            new() { Name = "Board", Sprite = "phone_board", Order = 5 },
            new() { Slot = "Battery", Name = "Battery", Sprite = "phone_battery", Broken = "phone_battery_swollen", Order = 20 },
            new() { Slot = "BatteryConnector", Name = "BatteryConnector", Sprite = "phone_battery_conn", Loose = "phone_battery_conn_open", Order = 30, Parent = "Battery" },
            new() { Slot = "DisplayConnector", Name = "DisplayConnector", Sprite = "phone_display_conn", Loose = "phone_display_conn_open", Order = 30 },
            new() { Slot = "BackCover", Name = "BackCover", Sprite = "phone_cover", Order = 40 },
        };

        private static readonly string[] Screws = { "Screw_01", "Screw_02", "Screw_03", "Screw_04" };

        public static GameObject Build(PhoneModelDefinition model, Material spriteMaterial)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var root = new GameObject("PHN_MochiOne_2D");
            try
            {
                var device = root.AddComponent<PhoneDevice>();
                EditorAssetUtil.Wire(device, ("model", model));
                Transform front = Child(root.transform, "Front");
                Transform back = Child(root.transform, "Back");
                var phone = root.AddComponent<Phone2D>();
                EditorAssetUtil.Wire(phone, ("front", front), ("back", back), ("startSide", Phone2D.Side.Front));

                foreach (Layer layer in FrontLayers)
                {
                    BuildLayer(front, layer, model, spriteMaterial, null);
                }

                foreach (Layer layer in BackLayers)
                {
                    Transform parent = layer.Parent != null ? back.Find(layer.Parent) : back;
                    BuildLayer(parent, layer, model, spriteMaterial, null);
                }

                Sprite[] screwFrames = PixelArtSetup.LoadFrames("phone_screw");
                for (int i = 0; i < Screws.Length; i++)
                {
                    Vector3 position = ArtContract.Anchor($"phone.screw_{i + 1}");
                    GameObject screw = BuildLayer(back, new Layer { Slot = Screws[i], Name = Screws[i], Sprite = "phone_screw", Order = 50 },
                        model, spriteMaterial, screwFrames);
                    screw.transform.localPosition = position;
                }

                EditorAssetUtil.EnsureFolder(System.IO.Path.GetDirectoryName(PrefabPath));
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                EditorAssetUtil.Wire(model, ("prefab", prefab));
                EditorUtility.SetDirty(model);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildLayer(Transform parent, Layer layer, PhoneModelDefinition model, Material material, Sprite[] turnFrames)
        {
            var go = new GameObject(layer.Name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sprite = turnFrames != null ? turnFrames[0] : PixelArtSetup.LoadSprite(layer.Sprite);
            int order = SortOrder.Phone + layer.Order;
            sr.sortingOrder = order;
            if (layer.Slot == null)
            {
                return go; // decoration
            }

            if (!model.TryGetSlot(layer.Slot, out PartSlot slot))
            {
                throw new InvalidOperationException($"{model.name} has no slot '{layer.Slot}'.");
            }

            var part = go.AddComponent<PhonePart>();
            EditorAssetUtil.Wire(part, ("slotId", layer.Slot), ("definition", slot.part));
            go.AddComponent<Interactable>();

            var view = go.AddComponent<PartView2D>();
            EditorAssetUtil.Wire(view,
                ("part", part),
                ("normal", sr.sprite),
                ("broken", layer.Broken != null ? PixelArtSetup.LoadSprite(layer.Broken) : null),
                ("loose", layer.Loose != null ? PixelArtSetup.LoadSprite(layer.Loose) : null),
                ("installedOrder", order),
                ("colliderFromSprite", turnFrames == null));
            if (turnFrames != null)
            {
                EditorAssetUtil.WireArray(view, "turnFrames", turnFrames);
                var circle = go.AddComponent<CircleCollider2D>();
                circle.radius = 6f; // generous: screws are tiny
            }
            else
            {
                go.AddComponent<PolygonCollider2D>(); // shaped from the sprite's pixels (refreshed at runtime)
            }

            return go;
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}

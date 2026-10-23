using System.Collections.Generic;
using PhoneRepair.Devices;
using PhoneRepair.Repair;
using PhoneRepair.Shop;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Game content for the 2D shop: the four tools (with rack icons and cursors), the five
    /// customers (looks, voice, job, goofy lines) and the day calendar. Assets that already exist
    /// keep whatever a designer typed into them; only sprite links are refreshed every run.
    /// </summary>
    internal static class ShopDataSetup
    {
        public const string ToolsFolder = "Assets/_Project/Data/Tools";
        public const string CustomersFolder = "Assets/_Project/Data/Customers";
        public const string CalendarPath = "Assets/_Project/Data/Shop/CAL_Default.asset";

        private sealed class ToolSpec
        {
            public string Asset, Id, Name, Description, Icon, Cursor;
            public ToolKind Kind;
        }

        private static readonly ToolSpec[] Tools =
        {
            new() { Asset = "TOOL_Hands", Id = "tool.hands", Name = "Hands", Kind = ToolKind.Hands, Icon = "tool_hands", Cursor = null,
                Description = "Your own two hands. Lifts parts out, seats them, snaps covers and plugs connectors." },
            new() { Asset = "TOOL_Screwdriver", Id = "tool.screwdriver.basic", Name = "Screwdriver", Kind = ToolKind.Screwdriver, Icon = "tool_screwdriver", Cursor = "cursor_screwdriver",
                Description = "P2 precision driver. Lefty-loosey, righty-tighty." },
            new() { Asset = "TOOL_Spudger", Id = "tool.spudger", Name = "Spudger", Kind = ToolKind.Spudger, Icon = "tool_spudger", Cursor = "cursor_spudger",
                Description = "Nylon pry stick. Pops covers and unplugs connectors without scratching anything (much)." },
            new() { Asset = "TOOL_SuctionCup", Id = "tool.suction_cup", Name = "Suction Cup", Kind = ToolKind.SuctionCup, Icon = "tool_suction", Cursor = "cursor_suction",
                Description = "Grabs a screen by its face and lifts it off. The 'thwop' is free." },
        };

        private sealed class CustomerSpec
        {
            public string Id, Name, BrokenSlot, ProblemLabel, Tip;
            public float Voice;
            public int Payment;
            public string Greeting, Problem, Accepted, Declined, Fixed, StillBroken;
        }

        private static readonly CustomerSpec[] Customers =
        {
            new()
            {
                Id = "fumi", Name = "Fumi", Voice = 1.25f, BrokenSlot = "Battery", ProblemLabel = "Battery gets warm", Payment = 2400, Tip = "a mikan",
                Greeting = "Good afternoon, dear! Is the repair person in? Oh, that's you!",
                Problem = "My Mochi One gets warm. Like a little hand warmer. Lovely in winter, but now it's all puffy...",
                Accepted = "Thank you, dear. I'll wait right here with the cat.",
                Declined = "Oh... then I'll keep it as a hand warmer, I suppose.",
                Fixed = "Cool as a cucumber! Here, dear, and take a mikan too.",
                StillBroken = "It's still toasty, dear. My pocket is a sauna.",
            },
            new()
            {
                Id = "sasaki", Name = "Mr. Sasaki", Voice = 0.8f, BrokenSlot = "Screen", ProblemLabel = "Cracked screen", Payment = 3000, Tip = "a business card",
                Greeting = "Hello. I have a meeting in twenty minutes.",
                Problem = "I dropped my phone. The screen is now a spiderweb. Nineteen minutes.",
                Accepted = "Good. I will stand here and look at my watch.",
                Declined = "...Then I will present from a cracked screen.",
                Fixed = "Acceptable. Very acceptable. Here is my business card.",
                StillBroken = "Still cracked. I am now late AND cracked.",
            },
            new()
            {
                Id = "mio", Name = "Mio", Voice = 1.4f, BrokenSlot = "Battery", ProblemLabel = "Puffy battery (ramen)", Payment = 2400, Tip = "a cat sticker",
                Greeting = "Hiii! Um. Don't laugh, okay?",
                Problem = "I dropped it in ramen. Tonkotsu. Now the battery is all puffy and it smells delicious.",
                Accepted = "You're the best! I'll do my homework on the counter.",
                Declined = "Noooo. Guess I'll just eat more ramen.",
                Fixed = "IT LIVES! Here, a sticker. It's a cat in a bowl.",
                StillBroken = "It still smells like ramen... and it won't turn on...",
            },
            new()
            {
                Id = "ren", Name = "Ren", Voice = 0.95f, BrokenSlot = "Screen", ProblemLabel = "Cracked screen", Payment = 3000, Tip = "an energy drink",
                Greeting = "yo. you open? i haven't slept since tuesday.",
                Problem = "phone fell off my bed mid-raid. screen's cracked. the boss is still alive.",
                Accepted = "legend. i'll wait. i'll just... stand here. blinking.",
                Declined = "rip. guess i'm playing on my fridge now.",
                Fixed = "GG. you're cracked. the good kind. here, an energy drink.",
                StillBroken = "screen's still broken. respawning disappointment.",
            },
            new()
            {
                Id = "madame_k", Name = "Madame K.", Voice = 1.1f, BrokenSlot = "Screen", ProblemLabel = "Cracked screen (dog)", Payment = 4500, Tip = "a pearl",
                Greeting = "Bonjour. I am Madame K. And this is Bijou.",
                Problem = "Bijou sat on my telephone. Bijou is a very large dog. The screen did not survive.",
                Accepted = "Splendid. Bijou will supervise you.",
                Declined = "How disappointing. Bijou is disappointed too.",
                Fixed = "Magnifique! Keep the change, darling. And this pearl. It is probably real.",
                StillBroken = "It is still broken. Bijou is growling. Softly.",
            },
        };

        private static readonly string[][] Days =
        {
            new[] { "fumi", "sasaki", "mio" },
            new[] { "ren", "madame_k", "fumi" },
            new[] { "sasaki", "mio", "madame_k" },
        };

        /// <summary>The starting tool belt in hotkey order (1 Hands, 2 Screwdriver, 3 Spudger, 4 Suction Cup).</summary>
        public static List<ToolDefinition> EnsureTools()
        {
            var result = new List<ToolDefinition>();
            foreach (ToolSpec spec in Tools)
            {
                var tool = EditorAssetUtil.LoadOrCreate<ToolDefinition>($"{ToolsFolder}/{spec.Asset}.asset", out bool created);
                if (created)
                {
                    EditorAssetUtil.Wire(tool, ("toolId", spec.Id), ("displayName", spec.Name), ("kind", spec.Kind), ("description", spec.Description));
                }

                EditorAssetUtil.Wire(tool,
                    ("icon", PixelArtSetup.LoadSprite(spec.Icon)),
                    ("cursor", spec.Cursor != null ? PixelArtSetup.LoadSprite(spec.Cursor) : null));
                EditorUtility.SetDirty(tool);
                result.Add(tool);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        public static List<CustomerDefinition> EnsureCustomers(PhoneModelDefinition phone)
        {
            var result = new List<CustomerDefinition>();
            foreach (CustomerSpec spec in Customers)
            {
                var customer = EditorAssetUtil.LoadOrCreate<CustomerDefinition>($"{CustomersFolder}/CUS_{Capitalise(spec.Id)}.asset", out bool created);
                if (created)
                {
                    EditorAssetUtil.Wire(customer,
                        ("customerId", spec.Id),
                        ("displayName", spec.Name),
                        ("voicePitch", spec.Voice),
                        ("brokenSlot", spec.BrokenSlot),
                        ("problemLabel", spec.ProblemLabel),
                        ("payment", spec.Payment),
                        ("tip", spec.Tip),
                        ("greeting", spec.Greeting),
                        ("problem", spec.Problem),
                        ("accepted", spec.Accepted),
                        ("declined", spec.Declined),
                        ("fixedLine", spec.Fixed),
                        ("stillBrokenLine", spec.StillBroken));
                }

                EditorAssetUtil.Wire(customer,
                    ("phone", phone),
                    ("happy", PixelArtSetup.LoadSprite($"cust_{spec.Id}_happy")),
                    ("sad", PixelArtSetup.LoadSprite($"cust_{spec.Id}_sad")),
                    ("faceNeutral", PixelArtSetup.LoadSprite($"face_{spec.Id}_neutral")),
                    ("faceHappy", PixelArtSetup.LoadSprite($"face_{spec.Id}_happy")),
                    ("faceWorried", PixelArtSetup.LoadSprite($"face_{spec.Id}_worried")));
                EditorAssetUtil.WireArray(customer, "idle", PixelArtSetup.LoadFrames($"cust_{spec.Id}_idle"));
                EditorAssetUtil.WireArray(customer, "talk", PixelArtSetup.LoadFrames($"cust_{spec.Id}_talk"));
                EditorUtility.SetDirty(customer);
                result.Add(customer);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        public static ShopCalendar EnsureCalendar(List<CustomerDefinition> customers)
        {
            var calendar = EditorAssetUtil.LoadOrCreate<ShopCalendar>(CalendarPath, out bool created);
            var so = new SerializedObject(calendar);
            SerializedProperty days = so.FindProperty("days");
            if (created || days.arraySize == 0)
            {
                var byId = new Dictionary<string, CustomerDefinition>();
                foreach (CustomerDefinition c in customers)
                {
                    byId[c.CustomerId] = c;
                }

                days.arraySize = Days.Length;
                for (int d = 0; d < Days.Length; d++)
                {
                    SerializedProperty list = days.GetArrayElementAtIndex(d).FindPropertyRelative("customers");
                    list.arraySize = Days[d].Length;
                    for (int i = 0; i < Days[d].Length; i++)
                    {
                        list.GetArrayElementAtIndex(i).objectReferenceValue = byId[Days[d][i]];
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(calendar);
            AssetDatabase.SaveAssets();
            return calendar;
        }

        private static string Capitalise(string id)
        {
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Length > 0 ? char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1) : parts[i];
            }

            return string.Join(string.Empty, parts);
        }
    }
}

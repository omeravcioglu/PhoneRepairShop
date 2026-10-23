using System;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>
    /// Paints a placeholder for every sprite in the art contract, at the contract's size and layout,
    /// so the game is playable and readable before the real art exists. Deliberately simple shapes in
    /// the Phone Fixer palette. Real art replaces these file by file.
    /// </summary>
    internal static class PlaceholderArt
    {
        // palette (Phone Fixer art direction v1)
        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 Cream = Hex("F5E8D3");
        private static readonly Color32 Paper = Hex("FFFDF8");
        private static readonly Color32 Kraft = Hex("E6D2B5");
        private static readonly Color32 Oak = Hex("E4C8A2");
        private static readonly Color32 Wood = Hex("B88B61");
        private static readonly Color32 Walnut = Hex("8A6244");
        private static readonly Color32 DarkWood = Hex("6B4A33");
        private static readonly Color32 Navy = Hex("283747");
        private static readonly Color32 DeepNavy = Hex("1C2733");
        private static readonly Color32 Sage = Hex("9DB89A");
        private static readonly Color32 MatGreen = Hex("7FB39A");
        private static readonly Color32 MatLine = Hex("98C7B0");
        private static readonly Color32 Dusty = Hex("8FAACC");
        private static readonly Color32 Sky = Hex("BFE0F2");
        private static readonly Color32 Peach = Hex("F7CFA6");
        private static readonly Color32 Butter = Hex("F2D98B");
        private static readonly Color32 Blush = Hex("F2B5B0");
        private static readonly Color32 Pink = Hex("F4A7BE");
        private static readonly Color32 DeepPink = Hex("D9829E");
        private static readonly Color32 Plum = Hex("8E5C7E");
        private static readonly Color32 Wisteria = Hex("B9A6E8");
        private static readonly Color32 Orange = Hex("F2A65A");
        private static readonly Color32 Teal = Hex("6FA9A3");
        private static readonly Color32 Coral = Hex("DE8175");
        private static readonly Color32 Leaf = Hex("8CCB6E");
        private static readonly Color32 Steel = Hex("C7CDD6");
        private static readonly Color32 SteelDark = Hex("8C94A0");
        private static readonly Color32 Charcoal = Hex("3A3F4A");
        private static readonly Color32 Pcb = Hex("3E7A5A");
        private static readonly Color32 Amber = Hex("E8A33D");
        private static readonly Color32 Grey = Hex("C9C4BC");
        private static readonly Color32 White = new(255, 255, 255, 255);

        public static PixelCanvas Paint(SpriteSpec spec, int frame)
        {
            var c = new PixelCanvas(spec.w, spec.h);
            string id = spec.id;
            switch (id)
            {
                case "shop_bg": ShopBackground(c); break;
                case "shop_door_closed": Door(c, open: false); break;
                case "shop_door_open": Door(c, open: true); break;
                case "shop_counter": Counter(c); break;
                case "prop_register": Register(c); break;
                case "prop_bell": Bell(c, frame); break;
                case "prop_cat": Cat(c, frame); break;
                case "prop_sign_open": Sign(c, "OPEN", Teal); break;
                case "prop_sign_closed": Sign(c, "CLOSED", Coral); break;
                case "prop_phone_counter": CounterPhone(c); break;
                case "bench_bg": BenchBackground(c); break;
                case "bench_spares_box": SparesBox(c); break;
                case "ui_bubble": c.RoundBox(0, 0, 24, 24, 5, Paper, Navy); break;
                case "ui_bubble_tail": BubbleTail(c); break;
                case "ui_more": MoreArrow(c); break;
                case "ui_paper": PaperPanel(c); break;
                case "ui_ticket": Ticket(c); break;
                case "ui_button": Button(c); break;
                case "ui_pill": c.RoundBox(0, 0, 16, 16, 4, new Color32(28, 39, 51, 230), Hex("4A5A6E")); break;
                case "ui_tool_slot": ToolSlot(c, frame == 1); break;
                case "ui_ring": Ring(c, frame, spec.FrameCount - 1); break;
                case "ui_portrait_frame": c.RoundBox(0, 0, 52, 52, 6, Cream, Navy); break;
                default:
                    if (id.StartsWith("cust_", StringComparison.Ordinal))
                    {
                        Customer(c, id, frame);
                    }
                    else if (id.StartsWith("face_", StringComparison.Ordinal))
                    {
                        Face(c, id);
                    }
                    else if (id.StartsWith("phone_", StringComparison.Ordinal))
                    {
                        PhoneLayer(c, id, frame);
                    }
                    else if (id.StartsWith("tool_", StringComparison.Ordinal) || id.StartsWith("cursor_", StringComparison.Ordinal))
                    {
                        ToolArt(c, id);
                    }
                    else
                    {
                        Generic(c, id);
                    }

                    break;
            }

            return c;
        }

        // ------------------------------------------------------------------ shop

        private static void ShopBackground(PixelCanvas c)
        {
            c.Fill(0, 0, 640, 360, Hex("F1E3CC"));                  // wall
            c.Fill(0, 0, 640, 112, Oak);                             // floor (hidden by the counter)
            c.Fill(0, 112, 640, 34, Wood);                           // wainscot
            c.Fill(0, 145, 640, 2, Walnut);
            for (int x = 0; x < 640; x += 40)
            {
                c.Fill(x, 112, 1, 33, Walnut);
            }

            c.Fill(0, 344, 640, 16, Walnut);                         // ceiling beam
            c.Fill(0, 343, 640, 1, DarkWood);

            // shelves left of the door
            for (int s = 0; s < 3; s++)
            {
                int y = 170 + s * 56;
                c.Fill(8, y, 92, 5, Walnut);
                c.Fill(8, y - 1, 92, 1, DarkWood);
                Color32[] boxes = { Coral, Teal, Butter, Wisteria, Pink, Sage, Orange, Dusty };
                int bx = 12;
                for (int b = 0; bx < 92; b++)
                {
                    int w = 10 + (b * 7 + s * 3) % 9;
                    int h = 16 + (b * 5 + s * 4) % 18;
                    c.Box(bx, y + 5, w, h, boxes[(b + s * 3) % boxes.Length], Navy);
                    bx += w + 2;
                }
            }

            // doorway (the door sprites cover it)
            c.Fill(110, 112, 80, 132, DeepNavy);
            c.Fill(104, 112, 6, 138, Walnut);
            c.Fill(190, 112, 6, 138, Walnut);
            c.Fill(104, 244, 92, 6, Walnut);

            // price board
            c.Box(214, 168, 96, 80, Navy, DeepNavy);
            c.Text("REPAIRS", 222, 240, Butter);
            c.Text("Screen  3000", 220, 222, Cream);
            c.Text("Battery 2400", 220, 208, Cream);
            c.Text("Port    1800", 220, 194, Cream);
            c.Text("Cat pets  free", 220, 180, Blush);

            // wall clock
            c.Ellipse(260, 300, 18, 18, Cream, Navy);
            c.Line(260, 300, 260, 312, Navy);
            c.Line(260, 300, 268, 296, Navy);
            c.Plot(260, 300, Coral);

            // poster: Mochi Mobile
            c.Box(318, 180, 64, 84, Pink, Navy);
            c.Ellipse(350, 232, 18, 15, Paper, Navy);
            c.Fill(343, 234, 2, 3, Navy);
            c.Fill(356, 234, 2, 3, Navy);
            c.Fill(339, 228, 3, 2, Blush);
            c.Fill(359, 228, 3, 2, Blush);
            c.TextCentered("MOCHI", 350, 210, Navy);
            c.TextCentered("MOBILE", 350, 198, Navy);

            // window onto the street, golden hour
            int wx = 400, wy = 150, ww = 232, wh = 186;
            c.Fill(wx, wy, ww, wh, Sky);
            for (int i = 0; i < 70; i++)
            {
                c.Fill(wx, wy + i, ww, 1, Lerp(Peach, Sky, i / 70f));
            }

            Color32[] houses = { Dusty, Hex("A9B8D6"), Hex("9CB0C8"), Hex("B3C2DA") };
            int hx = wx;
            for (int i = 0; hx < wx + ww; i++)
            {
                int w = Mathf.Min(30 + i * 13 % 24, wx + ww - hx);
                int h = 40 + i * 17 % 50;
                c.Fill(hx, wy, w, h, houses[i % houses.Length]);
                for (int lx = hx + 5; lx < hx + w - 6; lx += 9)
                {
                    c.Fill(lx, wy + h - 14, 4, 5, Butter);
                }

                hx += w + 3;
            }

            c.Ellipse(470, 300, 22, 7, White);
            c.Ellipse(560, 316, 28, 8, White);
            c.Fill(wx, wy, ww, 4, Walnut);                            // sill and frame
            c.Rect(wx - 4, wy - 4, ww + 8, wh + 8, Walnut);
            c.Rect(wx - 3, wy - 3, ww + 6, wh + 6, Walnut);
            c.Rect(wx - 2, wy - 2, ww + 4, wh + 4, DarkWood);
            c.Fill(wx + ww / 2 - 1, wy, 3, wh, Walnut);
            c.Fill(wx, wy + wh / 2, ww, 2, Walnut);

            // plant on the sill
            c.Box(420, 150, 18, 14, Coral, Navy);
            c.Ellipse(429, 172, 10, 8, Sage, Hex("6E8F6B"));
        }

        private static void Door(PixelCanvas c, bool open)
        {
            c.Fill(0, 0, 80, 132, DeepNavy);
            if (open)
            {
                // the street outside: sky, a lantern, the pavement
                for (int y = 20; y < 132; y++)
                {
                    c.Fill(0, y, 80, 1, Lerp(Peach, Sky, (y - 20) / 112f));
                }

                c.Fill(0, 0, 80, 20, Grey);
                c.Fill(0, 19, 80, 1, SteelDark);
                c.Fill(52, 20, 14, 70, Dusty);
                c.Ellipse(14, 104, 5, 7, Coral, Navy);
                c.Fill(0, 0, 9, 132, Wood);                           // the door swung inward
                c.Rect(0, 0, 9, 132, Walnut);
            }
            else
            {
                c.Box(0, 0, 80, 132, Wood, Walnut);
                c.Rect(1, 1, 78, 130, Walnut);
                c.Box(12, 66, 56, 52, Sky, Walnut);                   // glass panel
                c.Fill(12, 92, 56, 2, Walnut);
                c.Fill(39, 66, 2, 52, Walnut);
                c.Box(12, 12, 56, 44, Hex("C49A6C"), Walnut);         // lower panel
                c.Ellipse(70, 60, 3, 3, Butter, Walnut);              // knob
                c.Box(18, 96, 44, 16, Paper, Navy);                   // little sign
                c.TextCentered("WELCOME", 40, 110, Navy);
            }
        }

        private static void Counter(PixelCanvas c)
        {
            c.Fill(0, 0, 640, 100, Wood);
            for (int x = 0; x < 640; x += 64)
            {
                c.Fill(x, 0, 2, 100, Walnut);
                c.Fill(x + 2, 0, 1, 100, Hex("C99B6F"));
            }

            c.Fill(0, 98, 640, 2, DarkWood);
            c.Fill(0, 100, 640, 12, Oak);                              // top surface
            c.Fill(0, 110, 640, 2, Hex("F0D9B5"));
            c.Fill(0, 100, 640, 1, Walnut);

            // shop sign on the front
            c.RoundBox(212, 26, 216, 50, 6, Cream, Navy);
            c.TextCentered("PHONE FIXER", 320, 66, Navy, 2);
            c.TextCentered("repairs  -  cases  -  cat", 320, 42, Walnut);
        }

        private static void Register(PixelCanvas c)
        {
            c.RoundBox(4, 0, 64, 34, 3, Charcoal, DeepNavy);
            c.Fill(8, 4, 56, 2, Hex("2C313A"));
            for (int r = 0; r < 2; r++)
            {
                for (int k = 0; k < 6; k++)
                {
                    c.Box(9 + k * 9, 12 + r * 9, 7, 6, Cream, Navy);
                }
            }

            c.RoundBox(10, 34, 52, 20, 2, Charcoal, DeepNavy);          // display housing
            c.Fill(13, 36, 46, 15, Hex("1E3A3A"));                     // display window (y 36-50)
            c.Fill(13, 50, 46, 1, Hex("2E5050"));
        }

        private static void Bell(PixelCanvas c, int frame)
        {
            c.Ellipse(12, 3, 11, 3, Walnut, DarkWood);
            c.Shape(2, 4, 21, 15, (x, y) => (x - 11.5f) * (x - 11.5f) / 81f + (y - 4f) * (y - 4f) / 100f <= 1f && y >= 4, Steel, SteelDark);
            int knob = frame == 1 ? 14 : 16;
            c.Fill(10, knob, 4, 3, SteelDark);
            c.Fill(11, knob + 3, 2, 1, Charcoal);
            if (frame == 1)
            {
                c.Plot(2, 17, Butter);
                c.Plot(21, 17, Butter);
                c.Plot(4, 19, Butter);
                c.Plot(19, 19, Butter);
            }
        }

        private static void Cat(PixelCanvas c, int frame)
        {
            Color32 fur = Orange;
            Color32 line = Hex("9A5A2A");
            c.Ellipse(24, 10, 21, 10, fur, line);                     // loaf
            c.Ellipse(32, 21, 11, 10, fur, line);                     // head
            c.Shape(22, 26, 28, 35, (x, y) => y - 26 <= (x - 22) * 1.6f && y - 26 <= (28 - x) * 1.6f, fur, line);   // ears
            c.Shape(36, 26, 42, 35, (x, y) => y - 26 <= (x - 36) * 1.6f && y - 26 <= (42 - x) * 1.6f, fur, line);
            for (int s = 0; s < 3; s++)
            {
                c.Fill(8 + s * 6, 14, 2, 6, Hex("D98C4A"));             // stripes
            }

            if (frame == 1)
            {
                c.Fill(27, 22, 3, 1, Navy);
                c.Fill(35, 22, 3, 1, Navy);
            }
            else
            {
                c.Fill(28, 21, 2, 3, Navy);
                c.Fill(36, 21, 2, 3, Navy);
            }

            c.Plot(32, 18, Coral);
            c.Line(40, 18, 46, 19, line);
            c.Line(40, 17, 46, 16, line);
            c.Line(24, 18, 18, 19, line);
            c.Ellipse(6, 6, 5, 4, fur, line);                         // tail curl
        }

        private static void Sign(PixelCanvas c, string word, Color32 colour)
        {
            c.Line(12, 31, 20, 24, Walnut);
            c.Line(51, 31, 43, 24, Walnut);
            c.RoundBox(2, 2, 60, 22, 3, Oak, Walnut);
            c.TextCentered(word, 32, 17, colour);
            c.TextCentered(word, 33, 17, colour);
        }

        private static void CounterPhone(PixelCanvas c)
        {
            c.Shape(1, 1, 22, 9, (x, y) => y <= 8 && y >= 2 && x >= 2 + (8 - y) / 2 && x <= 21 - (8 - y) / 2, Pink, DeepPink);
            c.Fill(4, 1, 16, 1, Navy);
            c.Ellipse(8, 6, 1.5f, 1.2f, Charcoal);
        }

        // ------------------------------------------------------------------ bench

        private static void BenchBackground(PixelCanvas c)
        {
            c.Fill(0, 0, 640, 360, Oak);
            for (int y = 3; y < 360; y += 11)
            {
                for (int x = (y * 7) % 23; x < 640; x += 37 + (y % 13))
                {
                    c.Fill(x, y, 14 + (x % 9), 1, Hex("D8B98E"));
                }
            }

            // cutting mat
            c.RoundBox(150, 18, 290, 322, 8, MatGreen, Hex("5E9680"));
            for (int x = 160; x < 440; x += 10)
            {
                c.Fill(x, 20, 1, 318, MatLine);
            }

            for (int y = 28; y < 340; y += 10)
            {
                c.Fill(152, y, 286, 1, MatLine);
            }

            c.Fill(154, 22, 282, 1, Paper);
            for (int x = 160; x < 436; x += 10)
            {
                c.Fill(x, 22, 1, x % 50 == 10 ? 5 : 3, Paper);
            }

            // tool rack strip
            c.RoundBox(8, 72, 48, 208, 4, Walnut, DarkWood);
            c.Fill(12, 76, 40, 200, Hex("7A563C"));

            // screw tray, six compartments
            c.RoundBox(456, 272, 168, 72, 6, Hex("A9C2D0"), Hex("6E8A9C"));
            int[] xs = { 484, 540, 596 };
            int[] ys = { 326, 290 };
            foreach (int ty in ys)
            {
                foreach (int tx in xs)
                {
                    c.RoundBox(tx - 24, ty - 15, 48, 30, 4, Hex("C7D9E3"), Hex("8FA8B8"));
                }
            }

            // chalk marks where parts go
            DashedRect(c, 316, 62, 100, 196, Paper);
            DashedRect(c, 442, 142, 84, 100, Paper);
            DashedRect(c, 530, 142, 84, 100, Paper);
            DashedRect(c, 454, 79, 60, 50, Paper);
            DashedRect(c, 542, 79, 60, 50, Paper);
        }

        private static void DashedRect(PixelCanvas c, int x, int y, int w, int h, Color32 colour)
        {
            Color32 chalk = new(colour.r, colour.g, colour.b, 150);
            for (int i = 0; i < w; i += 6)
            {
                c.Fill(x + i, y, Mathf.Min(3, w - i), 1, chalk);
                c.Fill(x + i, y + h - 1, Mathf.Min(3, w - i), 1, chalk);
            }

            for (int j = 0; j < h; j += 6)
            {
                c.Fill(x, y + j, 1, Mathf.Min(3, h - j), chalk);
                c.Fill(x + w - 1, y + j, 1, Mathf.Min(3, h - j), chalk);
            }
        }

        private static void SparesBox(PixelCanvas c)
        {
            c.Box(4, 1, 88, 36, Kraft, Walnut);
            c.Fill(5, 30, 86, 2, Hex("D2BB98"));
            c.Shape(4, 37, 20, 46, (x, y) => y - 37 <= (x - 4) * 0.6f, Kraft, Walnut);       // flaps
            c.Shape(76, 37, 92, 46, (x, y) => y - 37 <= (92 - x) * 0.6f, Kraft, Walnut);
            c.Box(20, 30, 14, 12, Pcb, Navy);                                                 // a board peeking out
            c.Box(56, 31, 16, 10, Steel, SteelDark);                                          // a battery
            c.Box(28, 8, 40, 14, Paper, Navy);
            c.TextCentered("SPARES", 48, 19, Navy);
        }

        // ------------------------------------------------------------------ customers

        private sealed class Look
        {
            public Color32 Skin = Hex("F2D2B8");
            public Color32 Hair = Hex("4A3A34");
            public Color32 Shirt = Teal;
            public Color32 Trim = Cream;
            public Action<PixelCanvas, int, int> Extra;        // hair styles, accessories (canvas, headX, headY)
        }

        private static Look LookFor(string who)
        {
            switch (who)
            {
                case "fumi":
                    return new Look
                    {
                        Skin = Hex("F2D2B8"), Hair = Hex("E8E4EE"), Shirt = Wisteria, Trim = Paper,
                        Extra = (c, hx, hy) =>
                        {
                            c.Ellipse(hx, hy + 25, 10, 7, Hex("E8E4EE"), Hex("A9A2B5"));           // bun
                            c.Ellipse(hx - 8, hy + 2, 6, 5, Clear, Navy);                         // glasses
                            c.Ellipse(hx + 8, hy + 2, 6, 5, Clear, Navy);
                            c.Fill(hx - 2, hy + 3, 4, 1, Navy);
                        },
                    };
                case "sasaki":
                    return new Look
                    {
                        Skin = Hex("E8C4A8"), Hair = Hex("26242C"), Shirt = Navy, Trim = Paper,
                        Extra = (c, hx, hy) =>
                        {
                            c.Fill(hx - 21, hy + 10, 42, 6, Hex("26242C"));                     // side part
                            c.Fill(hx - 21, hy + 4, 5, 8, Hex("26242C"));
                        },
                    };
                case "mio":
                    return new Look
                    {
                        Skin = Hex("F5D7C0"), Hair = Hex("5A3E36"), Shirt = Paper, Trim = Navy,
                        Extra = (c, hx, hy) =>
                        {
                            c.Ellipse(hx - 26, hy - 6, 6, 14, Hex("5A3E36"), Hex("3E2A24"));       // twin tails
                            c.Ellipse(hx + 26, hy - 6, 6, 14, Hex("5A3E36"), Hex("3E2A24"));
                            c.Fill(hx - 25, hy + 6, 4, 4, Coral);
                            c.Fill(hx + 22, hy + 6, 4, 4, Coral);
                        },
                    };
                case "ren":
                    return new Look
                    {
                        Skin = Hex("EBCDB4"), Hair = Hex("6B4E8A"), Shirt = Hex("5E9C93"), Trim = Hex("4A7F77"),
                        Extra = (c, hx, hy) =>
                        {
                            c.Fill(hx - 24, hy + 14, 48, 4, Charcoal);                           // headphones
                            c.Box(hx - 27, hy - 6, 8, 14, Charcoal, DeepNavy);
                            c.Box(hx + 19, hy - 6, 8, 14, Charcoal, DeepNavy);
                            c.Fill(hx - 12, hy - 3, 6, 1, Hex("B79AC4"));                        // tired eyes
                            c.Fill(hx + 6, hy - 3, 6, 1, Hex("B79AC4"));
                        },
                    };
                case "madame_k":
                    return new Look
                    {
                        Skin = Hex("F0D0BC"), Hair = Hex("2E2230"), Shirt = Plum, Trim = Cream,
                        Extra = (c, hx, hy) =>
                        {
                            c.Ellipse(hx, hy + 18, 34, 6, Plum, Hex("5E3A54"));                  // hat brim
                            c.Ellipse(hx, hy + 26, 17, 9, Plum, Hex("5E3A54"));
                            c.Ellipse(hx + 14, hy + 26, 5, 5, Pink, DeepPink);                   // flower
                            for (int p = -14; p <= 14; p += 4)
                            {
                                c.Ellipse(hx + p, hy - 26 + Mathf.Abs(p) / 4, 1.6f, 1.6f, White); // pearls
                            }
                        },
                    };
                default:
                    return new Look();
            }
        }

        private enum Expression
        {
            Neutral,
            Blink,
            TalkOpen,
            Happy,
            Sad,
            Worried,
        }

        private static void Customer(PixelCanvas c, string id, int frame)
        {
            // id = cust_<who>_<pose>
            string rest = id.Substring("cust_".Length);
            int last = rest.LastIndexOf('_');
            string who = rest.Substring(0, last);
            string pose = rest.Substring(last + 1);
            Look look = LookFor(who);

            Expression expression = pose switch
            {
                "idle" => frame == 1 ? Expression.Blink : Expression.Neutral,
                "talk" => frame == 1 ? Expression.TalkOpen : Expression.Neutral,
                "happy" => Expression.Happy,
                "sad" => Expression.Sad,
                _ => Expression.Neutral,
            };

            const int headX = 48;
            const int headY = 124;
            // gumdrop body (only the top shows above the counter)
            c.RoundBox(16, 0, 64, 104, 22, look.Shirt, Navy);
            c.Shape(38, 80, 58, 103, (x, y) => Mathf.Abs(x - 48) <= (y - 80) * 0.45f, look.Trim, null);    // collar V
            if (who == "sasaki")
            {
                c.Shape(45, 72, 51, 98, (x, y) => Mathf.Abs(x - 48) <= 2 + (98 - y) * 0.08f, Coral, Hex("A85A50")); // tie
            }

            if (who == "mio")
            {
                c.Shape(30, 84, 66, 103, (x, y) => y >= 96 - Mathf.Abs(x - 48) * 0.4f, Navy, null);          // sailor collar
                c.Ellipse(48, 86, 6, 4, Coral, Hex("A85A50"));                                                 // bow
            }

            if (who == "fumi")
            {
                for (int b = 0; b < 3; b++)
                {
                    c.Ellipse(48, 92 - b * 9, 1.6f, 1.6f, Paper, Navy);                                      // buttons
                }
            }

            if (who == "madame_k")
            {
                c.Ellipse(48, 102, 26, 8, Cream, Kraft);                                                       // fur collar
                c.Ellipse(80, 76, 8, 7, Paper, Navy);                                                          // Bijou the dog
                c.Fill(76, 77, 2, 2, Navy);
                c.Fill(82, 77, 2, 2, Navy);
                c.Plot(80, 74, Navy);
                c.Ellipse(73, 80, 3, 4, Kraft, Navy);
                c.Ellipse(87, 80, 3, 4, Kraft, Navy);
            }

            // mitten hands resting on the counter edge
            c.Ellipse(24, 74, 8, 6, look.Skin, Navy);
            c.Ellipse(72, 74, 8, 6, look.Skin, Navy);

            Head(c, headX, headY, 22, 21, look, expression, who);
        }

        private static void Face(PixelCanvas c, string id)
        {
            // id = face_<who>_<mood>
            string rest = id.Substring("face_".Length);
            int last = rest.LastIndexOf('_');
            string who = rest.Substring(0, last);
            string mood = rest.Substring(last + 1);
            Look look = LookFor(who);
            Expression expression = mood switch
            {
                "happy" => Expression.Happy,
                "worried" => Expression.Worried,
                _ => Expression.Neutral,
            };

            c.Fill(0, 0, 48, 48, Hex("E9DCC6"));
            c.RoundBox(8, 0, 32, 10, 4, look.Shirt, Navy);
            Head(c, 24, 22, 17, 16, look, expression, who);
        }

        private static void Head(PixelCanvas c, int hx, int hy, int rx, int ry, Look look, Expression e, string who)
        {
            c.Ellipse(hx, hy + ry - 3, rx + 1, 9, look.Hair, Navy);                 // hair cap
            c.Ellipse(hx, hy, rx, ry, look.Skin, Navy);                              // face
            c.Shape(hx - rx, hy + 4, hx + rx, hy + ry + 2,
                (x, y) => Mathf.Pow((x + 0.5f - hx) / (rx + 0.5f), 2) + Mathf.Pow((y + 0.5f - hy - 6) / (ry - 2f), 2) <= 1f && y > hy + 8 - Mathf.Abs(x - hx) * 0.15f,
                look.Hair, null);                                                    // fringe
            look.Extra?.Invoke(c, hx, hy);

            int eyeY = hy - 2;
            int left = hx - 7;
            int right = hx + 6;
            switch (e)
            {
                case Expression.Blink:
                    c.Fill(left - 1, eyeY, 4, 1, Navy);
                    c.Fill(right - 1, eyeY, 4, 1, Navy);
                    break;
                case Expression.Happy:
                    c.Line(left - 1, eyeY - 1, left, eyeY + 1);
                    c.Line(left, eyeY + 1, left + 2, eyeY - 1);
                    c.Line(right - 1, eyeY - 1, right, eyeY + 1);
                    c.Line(right, eyeY + 1, right + 2, eyeY - 1);
                    break;
                case Expression.Worried:
                    c.Ellipse(left + 0.5f, eyeY + 0.5f, 2.2f, 2.6f, Paper, Navy);
                    c.Ellipse(right + 0.5f, eyeY + 0.5f, 2.2f, 2.6f, Paper, Navy);
                    c.Plot(left, eyeY, Navy);
                    c.Plot(right + 1, eyeY, Navy);
                    break;
                default:
                    c.Fill(left, eyeY - 1, 2, 3, Navy);
                    c.Fill(right, eyeY - 1, 2, 3, Navy);
                    c.Plot(left, eyeY + 1, Paper);
                    c.Plot(right, eyeY + 1, Paper);
                    break;
            }

            if (e == Expression.Sad)
            {
                c.Line(left - 2, eyeY + 4, left + 2, eyeY + 3, Navy);               // sad brows
                c.Line(right - 1, eyeY + 3, right + 3, eyeY + 4, Navy);
            }

            c.Fill(hx - 12, hy - 7, 4, 2, Blush);
            c.Fill(hx + 9, hy - 7, 4, 2, Blush);

            int mouthY = hy - 10;
            switch (e)
            {
                case Expression.TalkOpen:
                    c.Ellipse(hx + 0.5f, mouthY + 0.5f, 3f, 2.5f, Hex("7A2E3A"), Navy);
                    c.Fill(hx - 1, mouthY - 1, 3, 1, Coral);
                    break;
                case Expression.Happy:
                    c.Shape(hx - 5, mouthY - 3, hx + 5, mouthY + 1, (x, y) => y >= mouthY - 3 + Mathf.Abs(x - hx) * 0.5f && y <= mouthY + 1, Hex("7A2E3A"), Navy);
                    break;
                case Expression.Sad:
                    c.Line(hx - 3, mouthY - 1, hx, mouthY + 1, Navy);
                    c.Line(hx, mouthY + 1, hx + 3, mouthY - 1, Navy);
                    c.Ellipse(hx + rx - 2, hy + 6, 1.6f, 2.4f, Sky, Dusty);          // sweat drop
                    break;
                case Expression.Worried:
                    c.Line(hx - 4, mouthY, hx - 2, mouthY + 1, Navy);
                    c.Line(hx - 2, mouthY + 1, hx, mouthY, Navy);
                    c.Line(hx, mouthY, hx + 2, mouthY + 1, Navy);
                    c.Line(hx + 2, mouthY + 1, hx + 4, mouthY, Navy);
                    c.Ellipse(hx + rx - 2, hy + 6, 1.6f, 2.4f, Sky, Dusty);
                    break;
                default:
                    c.Fill(hx - 2, mouthY, 5, 1, Navy);
                    break;
            }
        }

        // ------------------------------------------------------------------ phone (104x200 canvas, centre 52,100)

        private const int Cx = 52;
        private const int Cy = 100;

        private static void PhoneLayer(PixelCanvas c, string id, int frame)
        {
            switch (id)
            {
                case "phone_front_body":
                    c.RoundBox(Cx - 48, Cy - 96, 96, 192, 12, Cream, Navy);
                    c.RoundBox(Cx - 45, Cy - 92, 90, 184, 9, Hex("EADCC4"), Hex("D8C7AA"));
                    c.Fill(Cx + 48, Cy + 30, 2, 22, Hex("D8C7AA"));               // buttons
                    c.Fill(Cx + 48, Cy + 58, 2, 10, Hex("D8C7AA"));
                    c.Fill(Cx - 8, Cy + 88, 16, 2, Charcoal);                     // earpiece
                    c.Ellipse(Cx + 16, Cy + 88, 1.5f, 1.5f, Charcoal);
                    break;
                case "phone_screen":
                case "phone_screen_cracked":
                    c.RoundBox(Cx - 42, Cy - 84, 84, 168, 4, DeepNavy, Hex("101820"));
                    c.Line(Cx - 34, Cy + 70, Cx - 20, Cy + 80, Hex("33465A"));
                    c.Line(Cx - 34, Cy + 64, Cx - 14, Cy + 78, Hex("33465A"));
                    c.Text("12:34", Cx - 14, Cy + 40, Hex("4A5E74"));
                    if (id == "phone_screen_cracked")
                    {
                        Cracks(c, Cx + 10, Cy + 28);
                    }

                    break;
                case "phone_back_body":
                    c.RoundBox(Cx - 48, Cy - 96, 96, 192, 12, Cream, Navy);
                    c.RoundBox(Cx - 44, Cy - 92, 88, 184, 8, Grey, Hex("A8A29A"));
                    foreach (Vector2Int s in ScrewSpots)
                    {
                        c.Ellipse(Cx + s.x + 0.5f, Cy + s.y + 0.5f, 5f, 5f, Hex("B5AFA6"), Hex("8C867E"));
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        c.Fill(Cx - 30 + i * 6, Cy - 84, 3, 3, Charcoal);         // speaker grille
                    }

                    c.Box(Cx + 6, Cy - 95, 20, 4, Charcoal, DeepNavy);           // charging port
                    break;
                case "phone_board":
                    c.RoundBox(Cx - 42, Cy + 30, 84, 60, 4, Pcb, Hex("2C5A42"));
                    c.Box(Cx - 6, Cy + 54, 18, 18, Charcoal, DeepNavy);           // SoC
                    c.Box(Cx + 18, Cy + 66, 14, 10, Charcoal, DeepNavy);
                    c.Box(Cx - 2, Cy + 36, 14, 8, SteelDark, Charcoal);
                    for (int i = 0; i < 7; i++)
                    {
                        c.Fill(Cx - 36 + i * 5, Cy + 34, 2, 2, Butter);           // pads
                    }

                    c.Ellipse(Cx - 24, Cy + 74, 9, 9, Charcoal, DeepNavy);       // camera
                    c.Ellipse(Cx - 24, Cy + 74, 4, 4, Dusty, Navy);
                    c.Ellipse(Cx + 26, Cy + 40, 5, 5, Butter, Walnut);           // smiley sticker
                    c.Plot(Cx + 24, Cy + 41, Navy);
                    c.Plot(Cx + 28, Cy + 41, Navy);
                    c.Fill(Cx + 25, Cy + 38, 3, 1, Navy);
                    break;
                case "phone_battery":
                case "phone_battery_swollen":
                    bool swollen = id == "phone_battery_swollen";
                    if (swollen)
                    {
                        c.RoundBox(Cx - 34, Cy - 70, 68, 88, 12, Hex("C8D2DC"), Navy);
                        c.Ellipse(Cx, Cy - 26, 26, 34, Hex("D6DEE6"));
                    }
                    else
                    {
                        c.RoundBox(Cx - 32, Cy - 68, 64, 84, 4, Hex("B8C4D0"), Navy);
                    }

                    c.Box(Cx - 24, Cy - 46, 48, 40, Paper, Navy);
                    c.TextCentered("MOCHI", Cx, Cy - 10, Navy);
                    c.TextCentered("3000", Cx, Cy - 22, Teal);
                    c.TextCentered("mAh", Cx, Cy - 33, Navy);
                    if (swollen)
                    {
                        c.Fill(Cx - 9, Cy + 6, 3, 3, Navy);                       // the battery itself looks worried
                        c.Fill(Cx + 7, Cy + 6, 3, 3, Navy);
                        c.Line(Cx - 4, Cy + 1, Cx - 2, Cy + 2, Navy);
                        c.Line(Cx - 2, Cy + 2, Cx, Cy + 1, Navy);
                        c.Line(Cx, Cy + 1, Cx + 2, Cy + 2, Navy);
                        c.Line(Cx + 2, Cy + 2, Cx + 4, Cy + 1, Navy);
                        c.Ellipse(Cx + 22, Cy + 8, 2f, 3f, Sky, Dusty);          // sweat drop
                    }

                    c.Box(Cx - 6, Cy - 76, 12, 10, Coral, Hex("A85A50"));         // pull tab
                    break;
                case "phone_battery_conn":
                    c.Box(Cx - 6, Cy + 14, 12, 12, Amber, Hex("A8722A"));
                    c.Box(Cx - 8, Cy + 24, 16, 8, Navy, DeepNavy);
                    break;
                case "phone_battery_conn_open":
                    c.Box(Cx - 6, Cy + 14, 12, 8, Amber, Hex("A8722A"));
                    c.Box(Cx - 9, Cy + 20, 18, 6, Hex("4A5E74"), DeepNavy);      // plug flipped up
                    c.Fill(Cx - 8, Cy + 25, 16, 1, Hex("6E86A0"));
                    c.Box(Cx - 7, Cy + 26, 14, 5, DeepNavy, Charcoal);           // empty socket
                    break;
                case "phone_display_conn":
                    c.Box(Cx + 37, Cy + 42, 8, 10, Amber, Hex("A8722A"));
                    c.Box(Cx + 22, Cy + 40, 16, 14, Navy, DeepNavy);
                    break;
                case "phone_display_conn_open":
                    c.Box(Cx + 37, Cy + 42, 8, 10, Amber, Hex("A8722A"));
                    c.Box(Cx + 28, Cy + 38, 12, 18, Hex("4A5E74"), DeepNavy);    // flipped up
                    c.Box(Cx + 22, Cy + 41, 7, 12, DeepNavy, Charcoal);          // empty socket
                    break;
                case "phone_cover":
                    c.RoundBox(Cx - 48, Cy - 96, 96, 192, 12, Pink, DeepPink);
                    c.Ellipse(Cx - 24, Cy + 74, 11, 11, DeepPink);              // camera window...
                    c.Ellipse(Cx - 24, Cy + 74, 9, 9, Clear);
                    CutHole(c, Cx - 24, Cy + 74, 9);                            // ...see-through
                    foreach (Vector2Int s in ScrewSpots)
                    {
                        c.Ellipse(Cx + s.x + 0.5f, Cy + s.y + 0.5f, 5f, 5f, DeepPink);
                    }

                    c.Ellipse(Cx, Cy - 6, 20, 16, Paper, DeepPink);              // mochi mascot
                    c.Fill(Cx - 8, Cy - 4, 3, 3, Navy);
                    c.Fill(Cx + 6, Cy - 4, 3, 3, Navy);
                    c.Fill(Cx - 13, Cy - 10, 4, 2, Blush);
                    c.Fill(Cx + 10, Cy - 10, 4, 2, Blush);
                    c.Fill(Cx - 1, Cy - 11, 3, 1, Navy);
                    c.TextCentered("mochi", Cx, Cy - 30, DeepPink);
                    break;
                case "phone_screw":
                    Screw(c, frame);
                    break;
                default:
                    Generic(c, id);
                    break;
            }
        }

        private static readonly Vector2Int[] ScrewSpots =
        {
            new(-40, 88), new(40, 88), new(40, -88), new(-40, -88),
        };

        private static void Cracks(PixelCanvas c, int x, int y)
        {
            Color32 crack = Hex("D8ECF6");
            int[,] rays = { { -30, 40 }, { 25, 50 }, { 34, -10 }, { 20, -70 }, { -24, -60 }, { -38, 4 }, { 4, 56 } };
            for (int i = 0; i < rays.GetLength(0); i++)
            {
                int ex = x + rays[i, 0];
                int ey = y + rays[i, 1];
                int mx = x + rays[i, 0] / 2 + (i % 2 == 0 ? 3 : -3);
                int my = y + rays[i, 1] / 2;
                c.Line(x, y, mx, my, crack);
                c.Line(mx, my, ex, ey, crack);
                c.Line(mx, my, mx + (i % 2 == 0 ? 6 : -6), my + 5, crack);
            }

            c.Ellipse(x, y, 4, 4, Hex("8FB6C8"), crack);
        }

        private static void CutHole(PixelCanvas c, int cx, int cy, int r)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r)
                    {
                        c.Clear(x, y);
                    }
                }
            }
        }

        private static void Screw(PixelCanvas c, int frame)
        {
            c.Ellipse(4.5f, 4.5f, 3.6f, 3.6f, Steel, SteelDark);
            c.Plot(3, 6, White);
            switch (frame % 4)
            {
                case 0: c.Line(4, 2, 4, 6, Charcoal); break;
                case 1: c.Line(2, 2, 6, 6, Charcoal); break;
                case 2: c.Line(2, 4, 6, 4, Charcoal); break;
                default: c.Line(2, 6, 6, 2, Charcoal); break;
            }
        }

        // ------------------------------------------------------------------ tools and cursors

        private static void ToolArt(PixelCanvas c, string id)
        {
            switch (id)
            {
                case "tool_hands":
                    Mitten(c, 14, 12, 9);
                    break;
                case "tool_screwdriver":
                    c.Line(5, 5, 14, 14, SteelDark);
                    c.Line(6, 5, 15, 14, Steel);
                    Handle(c, 14, 14, 24, 24, Hex("F28C82"));
                    break;
                case "tool_spudger":
                    c.Line(4, 4, 23, 23, Hex("8E7BC4"));
                    c.Line(5, 4, 24, 23, Wisteria);
                    c.Line(4, 5, 23, 24, Wisteria);
                    c.Fill(3, 3, 3, 2, Hex("8E7BC4"));
                    break;
                case "tool_suction":
                    c.Shape(4, 4, 23, 13, (x, y) => (x - 13.5f) * (x - 13.5f) / 100f + (y - 4f) * (y - 4f) / 81f <= 1f, Hex("9CC9E8"), Dusty);
                    c.Fill(12, 13, 4, 6, Dusty);
                    c.Ellipse(14, 21, 4, 3, Clear, Navy);
                    break;
                case "cursor_hand":
                case "cursor_hand_pressed":
                    bool pressed = id == "cursor_hand_pressed";
                    c.Fill(3, pressed ? 8 : 9, 3, pressed ? 6 : 7, Paper);     // finger
                    c.RoundBox(3, 1, 10, 9, 3, Paper, null);                   // palm
                    c.OutlineSilhouette(Navy);
                    break;
                case "cursor_screwdriver":
                    c.Line(1, 1, 9, 9, SteelDark);
                    c.Line(2, 1, 10, 9, Steel);
                    Handle(c, 10, 10, 21, 21, Hex("F28C82"));
                    c.OutlineSilhouette(new Color32(28, 39, 51, 200));
                    break;
                case "cursor_spudger":
                    c.Line(1, 1, 18, 18, Hex("8E7BC4"));
                    c.Line(2, 1, 19, 18, Wisteria);
                    c.Line(1, 2, 18, 19, Wisteria);
                    c.OutlineSilhouette(new Color32(28, 39, 51, 200));
                    break;
                case "cursor_suction":
                    c.Shape(1, 2, 13, 8, (x, y) => (x - 7f) * (x - 7f) / 42f + (y - 2f) * (y - 2f) / 36f <= 1f, Hex("9CC9E8"), Dusty);
                    c.Line(7, 8, 14, 15, Dusty);
                    c.Ellipse(16, 17, 3, 3, Clear, Navy);
                    c.OutlineSilhouette(new Color32(28, 39, 51, 200));
                    break;
                default:
                    Generic(c, id);
                    break;
            }
        }

        private static void Handle(PixelCanvas c, int x0, int y0, int x1, int y1, Color32 colour)
        {
            for (int t = 0; t <= 10; t++)
            {
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t / 10f));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t / 10f));
                c.Ellipse(x, y, 2.6f, 2.6f, colour);
            }

            c.Line(x0 + 2, y0 - 1, x1 - 1, y1 - 3, Hex("C4655E"));
        }

        private static void Mitten(PixelCanvas c, int x, int y, int r)
        {
            c.RoundBox(x - r, y - r + 2, r * 2, r * 2, r - 2, Hex("F2D2B8"), Navy);
            c.Ellipse(x - r, y, 3, 4, Hex("F2D2B8"), Navy);
            c.Fill(x - 3, y + r - 2, 1, 5, Hex("D9B79A"));
            c.Fill(x + 1, y + r - 2, 1, 5, Hex("D9B79A"));
        }

        // ------------------------------------------------------------------ ui

        private static void BubbleTail(PixelCanvas c)
        {
            c.Shape(0, 0, 11, 9, (x, y) => y >= 9 - (11 - x) * 0.85f && x <= 11 && y <= 9, Paper, null);
            c.Line(0, 0, 11, 9, Navy);
            c.Line(0, 0, 4, 9, Navy);
            c.Fill(1, 9, 10, 1, Paper);
        }

        private static void MoreArrow(PixelCanvas c)
        {
            c.Fill(0, 4, 7, 1, Navy);
            c.Fill(1, 3, 5, 1, Navy);
            c.Fill(2, 2, 3, 1, Navy);
            c.Fill(3, 1, 1, 1, Navy);
        }

        private static void PaperPanel(PixelCanvas c)
        {
            c.RoundBox(2, 0, 22, 22, 4, Navy);                                 // hard drop shadow
            c.RoundBox(0, 2, 22, 22, 4, Paper, Navy);
        }

        private static void Ticket(PixelCanvas c)
        {
            c.Box(0, 0, 24, 24, Kraft, Walnut);
            for (int x = 1; x < 23; x += 3)
            {
                c.Plot(x, 22, Walnut);                                           // perforation
            }
        }

        private static void Button(PixelCanvas c)
        {
            c.RoundBox(0, 0, 24, 18, 4, Navy);                                   // base
            c.RoundBox(0, 2, 24, 18, 4, Cream, Navy);                            // keycap face
            c.Fill(3, 16, 18, 1, Paper);
        }

        private static void ToolSlot(PixelCanvas c, bool selected)
        {
            c.RoundBox(0, 0, 36, 36, 5, selected ? Hex("A8D8D2") : Hex("9C7A5C"), selected ? Teal : DarkWood);
            c.RoundBox(3, 3, 30, 30, 4, selected ? Hex("D6F0EC") : Hex("B38E6C"), null);
        }

        private static void Ring(PixelCanvas c, int frame, int maxFrame)
        {
            float amount = maxFrame <= 0 ? 1f : frame / (float)maxFrame;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = x + 0.5f - 8f;
                    float dy = y + 0.5f - 8f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 7.6f || d < 3.6f)
                    {
                        continue;
                    }

                    // clockwise from the top
                    float angle = Mathf.Atan2(dx, dy) / (Mathf.PI * 2f);
                    if (angle < 0f)
                    {
                        angle += 1f;
                    }

                    bool edge = d > 6.6f || d < 4.6f;
                    Color32 colour = angle < amount ? (edge ? Hex("C47A30") : Orange) : (edge ? DeepNavy : new Color32(40, 55, 71, 200));
                    c.Plot(x, y, colour);
                }
            }
        }

        private static void Generic(PixelCanvas c, string id)
        {
            c.RoundBox(0, 0, c.Width, c.Height, 3, Kraft, Walnut);
            c.Text(id, 2, c.Height - 2, Navy);
        }

        // ------------------------------------------------------------------ helpers

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color colour);
            return colour;
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, Mathf.Clamp01(t));

        private static void Line(this PixelCanvas c, int x0, int y0, int x1, int y1) => c.Line(x0, y0, x1, y1, Navy);
    }
}

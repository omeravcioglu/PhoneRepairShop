using System;
using System.Collections.Generic;
using System.Linq;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.PhoneFixer;
using PhoneRepair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PhoneRepair.EditorTools.Trailer
{
    /// <summary>
    /// The trailer's shot list, in edit order. Lengths are bars/beats of the 100 BPM soundtrack
    /// (1 beat = 0.6 s = 36 frames, 1 bar = 2.4 s). Times inside a shot are seconds from its first frame.
    /// Captions, name tags and 2D effects are added by ArtSource/Trailer/edit_trailer.py.
    /// </summary>
    internal static class TrailerShots
    {
        private const float Beat = 0.6f;
        private const float Bar = 2.4f;
        private const string ShopScene = PhoneFixerPaths.ShopScene;
        private const string BenchScene = PhoneFixerPaths.BenchScene;
        private static readonly Color ReadyGlow = new(1.0f, 0.6f, 0.28f);
        private static readonly Color BlockedGlow = new(1.05f, 0.45f, 0.4f);
        private static readonly Color FixedGlow = new(0.55f, 0.95f, 0.45f);

        private static int Bars(float bars) => TrailerRenderer.Bars(bars);

        public static List<Shot> All() => new()
        {
            CatNap(),
            PlaqueReveal(),
            ShopOpens(),
            CounterCloseUp("b2_fumi", "Fumi", "Talk", 0.2f, 0f, 0.1f),
            CounterCloseUp("b3_ren", "Ren", "Impatient", 0.1f, 6f, 0.06f),
            CounterCloseUp("b4_sasaki", "Sasaki", "Impatient", 0.45f, -4f, 0.02f),
            CounterCloseUp("b5_mio", "Mio", "Confused", 0f, 4f, 0.24f, mio: true),
            CounterCloseUp("b6_kanemochi", "Kanemochi", "Idle", 0.6f, -2f, 0f, snooty: true),
            ToBench(),
            BenchIntro(),
            Screws(),
            Cover(),
            Diagnose(),
            Clean(),
            Exploded(),
            Rebuild(),
            PowerTest(),
            Receipt(),
            RenHappy(),
            Mikan(),
            Screens(),
            Batteries(),
            Ramen(),
            Upgrades(),
            PetCat(),
            Finale(),
            Title(),
            Stinger(),
        };

        // ==== A: cold open ======================================================================================

        /// <summary>a1: Denchi napping on the waiting chair, backlit by the low sun. Slow push.</summary>
        private static Shot CatNap()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "a1_cat_nap",
                Scene = ShopScene,
                Frames = Bars(2),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    ShopRig.Place(shop.Mio, new Vector3(1.25f, 0f, 3.75f), 200f); // out of the way of the close-up
                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    float u = t / (2f * Bar);
                    shop.Life(t);
                    shop.Cat(-1f, 1f, t);
                    var from = new CamKey(new Vector3(1.42f, 0.80f, 1.33f), new Vector3(0.58f, 0.63f, 1.68f), 31f);
                    var to = new CamKey(new Vector3(1.19f, 0.73f, 1.46f), new Vector3(0.58f, 0.625f, 1.68f), 28f);
                    CamKey.Lerp(from, to, Ease.InOutSine(u)).Apply(s);
                    s.Sway(0.12f, 0.3f, 1f);
                    s.Focus(new Vector3(0.70f, 0.64f, 1.67f), 1.8f);
                    s.Track("cat", shop.CatHead);
                },
            };
        }

        /// <summary>a2: from the PHONE FIXER plaque out to the whole diorama, landing on the drop.</summary>
        private static Shot PlaqueReveal()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "a2_plaque_reveal",
                Scene = ShopScene,
                Frames = Bars(2),
                Subframes = 3,
                Setup = s => shop = new ShopRig(s),
                Pose = (s, t) =>
                {
                    float u = t / (2f * Bar);
                    shop.Life(t + 4.8f);
                    var start = new CamKey(new Vector3(4.15f, 2.02f, 2.65f), new Vector3(4.15f, 2.14f, 4.45f), 24f);
                    var creepEnd = new CamKey(new Vector3(4.12f, 2.0f, 2.35f), new Vector3(4.15f, 2.12f, 4.45f), 25f);
                    var end = new CamKey(ShopRig.DioramaPosition, ShopRig.DioramaTarget, 20f);
                    CamKey creep = CamKey.Lerp(start, creepEnd, Ease.InOutSine(Ease.Window(u, 0f, 0.3f)));
                    float e = Ease.InOutQuint(Ease.Window(u, 0.2f, 1f));
                    Vector3 control = new(7.8f, 3.4f, -1.2f);
                    Vector3 p = Bezier(creep.Position, control, end.Position, e);
                    Vector3 target = Vector3.Lerp(creep.Target, end.Target, Ease.InOutCubic(Ease.Window(u, 0.2f, 0.97f)));
                    s.Aim(p, target, Mathf.Lerp(creep.Fov, end.Fov, e));
                    s.Cue("whoosh_big", 0.2f * 4.8f + 0.5f, 0.55f);
                },
            };
        }

        // ==== B: the shop and the neighbours ===================================================================

        /// <summary>b1: the diorama, a slow orbit; the door slides open and Madame K. walks in.</summary>
        private static Shot ShopOpens()
        {
            ShopRig shop = null;
            Vector3[] path =
            {
                new(-0.95f, 0.025f, 3.5f), new(-0.25f, 0.0f, 3.5f), new(0.55f, 0f, 3.45f), new(0.95f, 0f, 3.62f),
            };
            const float walkStart = 0.45f;
            const float speed = 0.52f;
            return new Shot
            {
                Name = "b1_shop_opens",
                Scene = ShopScene,
                Frames = Bars(2),
                Setup = s => shop = new ShopRig(s),
                Pose = (s, t) =>
                {
                    float u = t / (2f * Bar);
                    Vector3 pivot = ShopRig.DioramaTarget;
                    Vector3 offset = ShopRig.DioramaPosition - pivot;
                    float k = Ease.InOutSine(u);
                    Vector3 position = pivot + Quaternion.Euler(0f, Mathf.Lerp(0f, 6f, k), 0f) * offset * Mathf.Lerp(1f, 0.86f, k);
                    s.Aim(position, pivot + new Vector3(0f, 0.05f, 0f) * k, 20f);

                    shop.Door(Ease.Window(t, 0.15f, 0.7f) - Ease.Window(t, 4.0f, 4.6f));
                    float walked = ShopRig.WalkAlong(shop.Kanemochi, path, speed, t - walkStart, out bool arrived);
                    float walkTime = walked / speed;
                    if (t < walkStart)
                    {
                        s.Animate(shop.Kanemochi, "Idle", t + 1.3f);
                    }
                    else if (!arrived)
                    {
                        s.Animate(shop.Kanemochi, "Walk", walkTime);
                    }
                    else
                    {
                        float since = t - walkStart - walkTime;
                        s.Blend(shop.Kanemochi, "Walk", walkTime, "Idle", since, Ease.Smooth(since / 0.3f));
                    }

                    shop.Life(t, shop.Kanemochi);
                    s.Cue("door_slide", 0.15f, 0.7f);
                    s.Cue("chime", 0.25f, 0.8f);
                    s.Cue("door_slide", 4.0f, 0.5f, 0.9f);
                    for (float step = walkStart + 0.1f; step < walkStart + walkTime && step < 4.8f; step += 0.4f)
                    {
                        s.Cue(((int)((step - walkStart) / 0.4f)) % 2 == 0 ? "footstep" : "footstep2", step, 0.35f, 1f, -0.3f);
                    }

                    s.Track("kanemochi", shop.HeadTop(shop.Kanemochi));
                },
            };
        }

        /// <summary>
        /// Where customers stand for close-ups: in front of the clear end of the counter (the register and the
        /// PC sit further along), so the shopkeeper's-eye camera looks straight across the counter top.
        /// </summary>
        private static readonly Vector3 CloseUpSpot = new(1.68f, 0f, 2.85f);

        /// <summary>Shopkeeper's view across the counter: a little above the eyes, never lower than 1.28 m (so short
        /// customers are seen over the counter top), <paramref name="distance"/> metres away.</summary>
        private static void AcrossTheCounter(Stage s, Vector3 eyes, float distance, float fov, float sideDegrees, float lift = 0.16f)
        {
            Vector3 target = eyes + new Vector3(0f, -0.04f, 0f);
            Vector3 flat = Quaternion.Euler(0f, sideDegrees, 0f) * new Vector3(1f, 0f, -0.1f).normalized;
            float height = Mathf.Max(eyes.y + lift, 1.28f);
            Vector3 position = target + flat * distance;
            position.y = height;
            s.Aim(position, target, fov);
        }

        /// <summary>Only the star of a close-up stays in the shop; the others step out of frame.</summary>
        private static void ClearTheShop(ShopRig shop, GameObject star)
        {
            foreach (GameObject other in shop.Everyone)
            {
                if (other != star)
                {
                    other.SetActive(false);
                }
            }
        }

        /// <summary>b2..b6: the neighbours, one bar each, at the counter seen from the shopkeeper's side.</summary>
        /// <param name="box">The "apple box": how far the customer is raised (unseen behind the counter) so the face clears it.</param>
        private static Shot CounterCloseUp(string name, string who, string clip, float clipOffset, float sideDegrees, float box, bool mio = false, bool snooty = false)
        {
            ShopRig shop = null;
            GameObject star = null;
            return new Shot
            {
                Name = name,
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    star = s.Customer(who);
                    ClearTheShop(shop, star);
                    ShopRig.Place(star, CloseUpSpot + Vector3.up * box, 90f);
                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    float u = t / Bar;
                    if (mio)
                    {
                        // a sheepish head tilt, held: Idle into Confused, frozen near its peak
                        s.Blend(star, "Idle", t + 0.4f, "Confused", Mathf.Min(t, 0.75f) + 0.2f, Ease.Smooth(t / 0.3f));
                    }
                    else
                    {
                        s.Animate(star, clip, t + clipOffset);
                    }

                    if (snooty)
                    {
                        s.Nudge(star, "Head", new Vector3(-9f, 0f, 4f)); // chin up
                    }

                    Vector3 eyes = shop.Eyes(star);
                    Vector3 aim = eyes + Vector3.up * (snooty ? 0.09f : 0.02f); // Madame K.'s hair needs headroom
                    AcrossTheCounter(s, aim, Mathf.Lerp(1.36f, 1.2f, Ease.InOutSine(u)), snooty ? 33f : 30f, sideDegrees);
                    s.Sway(0.15f, 0.5f, sideDegrees);
                    s.Focus(eyes, 2.8f);
                    s.Track("head", shop.HeadTop(star));
                    s.Track("face", eyes);
                },
            };
        }

        /// <summary>b7: the workbench glows "you can pick it", its label pops, and we dive in.</summary>
        private static Shot ToBench()
        {
            ShopRig shop = null;
            List<Renderer> glow = null;
            RectTransform pill = null;
            Bounds benchBounds = default;
            return new Shot
            {
                Name = "b7_to_bench",
                Scene = ShopScene,
                Frames = Bars(1),
                Subframes = 4,
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    ShopStation station = shop.Station(PhoneFixerPaths.BenchSceneName);
                    var interactable = station.GetComponent<Interactable>();
                    glow = s.AddGlow(interactable, ReadyGlow);
                    benchBounds = interactable.GetWorldBounds();
                    var label = Object.FindFirstObjectByType<StationLabel>();
                    var so = new SerializedObject(label);
                    ((TMP_Text)so.FindProperty("nameLabel").objectReferenceValue).text = "Workbench";
                    ((TMP_Text)so.FindProperty("verbLabel").objectReferenceValue).text = "Repair";
                    pill = (RectTransform)so.FindProperty("pill").objectReferenceValue;
                    s.UiOnly(pill.name);
                },
                Pose = (s, t) =>
                {
                    shop.Life(t + 7f);
                    Vector3 pivot = ShopRig.DioramaTarget;
                    Vector3 offset = ShopRig.DioramaPosition - pivot;
                    var wide = new CamKey(pivot + Quaternion.Euler(0f, 1.5f, 0f) * offset * 0.84f, pivot + new Vector3(0f, 0.05f, 0f), 20f);
                    // Ends exactly where the bench scene's first shot starts (bench at Spot_Bench in the shop).
                    var close = new CamKey(new Vector3(4.15f, 1.35f, 3.09f), new Vector3(4.15f, 0.86f, 4.04f), 38f);
                    float e = Ease.InCubic(Ease.Window(t, 0.75f, Bar));
                    float drift = Ease.InOutSine(Ease.Window(t, 0f, 0.75f)) * 0.02f;
                    CamKey key = CamKey.Arc(CamKey.Lerp(wide, close, drift), close, (pivot + close.Target) * 0.5f, e);
                    key.Apply(s);

                    float on = Ease.Window(t, 0f, 0.12f);
                    float pulse = 0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 2f * 1.7f);
                    Stage.SetGlow(glow, ReadyGlow * (on * pulse * (1f - Ease.Window(t, 1.6f, 2.2f))));
                    float labelAlpha = Ease.Window(t, 0.05f, 0.2f) * (1f - Ease.Window(t, 0.9f, 1.15f));
                    s.UiAlpha(pill.name, labelAlpha);
                    pill.anchoredPosition = s.CanvasPoint(benchBounds.center + Vector3.up * benchBounds.extents.y) + new Vector2(0f, 18f + 10f * (1f - Ease.OutBack(Ease.Window(t, 0.05f, 0.3f))));
                    pill.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Ease.OutBack(Ease.Window(t, 0.05f, 0.3f)));
                    s.UiVisible = labelAlpha > 0.001f;

                    s.Cue("glow", 0.02f, 0.8f);
                    s.Cue("keycap", Beat, 0.7f);
                    s.Cue("whoosh_big", 1.25f, 0.9f);
                    s.Track("bench", benchBounds.center);
                },
            };
        }

        // ==== C: the repair ====================================================================================

        /// <summary>c1: the bench, Ren's Mochi Dango on the mat; the repair HUD slides in.</summary>
        private static Shot BenchIntro()
        {
            BenchRig bench = null;
            var rests = new Dictionary<string, Vector2>();
            string[] panels = { "JobTicket", "Objective", "Diagnosis" };
            return new Shot
            {
                Name = "c1_bench_intro",
                Scene = BenchScene,
                Frames = Bars(1),
                Subframes = 2,
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults("ChargingPort_Lint");
                    s.UiOnly(panels);
                    foreach (string p in panels)
                    {
                        rests[p] = s.UiElement(p).anchoredPosition;
                    }
                },
                Pose = (s, t) =>
                {
                    float e = Ease.OutCubic(t / Bar);
                    var start = new CamKey(new Vector3(0f, 1.35f, -0.95f), new Vector3(0f, 0.86f, 0f), 38f);
                    var end = new CamKey(new Vector3(0.015f, 1.03f, -0.37f), new Vector3(0f, 0.812f, -0.025f), 32f);
                    CamKey.Lerp(start, end, e).Apply(s);
                    s.Sway(0.08f);
                    s.Focus(new Vector3(0f, 0.812f, -0.03f), 4.5f);
                    for (int i = 0; i < panels.Length; i++)
                    {
                        float a = Ease.OutCubic(Ease.Window(t, 0.55f + i * 0.15f, 0.85f + i * 0.15f));
                        s.UiAlpha(panels[i], a);
                        RectTransform rect = s.UiElement(panels[i]);
                        rect.anchoredPosition = rests[panels[i]] + new Vector2(0f, 24f * (1f - a));
                        s.Cue("swoosh_ui", 0.55f + i * 0.15f, 0.35f, 1f + i * 0.08f, i == 2 ? 0.6f : -0.6f);
                    }

                    s.Track("phone", new Vector3(0f, 0.812f, -0.03f));
                },
            };
        }

        /// <summary>c2: four screws, two beats each, ratchet ticks on the 16ths, popping into the screw map.</summary>
        private static Shot Screws()
        {
            BenchRig bench = null;
            Transform driver = null;
            RectTransform ring = null, tooltip = null;
            Image ringFill = null;
            const float per = 2f * Beat;
            const float turns = 1.25f;
            const float rise = 0.0025f;

            float Loose(float local) => Ease.InOutSine(Ease.Window(local, 0.06f, 0.84f));

            return new Shot
            {
                Name = "c2_screws",
                Scene = BenchScene,
                Frames = Bars(2),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults("ChargingPort_Lint");
                    bench.Flip(1f);
                    foreach (string id in BenchRig.Screws)
                    {
                        _ = bench.Landing(id); // tray slots 1-4 in order
                    }

                    driver = bench.Tool("Screwdriver", 0.42f);
                    s.UiOnly("ActionProgress", "HoverTooltip");
                    var progress = Object.FindFirstObjectByType<ActionProgressView>();
                    ring = (RectTransform)progress.transform;
                    ringFill = Reflect.Field<Image>(progress, "ring");
                    var tip = Object.FindFirstObjectByType<HoverTooltip>();
                    tooltip = (RectTransform)tip.transform;
                    var so = new SerializedObject(tip);
                    ((TMP_Text)so.FindProperty("label").objectReferenceValue).text = "P2 Mochi Screw";
                    var hint = (TMP_Text)so.FindProperty("hintLabel").objectReferenceValue;
                    hint.text = "Hold · Unscrew";
                    hint.color = so.FindProperty("readyColor").colorValue;
                    s.ShadowDistance(1.5f);
                },
                Pose = (s, t) =>
                {
                    int i = Mathf.Min(3, Mathf.FloorToInt(t / per));
                    float local = t - i * per;
                    for (int j = 0; j < 4; j++)
                    {
                        string id = BenchRig.Screws[j];
                        if (j < i)
                        {
                            bench.Place(id, bench.Landing(id));
                        }
                        else if (j > i)
                        {
                            bench.Installed(id);
                        }
                    }

                    string current = BenchRig.Screws[i];
                    float loose = Loose(local);
                    bench.Unscrew(current, Mathf.Min(1f, loose), turns, rise);
                    Vector3 head = bench.ScrewHead(current);
                    if (local >= 0.9f)
                    {
                        // pop: from the raised pose into its tray slot, spinning
                        float f = Ease.Window(local, 0.9f, per);
                        Pose from = new(bench.T(current).position, bench.T(current).rotation);
                        Pose to = bench.Landing(current);
                        float e = Ease.InOutCubic(f);
                        Vector3 position = Arc3.Flight(from.position, to.position, Vector3.up, Vector3.up, 0.05f, e);
                        bench.T(current).SetPositionAndRotation(position, Quaternion.Slerp(from.rotation, to.rotation, e) * Quaternion.AngleAxis(540f * e, Vector3.up));
                    }

                    // screwdriver: seated while turning, hops to the next screw while this one flies
                    Vector3 tip = head;
                    float spin = -360f * turns * loose;
                    if (i == 0 && local < 0.06f)
                    {
                        tip = head + Vector3.up * (0.03f * (1f - Ease.OutCubic(local / 0.06f)));
                    }
                    else if (local >= 0.9f)
                    {
                        float h = Ease.InOutCubic(Ease.Window(local, 0.9f, per));
                        Vector3 next = i < 3 ? bench.ScrewHead(BenchRig.Screws[i + 1]) : head + new Vector3(0.02f, 0.06f, -0.02f);
                        Vector3 lifted = Vector3.Lerp(head, next, h) + Vector3.up * (0.012f * Mathf.Sin(Mathf.PI * h));
                        tip = lifted;
                        spin = -360f * turns;
                    }

                    bench.Hold(driver, tip, Vector3.up, spin);

                    // camera follows the work: from screw to screw, a little orbit over the two bars
                    Vector3 focus = head;
                    if (local >= 0.9f && i < 3)
                    {
                        focus = Vector3.Lerp(bench.InstalledWorld(current).position, bench.InstalledWorld(BenchRig.Screws[i + 1]).position,
                            Ease.InOutCubic(Ease.Window(local, 0.9f, per)));
                    }
                    else
                    {
                        focus = bench.InstalledWorld(current).position;
                    }

                    var center = new Vector3(0f, 0.818f, -0.03f);
                    Vector3 look = Vector3.Lerp(center, focus, 0.72f);
                    float yaw = Mathf.Lerp(-14f, 10f, Ease.InOutSine(t / (2f * Bar)));
                    CamKey.Orbit(look, yaw, 38f, 0.165f, 32f).Apply(s);
                    s.Sway(0.08f, 0.6f, 3f);
                    s.Focus(head, 6.3f);

                    // the game's progress ring and tooltip on the screw
                    Vector2 at = s.CanvasPoint(head);
                    bool turning = local > 0.04f && local < 0.88f;
                    ringFill.fillAmount = loose;
                    ring.anchoredPosition = at + new Vector2(-34f, 30f);
                    tooltip.anchoredPosition = at + new Vector2(26f, -26f);
                    s.UiAlpha("ActionProgress", turning ? 1f : 0f);
                    s.UiAlpha("HoverTooltip", turning ? Ease.Window(local, 0.04f, 0.12f) : 0f);
                    s.UiVisible = turning;

                    // sounds on the 16th-note grid
                    float start = i * per;
                    s.Cue("screw_turn", start + 0.06f, 0.35f, 1f + 0.03f * i);
                    float prevLoose = Loose(local - s.FrameSeconds);
                    if (local > 0.02f && Mathf.FloorToInt(loose * turns * 4f + 1e-4f) > Mathf.FloorToInt(prevLoose * turns * 4f + 1e-4f))
                    {
                        s.CueAt("tick", t, head, 0.9f, 0.92f + 0.05f * ((Mathf.FloorToInt(loose * turns * 4f) + i) % 3));
                    }

                    s.CueAt("pop", start + 0.9f, head, 1f, 1f + 0.04f * i);
                    s.CueAt("whoosh", start + 0.92f, head, 0.5f, 1.1f);
                    s.Cue("tink", start + per - 0.02f, 0.9f, 0.96f + 0.05f * i, -0.5f);
                    s.Track("screw", head);
                },
            };
        }

        /// <summary>c3: spudger in the seam, a little creak, the back cover pops and flips onto the mat.</summary>
        private static Shot Cover()
        {
            BenchRig bench = null;
            Transform spudger = null;
            return new Shot
            {
                Name = "c3_cover",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults("ChargingPort_Lint");
                    bench.Flip(1f);
                    foreach (string id in BenchRig.Screws)
                    {
                        bench.Place(id, bench.Landing(id));
                    }

                    _ = bench.Landing("BackCover");
                    spudger = bench.Tool("Spudger", 0.55f);
                    s.ShadowDistance(1.5f);
                },
                Pose = (s, t) =>
                {
                    Bounds cover = new(bench.InstalledWorld("BackCover").position, Vector3.zero);
                    Pose installed = bench.InstalledWorld("BackCover");
                    // the bottom edge of the face-down phone (charging port end) is at -Z
                    var seam = new Vector3(0f, installed.position.y + 0.004f, -0.107f);
                    float pry = Ease.Hump(Ease.Window(t, 0.12f, 0.6f));
                    float leave = Ease.InOutCubic(Ease.Window(t, 0.62f, 1.0f));
                    Vector3 axis = Quaternion.AngleAxis(-38f + 14f * pry, Vector3.right) * Vector3.up; // tilted toward the camera
                    Vector3 tip = seam + new Vector3(0.006f * Mathf.Sin(t * 30f) * pry, 0f, 0f) + new Vector3(0.02f, 0.05f, -0.05f) * leave;
                    tip += new Vector3(0f, 0.03f, -0.03f) * (1f - Ease.OutCubic(Ease.Window(t, 0f, 0.15f)));
                    bench.Hold(spudger, tip, axis);

                    if (t < Beat)
                    {
                        // the cover's bottom edge lifts a little as it is pried (hinge on the top edge)
                        bench.Installed("BackCover");
                        Transform c = bench.T("BackCover");
                        var hinge = new Vector3(0f, installed.position.y, 0.046f);
                        float angle = 5f * pry;
                        c.RotateAround(hinge, Vector3.right, -angle);
                    }
                    else
                    {
                        float f = Ease.Window(t, Beat, 2f * Beat);
                        bench.FlyOut("BackCover", f, 0f, 0.1f);
                    }

                    var from = new CamKey(new Vector3(0.07f, 0.935f, -0.27f), new Vector3(0.0f, 0.812f, -0.04f), 33f);
                    var to = new CamKey(new Vector3(0.05f, 0.975f, -0.215f), new Vector3(0.06f, 0.812f, 0.0f), 37f);
                    CamKey.Lerp(from, to, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.08f, 0.6f, 5f);
                    s.Focus(new Vector3(0f, 0.815f, -0.04f), 6.3f);
                    s.Cue("pry", 0.12f, 0.9f);
                    s.Cue("pop", Beat, 1f, 0.85f);
                    s.Cue("whoosh", Beat + 0.02f, 0.7f, 0.9f, 0.3f);
                    s.Cue("thunk", 2f * Beat, 0.9f, 1f, 0.6f);
                    s.Track("cover", bench.T("BackCover").position);
                    _ = cover;
                },
            };
        }

        /// <summary>Phone face down, screws in the tray, back cover on the mat.</summary>
        private static BenchRig OpenedBench(Stage s, params string[] faults)
        {
            var bench = new BenchRig(s);
            bench.Faults(faults);
            bench.Flip(1f);
            foreach (string id in BenchRig.Screws)
            {
                bench.Place(id, bench.Landing(id));
            }

            bench.Place("BackCover", bench.Landing("BackCover"));
            return bench;
        }

        /// <summary>c4: looking for the fault. The port glows coral; the part card tells you about pocket lint.</summary>
        private static Shot Diagnose()
        {
            BenchRig bench = null;
            List<Renderer> glow = null;
            RectTransform card = null;
            Vector2 cardRest = default;
            return new Shot
            {
                Name = "c4_diagnose",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = OpenedBench(s, "ChargingPort_Lint");
                    glow = s.AddGlow(bench.P("ChargingPort").GetComponent<Interactable>(), BlockedGlow);
                    var panel = Object.FindFirstObjectByType<SelectionInfoPanel>();
                    s.UiOnly("Diagnosis", panel.name);
                    Reflect.Call(panel, "Show", bench.P("ChargingPort").GetComponent<Interactable>());
                    card = (RectTransform)panel.transform;
                    cardRest = card.anchoredPosition;
                    s.ShadowDistance(1.5f);
                },
                Pose = (s, t) =>
                {
                    var pivot = new Vector3(0f, 0.815f, -0.045f);
                    CamKey a = CamKey.Orbit(pivot, -16f, 64f, 0.27f, 33f);
                    CamKey b = CamKey.Orbit(pivot + new Vector3(0f, 0f, -0.03f), 8f, 56f, 0.205f, 32f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.06f);
                    Vector3 port = bench.T("ChargingPort").position;
                    s.Focus(port, 7f);
                    float pulse = 0.65f + 0.35f * Mathf.Sin((t - 0.3f) * Mathf.PI * 2f * 1.66f);
                    Stage.SetGlow(glow, BlockedGlow * (Ease.Window(t, 0.25f, 0.4f) * pulse));
                    float c = Ease.OutBack(Ease.Window(t, Beat, Beat + 0.3f));
                    s.UiAlpha(card.name, Ease.Window(t, Beat, Beat + 0.12f));
                    card.anchoredPosition = cardRest + new Vector2(30f * (1f - c), 0f);
                    s.UiAlpha("Diagnosis", 1f);
                    s.Cue("glow", 0.25f, 0.8f, 0.9f);
                    s.Cue("swoosh_ui", Beat, 0.5f, 1f, 0.6f);
                    s.Track("port", port);
                },
            };
        }

        /// <summary>c5: brush the lint out of the port, four sweeps on the 8ths, then a clean sparkle.</summary>
        private static Shot Clean()
        {
            BenchRig bench = null;
            Transform brush = null, lint = null;
            Vector3 lintRest = default;
            List<Renderer> glow = null;
            return new Shot
            {
                Name = "c5_clean",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = OpenedBench(s, "ChargingPort_Lint");
                    brush = bench.Tool("Brush", 0.5f);
                    lint = bench.Fault("ChargingPort_Lint");
                    lintRest = lint.localScale;
                    glow = s.AddGlow(bench.P("ChargingPort").GetComponent<Interactable>(), FixedGlow);
                    s.ShadowDistance(1.2f);
                },
                Pose = (s, t) =>
                {
                    Vector3 port = bench.T("ChargingPort").position;
                    Vector3 opening = port + new Vector3(0f, 0f, -0.0065f); // the port's mouth on the bottom edge
                    float sweep = t >= 0.3f && t < 1.5f ? Mathf.Sin((t - 0.3f) * Mathf.PI * 2f / 0.6f) : 0f;
                    float away = Ease.InOutCubic(Ease.Window(t, 1.5f, 1.85f));
                    float come = 1f - Ease.OutCubic(Ease.Window(t, 0f, 0.3f));
                    Vector3 tip = opening + new Vector3(0.0055f * sweep, 0.0012f, -0.003f) + new Vector3(0.03f, 0.04f, -0.02f) * (away + come);
                    Vector3 axis = Quaternion.AngleAxis(sweep * 10f, Vector3.forward) * new Vector3(0.55f, 0.7f, -0.45f).normalized;
                    bench.Hold(brush, tip, axis);

                    float cleaned = Ease.Smooth(Ease.Window(t, 0.35f, 1.5f));
                    lint.gameObject.SetActive(cleaned < 0.999f);
                    lint.localScale = lintRest * Mathf.Max(0.001f, 1f - cleaned);

                    float shine = Ease.Window(t, 1.62f, 1.72f) * (1f - Ease.Window(t, 1.9f, 2.4f));
                    Stage.SetGlow(glow, FixedGlow * shine);

                    CamKey a = CamKey.Orbit(opening + new Vector3(0f, 0.002f, 0f), -14f, 17f, 0.082f, 30f);
                    CamKey b = CamKey.Orbit(opening + new Vector3(0f, 0.002f, 0f), -5f, 13f, 0.07f, 29f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.06f, 0.7f, 2f);
                    s.Focus(opening, 8f);
                    for (int k = 0; k < 4; k++)
                    {
                        s.CueAt("brush", 0.3f + k * 0.3f, port, 0.9f, 0.95f + 0.06f * (k % 2));
                    }

                    s.CueAt("puff", 0.55f, port, 0.6f);
                    s.CueAt("puff", 1.15f, port, 0.5f, 1.1f);
                    s.CueAt("sparkle", 1.62f, port, 1f);
                    s.Track("port", opening);
                },
            };
        }

        private static readonly (string id, float offset)[] Layers =
        {
            ("Screen", -0.03f), ("Frame", 0f), ("DisplayConnector", 0.019f), ("Logicboard", 0.027f), ("RearCamera", 0.046f),
            ("Battery", 0.036f), ("BatteryConnector", 0.056f), ("Speaker", 0.042f), ("ChargingPort", 0.034f), ("BackCover", 0.078f),
            ("Screw_01", 0.104f), ("Screw_02", 0.104f), ("Screw_03", 0.104f), ("Screw_04", 0.104f),
        };

        private const float ExplodeLift = 0.07f;

        /// <summary>Exploded view at amount <paramref name="k"/> (0 = assembled). Parts lift along the phone's back normal.</summary>
        private static void Explode(BenchRig bench, float k, float yaw, Func<string, float> perPart = null)
        {
            bench.Reassemble();
            bench.Pivot.SetPositionAndRotation(bench.PivotRestPosition + Vector3.up * ExplodeLift,
                Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(180f, Vector3.forward) * bench.PivotRestRotation);
            Vector3 back = -bench.Device.transform.up; // the back faces up when the phone lies on its screen
            Vector3 center = bench.Device.transform.position;
            foreach ((string id, float offset) in Layers)
            {
                float amount = perPart?.Invoke(id) ?? k;
                Transform part = bench.T(id);
                Vector3 lateral = Vector3.ProjectOnPlane(part.position - center, back) * (id.StartsWith("Screw") ? 0.25f : 0.08f);
                part.position += (back * offset + lateral) * amount;
            }
        }

        /// <summary>c6: the exploded view, everything floats apart in layers while we orbit.</summary>
        private static Shot Exploded()
        {
            BenchRig bench = null;
            return new Shot
            {
                Name = "c6_exploded",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults();
                    s.ShadowDistance(1.5f);
                },
                Pose = (s, t) =>
                {
                    float u = t / Bar;
                    float lift = Ease.OutCubic(Ease.Window(t, 0f, 0.35f));
                    float k = Ease.OutBack(Ease.Window(t, 0.15f, 1.0f), 1.1f);
                    float yaw = Mathf.Lerp(0f, 24f, Ease.InOutSine(u));
                    Explode(bench, k, yaw);
                    bench.Pivot.position = bench.PivotRestPosition + Vector3.up * (ExplodeLift * lift);
                    Vector3 center = bench.PivotRestPosition + Vector3.up * (ExplodeLift * lift + 0.02f);
                    CamKey a = CamKey.Orbit(center + Vector3.up * 0.015f, 28f, 22f, 0.40f, 30f);
                    CamKey b = CamKey.Orbit(center + Vector3.up * 0.02f, 48f, 28f, 0.36f, 30f);
                    CamKey.Arc(a, b, center, Ease.InOutSine(u)).Apply(s);
                    s.Focus(center, 8f);
                    s.Cue("whoosh", 0.15f, 0.8f, 0.8f);
                    s.Cue("glow", 0.2f, 0.5f, 1.2f);
                    s.Track("phone", center);
                },
            };
        }

        /// <summary>c7: and back together, a part on every 8th, screws on the 16ths, then it flips face up.</summary>
        private static Shot Rebuild()
        {
            BenchRig bench = null;
            var snaps = new Dictionary<string, float>
            {
                ["Screen"] = 0.00f, ["DisplayConnector"] = 0.15f, ["Logicboard"] = 0.30f, ["RearCamera"] = 0.45f, ["Battery"] = 0.60f,
                ["BatteryConnector"] = 0.75f, ["Speaker"] = 0.90f, ["ChargingPort"] = 1.05f, ["BackCover"] = 1.20f,
                ["Screw_01"] = 1.35f, ["Screw_02"] = 1.42f, ["Screw_03"] = 1.50f, ["Screw_04"] = 1.57f, ["Frame"] = -1f,
            };
            return new Shot
            {
                Name = "c7_rebuild",
                Scene = BenchScene,
                Frames = Bars(1),
                Subframes = 3,
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults();
                    s.ShadowDistance(1.5f);
                },
                Pose = (s, t) =>
                {
                    float Amount(string id) => snaps[id] < 0f ? 0f : 1f - Ease.InCubic(Ease.Window(t, snaps[id], snaps[id] + 0.13f));
                    float flip = Ease.InOutCubic(Ease.Window(t, 1.75f, 2.32f));
                    if (flip <= 0f)
                    {
                        Explode(bench, 1f, 24f, Amount);
                        foreach (string id in BenchRig.Screws)
                        {
                            bench.T(id).Rotate(Vector3.up, 720f * Amount(id), Space.Self);
                        }
                    }
                    else
                    {
                        bench.Reassemble();
                        float lift = ExplodeLift * (1f - flip) + 0.045f * Ease.Hump(flip);
                        bench.Pivot.SetPositionAndRotation(bench.PivotRestPosition + Vector3.up * lift,
                            Quaternion.Euler(0f, 24f * (1f - flip), 0f) * Quaternion.AngleAxis(180f * (1f - flip), Vector3.forward) * bench.PivotRestRotation);
                    }

                    Vector3 center = bench.PivotRestPosition + Vector3.up * 0.02f;
                    float u = Ease.InOutSine(t / Bar);
                    CamKey a = CamKey.Orbit(center + Vector3.up * 0.09f, 48f, 28f, 0.36f, 30f);
                    CamKey b = CamKey.Orbit(center, 0f, 52f, 0.30f, 32f);
                    CamKey.Arc(a, b, center, u).Apply(s);
                    s.Focus(center, 8f);
                    foreach (KeyValuePair<string, float> snap in snaps)
                    {
                        if (snap.Value >= 0f)
                        {
                            bool screw = snap.Key.StartsWith("Screw", StringComparison.Ordinal);
                            s.Cue(screw ? "tick" : "click", snap.Value + 0.13f, screw ? 0.8f : 0.75f, screw ? 1.1f : 0.9f + 0.05f * (snap.Value * 7f % 3f));
                        }
                    }

                    s.Cue("whoosh", 1.72f, 0.6f, 1.2f);
                    s.Cue("thunk", 2.32f, 0.9f, 1.05f);
                    s.Track("phone", bench.Device.transform.position);
                },
            };
        }

        // ==== D: fixed! and the montage ========================================================================

        /// <summary>d0: the break. Silence, the Power test key, the screen wakes up with the Mochi jingle.</summary>
        private static Shot PowerTest()
        {
            BenchRig bench = null;
            Renderer screen = null;
            RectTransform cap = null;
            Vector2 capRest = default;
            return new Shot
            {
                Name = "d0_power",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults();
                    screen = bench.CreateScreen("screen_boot.png", 1.0f);
                    BenchRig.SetScreen(screen, null, 0f);
                    s.UiOnly("PowerTest");
                    var press = s.UiElement("PowerTest").GetComponentInChildren<KeycapPress>(true) ?? s.UiElement("PowerTest").GetComponent<KeycapPress>();
                    cap = press != null ? Reflect.Field<RectTransform>(press, "content") : null;
                    if (cap != null)
                    {
                        capRest = cap.anchoredPosition;
                    }

                    s.ShadowDistance(1.2f);
                },
                Pose = (s, t) =>
                {
                    var a = new CamKey(new Vector3(0.0f, 0.995f, -0.30f), new Vector3(0f, 0.812f, -0.035f), 30f);
                    var b = new CamKey(new Vector3(0.0f, 0.955f, -0.245f), new Vector3(0f, 0.812f, -0.032f), 29f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.05f);
                    s.Focus(new Vector3(0f, 0.817f, -0.03f), 5.6f);
                    bool pressed = t >= Beat && t < Beat + 0.16f;
                    if (cap != null)
                    {
                        cap.anchoredPosition = capRest + new Vector2(0f, pressed ? -5f : 0f);
                    }

                    s.UiAlpha("PowerTest", 1f - Ease.Window(t, 1.3f, 1.6f));
                    float on = Ease.OutCubic(Ease.Window(t, 0.78f, 1.0f));
                    float flash = 1f + 0.35f * Ease.Hump(Ease.Window(t, 0.78f, 1.1f));
                    BenchRig.SetScreen(screen, null, on * flash * 1.15f);
                    s.Cue("keycap", Beat, 1f);
                    s.Cue("boot", 0.76f, 0.9f);
                    s.Track("phone", new Vector3(0f, 0.817f, -0.03f));
                },
            };
        }

        /// <summary>d1: "Fixed!" The receipt pops over the bench and the REPAIRED stamp slams down.</summary>
        private static Shot Receipt()
        {
            BenchRig bench = null;
            RectTransform card = null, stamp = null;
            Quaternion stampRest = Quaternion.identity;
            return new Shot
            {
                Name = "d1_fixed",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults();
                    Renderer screen = bench.CreateScreen("screen_happy.png", 1.0f);
                    BenchRig.SetScreen(screen, null, 1.1f);
                    s.UiOnly("Receipt");
                    card = (RectTransform)EditorAssetUtil.FindDeep(s.UiElement("Receipt"), "Card");
                    stamp = s.UiElement("Receipt").GetComponentsInChildren<RectTransform>(true)
                        .First(r => r.name.Contains("REPAIRED", StringComparison.Ordinal));
                    stampRest = stamp.localRotation;
                    s.ShadowDistance(1.2f);
                },
                Pose = (s, t) =>
                {
                    var a = new CamKey(new Vector3(0.0f, 1.13f, -0.17f), new Vector3(0f, 0.81f, -0.015f), 36f);
                    var b = new CamKey(new Vector3(0.0f, 1.09f, -0.145f), new Vector3(0f, 0.81f, -0.02f), 35f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Focus(new Vector3(0f, 0.815f, -0.03f), 8f);
                    float pop = Ease.OutBack(Ease.Window(t, 0f, 0.28f), 2f);
                    card.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, pop);
                    s.UiAlpha("Receipt", Ease.Window(t, 0f, 0.1f));
                    float slam = Ease.InCubic(Ease.Window(t, 0.5f, 0.64f));
                    float shake = t > 0.64f ? Mathf.Sin((t - 0.64f) * 60f) * Mathf.Exp(-(t - 0.64f) * 14f) : 0f;
                    stamp.localScale = Vector3.one * (Mathf.Lerp(2.6f, 1f, slam) + 0.06f * shake);
                    stamp.localRotation = stampRest * Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 0f, slam) + 2f * shake);
                    stamp.GetComponentsInChildren<Graphic>(true).ToList().ForEach(g => g.enabled = t >= 0.5f);
                    s.Cue("swoosh_ui", 0f, 0.8f);
                    s.Cue("stamp", 0.64f, 1f);
                    s.Cue("coin", 1.2f, 0.9f);
                    s.Track("stamp", new Vector3(0f, 0.815f, -0.03f));
                },
            };
        }

        /// <summary>d2: Ren gets his phone back. Two little jumps.</summary>
        private static Shot RenHappy()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "d2_ren_happy",
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    ClearTheShop(shop, shop.Ren);
                    ShopRig.Place(shop.Ren, CloseUpSpot + Vector3.up * 0.06f, 90f);
                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    float c = t < 1.25f ? t - 0.02f : t - 1.25f;
                    s.Animate(shop.Ren, "Celebrate", Mathf.Max(0f, c));
                    var head = new Vector3(CloseUpSpot.x + 0.15f, 1.33f, CloseUpSpot.z);
                    AcrossTheCounter(s, head, Mathf.Lerp(1.58f, 1.48f, Ease.InOutSine(t / Bar)), 42f, -3f, 0.02f);
                    s.Sway(0.12f);
                    s.Focus(head, 3.2f);
                    s.Cue("jump", 0.4f, 0.6f, 1.05f);
                    s.Cue("jump", 1.63f, 0.6f, 1.15f);
                    s.Track("head", shop.HeadTop(shop.Ren));
                },
            };
        }

        /// <summary>d3: Mrs. Tanabe pays in mikan. Three oranges plop onto the counter.</summary>
        private static Shot Mikan()
        {
            ShopRig shop = null;
            var mikan = new List<(Transform t, Vector3 rest, float drop)>();
            return new Shot
            {
                Name = "d3_mikan",
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    Material peel = s.Lit("#F29A38", 0.42f);
                    Material leaf = s.Lit("#6E9F55", 0.3f);
                    Material stem = s.Lit("#5E4A2E", 0.2f);
                    ClearTheShop(shop, shop.Fumi);
                    ShopRig.Place(shop.Fumi, CloseUpSpot + Vector3.up * 0.1f, 90f);
                    (Vector3 p, float d)[] spots =
                    {
                        (new Vector3(2.45f, 0.98f + 0.03f, 2.78f), 0.3f),
                        (new Vector3(2.49f, 0.98f + 0.03f, 2.88f), 0.6f),
                        (new Vector3(2.47f, 0.98f + 0.03f + 0.05f, 2.83f), 0.9f),
                    };
                    int n = 0;
                    foreach ((Vector3 p, float d) in spots)
                    {
                        GameObject m = s.Primitive(PrimitiveType.Sphere, $"Mikan_{n++}", peel);
                        m.transform.localScale = new Vector3(0.072f, 0.06f, 0.072f);
                        GameObject l = s.Primitive(PrimitiveType.Sphere, "Leaf", leaf, m.transform);
                        l.transform.localPosition = new Vector3(0.12f, 0.5f, 0f);
                        l.transform.localScale = new Vector3(0.42f, 0.08f, 0.22f);
                        l.transform.localRotation = Quaternion.Euler(0f, 30f, 15f);
                        GameObject st = s.Primitive(PrimitiveType.Cylinder, "Stem", stem, m.transform);
                        st.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                        st.transform.localScale = new Vector3(0.08f, 0.06f, 0.08f);
                        mikan.Add((m.transform, p, d));
                    }

                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    if (t < 1.2f)
                    {
                        s.Animate(shop.Fumi, "Talk", t + 0.2f);
                    }
                    else
                    {
                        s.Blend(shop.Fumi, "Talk", t + 0.2f, "Celebrate", t - 1.2f, Ease.Smooth((t - 1.2f) / 0.15f));
                    }

                    foreach ((Transform m, Vector3 rest, float drop) in mikan)
                    {
                        float f = Ease.Window(t, drop, drop + 0.4f);
                        float y = f <= 0f ? 1f : Bounce(f);
                        m.gameObject.SetActive(t >= drop - 0.12f);
                        m.position = rest + Vector3.up * (0.32f * y);
                        m.rotation = Quaternion.Euler(10f * y, 40f * drop + 60f * y, 0f);
                        s.Cue("thunk", drop + 0.18f, 0.55f, 1.35f + drop * 0.2f, 0.2f);
                    }

                    var a = new CamKey(new Vector3(3.12f, 1.34f, 2.58f), new Vector3(2.2f, 1.02f, 2.86f), 36f);
                    var b = new CamKey(new Vector3(3.04f, 1.32f, 2.62f), new Vector3(2.2f, 1.02f, 2.86f), 34f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.1f);
                    s.Focus(new Vector3(2.35f, 1.04f, 2.85f), 4f);
                    s.Cue("heart", 1.25f, 0.6f);
                    s.Track("head", shop.HeadTop(shop.Fumi));
                    s.Track("mikan", mikan[2].rest);
                },
            };

            // 1 -> 0 with two small bounces (height fraction above the rest position)
            static float Bounce(float f)
            {
                if (f < 0.45f)
                {
                    float k = f / 0.45f;
                    return 1f - k * k;
                }

                if (f < 0.75f)
                {
                    float k = (f - 0.6f) / 0.15f;
                    return 0.12f * (1f - k * k);
                }

                float q = (f - 0.875f) / 0.125f;
                return Mathf.Max(0f, 0.03f * (1f - q * q));
            }
        }

        /// <summary>d4: "Screens." A cracked display comes off with the suction cup.</summary>
        private static Shot Screens()
        {
            BenchRig bench = null;
            Transform cup = null;
            return new Shot
            {
                Name = "d4_screens",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = new BenchRig(s);
                    bench.Faults("Screen_Crack");
                    cup = bench.Tool("SuctionCup", 0.9f);
                    s.ShadowDistance(1.2f);
                },
                Pose = (s, t) =>
                {
                    bench.Installed("Screen");
                    float lift = Ease.OutBack(Ease.Window(t, Beat, Beat + 0.45f), 1.4f);
                    float wobble = Mathf.Sin((t - Beat) * 18f) * Mathf.Exp(-(t - Beat) * 5f) * (t > Beat ? 1f : 0f);
                    Transform screen = bench.T("Screen");
                    var hinge = new Vector3(0f, 0.817f, 0.046f);
                    screen.RotateAround(hinge, Vector3.right, -9f * lift - 2f * wobble);
                    screen.position += Vector3.up * (0.022f * lift);
                    Bounds b = bench.BoundsOf("Screen");
                    Vector3 surface = screen.TransformPoint(new Vector3(0f, 0.0025f, -0.02f));
                    float landing = 1f - Ease.OutCubic(Ease.Window(t, 0f, 0.35f));
                    bench.Hold(cup, surface + Vector3.up * (0.04f * landing), screen.up);
                    var a = new CamKey(new Vector3(0.21f, 0.885f, -0.235f), new Vector3(0f, 0.826f, -0.03f), 30f);
                    var c = new CamKey(new Vector3(0.175f, 0.893f, -0.215f), new Vector3(0f, 0.832f, -0.025f), 29f);
                    CamKey.Lerp(a, c, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.08f);
                    s.Focus(b.center, 5.6f);
                    s.Cue("click", 0.3f, 0.6f, 0.7f);
                    s.Cue("pop", Beat, 1f, 0.62f);
                    s.Track("screen", b.center);
                },
            };
        }

        /// <summary>d5: "Batteries." The puffy one comes out, wobbling, with its warning sticker.</summary>
        private static Shot Batteries()
        {
            BenchRig bench = null;
            Vector3 batteryScale = Vector3.one;
            return new Shot
            {
                Name = "d5_batteries",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = OpenedBench(s, "Battery_Bulge", "Battery_Warning");
                    batteryScale = bench.T("Battery").localScale;
                    s.ShadowDistance(1.2f);
                },
                Pose = (s, t) =>
                {
                    PhonePart connector = bench.P("BatteryConnector");
                    float open = Ease.OutBack(Ease.Window(t, 0.2f, 0.42f));
                    bench.Installed("BatteryConnector");
                    connector.transform.localRotation = connector.InstalledLocalRotation *
                                                        Quaternion.AngleAxis(connector.Definition.ReleaseAngle * open, connector.Definition.ReleaseAxis);
                    bench.Installed("Battery");
                    float lift = Ease.OutBack(Ease.Window(t, Beat, Beat + 0.5f), 1.6f);
                    Transform battery = bench.T("Battery");
                    battery.position += Vector3.up * (0.032f * lift);
                    battery.Rotate(Vector3.right, 7f * lift, Space.World);
                    float jiggle = t > Beat ? Mathf.Sin((t - Beat) * 16f) * Mathf.Exp(-(t - Beat) * 3.2f) : 0f;
                    battery.localScale = Vector3.Scale(batteryScale, new Vector3(1f + 0.05f * jiggle, 1f - 0.07f * jiggle, 1f + 0.05f * jiggle));
                    Vector3 c = bench.BoundsOf("Battery").center;
                    var a = new CamKey(new Vector3(0.0f, 0.955f, -0.245f), new Vector3(0f, 0.825f, -0.05f), 31f);
                    var b = new CamKey(new Vector3(0.03f, 0.95f, -0.222f), new Vector3(0f, 0.83f, -0.05f), 31f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.08f);
                    s.Focus(c, 6.3f);
                    s.Cue("click", 0.22f, 0.8f, 1.2f);
                    s.Cue("pop", Beat, 0.9f, 0.8f);
                    s.Cue("jump", Beat + 0.05f, 0.35f, 0.7f);
                    s.Track("battery", c);
                },
            };
        }

        /// <summary>d6: "Ramen damage." Corrosion on the board, and one noodle.</summary>
        private static Shot Ramen()
        {
            BenchRig bench = null;
            return new Shot
            {
                Name = "d6_ramen",
                Scene = BenchScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    bench = OpenedBench(s, "Logicboard_Corrosion");
                    Bounds board = bench.BoundsOf("Logicboard");
                    float top = board.max.y;
                    Material noodleMaterial = s.Lit("#F8E2A6", 0.78f);
                    noodleMaterial.EnableKeyword("_EMISSION");
                    noodleMaterial.SetColor("_EmissionColor", new Color(0.16f, 0.13f, 0.06f));
                    for (int k = 0; k < 2; k++)
                    {
                        var noodle = new GameObject($"Trailer_Noodle_{k}") { hideFlags = HideFlags.DontSave };
                        s.Own(noodle);
                        Vector3 start = new(board.center.x - 0.004f + 0.003f * k, top + 0.0016f, board.center.z - 0.006f + 0.009f * k);
                        noodle.AddComponent<MeshFilter>().sharedMesh = Noodle(start, 0.0016f, 0.046f - 0.012f * k, 0.007f + 0.002f * k, 1.3f * k);
                        noodle.AddComponent<MeshRenderer>().sharedMaterial = noodleMaterial;
                    }

                    // a slice of naruto (fish cake) on the board: no mistaking where this phone has been
                    Texture2D narutoTexture = TrailerAssets.Texture("naruto.png");
                    var naruto = s.Lit("#FFFFFF", 0.35f);
                    naruto.SetTexture("_BaseMap", narutoTexture);
                    naruto.SetFloat("_AlphaClip", 1f);
                    naruto.SetFloat("_Cutoff", 0.5f);
                    naruto.EnableKeyword("_ALPHATEST_ON");
                    naruto.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    var center = new Vector3(board.center.x + 0.016f, top + 0.0024f, board.center.z + 0.01f);
                    GameObject face = s.Primitive(PrimitiveType.Quad, "Trailer_Naruto", naruto);
                    face.transform.SetPositionAndRotation(center + Vector3.up * 0.0012f, Quaternion.Euler(90f, 35f, 0f));
                    face.transform.localScale = Vector3.one * 0.0135f;
                    GameObject body = s.Primitive(PrimitiveType.Cylinder, "Trailer_NarutoBody", s.Lit("#FBF6EC", 0.3f));
                    body.transform.SetPositionAndRotation(center, Quaternion.identity);
                    body.transform.localScale = new Vector3(0.0118f, 0.0012f, 0.0118f);
                    s.ShadowDistance(1.0f);
                },
                Pose = (s, t) =>
                {
                    Bounds board = bench.BoundsOf("Logicboard");
                    Vector3 c = board.center;
                    float u = Ease.InOutSine(t / Bar);
                    var a = new CamKey(c + new Vector3(-0.07f, 0.075f, -0.075f), c + new Vector3(-0.008f, 0f, 0f), 30f);
                    var b = new CamKey(c + new Vector3(0.05f, 0.07f, -0.085f), c + new Vector3(0.006f, 0f, 0.002f), 28f);
                    CamKey.Arc(a, b, c, u).Apply(s);
                    s.Focus(c, 4.5f);
                    s.Track("board", c);
                },
            };
        }

        /// <summary>A wavy ramen noodle lying on a surface (a tube along a squiggle).</summary>
        private static Mesh Noodle(Vector3 start, float radius, float length, float wave, float phase)
        {
            const int segments = 64, sides = 10;
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                points[i] = start + new Vector3(Mathf.Lerp(-0.5f, 0.5f, u) * length, 0f,
                    wave * Mathf.Sin(u * Mathf.PI * 3.4f + phase) + wave * 0.35f * Mathf.Sin(u * 19f + phase));
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                Vector3 tangent = (points[Mathf.Min(segments, i + 1)] - points[Mathf.Max(0, i - 1)]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                for (int k = 0; k < sides; k++)
                {
                    float a = k / (float)sides * Mathf.PI * 2f;
                    Vector3 n = Mathf.Cos(a) * side + Mathf.Sin(a) * Vector3.up;
                    float taper = Mathf.Min(1f, Mathf.Min(i, segments - i) / 2f + 0.35f);
                    vertices.Add(points[i] + n * radius * taper);
                    normals.Add(n);
                }
            }

            for (int i = 0; i < segments; i++)
            {
                for (int k = 0; k < sides; k++)
                {
                    int a = i * sides + k, b = i * sides + (k + 1) % sides, c = a + sides, d = b + sides;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }

            var mesh = new Mesh { name = "Noodle", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>d7: "Grow your shop." The supply catalog pops over the diorama.</summary>
        private static Shot Upgrades()
        {
            ShopRig shop = null;
            RectTransform panel = null;
            return new Shot
            {
                Name = "d7_upgrades",
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    s.UiOnly("ShopScreens/Upgrades");
                    panel = s.UiElement("ShopScreens/Upgrades");
                },
                Pose = (s, t) =>
                {
                    shop.Life(t + 3f);
                    Vector3 pivot = ShopRig.DioramaTarget;
                    Vector3 offset = ShopRig.DioramaPosition - pivot;
                    float u = Ease.InOutSine(t / Bar);
                    s.Aim(pivot + Quaternion.Euler(0f, Mathf.Lerp(4f, 1f, u), 0f) * offset * Mathf.Lerp(0.92f, 0.88f, u), pivot, 20f);
                    float pop = Ease.OutBack(Ease.Window(t, 0.05f, 0.35f), 1.6f);
                    RectTransform content = panel.Cast<Transform>().OfType<RectTransform>().FirstOrDefault(c => c.name != "Dim") ?? panel;
                    content.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, pop);
                    s.UiAlpha("ShopScreens/Upgrades", Ease.Window(t, 0.05f, 0.15f));
                    s.Cue("swoosh_ui", 0.05f, 0.9f);
                    s.Cue("paper", 0.1f, 0.5f);
                },
            };
        }

        /// <summary>d8: "Pet Denchi." Two squishy bounces.</summary>
        private static Shot PetCat()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "d8_cat",
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    ShopRig.Place(shop.Mio, new Vector3(1.25f, 0f, 3.75f), 200f);
                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    shop.Life(t + 2f);
                    float bounce = t >= 1.2f ? t - 1.2f : t - 0.02f;
                    shop.Cat(bounce, 0.4f, t);
                    var a = new CamKey(new Vector3(1.22f, 0.88f, 1.24f), new Vector3(0.6f, 0.62f, 1.68f), 31f);
                    var b = new CamKey(new Vector3(1.14f, 0.85f, 1.3f), new Vector3(0.6f, 0.62f, 1.68f), 30f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.12f);
                    s.Focus(new Vector3(0.68f, 0.64f, 1.67f), 2.0f);
                    s.Cue("heart", 0.05f, 0.7f);
                    s.Cue("purr", 0.1f, 0.6f);
                    s.Cue("heart", 1.22f, 0.7f, 1.12f);
                    s.Cue("mew", 1.3f, 0.6f);
                    s.Track("cat", shop.CatHead);
                },
            };
        }

        /// <summary>d9: everybody celebrates; the camera lifts away from the diorama.</summary>
        private static Shot Finale()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "d9_finale",
                Scene = ShopScene,
                Frames = Bars(1),
                Subframes = 2,
                Setup = s => shop = new ShopRig(s),
                Pose = (s, t) =>
                {
                    float[] delays = { 0.0f, 0.15f, 0.3f, 0.08f, 0.22f };
                    int n = 0;
                    foreach (GameObject actor in shop.Everyone)
                    {
                        float c = t - delays[n++];
                        s.Animate(actor, "Celebrate", c < 0f ? 0f : c % 1.33f);
                    }

                    Vector3 pivot = ShopRig.DioramaTarget;
                    Vector3 offset = ShopRig.DioramaPosition - pivot;
                    float u = Ease.InOutSine(t / Bar);
                    Vector3 position = pivot + Quaternion.Euler(Mathf.Lerp(0f, -3f, u), Mathf.Lerp(-2f, 3f, u), 0f) * offset * Mathf.Lerp(0.86f, 0.95f, u);
                    s.Aim(position, pivot + new Vector3(0f, 0.1f, 0f), Mathf.Lerp(19f, 21f, u));
                    for (int k = 0; k < 5; k++)
                    {
                        s.Cue("jump", 0.35f + delays[k], 0.25f, 1f + 0.08f * k, -0.4f + 0.2f * k);
                    }
                },
            };
        }

        // ==== E: title and stinger ===========================================================================

        /// <summary>e1: the title card plays over the diorama, framed like the main menu (shop on the right).</summary>
        private static Shot Title()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "e1_title",
                Scene = ShopScene,
                Frames = Bars(2),
                Setup = s => shop = new ShopRig(s),
                Pose = (s, t) =>
                {
                    shop.Life(t + 5f);
                    float u = Ease.InOutSine(t / (2f * Bar));
                    // like the main menu (shop on the right), nudged further right so the logo card sits clear of it
                    var a = new CamKey(new Vector3(12.9f, 8.9f, -12.3f), new Vector3(-0.1f, 0.85f, 1.98f), 24.5f);
                    var b = new CamKey(new Vector3(12.3f, 8.5f, -11.6f), new Vector3(-0.05f, 0.9f, 2.0f), 24f);
                    CamKey.Lerp(a, b, u).Apply(s);
                },
            };
        }

        /// <summary>e2: Mr. Sasaki at the window. Knock, knock, knock.</summary>
        private static Shot Stinger()
        {
            ShopRig shop = null;
            return new Shot
            {
                Name = "e2_stinger",
                Scene = ShopScene,
                Frames = Bars(1),
                Setup = s =>
                {
                    shop = new ShopRig(s);
                    ShopRig.Place(shop.Sasaki, new Vector3(-0.4f, 0.025f, 2.12f), 90f);
                    s.Hide("Shop_WindowGlass"); // the glass haze hides his face; the frame still says "window"
                    s.ShadowDistance(8f);
                },
                Pose = (s, t) =>
                {
                    shop.Life(t, shop.Sasaki);
                    s.Animate(shop.Sasaki, "Impatient", t * 1.15f);
                    float knock = t > 0.25f && t < 0.95f ? Mathf.Max(0f, Mathf.Sin((t - 0.25f) * Mathf.PI * 2f / 0.233f)) : 0f;
                    s.Nudge(shop.Sasaki, "UpperArm.R", new Vector3(-60f * knock - 35f, 0f, 0f));
                    Vector3 eyes = shop.Eyes(shop.Sasaki);
                    var a = new CamKey(new Vector3(1.25f, 1.24f, 1.98f), eyes + new Vector3(0f, -0.05f, 0f), 28f);
                    var b = new CamKey(new Vector3(1.1f, 1.22f, 2.0f), eyes + new Vector3(0f, -0.05f, 0f), 27f);
                    CamKey.Lerp(a, b, Ease.InOutSine(t / Bar)).Apply(s);
                    s.Sway(0.12f);
                    s.Focus(eyes, 2.6f);
                    s.Cue("thunk", 0.3f, 0.7f, 1.7f, -0.3f);
                    s.Cue("thunk", 0.53f, 0.7f, 1.75f, -0.3f);
                    s.Cue("thunk", 0.76f, 0.75f, 1.7f, -0.3f);
                    s.Track("head", shop.HeadTop(shop.Sasaki));
                },
            };
        }

        private static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }
    }
}

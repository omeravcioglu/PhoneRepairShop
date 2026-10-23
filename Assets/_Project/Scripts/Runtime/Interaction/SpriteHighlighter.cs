using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Hover outlines and colour flashes on sprites, through the "PhoneRepair/Pixel Sprite" shader's
    /// _OutlineColor and _Flash (set per renderer with a MaterialPropertyBlock, never touching materials).
    /// Outline colour follows the hover tone: orange = pressing does something, cream = just look,
    /// coral = the right tool but not yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteHighlighter : MonoBehaviour
    {
        [SerializeField] private Color readyColor = new(0.95f, 0.65f, 0.35f, 1f);
        [SerializeField] private Color neutralColor = new(1f, 0.97f, 0.9f, 1f);
        [SerializeField] private Color blockedColor = new(0.87f, 0.51f, 0.46f, 1f);

        private static readonly int OutlineId = Shader.PropertyToID("_OutlineColor");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        private sealed class FlashState
        {
            public Interactable Target;
            public Color Color;
            public float Duration;
            public float Elapsed;
        }

        private readonly List<FlashState> flashes = new();
        private readonly Dictionary<SpriteRenderer, (Color outline, Color flash)> wanted = new();
        private readonly HashSet<SpriteRenderer> touched = new();
        private readonly List<SpriteRenderer> stale = new();
        private MaterialPropertyBlock block;
        private Interactable hovered;
        private InteractionTone hoveredTone;

        public Interactable Hovered => hovered;

        public void SetHover(Interactable target, InteractionTone tone)
        {
            hovered = target;
            hoveredTone = tone;
        }

        /// <summary>Tints the target towards a colour that fades out ("nope" red, "done" green).</summary>
        public void Flash(Interactable target, Color color, float seconds = 0.3f)
        {
            if (target == null)
            {
                return;
            }

            flashes.RemoveAll(f => f.Target == target);
            flashes.Add(new FlashState { Target = target, Color = color, Duration = Mathf.Max(0.05f, seconds) });
        }

        public Color ColorFor(InteractionTone tone) => tone switch
        {
            InteractionTone.Ready => readyColor,
            InteractionTone.Blocked => blockedColor,
            _ => neutralColor,
        };

        private void LateUpdate()
        {
            wanted.Clear();

            if (hovered != null && hovered.GetComponent<ICustomHover>() == null)
            {
                Color outline = ColorFor(hoveredTone);
                foreach (SpriteRenderer sr in SpritesOf(hovered))
                {
                    wanted[sr] = (outline, Color.clear);
                }
            }

            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                FlashState flash = flashes[i];
                flash.Elapsed += Time.unscaledDeltaTime;
                if (flash.Target == null || flash.Elapsed >= flash.Duration)
                {
                    flashes.RemoveAt(i);
                    continue;
                }

                float amount = 1f - flash.Elapsed / flash.Duration;
                Color tint = new(flash.Color.r, flash.Color.g, flash.Color.b, flash.Color.a * amount * 0.8f);
                foreach (SpriteRenderer sr in SpritesOf(flash.Target))
                {
                    Color outline = wanted.TryGetValue(sr, out var current) ? current.outline : Color.clear;
                    wanted[sr] = (outline, tint);
                }
            }

            block ??= new MaterialPropertyBlock();
            foreach (KeyValuePair<SpriteRenderer, (Color outline, Color flash)> pair in wanted)
            {
                Apply(pair.Key, pair.Value.outline, pair.Value.flash);
            }

            stale.Clear();
            foreach (SpriteRenderer sr in touched)
            {
                if (!wanted.ContainsKey(sr))
                {
                    stale.Add(sr);
                }
            }

            foreach (SpriteRenderer sr in stale)
            {
                if (sr != null)
                {
                    Apply(sr, Color.clear, Color.clear);
                }

                touched.Remove(sr);
            }

            foreach (SpriteRenderer sr in wanted.Keys)
            {
                touched.Add(sr);
            }
        }

        private void Apply(SpriteRenderer sr, Color outline, Color flash)
        {
            sr.GetPropertyBlock(block);
            block.SetColor(OutlineId, outline);
            block.SetColor(FlashId, flash);
            sr.SetPropertyBlock(block);
        }

        private static IEnumerable<SpriteRenderer> SpritesOf(Interactable target)
        {
            foreach (Renderer r in target.HighlightRenderers)
            {
                if (r is SpriteRenderer sr && sr != null)
                {
                    yield return sr;
                }
            }
        }
    }
}

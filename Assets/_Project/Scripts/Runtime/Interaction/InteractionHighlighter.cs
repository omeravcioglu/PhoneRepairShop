using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Draws a soft rim-light overlay on the hovered and selected Interactables by re-rendering
    /// their meshes with an additive material. The objects' own materials are never touched, so
    /// it works with any shader and any number of submeshes.
    /// Hover colour follows the hover hint (ready / neutral / blocked); selection pulses gently;
    /// <see cref="Flash"/> gives one-off pulses (e.g. red "nope").
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class InteractionHighlighter : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private SelectionController selection;
        [Tooltip("Material using the 'PhoneRepair/Interactable Highlight' shader.")]
        [SerializeField] private Material highlightMaterial;

        [Header("Look")]
        [Tooltip("Hover when pressing would do something.")]
        [SerializeField, ColorUsage(false, true)] private Color readyColor = new(0.95f, 0.72f, 0.38f);
        [Tooltip("Hover when there is nothing to do with the current tool (inspect only).")]
        [SerializeField, ColorUsage(false, true)] private Color neutralColor = new(0.6f, 0.62f, 0.75f);
        [Tooltip("Hover when the right tool is out but the rules say not yet.")]
        [SerializeField, ColorUsage(false, true)] private Color blockedColor = new(1.1f, 0.4f, 0.34f);
        [SerializeField, ColorUsage(false, true)] private Color selectedColor = new(0.5f, 1.05f, 1.2f);
        [SerializeField, Min(0.1f)] private float fadeSpeed = 9f;
        [SerializeField, Min(0f)] private float pulseFrequency = 1.3f;
        [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.3f;

        private readonly List<Entry> entries = new();
        private readonly List<FlashEntry> flashes = new();

        private sealed class Entry
        {
            public Interactable Target;
            public float Hover;
            public float Selected;
            public MaterialPropertyBlock Block;
        }

        private sealed class FlashEntry
        {
            public Interactable Target;
            public Color Color;
            public float Duration;
            public float Remaining;
            public MaterialPropertyBlock Block;
        }

        /// <summary>One-off glow that fades out over <paramref name="duration"/> seconds.</summary>
        public void Flash(Interactable target, Color color, float duration = 0.45f)
        {
            if (target == null)
            {
                return;
            }

            flashes.Add(new FlashEntry
            {
                Target = target,
                Color = color,
                Duration = Mathf.Max(0.05f, duration),
                Remaining = Mathf.Max(0.05f, duration),
                Block = new MaterialPropertyBlock(),
            });
        }

        private void LateUpdate()
        {
            if (selection == null || highlightMaterial == null)
            {
                return;
            }

            Track(selection.Hovered);
            Track(selection.Selected);

            float deltaTime = Time.unscaledDeltaTime;
            float step = fadeSpeed * deltaTime;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseFrequency * 2f * Mathf.PI) * pulseAmount;
            Color hoverColor = ToneColor(selection.Hint.Tone);

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];
                if (entry.Target == null)
                {
                    entries.RemoveAt(i);
                    continue;
                }

                bool isSelected = entry.Target == selection.Selected;
                bool isHovered = entry.Target == selection.Hovered;
                entry.Hover = Mathf.MoveTowards(entry.Hover, isHovered && !isSelected ? 1f : 0f, step);
                entry.Selected = Mathf.MoveTowards(entry.Selected, isSelected ? 1f : 0f, step);
                if (entry.Hover <= 0f && entry.Selected <= 0f)
                {
                    entries.RemoveAt(i);
                    continue;
                }

                DrawOverlay(entry.Target, highlightMaterial, entry.Block, hoverColor * entry.Hover + selectedColor * (entry.Selected * pulse));
            }

            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                FlashEntry flash = flashes[i];
                flash.Remaining -= deltaTime;
                if (flash.Target == null || flash.Remaining <= 0f)
                {
                    flashes.RemoveAt(i);
                    continue;
                }

                float t = flash.Remaining / flash.Duration;
                DrawOverlay(flash.Target, highlightMaterial, flash.Block, flash.Color * (t * t));
            }
        }

        private Color ToneColor(InteractionTone tone) => tone switch
        {
            InteractionTone.Ready => readyColor,
            InteractionTone.Blocked => blockedColor,
            _ => neutralColor,
        };

        private void Track(Interactable target)
        {
            if (target == null)
            {
                return;
            }

            foreach (Entry entry in entries)
            {
                if (entry.Target == target)
                {
                    return;
                }
            }

            entries.Add(new Entry { Target = target, Block = new MaterialPropertyBlock() });
        }

        /// <summary>
        /// Queues the overlay for one frame on every camera. Call each frame for as long as it should show
        /// (also handy for tutorials: "pulse the screwdriver").
        /// </summary>
        public static void DrawOverlay(Interactable target, Material material, MaterialPropertyBlock block, Color color)
        {
            if (target == null || material == null || block == null)
            {
                return;
            }

            block.SetColor(ColorId, color);
            IReadOnlyList<Renderer> renderers = target.HighlightRenderers;
            IReadOnlyList<MeshFilter> filters = target.HighlightMeshFilters;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer r = renderers[i];
                MeshFilter filter = filters[i];
                if (r == null || filter == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var renderParams = new RenderParams(material)
                {
                    matProps = block,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    layer = r.gameObject.layer,
                    renderingLayerMask = r.renderingLayerMask,
                    worldBounds = r.bounds,
                };

                Matrix4x4 matrix = r.localToWorldMatrix;
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    Graphics.RenderMesh(renderParams, mesh, subMesh, matrix);
                }
            }
        }
    }
}

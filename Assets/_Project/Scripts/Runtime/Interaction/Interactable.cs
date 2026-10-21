using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Anything the player can point at and select: phone parts now; tools, shop stations and
    /// customers later. Only knows about interaction (hover/selection state, which renderers glow,
    /// world bounds). Domain meaning comes from a sibling <see cref="IInspectionInfoProvider"/>.
    /// Needs a Collider on itself or a child to be pickable.
    /// </summary>
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Fallback name when no IInspectionInfoProvider sits on this GameObject.")]
        [SerializeField] private string displayName;
        [SerializeField] private bool interactable = true;
        [Tooltip("Renderers that glow when hovered/selected. Leave empty to collect child renderers automatically (stopping at nested Interactables).")]
        [SerializeField] private Renderer[] highlightRenderers = Array.Empty<Renderer>();

        [Header("Events")]
        [SerializeField] private UnityEvent onHoverEnter = new();
        [SerializeField] private UnityEvent onHoverExit = new();
        [SerializeField] private UnityEvent onSelected = new();
        [SerializeField] private UnityEvent onDeselected = new();

        private IInspectionInfoProvider infoProvider;
        private Renderer[] resolvedRenderers;
        private MeshFilter[] resolvedFilters;

        /// <summary>Raised when the text from <see cref="GetInfo"/> changed (e.g. a part was unplugged).</summary>
        public event Action<Interactable> InfoChanged;

        public bool IsInteractable => interactable && isActiveAndEnabled;
        public bool IsHovered { get; private set; }
        public bool IsSelected { get; private set; }

        public UnityEvent HoverEntered => onHoverEnter;
        public UnityEvent HoverExited => onHoverExit;
        public UnityEvent Selected => onSelected;
        public UnityEvent Deselected => onDeselected;

        public IReadOnlyList<Renderer> HighlightRenderers
        {
            get
            {
                Resolve();
                return resolvedRenderers;
            }
        }

        /// <summary>MeshFilter for each entry in <see cref="HighlightRenderers"/> (null for non-mesh renderers).</summary>
        public IReadOnlyList<MeshFilter> HighlightMeshFilters
        {
            get
            {
                Resolve();
                return resolvedFilters;
            }
        }

        public string DisplayName => GetInfo().Title;

        public void SetInteractable(bool value) => interactable = value;

        public InspectionInfo GetInfo()
        {
            Resolve();
            if (infoProvider != null)
            {
                return infoProvider.GetInspectionInfo();
            }

            return new InspectionInfo(string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName);
        }

        /// <summary>World-space bounds of the visible renderers (used for camera framing).</summary>
        public Bounds GetWorldBounds()
        {
            Resolve();
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Renderer r in resolvedRenderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(r.bounds);
                }
                else
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds ? bounds : new Bounds(transform.position, Vector3.one * 0.01f);
        }

        /// <summary>Domain components call this when what they report through GetInfo changed.</summary>
        public void NotifyInfoChanged() => InfoChanged?.Invoke(this);

        /// <summary>Call after swapping meshes/children at runtime (e.g. a replaced part).</summary>
        public void RefreshRenderers()
        {
            resolvedRenderers = null;
            Resolve();
        }

        /// <summary>Renderers in this hierarchy that are not owned by a nested Interactable.</summary>
        public Renderer[] CollectOwnedRenderers()
        {
            var owned = new List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponentInParent<Interactable>(true) == this)
                {
                    owned.Add(r);
                }
            }

            return owned.ToArray();
        }

        internal void SetHovered(bool value)
        {
            if (IsHovered == value)
            {
                return;
            }

            IsHovered = value;
            (value ? onHoverEnter : onHoverExit).Invoke();
        }

        internal void SetSelected(bool value)
        {
            if (IsSelected == value)
            {
                return;
            }

            IsSelected = value;
            (value ? onSelected : onDeselected).Invoke();
        }

        protected virtual void Awake() => Resolve();

        private void Resolve()
        {
            if (resolvedRenderers != null)
            {
                return;
            }

            infoProvider = GetComponent<IInspectionInfoProvider>();
            resolvedRenderers = highlightRenderers != null && highlightRenderers.Length > 0
                ? highlightRenderers
                : CollectOwnedRenderers();
            resolvedFilters = new MeshFilter[resolvedRenderers.Length];
            for (int i = 0; i < resolvedRenderers.Length; i++)
            {
                resolvedFilters[i] = resolvedRenderers[i] != null ? resolvedRenderers[i].GetComponent<MeshFilter>() : null;
            }
        }
    }
}

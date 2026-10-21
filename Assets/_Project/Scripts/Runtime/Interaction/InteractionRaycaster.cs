using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhoneRepair.Interaction
{
    /// <summary>Result of a pick: the Interactable (if any) and the physics hit that found it.</summary>
    public readonly struct PickResult
    {
        public Interactable Interactable { get; }
        public RaycastHit Hit { get; }

        public PickResult(Interactable interactable, RaycastHit hit)
        {
            Interactable = interactable;
            Hit = hit;
        }
    }

    /// <summary>
    /// Screen point -> Interactable. The only place that knows about cameras, raycast layers and
    /// UI blocking, so every mode (repair bench, shop floor, minigames) picks things the same way.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float maxDistance = 10f;
        [Tooltip("Ignore the world while the pointer is over a UI element that is a raycast target.")]
        [SerializeField] private bool blockedByUI = true;

        private readonly List<RaycastResult> uiResults = new();
        private PointerEventData uiPointer;
        private EventSystem uiPointerOwner;

        public Camera TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        /// <summary>True when a raycast-target UI element sits under the given screen position.</summary>
        public bool IsOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (!blockedByUI || eventSystem == null)
            {
                return false;
            }

            if (uiPointer == null || uiPointerOwner != eventSystem)
            {
                uiPointer = new PointerEventData(eventSystem);
                uiPointerOwner = eventSystem;
            }

            uiPointer.Reset();
            uiPointer.position = screenPosition;
            uiResults.Clear();
            eventSystem.RaycastAll(uiPointer, uiResults);
            return uiResults.Count > 0;
        }

        /// <summary>Nearest surface of anything under the screen position (UI blocks it). For cursor-following visuals.</summary>
        public bool TryRaycast(Vector2 screenPosition, out RaycastHit hit)
        {
            hit = default;
            if (targetCamera == null || IsOverUI(screenPosition))
            {
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Finds the nearest Interactable under the screen position (occluders block it).</summary>
        public bool TryPick(Vector2 screenPosition, out PickResult result)
        {
            result = default;
            if (targetCamera == null || IsOverUI(screenPosition))
            {
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Interactable target = hit.collider.GetComponentInParent<Interactable>();
            if (target == null || !target.IsInteractable)
            {
                result = new PickResult(null, hit);
                return false;
            }

            result = new PickResult(target, hit);
            return true;
        }
    }
}

using UnityEngine;

namespace PhoneRepair.Cameras
{
    /// <summary>
    /// Repair-mode camera. Looks at an anchor (the phone's turntable pivot) from a fixed, comfortable
    /// angle; the player zooms towards the cursor, pans, or frames a component. The phone rotates
    /// instead of the camera orbiting, so lighting and the workbench stay put.
    /// When the focus is ON the phone it follows the phone's rotation (a framed screw stays centred
    /// while you turn the phone); focus on the mat stays put in the world.
    /// </summary>
    public sealed class InspectionCameraView : CameraView
    {
        [Header("Target")]
        [Tooltip("What the camera looks at. Usually the InspectionTurntable pivot.")]
        [SerializeField] private Transform anchor;

        [Header("Viewing angle")]
        [SerializeField, Range(5f, 89f)] private float pitch = 58f;
        [SerializeField, Range(-180f, 180f)] private float yaw;
        [SerializeField, Range(10f, 90f)] private float fieldOfView = 35f;

        [Header("Zoom")]
        [SerializeField, Min(0.01f)] private float minDistance = 0.07f;
        [SerializeField, Min(0.01f)] private float maxDistance = 0.75f;
        [SerializeField, Min(0.01f)] private float defaultDistance = 0.36f;
        [Tooltip("Fraction of the current distance removed per wheel notch.")]
        [SerializeField, Range(0.02f, 0.5f)] private float zoomStep = 0.14f;
        [SerializeField] private bool zoomTowardsCursor = true;
        [SerializeField] private LayerMask zoomRaycastMask = Physics.DefaultRaycastLayers;

        [Header("Pan")]
        [Tooltip("How far the focus may move away from the anchor when fully zoomed in (reaches parts laid out on the mat). Shrinks to zero at max distance, so zooming out always recentres.")]
        [SerializeField, Min(0f)] private float panRadiusAtMinDistance = 0.28f;

        [Header("Framing")]
        [SerializeField, Min(1f)] private float focusPadding = 1.35f;

        [Header("Smoothing")]
        [SerializeField, Min(0.1f)] private float sharpness = 12f;

        private Vector3 targetOffset;           // anchor-local when following, else world offset from the anchor position
        private bool followAnchorRotation = true;
        private Vector3 currentFocus;           // smoothed, world space
        private float targetDistance;
        private float currentDistance;

        public Transform Anchor
        {
            get => anchor;
            set => anchor = value;
        }

        public float TargetDistance => targetDistance;
        public float CurrentDistance => currentDistance;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float DefaultDistance => defaultDistance;
        public Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0f);
        public bool FollowsAnchorRotation => followAnchorRotation;

        public Vector3 TargetFocusWorld => followAnchorRotation
            ? AnchorTransform.TransformPoint(targetOffset)
            : AnchorTransform.position + targetOffset;

        public Vector3 CurrentFocusWorld => currentFocus;

        private Transform AnchorTransform => anchor != null ? anchor : transform;
        private Camera OutputCamera => Director != null ? Director.Camera : null;

        private void Awake()
        {
            ResetView(instant: true);
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(maxDistance, minDistance);
            defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        }

        public void ResetView(bool instant = false)
        {
            targetOffset = Vector3.zero;
            followAnchorRotation = true;
            targetDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            if (instant)
            {
                SnapToTarget();
            }
        }

        /// <summary>Zoom by wheel notches (positive = closer). The world point under the cursor stays put.</summary>
        public void Zoom(float notches, Vector2 screenPoint)
        {
            if (Mathf.Approximately(notches, 0f))
            {
                return;
            }

            float newDistance = Mathf.Clamp(targetDistance * Mathf.Pow(1f - zoomStep, notches), minDistance, maxDistance);
            if (Mathf.Approximately(newDistance, targetDistance))
            {
                return;
            }

            Vector3 focus = TargetFocusWorld;
            bool follow = followAnchorRotation;
            if (zoomTowardsCursor && TryGetZoomPivot(screenPoint, focus, out Vector3 pivot, out Transform hitTransform))
            {
                focus = ScaleAbout(pivot, focus, newDistance / targetDistance);
                if (hitTransform != null)
                {
                    follow = hitTransform.IsChildOf(AnchorTransform);
                }
            }

            targetDistance = newDistance;
            SetTargetFocus(focus, follow);
        }

        /// <summary>Drag the view by a screen-space delta (content follows the cursor 1:1).</summary>
        public void Pan(Vector2 screenDelta)
        {
            Camera cam = OutputCamera;
            if (cam == null || screenDelta == Vector2.zero)
            {
                return;
            }

            float worldPerPixel = 2f * targetDistance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad)
                                  / Mathf.Max(1f, cam.pixelHeight);
            Quaternion rotation = ViewRotation;
            Vector3 move = -(rotation * Vector3.right) * (screenDelta.x * worldPerPixel)
                           - (rotation * Vector3.up) * (screenDelta.y * worldPerPixel);
            SetTargetFocus(TargetFocusWorld + move, followAnchorRotation);
        }

        /// <summary>
        /// Centre and fit the given world bounds. Pass the owning transform so the camera knows
        /// whether to follow the phone's rotation (owner on the phone) or stay put (owner on the mat).
        /// </summary>
        public void FocusOn(Bounds worldBounds, Transform owner = null)
        {
            float radius = Mathf.Max(worldBounds.extents.magnitude, 0.001f);
            float halfVertical = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float aspect = OutputCamera != null ? OutputCamera.aspect : 16f / 9f;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * aspect);
            float distance = radius * focusPadding / Mathf.Sin(Mathf.Min(halfVertical, halfHorizontal));
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            bool follow = owner == null || owner.IsChildOf(AnchorTransform);
            SetTargetFocus(worldBounds.center, follow);
        }

        /// <summary>Jump the smoothed state to the targets (cuts, tests).</summary>
        public void SnapToTarget()
        {
            currentFocus = TargetFocusWorld;
            currentDistance = targetDistance;
        }

        public override void Tick(float deltaTime)
        {
            float k = 1f - Mathf.Exp(-sharpness * deltaTime);
            currentFocus = Vector3.Lerp(currentFocus, TargetFocusWorld, k);
            // interpolate in log space so zooming feels uniform at every scale
            currentDistance = Mathf.Exp(Mathf.Lerp(Mathf.Log(currentDistance), Mathf.Log(targetDistance), k));
        }

        public override CameraPose GetPose()
        {
            Quaternion rotation = ViewRotation;
            return new CameraPose(currentFocus - rotation * Vector3.forward * currentDistance, rotation, fieldOfView);
        }

        /// <summary>How far the focus may sit from the anchor at a given distance (0 at max distance).</summary>
        public float AllowedPanRadius(float distance)
        {
            float t = Mathf.InverseLerp(Mathf.Log(maxDistance), Mathf.Log(minDistance), Mathf.Log(Mathf.Max(distance, 1e-4f)));
            return panRadiusAtMinDistance * t;
        }

        /// <summary>
        /// Scaling the whole camera rig about a point keeps that point at the same screen position,
        /// which is what makes zoom-to-cursor feel "anchored".
        /// </summary>
        public static Vector3 ScaleAbout(Vector3 pivot, Vector3 point, float scale) => pivot + (point - pivot) * scale;

        private void SetTargetFocus(Vector3 worldFocus, bool follow)
        {
            Transform a = AnchorTransform;
            followAnchorRotation = follow;
            Vector3 offset = follow ? a.InverseTransformPoint(worldFocus) : worldFocus - a.position;
            targetOffset = Vector3.ClampMagnitude(offset, AllowedPanRadius(targetDistance));
        }

        private bool TryGetZoomPivot(Vector2 screenPoint, Vector3 focus, out Vector3 pivot, out Transform hitTransform)
        {
            pivot = default;
            hitTransform = null;
            Camera cam = OutputCamera;
            if (cam == null)
            {
                return false;
            }

            // Build the ray from the TARGET pose rather than the smoothed camera, so fast scrolling stays exact.
            Rect rect = cam.pixelRect;
            float nx = (screenPoint.x - rect.x) / Mathf.Max(1f, rect.width) * 2f - 1f;
            float ny = (screenPoint.y - rect.y) / Mathf.Max(1f, rect.height) * 2f - 1f;
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            Quaternion rotation = ViewRotation;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 direction = rotation * new Vector3(nx * tan * cam.aspect, ny * tan, 1f).normalized;
            var ray = new Ray(focus - forward * targetDistance, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance * 4f, zoomRaycastMask, QueryTriggerInteraction.Ignore))
            {
                pivot = hit.point;
                hitTransform = hit.transform;
                return true;
            }

            var focusPlane = new Plane(-forward, focus);
            if (focusPlane.Raycast(ray, out float enter))
            {
                pivot = ray.GetPoint(enter);
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            bool playing = Application.isPlaying;
            Vector3 focus = playing ? currentFocus : AnchorTransform.position;
            float distance = playing ? currentDistance : defaultDistance;
            Gizmos.color = new Color(1f, 0.75f, 0.3f);
            Gizmos.DrawWireSphere(focus, 0.005f);
            Gizmos.DrawLine(focus, focus - ViewRotation * Vector3.forward * distance);
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(AnchorTransform.position, AllowedPanRadius(distance));
        }
    }
}

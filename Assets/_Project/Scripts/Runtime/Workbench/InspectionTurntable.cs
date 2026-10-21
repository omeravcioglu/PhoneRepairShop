using UnityEngine;

namespace PhoneRepair.Workbench
{
    /// <summary>
    /// Holds the device on the bench and turns it the way a hand would: drag to tumble, flip it
    /// over, reset. It lifts off the mat exactly as far as its rotated bounds require (it never
    /// clips into the bench) and settles back down when laid flat again.
    /// Put it on an empty "pivot" whose child is the device, centred on the pivot.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    public sealed class InspectionTurntable : MonoBehaviour
    {
        [Header("Rotation")]
        [Tooltip("Degrees turned by a drag across the full screen height.")]
        [SerializeField, Min(0f)] private float dragDegreesPerScreenHeight = 240f;
        [SerializeField, Min(0f)] private float keyboardDegreesPerSecond = 120f;
        [SerializeField, Min(0.1f)] private float rotationSharpness = 16f;

        [Header("Resting")]
        [Tooltip("A drag that ends within this angle of lying flat settles the device flat.")]
        [SerializeField, Range(0f, 45f)] private float settleAngle = 22f;

        [Header("Lift")]
        [Tooltip("Extra air between device and mat while it is tilted.")]
        [SerializeField, Min(0f)] private float liftClearance = 0.012f;
        [Tooltip("Tilt (degrees) over which the extra clearance fades in.")]
        [SerializeField, Range(0.1f, 45f)] private float clearanceRampAngle = 12f;
        [SerializeField, Min(0.1f)] private float liftSharpness = 12f;
        [Tooltip("Half size of the held device in pivot space. Measured from child renderers on Awake when left at zero.")]
        [SerializeField] private Vector3 halfExtents;

        private Quaternion restRotation;
        private Quaternion targetRotation;
        private Vector3 restPosition;
        private Vector3 restUpLocal;
        private float restVerticalHalfExtent;
        private float currentLift;
        private bool initialised;

        public Quaternion TargetRotation => targetRotation;
        public Quaternion RestRotation => restRotation;
        public Vector3 HalfExtents => halfExtents;
        public float CurrentLift => currentLift;

        /// <summary>Angle (degrees) between where the device's top face points and straight up/down.</summary>
        public float CurrentTilt => TiltFromFlat(transform.rotation);

        private void Awake() => Initialise();

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Quaternion rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationSharpness * deltaTime));

            float required = RequiredLift(rotation, halfExtents, restVerticalHalfExtent);
            float clearance = liftClearance * Mathf.SmoothStep(0f, 1f, TiltFromFlat(rotation) / clearanceRampAngle);
            currentLift = Mathf.Lerp(currentLift, required + clearance, 1f - Mathf.Exp(-liftSharpness * deltaTime));
            currentLift = Mathf.Max(currentLift, required); // never sink into the mat while smoothing lags

            transform.SetPositionAndRotation(restPosition + Vector3.up * currentLift, rotation);
        }

        /// <summary>Trackball-style rotation from a pointer drag (pixels).</summary>
        public void RotateByScreenDelta(Vector2 pixelDelta, Transform viewer)
        {
            float degreesPerPixel = dragDegreesPerScreenHeight / Mathf.Max(1, Screen.height);
            ApplyYawPitch(-pixelDelta.x * degreesPerPixel, pixelDelta.y * degreesPerPixel, viewer);
        }

        /// <summary>Rotation from a held axis (keyboard / stick), each component in [-1, 1].</summary>
        public void RotateByAxis(Vector2 axis, float deltaTime, Transform viewer)
        {
            float degrees = keyboardDegreesPerSecond * deltaTime;
            ApplyYawPitch(-axis.x * degrees, axis.y * degrees, viewer);
        }

        /// <summary>Turn the device over around its long axis, like flipping a phone in your hand.</summary>
        public void Flip(Transform viewer)
        {
            Initialise();
            Vector3 longAxisLocal = halfExtents.z >= halfExtents.x ? Vector3.forward : Vector3.right;
            Vector3 axis = Vector3.ProjectOnPlane(targetRotation * longAxisLocal, Vector3.up);
            if (axis.sqrMagnitude < 0.04f)
            {
                // standing on its end: flip towards/away from the viewer instead
                axis = viewer != null ? Vector3.ProjectOnPlane(viewer.right, Vector3.up) : Vector3.right;
            }

            if (axis.sqrMagnitude < 1e-6f)
            {
                axis = Vector3.forward;
            }

            targetRotation = (Quaternion.AngleAxis(180f, axis.normalized) * targetRotation).normalized;
        }

        public void ResetOrientation()
        {
            Initialise();
            targetRotation = restRotation;
        }

        /// <summary>If the device is almost flat (either face down), finish laying it flat.</summary>
        public void SettleIfNearlyFlat()
        {
            Initialise();
            Vector3 up = targetRotation * restUpLocal;
            float toUp = Vector3.Angle(up, Vector3.up);
            float toDown = 180f - toUp;
            float angle = Mathf.Min(toUp, toDown);
            if (angle < 0.01f || angle > settleAngle)
            {
                return;
            }

            Vector3 goal = toUp <= toDown ? Vector3.up : Vector3.down;
            targetRotation = (Quaternion.FromToRotation(up, goal) * targetRotation).normalized;
        }

        /// <summary>Jump straight to the target orientation and lift (cuts, tests).</summary>
        public void SnapToTarget()
        {
            Initialise();
            float required = RequiredLift(targetRotation, halfExtents, restVerticalHalfExtent);
            currentLift = required + liftClearance * Mathf.SmoothStep(0f, 1f, TiltFromFlat(targetRotation) / clearanceRampAngle);
            transform.SetPositionAndRotation(restPosition + Vector3.up * currentLift, targetRotation);
        }

        /// <summary>Vertical half-size of an oriented box: how far it reaches above/below its centre.</summary>
        public static float VerticalHalfExtent(Quaternion rotation, Vector3 halfExtents)
        {
            Vector3 x = rotation * Vector3.right;
            Vector3 y = rotation * Vector3.up;
            Vector3 z = rotation * Vector3.forward;
            return Mathf.Abs(x.y) * halfExtents.x + Mathf.Abs(y.y) * halfExtents.y + Mathf.Abs(z.y) * halfExtents.z;
        }

        /// <summary>How far the pivot must rise above its resting height so the rotated box clears the mat.</summary>
        public static float RequiredLift(Quaternion rotation, Vector3 halfExtents, float restVerticalHalfExtent)
        {
            return Mathf.Max(0f, VerticalHalfExtent(rotation, halfExtents) - restVerticalHalfExtent);
        }

        private void Initialise()
        {
            if (initialised)
            {
                return;
            }

            initialised = true;
            restRotation = transform.rotation;
            targetRotation = restRotation;
            restPosition = transform.position;
            restUpLocal = Quaternion.Inverse(restRotation) * Vector3.up;
            if (halfExtents == Vector3.zero)
            {
                halfExtents = MeasureHalfExtents();
            }

            restVerticalHalfExtent = VerticalHalfExtent(restRotation, halfExtents);
        }

        private float TiltFromFlat(Quaternion rotation)
        {
            float angle = Vector3.Angle(rotation * restUpLocal, Vector3.up);
            return Mathf.Min(angle, 180f - angle);
        }

        private void ApplyYawPitch(float yawDegrees, float pitchDegrees, Transform viewer)
        {
            Initialise();
            Vector3 right = viewer != null ? Vector3.ProjectOnPlane(viewer.right, Vector3.up) : Vector3.right;
            if (right.sqrMagnitude < 1e-6f)
            {
                right = Vector3.right;
            }

            targetRotation = (Quaternion.AngleAxis(yawDegrees, Vector3.up)
                              * Quaternion.AngleAxis(pitchDegrees, right.normalized)
                              * targetRotation).normalized;
        }

        private Vector3 MeasureHalfExtents()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Vector3(0.04f, 0.006f, 0.08f);
            }

            Matrix4x4 toPivot = transform.worldToLocalMatrix;
            Vector3 max = Vector3.zero;
            foreach (Renderer r in renderers)
            {
                Bounds local = r.localBounds;
                Matrix4x4 toPivotFromRenderer = toPivot * r.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    Vector3 p = toPivotFromRenderer.MultiplyPoint3x4(corner);
                    max = Vector3.Max(max, new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)));
                }
            }

            return max;
        }
    }
}

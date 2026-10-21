using System;
using UnityEngine;

namespace PhoneRepair.Cameras
{
    /// <summary>
    /// Owns the real Camera. Exactly one <see cref="CameraView"/> is active; switching views blends
    /// position, rotation and field of view with an ease curve. Runs late so views can follow
    /// objects that moved this frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] private CameraView initialView;
        [SerializeField, Min(0f)] private float defaultBlendDuration = 0.75f;
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("On start, blend from where the camera is placed in the scene into the initial view.")]
        [SerializeField] private bool blendIntoInitialView = true;
        [SerializeField, Min(0f)] private float introBlendDuration = 1.6f;

        private Camera cachedCamera;
        private CameraView activeView;
        private CameraPose blendFrom;
        private float blendDuration;
        private float blendElapsed;
        private bool blending;

        public event Action<CameraView> ViewChanged;

        public Camera Camera => cachedCamera != null ? cachedCamera : cachedCamera = GetComponent<Camera>();
        public CameraView ActiveView => activeView;
        public bool IsBlending => blending;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (initialView != null && activeView == null)
            {
                Activate(initialView, blendIntoInitialView ? introBlendDuration : 0f);
            }
        }

        public void Activate(CameraView view) => Activate(view, defaultBlendDuration);

        public void Activate(CameraView view, float blendSeconds)
        {
            if (view == null)
            {
                Debug.LogWarning($"{nameof(CameraDirector)}: tried to activate a null view.", this);
                return;
            }

            if (view == activeView)
            {
                return;
            }

            CameraView previous = activeView;
            activeView = view;
            view.AttachTo(this);
            if (previous != null)
            {
                previous.OnViewDeactivated();
            }

            view.OnViewActivated();

            blendFrom = CameraPose.FromCamera(Camera);
            blendDuration = Mathf.Max(0f, blendSeconds);
            blendElapsed = 0f;
            blending = blendDuration > 0f;
            if (!blending)
            {
                Apply(view.GetPose());
            }

            ViewChanged?.Invoke(view);
        }

        /// <summary>Skip any running blend and jump straight to the active view's pose.</summary>
        public void SnapToActiveView()
        {
            blending = false;
            if (activeView != null)
            {
                Apply(activeView.GetPose());
            }
        }

        private void LateUpdate()
        {
            if (activeView == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            activeView.Tick(deltaTime);
            CameraPose pose = activeView.GetPose();

            if (blending)
            {
                blendElapsed += deltaTime;
                float t = blendDuration > 0f ? Mathf.Clamp01(blendElapsed / blendDuration) : 1f;
                pose = CameraPose.Lerp(blendFrom, pose, blendCurve.Evaluate(t));
                blending = t < 1f;
            }

            Apply(pose);
        }

        private void Apply(in CameraPose pose)
        {
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            Camera.fieldOfView = pose.FieldOfView;
        }
    }
}

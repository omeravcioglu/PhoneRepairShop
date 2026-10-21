using UnityEngine;

namespace PhoneRepair.Cameras
{
    /// <summary>
    /// A view that sits exactly where its transform is: the shop's fixed diorama camera (Milestone 4),
    /// customer close-ups, menu shots. An optional slow yaw drift keeps the diorama feeling alive.
    /// Place and aim the GameObject; the <see cref="CameraDirector"/> does the rest.
    /// </summary>
    public sealed class FixedCameraView : CameraView
    {
        [SerializeField, Range(5f, 90f)] private float fieldOfView = 20f;
        [Tooltip("Idle drift in degrees of yaw around the view's own position. 0 = locked.")]
        [SerializeField, Range(0f, 3f)] private float driftDegrees = 0.4f;
        [SerializeField, Min(1f)] private float driftPeriod = 18f;

        public float FieldOfView
        {
            get => fieldOfView;
            set => fieldOfView = value;
        }

        public override CameraPose GetPose()
        {
            Quaternion rotation = transform.rotation;
            if (driftDegrees > 0f && Application.isPlaying)
            {
                float yaw = Mathf.Sin(Time.time * 2f * Mathf.PI / driftPeriod) * driftDegrees;
                rotation = Quaternion.AngleAxis(yaw, Vector3.up) * rotation;
            }

            return new CameraPose(transform.position, rotation, fieldOfView);
        }
    }
}

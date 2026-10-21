using UnityEngine;

namespace PhoneRepair.Cameras
{
    /// <summary>Everything a CameraView decides about the real camera for one frame.</summary>
    public readonly struct CameraPose
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float FieldOfView { get; }

        public CameraPose(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
        }

        public static CameraPose FromCamera(Camera camera)
        {
            Transform t = camera.transform;
            return new CameraPose(t.position, t.rotation, camera.fieldOfView);
        }

        public static CameraPose Lerp(in CameraPose a, in CameraPose b, float t)
        {
            t = Mathf.Clamp01(t);
            return new CameraPose(
                Vector3.Lerp(a.Position, b.Position, t),
                Quaternion.Slerp(a.Rotation, b.Rotation, t),
                Mathf.Lerp(a.FieldOfView, b.FieldOfView, t));
        }
    }
}

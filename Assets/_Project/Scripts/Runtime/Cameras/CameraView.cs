using UnityEngine;

namespace PhoneRepair.Cameras
{
    /// <summary>
    /// A "virtual camera": decides where the real camera should be, but never touches it.
    /// <see cref="CameraDirector"/> owns the real Camera and blends between views, so every new
    /// camera mode (shop overview, customer close-up, minigame) is just another subclass.
    /// </summary>
    public abstract class CameraView : MonoBehaviour
    {
        /// <summary>The director currently driving this view (null while inactive and never used).</summary>
        public CameraDirector Director { get; private set; }

        internal void AttachTo(CameraDirector director) => Director = director;

        /// <summary>Called once when the director switches to this view (before blending in).</summary>
        public virtual void OnViewActivated() { }

        /// <summary>Called once when the director switches away from this view.</summary>
        public virtual void OnViewDeactivated() { }

        /// <summary>Advance smoothing/animation. Called every LateUpdate while active, before GetPose.</summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>The pose this view wants the camera to have right now.</summary>
        public abstract CameraPose GetPose();
    }
}

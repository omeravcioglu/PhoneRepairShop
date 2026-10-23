using System;
using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>
    /// Plays sprite frames on a SpriteRenderer: pixel-art animation without an Animator.
    /// Used for little loops (the shop cat blinking) and one-shots (the counter bell, puffs).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FlipbookAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
        [SerializeField] private bool loop = true;
        [Tooltip("Looping only: hold the first frame this long between loops (e.g. a blink every few seconds).")]
        [SerializeField, Min(0f)] private float holdFirstFrame;
        [SerializeField] private bool playOnEnable = true;

        private SpriteRenderer target;
        private float time;
        private bool playing;

        public bool IsPlaying => playing;
        public int FrameCount => frames.Length;

        public void SetFrames(Sprite[] newFrames, float fps, bool looping, float hold = 0f)
        {
            frames = newFrames ?? Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(0.1f, fps);
            loop = looping;
            holdFirstFrame = Mathf.Max(0f, hold);
        }

        public void Play()
        {
            time = 0f;
            playing = frames.Length > 0;
            ShowFrame(0);
        }

        public void Stop(int showFrame = 0)
        {
            playing = false;
            ShowFrame(showFrame);
        }

        public void ShowFrame(int index)
        {
            if (frames.Length == 0)
            {
                return;
            }

            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }

            target.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        private void OnEnable()
        {
            if (playOnEnable && Application.isPlaying)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            time += Time.deltaTime;
            float frameTime = 1f / framesPerSecond;
            float cycle = holdFirstFrame + frames.Length * frameTime;
            if (loop)
            {
                float t = time % cycle;
                ShowFrame(t < holdFirstFrame ? 0 : Mathf.FloorToInt((t - holdFirstFrame) / frameTime));
                return;
            }

            int index = Mathf.FloorToInt(time / frameTime);
            if (index >= frames.Length)
            {
                playing = false;
                index = 0; // one-shots return to rest
            }

            ShowFrame(index);
        }
    }
}

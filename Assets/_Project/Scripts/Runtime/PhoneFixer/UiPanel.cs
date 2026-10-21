using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// A screen of the Paper &amp; Keys UI (dialogue, inventory, upgrades, pause, receipt): fades and pops
    /// in, blocks clicks to the world while open, closes on Esc. An optional toggle key opens it when no
    /// other panel is open (Esc for the pause menu, I for inventory...). Can pause game time.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class UiPanel : MonoBehaviour
    {
        [SerializeField] private bool startOpen;
        [Tooltip("Opens/closes the panel. None = only through Show/Hide (buttons, stations).")]
        [SerializeField] private Key toggleKey = Key.None;
        [SerializeField] private bool closeOnEscape = true;
        [Tooltip("Sets Time.timeScale to 0 while open (pause menu).")]
        [SerializeField] private bool pauseTime;
        [Tooltip("Scaled for the pop animation. Usually the card.")]
        [SerializeField] private RectTransform popTarget;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 9f;
        [SerializeField] private UnityEvent onOpened = new();
        [SerializeField] private UnityEvent onClosed = new();

        private static readonly List<UiPanel> Open = new();
        private static int lastEscapeFrame = -1;

        private CanvasGroup group;
        private float pop;
        private float timeScaleBefore = 1f;

        public bool IsOpen { get; private set; }
        public UnityEvent Opened => onOpened;
        public UnityEvent Closed => onClosed;

        /// <summary>Frame the panel last opened on (so the key that opened it does not also press a button in it).</summary>
        public int OpenedFrame { get; private set; } = -1;

        /// <summary>True while any panel is open (world input should wait).</summary>
        public static bool AnyOpen => Open.Count > 0;

        /// <summary>Frame any panel last closed on.</summary>
        public static int LastClosedFrame { get; private set; } = -1;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            SetVisible(startOpen, instant: true);
        }

        private void OnDisable()
        {
            if (IsOpen)
            {
                Open.Remove(this);
                if (pauseTime)
                {
                    Time.timeScale = timeScaleBefore;
                }
            }
        }

        private void OnEnable()
        {
            if (IsOpen && !Open.Contains(this))
            {
                Open.Add(this);
            }
        }

        public void Show() => SetVisible(true, instant: false);

        public void Hide() => SetVisible(false, instant: false);

        public void Toggle() => SetVisible(!IsOpen, instant: false);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool escape = keyboard.escapeKey.wasPressedThisFrame;
                if (IsOpen && closeOnEscape && escape && IsTopmost)
                {
                    lastEscapeFrame = Time.frameCount;
                    Hide();
                }
                else if (toggleKey != Key.None && keyboard[toggleKey].wasPressedThisFrame)
                {
                    bool escapeUsed = toggleKey == Key.Escape && lastEscapeFrame == Time.frameCount;
                    if (IsOpen && IsTopmost && toggleKey != Key.Escape)
                    {
                        Hide();
                    }
                    else if (!IsOpen && !AnyOpen && !escapeUsed)
                    {
                        if (toggleKey == Key.Escape)
                        {
                            lastEscapeFrame = Time.frameCount;
                        }

                        Show();
                    }
                }
            }

            float delta = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, IsOpen ? 1f : 0f, fadeSpeed * delta);
            pop = Mathf.MoveTowards(pop, 0f, 6f * delta);
            if (popTarget != null)
            {
                popTarget.localScale = Vector3.one * (1f - 0.06f * pop * pop);
            }
        }

        private bool IsTopmost => Open.Count > 0 && Open[^1] == this;

        private void SetVisible(bool visible, bool instant)
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }

            bool changed = visible != IsOpen;
            IsOpen = visible;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (instant)
            {
                group.alpha = visible ? 1f : 0f;
            }

            if (visible)
            {
                if (!Open.Contains(this))
                {
                    Open.Add(this);
                }

                pop = instant ? 0f : 1f;
                if (changed)
                {
                    OpenedFrame = Time.frameCount;
                }
            }
            else
            {
                Open.Remove(this);
                if (changed)
                {
                    LastClosedFrame = Time.frameCount;
                }
            }

            if (pauseTime && changed)
            {
                if (visible)
                {
                    timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = timeScaleBefore;
                }
            }

            if (changed && !instant)
            {
                (visible ? onOpened : onClosed).Invoke();
            }
        }
    }
}

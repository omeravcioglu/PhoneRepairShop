using PhoneRepair.Audio;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// The speech bubble over the counter. Types a line out with talk blips in the customer's voice;
    /// a click skips to the end, and a blinking arrow says "click to continue". The transform is the
    /// bubble's top-left corner; it grows right and down to fit the text.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer box;
        [SerializeField] private SpriteRenderer tail;
        [SerializeField] private SpriteRenderer moreArrow;
        [SerializeField] private PixelText text;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private Vector2Int padding = new(8, 7);
        [SerializeField, Min(40)] private int wrapWidth = 200;
        [SerializeField, Min(1f)] private float charactersPerSecond = 42f;
        [Tooltip("A blip every N characters.")]
        [SerializeField, Min(1)] private int blipEvery = 2;

        private float typed;
        private int lastBlip;
        private float pitch = 1f;

        public bool IsShowing => box != null && box.enabled;
        public bool IsTyping { get; private set; }
        public string CurrentText => text != null ? text.Text : string.Empty;

        public void Show(string line, float voicePitch)
        {
            pitch = voicePitch;
            SetVisible(true);
            text.WrapWidth = wrapWidth;
            text.Text = line ?? string.Empty;
            text.VisibleCharacters = 0;
            typed = 0f;
            lastBlip = 0;
            IsTyping = text.CharacterCount > 0;
            Layout();
        }

        public void FinishTyping()
        {
            IsTyping = false;
            text.VisibleCharacters = -1;
        }

        public void Hide()
        {
            IsTyping = false;
            SetVisible(false);
        }

        private void Awake()
        {
            SetVisible(false);
        }

        private void Update()
        {
            if (!IsShowing)
            {
                return;
            }

            if (IsTyping)
            {
                typed += Time.deltaTime * charactersPerSecond;
                int shown = Mathf.Min(text.CharacterCount, Mathf.FloorToInt(typed));
                text.VisibleCharacters = shown;
                if (shown - lastBlip >= blipEvery)
                {
                    lastBlip = shown;
                    if (sfx != null)
                    {
                        sfx.Play(SfxId.Blip, pitch * Random.Range(0.94f, 1.06f), 0.8f);
                    }
                }

                if (shown >= text.CharacterCount)
                {
                    FinishTyping();
                }
            }

            if (moreArrow != null)
            {
                moreArrow.enabled = !IsTyping && Mathf.FloorToInt(Time.time * 2.5f) % 2 == 0;
            }
        }

        private void Layout()
        {
            // measure the full line (not the typed part) so the bubble doesn't grow while typing
            int visible = text.VisibleCharacters;
            text.VisibleCharacters = -1;
            Vector2Int size = text.Size;
            text.VisibleCharacters = visible;

            int width = size.x + padding.x * 2;
            int height = size.y + padding.y * 2 + 4;
            box.drawMode = SpriteDrawMode.Sliced;
            box.size = new Vector2(width, height);
            text.transform.localPosition = new Vector3(padding.x, -padding.y, 0f);
            if (tail != null)
            {
                tail.transform.localPosition = new Vector3(14f, -height + 1f, 0f);
            }

            if (moreArrow != null)
            {
                moreArrow.transform.localPosition = new Vector3(width - 10f, -height + 6f, 0f);
            }
        }

        private void SetVisible(bool visible)
        {
            if (box != null)
            {
                box.enabled = visible;
            }

            if (tail != null)
            {
                tail.enabled = visible;
            }

            if (moreArrow != null)
            {
                moreArrow.enabled = false;
            }

            if (text != null)
            {
                text.gameObject.SetActive(visible);
            }
        }
    }
}

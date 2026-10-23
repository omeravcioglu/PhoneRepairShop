using System;
using PhoneRepair.Controls;
using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>A little pixel ring next to the cursor that fills while you hold an action or turn a screw.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ProgressRing : MonoBehaviour
    {
        [SerializeField] private BenchRepair repair;
        [SerializeField] private GameInput input;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Frames from empty to full.")]
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField] private Vector2 offset = new(-14f, 12f);

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            bool visible = repair != null && repair.ShowsProgress && frames.Length > 0 && input != null && viewCamera != null;
            spriteRenderer.enabled = visible;
            if (!visible)
            {
                return;
            }

            int frame = Mathf.Clamp(Mathf.FloorToInt(repair.ActiveProgress * (frames.Length - 1) + 0.0001f), 0, frames.Length - 1);
            spriteRenderer.sprite = frames[frame];
            Vector2 pointer = input.PointerPosition;
            Vector3 world = viewCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, 0f));
            transform.position = PixelGrid.Snap(new Vector3(world.x + offset.x, world.y + offset.y, transform.position.z));
        }
    }
}

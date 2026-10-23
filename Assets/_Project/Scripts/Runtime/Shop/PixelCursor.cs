using PhoneRepair.Controls;
using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// The pixel mouse cursor: a hand in the shop, the tool in hand at the bench (its pivot is the
    /// working tip). Drawn in the world so it is pixelated like everything else; hides the OS cursor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelCursor : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField] private CameraRig2D rig;
        [SerializeField] private Sprite hand;
        [SerializeField] private Sprite handPressed;
        [SerializeField] private bool hideSystemCursor = true;

        private SpriteRenderer spriteRenderer;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (hideSystemCursor && Application.isPlaying && !Application.isBatchMode)
            {
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            if (hideSystemCursor && Application.isPlaying && !Application.isBatchMode)
            {
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            if (input == null || viewCamera == null)
            {
                return;
            }

            Vector2 pointer = input.PointerPosition;
            Vector3 world = viewCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, 0f));
            transform.position = PixelGrid.Snap(new Vector3(world.x, world.y, transform.position.z));

            Sprite sprite = input.PrimaryHeld && handPressed != null ? handPressed : hand;
            bool atBench = rig != null && rig.Current == CameraRig2D.View.Bench && !rig.IsSliding;
            if (atBench && toolBelt != null && toolBelt.Current != null && toolBelt.Current.Cursor != null)
            {
                sprite = toolBelt.Current.Cursor;
            }

            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}

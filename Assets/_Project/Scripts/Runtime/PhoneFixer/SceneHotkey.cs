using UnityEngine;
using UnityEngine.InputSystem;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Goes to another scene on a key or a UI button (Go). The bench uses B to return to the shop,
    /// because Esc already means "deselect" there.
    /// </summary>
    public sealed class SceneHotkey : MonoBehaviour
    {
        [SerializeField] private string sceneName = "PF_Shop";
        [SerializeField] private Key key = Key.B;

        public string SceneName => sceneName;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame)
            {
                Go();
            }
        }

        public void Go() => SceneFader.LoadScene(sceneName);
    }
}

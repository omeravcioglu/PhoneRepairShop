using PhoneRepair.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// A clickable place in the shop view (workbench, counter, shelf, upgrade slot, the cat).
    /// Hovering glows orange ("you can pick it"); clicking fires the event and, if set, loads a scene.
    /// Needs an <see cref="Interactable"/> and a collider on itself or a child.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class ShopStation : MonoBehaviour
    {
        [Tooltip("What clicking does, shown under the name in the hover label.")]
        [SerializeField] private string verb = "Open";
        [Tooltip("Scene loaded when the station is used. Empty = only raise the event.")]
        [SerializeField] private string sceneToLoad;
        [SerializeField] private UnityEvent onUsed = new();

        public string Verb => verb;
        public string SceneToLoad => sceneToLoad;
        public UnityEvent Used => onUsed;

        public void Use()
        {
            onUsed.Invoke();
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneFader.LoadScene(sceneToLoad);
            }
        }
    }
}

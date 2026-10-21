using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Shows a phone's visible damage. The Blender generators export every damage state as a child
    /// mesh of its part, named &lt;Part&gt;_&lt;State&gt; (Screen_Crack, Battery_Bulge, ...). This switches them
    /// on and off, so a fault system (Milestone 3) only has to call <see cref="SetState"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhoneFaultVisuals : MonoBehaviour
    {
        public static readonly string[] KnownStates =
        {
            "Screen_Crack", "Battery_Bulge", "Battery_Warning", "ChargingPort_Lint",
            "Logicboard_Corrosion", "DisplayConnector_Tear", "RearCamera_Scratch",
        };

        [Tooltip("Damage meshes that are visible. Every other entry of KnownStates is hidden.")]
        [SerializeField] private List<string> activeStates = new();

        public IReadOnlyList<string> ActiveStates => activeStates;

        private void Awake() => Apply();

        public bool IsShown(string state) => activeStates.Contains(state);

        public void SetState(string state, bool visible)
        {
            activeStates.Remove(state);
            if (visible)
            {
                activeStates.Add(state);
            }

            Apply();
        }

        /// <summary>Activates exactly the meshes listed in the active states. Safe to call in edit mode.</summary>
        public void Apply()
        {
            foreach (string state in KnownStates)
            {
                Transform node = FindDeep(transform, state);
                if (node != null)
                {
                    node.gameObject.SetActive(activeStates.Contains(state));
                }
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                Transform t = queue.Dequeue();
                if (t.name == name)
                {
                    return t;
                }

                foreach (Transform child in t)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }
    }
}

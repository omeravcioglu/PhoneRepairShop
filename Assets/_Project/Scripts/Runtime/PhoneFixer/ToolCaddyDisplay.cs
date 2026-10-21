using System.Collections.Generic;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// The tools standing in the bench caddy. The one in your hand leaves its slot, so the caddy always
    /// shows what you could pick up next. Props without a tool entry (tweezers, brush...) just stay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolCaddyDisplay : MonoBehaviour
    {
        [SerializeField] private ToolBelt toolBelt;
        [Tooltip("Same order as props.")]
        [SerializeField] private List<ToolDefinition> tools = new();
        [SerializeField] private List<GameObject> props = new();

        private void OnEnable()
        {
            if (toolBelt != null)
            {
                toolBelt.ToolChanged += Refresh;
                Refresh(toolBelt.Current);
            }
        }

        private void OnDisable()
        {
            if (toolBelt != null)
            {
                toolBelt.ToolChanged -= Refresh;
            }
        }

        private void Refresh(ToolDefinition current)
        {
            for (int i = 0; i < props.Count && i < tools.Count; i++)
            {
                if (props[i] != null)
                {
                    props[i].SetActive(tools[i] == null || tools[i] != current);
                }
            }
        }
    }
}

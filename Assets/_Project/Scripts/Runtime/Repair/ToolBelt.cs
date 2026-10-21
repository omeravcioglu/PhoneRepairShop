using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The tools the player owns, in hotkey order (1, 2, 3...), and which one is in hand.
    /// The first entry is the default (Hands). Milestone 5 adds tools bought in the shop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolBelt : MonoBehaviour
    {
        [SerializeField] private List<ToolDefinition> tools = new();

        private int currentIndex;

        public event Action<ToolDefinition> ToolChanged;

        public IReadOnlyList<ToolDefinition> Tools => tools;
        public int CurrentIndex => tools.Count == 0 ? -1 : Mathf.Clamp(currentIndex, 0, tools.Count - 1);
        public ToolDefinition Current => tools.Count == 0 ? null : tools[CurrentIndex];
        public ToolKind CurrentKind => Current != null ? Current.Kind : ToolKind.Hands;

        public void Equip(int index)
        {
            if (index < 0 || index >= tools.Count || index == CurrentIndex)
            {
                return;
            }

            currentIndex = index;
            ToolChanged?.Invoke(Current);
        }

        public void Equip(ToolDefinition tool) => Equip(tools.IndexOf(tool));

        public bool TryFind(ToolKind kind, out ToolDefinition tool, out int index)
        {
            for (int i = 0; i < tools.Count; i++)
            {
                if (tools[i] != null && tools[i].Kind == kind)
                {
                    tool = tools[i];
                    index = i;
                    return true;
                }
            }

            tool = null;
            index = -1;
            return false;
        }

        /// <summary>"Screwdriver [2]", or a gentle note when the player doesn't own that kind yet.</summary>
        public string Describe(ToolKind kind)
        {
            return TryFind(kind, out ToolDefinition tool, out int index)
                ? $"{tool.DisplayName} [{index + 1}]"
                : $"a {kind} (not owned yet)";
        }
    }
}

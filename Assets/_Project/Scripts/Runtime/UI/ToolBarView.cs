using System.Collections.Generic;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.UI
{
    /// <summary>
    /// Bottom tool bar built from the ToolBelt at runtime (so bought tools appear automatically).
    /// Click a button or press its number to take that tool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolBarView : MonoBehaviour
    {
        [SerializeField] private ToolBelt toolBelt;
        [Tooltip("Inactive button used as the template for each tool.")]
        [SerializeField] private ToolButtonView template;
        [SerializeField] private RectTransform container;

        [Header("Look")]
        [SerializeField] private Color normalBackground = new(0.17f, 0.14f, 0.2f, 0.82f);
        [SerializeField] private Color selectedBackground = new(1f, 0.97f, 0.94f, 0.98f);
        [SerializeField] private Color normalText = new(1f, 0.97f, 0.94f);
        [SerializeField] private Color selectedText = new(0.23f, 0.2f, 0.27f);
        [SerializeField, Range(1f, 1.3f)] private float selectedScale = 1.08f;

        private readonly List<ToolButtonView> buttons = new();

        private void Start()
        {
            if (toolBelt == null || template == null)
            {
                return;
            }

            template.gameObject.SetActive(false);
            RectTransform parent = container != null ? container : (RectTransform)template.transform.parent;
            for (int i = 0; i < toolBelt.Tools.Count; i++)
            {
                ToolDefinition tool = toolBelt.Tools[i];
                ToolButtonView button = Instantiate(template, parent);
                button.name = $"Tool_{i + 1}_{(tool != null ? tool.DisplayName : "Empty")}";
                button.gameObject.SetActive(true);
                button.Bind(i + 1, tool != null ? tool.DisplayName : "?");
                int index = i;
                button.Button.onClick.AddListener(() => toolBelt.Equip(index));
                buttons.Add(button);
            }

            toolBelt.ToolChanged += OnToolChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (toolBelt != null)
            {
                toolBelt.ToolChanged -= OnToolChanged;
            }
        }

        private void OnToolChanged(ToolDefinition tool) => Refresh();

        private void Refresh()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                bool selected = i == toolBelt.CurrentIndex;
                buttons[i].SetLook(selected ? selectedBackground : normalBackground, selected ? selectedText : normalText,
                    selected ? selectedScale : 1f);
            }
        }
    }
}

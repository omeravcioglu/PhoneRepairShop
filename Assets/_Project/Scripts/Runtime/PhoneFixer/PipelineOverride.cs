using UnityEngine;
using UnityEngine.Rendering;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Uses another URP asset while this scene is loaded. The shop diorama needs ~30 m shadow
    /// distance; the repair bench keeps the default asset's 4 m for crisp millimetre shadows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PipelineOverride : MonoBehaviour
    {
        [SerializeField] private RenderPipelineAsset pipeline;

        private RenderPipelineAsset previous;
        private bool applied;

        public RenderPipelineAsset Pipeline => pipeline;

        private void OnEnable()
        {
            if (pipeline == null)
            {
                return;
            }

            previous = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = pipeline;
            applied = true;
        }

        private void OnDisable()
        {
            if (!applied)
            {
                return;
            }

            QualitySettings.renderPipeline = previous;
            applied = false;
        }
    }
}

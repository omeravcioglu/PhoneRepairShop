using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Cream fade between the shop and the bench. Fades in when the scene starts; LoadScene fades out,
    /// then loads. Without a fader in the scene, LoadScene just loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.35f;

        private static SceneFader instance;
        private bool busy;

        public static void LoadScene(string sceneName)
        {
            if (instance != null && instance.isActiveAndEnabled)
            {
                instance.StartCoroutine(instance.FadeAndLoad(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void Awake()
        {
            instance = this;
            if (overlay != null)
            {
                overlay.alpha = 1f;
                overlay.blocksRaycasts = false;
            }
        }

        private IEnumerator Start()
        {
            yield return Fade(1f, 0f);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            if (busy)
            {
                yield break;
            }

            busy = true;
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (overlay == null)
            {
                yield break;
            }

            overlay.blocksRaycasts = to > 0.5f;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                overlay.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }

            overlay.alpha = to;
        }
    }
}

using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>Targets for UI buttons wired in the editor (persistent listeners need instance methods).</summary>
    public sealed class UiActions : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f; // leaving from the pause menu must not freeze the next scene
            SceneFader.LoadScene(sceneName);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

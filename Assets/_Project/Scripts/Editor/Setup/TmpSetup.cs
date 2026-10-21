using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PhoneRepair.EditorTools
{
    /// <summary>Makes sure TextMeshPro's essential resources (default font + settings) are imported.</summary>
    internal static class TmpSetup
    {
        public static void EnsureEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            {
                return;
            }

            var ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            string package = ugui != null
                ? Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage")
                : null;
            if (package == null || !File.Exists(package))
            {
                throw new FileNotFoundException("TMP Essential Resources package not found. Use Window > TextMeshPro > Import TMP Essential Resources.");
            }

            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                Debug.LogWarning("[PhoneRepair] TMP Essential Resources import was queued; run the setup again if UI text is missing.");
            }
        }
    }
}

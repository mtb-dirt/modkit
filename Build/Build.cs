using UnityEditor;
using UnityEngine;

namespace Build
{
    public class Build : EditorWindow
    {
        [MenuItem("Modkit/Build", false, 1)]
        public static void BuildAssetBundle()
        {
            Caching.ClearCache();
            BuildPipeline.BuildAssetBundles("Assets", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        }
    }
}
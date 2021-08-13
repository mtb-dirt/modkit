using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace Editor.Build
{
    public class Build : EditorWindow
    {
        private string _name;
        private string _path;
        private string _author;

        private const string CompanyName = "Lucas Loeffel";
        private const string ProductName = "MTB Dirt";

        [MenuItem("Modkit/Build", false, 1)]
        private static void OpenWindow()
        {
            GetWindow<Build>(true);
        }

        private void OnGUI()
        {
            _author = EditorGUILayout.TextField("Author", _author);
            EditorGUILayout.LabelField("Map name", _name = SceneManager.GetActiveScene().name);
            EditorGUILayout.LabelField("Output path", _path = BundlePath(Application.persistentDataPath));

            if (!GUILayout.Button("Build"))
            {
                return;
            }

            if (!FindPlayer())
            {
                EditorUtility.DisplayDialog("No player", "Player is required", "Ok");
                return;
            }

            if (_author.Length == 0)
            {
                EditorUtility.DisplayDialog("No author", "Author is required", "Ok");
                return;
            }

            BuildAssetBundle();
        }

        private static GameObject FindPlayer()
        {
            foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject.name != "Player")
                {
                    continue;
                }

                return gameObject;
            }

            return null;
        }

        private void OnEnable()
        {
            _author = EditorPrefs.GetString(EditorPrefsKey("Author"));
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(EditorPrefsKey("Author"), _author);
        }

        private static string EditorPrefsKey(string key)
        {
            return $"Modkit.{CompanyName}.{ProductName}.{key}";
        }

        private static string BundlePath(string persistentDataPath)
        {
            var count = 0;
            var index = 0;

            for (var i = persistentDataPath.Length - 1; i > 0; i--)
            {
                if (persistentDataPath[i] != Path.AltDirectorySeparatorChar)
                {
                    continue;
                }

                if (count != 1)
                {
                    count++;
                    continue;
                }

                index = i;
                break;
            }

            return $"{persistentDataPath.Substring(0, index)}{Path.AltDirectorySeparatorChar}{CompanyName}{Path.AltDirectorySeparatorChar}{ProductName}{Path.AltDirectorySeparatorChar}{SceneManager.GetActiveScene().name}";
        }

        private void BuildAssetBundle()
        {
            Caching.ClearCache();

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            FindPlayer().SetActive(false);
            BuildPipeline.BuildAssetBundles(_path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            FindPlayer().SetActive(true);
        }
    }
}
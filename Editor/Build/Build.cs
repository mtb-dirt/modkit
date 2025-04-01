using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Protobuf;

namespace Modkit.Editor.Build
{
    public class Build : EditorWindow
    {
        private string _path;

        private string _name;
        private string _author;
        private string _description;
        private string _version;

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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Description", GUILayout.MaxWidth(150.0f));
            _description = EditorGUILayout.TextArea(_description, GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();
            _version = EditorGUILayout.TextField("Version", _version);
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

            if (_description.Length == 0)
            {
                EditorUtility.DisplayDialog("No description", "Description is required", "Ok");
                return;
            }

            if (_version.Length == 0 || !Semver.Semver.Validate(_version))
            {
                EditorUtility.DisplayDialog("No version", "Version is required and needs to be a semver version (semver.org)", "Ok");
                return;
            }

            ClearCache();
            CreateDirectory();
            BuildJson();
            BuildAssetBundle();

            EditorUtility.DisplayDialog("Done", "Built successfully", "Ok");
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
            _description = EditorPrefs.GetString(EditorPrefsKey("Description"));
            _version = EditorPrefs.GetString(EditorPrefsKey("Version"));
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(EditorPrefsKey("Author"), _author);
            EditorPrefs.SetString(EditorPrefsKey("Description"), _description);
            EditorPrefs.SetString(EditorPrefsKey("Version"), _version);
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

            return $"{persistentDataPath.Substring(0, index)}{Path.AltDirectorySeparatorChar}{CompanyName}{Path.AltDirectorySeparatorChar}{ProductName}{Path.AltDirectorySeparatorChar}Mods{Path.AltDirectorySeparatorChar}{SceneManager.GetActiveScene().name}";
        }

        private static string JsonPath(string persistentDataPath)
        {
            return $"{persistentDataPath}{Path.AltDirectorySeparatorChar}scene.json";
        }

        private static void ClearCache()
        {
            Caching.ClearCache();
        }

        private void CreateDirectory()
        {
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
        }

        private void BuildJson()
        {
            var player = FindPlayer();
            var playerPosition = player.transform.position;
            var playerRotation = player.transform.rotation.eulerAngles;
            var mod = new Mod
            {
                Name = _name,
                Description = _description.Trim(),
                Version = _version.Trim(),
                Author = _author.Trim(),

                Player = new Player
                {
                    Position = new Xyz
                    {
                        X = playerPosition.x,
                        Y = playerPosition.y,
                        Z = playerPosition.z
                    },
                    Rotation = new Xyz
                    {
                        X = playerRotation.x,
                        Y = playerRotation.y,
                        Z = playerRotation.z
                    }
                }
            };

            using var writer = File.CreateText(JsonPath(_path));
            writer.WriteLine(mod.ToString());
            writer.Close();
        }

        private void BuildAssetBundle()
        {
            FindPlayer().SetActive(false);
            BuildPipeline.BuildAssetBundles(_path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            FindPlayer().SetActive(true);
        }
    }
}

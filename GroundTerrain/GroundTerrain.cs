using UnityEditor;
using UnityEngine;

namespace GroundTerrain
{
    public class GroundTerrain : EditorWindow
    {
        private int _resolution = 2049;

        [MenuItem("MTB Dirt/Terrain/Ground", false, 2000)]
        private static void OpenWindow()
        {
            GetWindow<GroundTerrain>(true);
        }

        private void OnGUI()
        {
            _resolution = EditorGUILayout.IntField("Resolution", _resolution);

            if (!GUILayout.Button("Create Terrain"))
            {
                return;
            }

            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No object selected", "Please select an object.", "Ok");
                return;
            }

            CreateTerrain();
        }

        private delegate void CleanUp();

        private void CreateTerrain()
        {
            ShowProgressBar(1, 100);

            var terrain = new TerrainData {heightmapResolution = _resolution};
            var terrainObject = Terrain.CreateTerrainGameObject(terrain);

            Undo.RegisterCreatedObjectUndo(terrainObject, "Revert Terrain");

            var collider = Selection.activeGameObject.GetComponent<MeshCollider>();
            CleanUp cleanUp = null;

            if (!collider)
            {
                collider = Selection.activeGameObject.AddComponent<MeshCollider>();
                cleanUp = () => DestroyImmediate(collider);
            }

            var bounds = collider.bounds;
            terrain.size = bounds.size;
            bounds.size = new Vector3(terrain.size.x, bounds.size.y, terrain.size.z);

            // Do raycasting samples over the object to see what terrain heights should be
            var heights = new float[terrain.heightmapResolution, terrain.heightmapResolution];
            var ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
            var hit = new RaycastHit();
            var meshHeightInverse = 1 / bounds.size.y;
            var rayOrigin = ray.origin;

            var maxHeight = heights.GetLength(0);
            var maxLength = heights.GetLength(1);

            var stepXZ = new Vector2(bounds.size.x / maxLength, bounds.size.z / maxHeight);

            for (var zCount = 0; zCount < maxHeight; zCount++)
            {
                ShowProgressBar(zCount, maxHeight);

                for (var xCount = 0; xCount < maxLength; xCount++)
                {
                    var height = 0.0f;

                    if (collider.Raycast(ray, out hit, bounds.size.y * 3))
                    {
                        height = (hit.point.y - bounds.min.y) * meshHeightInverse;

                        if (height < 0)
                        {
                            height = 0;
                        }
                    }

                    heights[zCount, xCount] = height;
                    rayOrigin.x += stepXZ[0];
                    ray.origin = rayOrigin;
                }

                rayOrigin.z += stepXZ[1];
                rayOrigin.x = bounds.min.x;
                ray.origin = rayOrigin;
            }

            terrain.SetHeights(0, 0, heights);
            EditorUtility.ClearProgressBar();
            cleanUp?.Invoke();
        }

        private static void ShowProgressBar(float progress, float maxProgress)
        {
            var p = progress / maxProgress;
            EditorUtility.DisplayProgressBar("Creating Terrain...", Mathf.RoundToInt(p * 100f) + " %", p);
        }
    }
}
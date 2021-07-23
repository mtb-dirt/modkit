using UnityEditor;
using UnityEngine;

namespace StampMesh
{
    public class MeshTerrain : EditorWindow
    {
        private bool _additive = true;

        private Terrain _terrain;

        [MenuItem("MTB Dirt/Terrain/Stamp mesh", false, 2001)]
        private static void OpenWindow()
        {
            GetWindow<MeshTerrain>(true);
        }

        private void OnGUI()
        {
            _additive = EditorGUILayout.Toggle("Additive", _additive);
            _terrain = FindObjectOfType<Terrain>();
            ;

            if (!GUILayout.Button("Stamp mesh"))
            {
                return;
            }

            if (!_terrain)
            {
                EditorUtility.DisplayDialog("No terrain", "Please create a terrain.", "Ok");
                return;
            }

            if (!Selection.activeGameObject)
            {
                EditorUtility.DisplayDialog("No object selected", "Please select an object.", "Ok");
                return;
            }

            StampTerrain();
        }

        private void StampTerrain()
        {
            ShowProgressBar(1, 100);

            var collider = Selection.activeGameObject.GetComponent<MeshCollider>();
            CleanUp cleanUp = null;

            if (!collider)
            {
                collider = Selection.activeGameObject.AddComponent<MeshCollider>();
                cleanUp = () => DestroyImmediate(collider);
            }

            if (!_terrain.GetComponent<Collider>().bounds.Intersects(collider.bounds))
            {
                EditorUtility.DisplayDialog("No intersection", "Terrain collider and mesh collider do not intersect", "Ok");

                Clear(cleanUp);
                return;
            }

            var parent = collider.transform.parent;

            if (parent)
            {
                collider.transform.parent = null;
            }

            var colliderBounds = collider.bounds;
            var terrainData = _terrain.terrainData;
            var terrainPos = _terrain.transform.position;
            var terrainSize = terrainData.size;
            var tMin = Vector2.zero;
            var tMax = Vector2.zero;
            tMin.x = (colliderBounds.min.x - terrainPos.x) / terrainSize.x;
            tMin.y = (colliderBounds.min.z - terrainPos.z) / terrainSize.z;
            tMax.x = (colliderBounds.max.x - terrainPos.x) / terrainSize.x;
            tMax.y = (colliderBounds.max.z - terrainPos.z) / terrainSize.z;

            var minIndexX = Mathf.FloorToInt(tMin.x * terrainData.heightmapResolution);
            var minIndexY = Mathf.FloorToInt(tMin.y * terrainData.heightmapResolution);
            var maxIndexX = Mathf.CeilToInt(tMax.x * terrainData.heightmapResolution);
            var maxIndexY = Mathf.CeilToInt(tMax.y * terrainData.heightmapResolution);

            minIndexX = Mathf.Clamp(minIndexX, 0, terrainData.heightmapResolution - 1);
            minIndexY = Mathf.Clamp(minIndexY, 0, terrainData.heightmapResolution - 1);
            maxIndexX = Mathf.Clamp(maxIndexX, 0, terrainData.heightmapResolution - 1);
            maxIndexY = Mathf.Clamp(maxIndexY, 0, terrainData.heightmapResolution - 1);

            var mapWidth = maxIndexX - minIndexX;
            var mapHeight = maxIndexY - minIndexY;

            var heights = _terrain.terrainData.GetHeights(minIndexX, minIndexY, mapWidth, mapHeight);

            for (var iz = 0; iz < mapHeight; iz++)
            {
                ShowProgressBar(iz, mapHeight);

                for (var ix = 0; ix < mapWidth; ix++)
                {
                    var originalHeight = heights[iz, ix];
                    var height = terrainPos.y + originalHeight * _terrain.terrainData.size.y;
                    var worldPos = terrainPos;

                    worldPos.x += terrainData.size.x * (minIndexX + ix) / (terrainData.heightmapResolution - 1);
                    worldPos.y += height;
                    worldPos.z += terrainData.size.z * (minIndexY + iz) / (terrainData.heightmapResolution - 1);

                    var strength = 1.0f;
                    var newHeight = height;

                    var rayOrigin = worldPos;
                    rayOrigin.y = _terrain.transform.position.y + terrainData.size.y;
                    var ray = new Ray(rayOrigin, Vector3.down);

                    if (collider.Raycast(ray, out var hit, terrainData.size.y * 10))
                    {
                        newHeight = hit.point.y - strength;
                    }
                    
                    Debug.Log(strength);

                    if (_additive)
                    {
                        newHeight = Mathf.Max(height, newHeight);
                    }

                    var targetHeight = GetHeightInTerrainSpace(newHeight, _terrain);
                    heights[iz, ix] = Mathf.Lerp(originalHeight, targetHeight, strength);
                }
            }

            terrainData.SetHeights(minIndexX, minIndexY, heights);
            collider.transform.parent = parent;

            Clear(cleanUp);
        }

        // public static string MoldToMesh(Terrain terrain, MeshCollider collider, bool MoldAdditive, float saftyMargin, float offset, bool strengthFromColor, bool doNotAddHeight = false, bool invertStrength = false)
        // {
        //     if (collider == null)
        //         return "error no collider";
        //
        //     Transform parent = collider.transform.parent;
        //     if (parent != null)
        //         collider.transform.parent = null;
        //
        //     if (!terrain.GetComponent<Collider>().bounds.Intersects(collider.bounds))
        //         return ".";
        //
        //
        //     Vector3 terrainPos = terrain.transform.position;
        //     Vector3 terrainSize = terrain.terrainData.size;
        //     Vector2 tmin = Vector2.zero;
        //     Vector2 tmax = Vector2.zero;
        //     tmin.x = (collider.bounds.min.x - terrainPos.x) / terrainSize.x;
        //     tmin.y = (collider.bounds.min.z - terrainPos.z) / terrainSize.z;
        //     tmax.x = (collider.bounds.max.x - terrainPos.x) / terrainSize.x;
        //     tmax.y = (collider.bounds.max.z - terrainPos.z) / terrainSize.z;
        //
        //     int minIndexX = Mathf.FloorToInt(tmin.x * terrain.terrainData.heightmapResolution - saftyMargin);
        //     int minIndexY = Mathf.FloorToInt(tmin.y * terrain.terrainData.heightmapResolution - saftyMargin);
        //     int maxIndexX = Mathf.CeilToInt(tmax.x * terrain.terrainData.heightmapResolution + saftyMargin);
        //     int maxIndexY = Mathf.CeilToInt(tmax.y * terrain.terrainData.heightmapResolution + saftyMargin);
        //
        //     //Make sure rectangle is inside terrain
        //     minIndexX = Mathf.Clamp(minIndexX, 0, terrain.terrainData.heightmapResolution - 1);
        //     minIndexY = Mathf.Clamp(minIndexY, 0, terrain.terrainData.heightmapResolution - 1);
        //     maxIndexX = Mathf.Clamp(maxIndexX, 0, terrain.terrainData.heightmapResolution - 1);
        //     maxIndexY = Mathf.Clamp(maxIndexY, 0, terrain.terrainData.heightmapResolution - 1);
        //
        //     int mapWidth = maxIndexX - minIndexX;
        //     int mapHeight = maxIndexY - minIndexY;
        //
        //     float[,] heights = terrain.terrainData.GetHeights(minIndexX, minIndexY, mapWidth, mapHeight);
        //     Vector3 worldPos;
        //
        //     for (int iz = 0; iz < mapHeight; iz++)
        //     {
        //         for (int ix = 0; ix < mapWidth; ix++)
        //         {
        //             float originalHeight = heights[iz, ix];
        //             float height = terrainPos.y + originalHeight * terrain.terrainData.size.y;
        //             worldPos = terrainPos;
        //
        //             worldPos.x += terrain.terrainData.size.x * ((float) (minIndexX + ix)) / ((float) (terrain.terrainData.heightmapResolution - 1));
        //             worldPos.y += height;
        //             worldPos.z += terrain.terrainData.size.z * ((float) (minIndexY + iz)) / ((float) (terrain.terrainData.heightmapResolution - 1));
        //
        //
        //             float strength = 1;
        //             float newHeight = height;
        //
        //             Vector3 rayOrigin = worldPos;
        //             rayOrigin.y = terrain.transform.position.y + terrain.terrainData.size.y;
        //             Ray ray = new Ray(rayOrigin, Vector3.down);
        //             RaycastHit hit;
        //
        //             if (collider.Raycast(ray, out hit, terrain.terrainData.size.y * 10))
        //             {
        //                 bool hasError = false;
        //                 try
        //                 {
        //                     if (strengthFromColor && collider.sharedMesh.colors != null && collider.sharedMesh.triangles.Length > hit.triangleIndex * 3 + 3)
        //                     {
        //                         int[] triangles = collider.sharedMesh.triangles;
        //                         float[] barys = new float[3];
        //                         barys[0] = hit.barycentricCoordinate.x;
        //                         barys[1] = hit.barycentricCoordinate.y;
        //                         barys[2] = hit.barycentricCoordinate.z;
        //
        //                         float tsmooth = 0;
        //                         for (int i = 0; i < 3; i++)
        //                         {
        //                             tsmooth += collider.sharedMesh.colors[triangles[hit.triangleIndex * 3 + i]].a * barys[i];
        //                         }
        //
        //                         strength = tsmooth;
        //                     }
        //                     else
        //                     {
        //                         strength = 1;
        //                     }
        //                 }
        //                 catch
        //                 {
        //                     hasError = true;
        //                 }
        //
        //                 if (hasError)
        //                     strength = 1;
        //
        //                 if (invertStrength)
        //                     strength = 1 - strength;
        //                 newHeight = hit.point.y - offset * strength;
        //             }
        //             else
        //             {
        //                 //newHeight = 1;
        //             }
        //
        //             if (MoldAdditive)
        //             {
        //                 newHeight = Mathf.Max(height, newHeight);
        //             }
        //
        //             float targetHeight = TerrainManager.GetHeightInTerrainSpace(newHeight, terrain);
        //
        //             if (doNotAddHeight)
        //                 targetHeight = Mathf.Min(originalHeight, targetHeight);
        //             heights[iz, ix] = Mathf.Lerp(originalHeight, targetHeight, strength);
        //         }
        //     }
        //
        //     terrain.terrainData.SetHeights(minIndexX, minIndexY, heights);
        //     collider.transform.parent = parent;
        //     return "ok";
        // }

        private delegate void CleanUp();

        private static void Clear(CleanUp cleanUp)
        {
            EditorUtility.ClearProgressBar();
            cleanUp?.Invoke();
        }

        private static float GetHeightInTerrainSpace(float height, Terrain terrain)
        {
            return (height - terrain.transform.position.y) / terrain.terrainData.size.y;
        }

        private static void ShowProgressBar(float progress, float maxProgress)
        {
            var p = progress / maxProgress;
            EditorUtility.DisplayProgressBar("Stamp mesh...", Mathf.RoundToInt(p * 100f) + " %", p);
        }
    }
}
using UnityEngine;

/// <summary>
/// Randomly scatters one or more prefabs (e.g. palm trees, rocks, bushes)
/// across a Unity Terrain, snapping each spawned object to the terrain's
/// height at that point.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g. "TreeScatterer").
/// 2. Attach this script to it.
/// 3. Drag your Terrain object into the "Terrain" field in the Inspector.
/// 4. Drag one or more tree/rock prefabs into the "Prefabs To Scatter" list.
/// 5. Adjust "Number Of Objects" and other settings as needed.
/// 6. Right-click this script in the Inspector and choose "Scatter Objects Now"
///    (or press Play and it will scatter automatically if "Scatter On Start" is checked).
/// </summary>
public class TerrainObjectScatterer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Terrain to scatter objects on.")]
    public Terrain terrain;

    [Tooltip("One or more prefabs to randomly place (e.g. palm tree variants).")]
    public GameObject[] prefabsToScatter;

    [Header("Scatter Settings")]
    [Tooltip("How many objects to place in total.")]
    public int numberOfObjects = 100;

    [Tooltip("Minimum distance between scattered objects (rough, not perfectly enforced).")]
    public float minSpacing = 2f;

    [Tooltip("Random Y-axis rotation range (degrees) applied to each object.")]
    public bool randomizeRotation = true;

    [Tooltip("Randomize scale slightly for natural variation.")]
    public bool randomizeScale = true;
    public Vector2 scaleRange = new Vector2(0.85f, 1.15f);

    [Header("Height / Slope Constraints")]
    [Tooltip("Don't place objects below this normalized terrain height (0 = lowest point, 1 = highest).")]
    [Range(0f, 1f)]
    public float minNormalizedHeight = 0.0f;

    [Tooltip("Don't place objects above this normalized terrain height.")]
    [Range(0f, 1f)]
    public float maxNormalizedHeight = 1.0f;

    [Tooltip("Maximum terrain steepness (in degrees) an object can be placed on. Useful to avoid trees on cliffs.")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 35f;

    [Header("Behaviour")]
    [Tooltip("If true, objects are scattered automatically when the scene starts playing.")]
    public bool scatterOnStart = false;

    [Tooltip("Parent transform to keep the Hierarchy tidy. Leave empty to parent under this object.")]
    public Transform parentForSpawnedObjects;

    private const int MAX_ATTEMPTS_PER_OBJECT = 30;

    private void Start()
    {
        if (scatterOnStart)
        {
            ScatterObjectsNow();
        }
    }

    [ContextMenu("Scatter Objects Now")]
    public void ScatterObjectsNow()
    {
        if (terrain == null)
        {
            Debug.LogError("[TerrainObjectScatterer] No Terrain assigned. Drag a Terrain into the 'Terrain' field.");
            return;
        }

        if (prefabsToScatter == null || prefabsToScatter.Length == 0)
        {
            Debug.LogError("[TerrainObjectScatterer] No prefabs assigned. Add at least one prefab to 'Prefabs To Scatter'.");
            return;
        }

        ClearPreviousObjects();

        Transform parent = parentForSpawnedObjects != null ? parentForSpawnedObjects : this.transform;

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        float width = terrainData.size.x;
        float length = terrainData.size.z;
        float terrainHeight = terrainData.size.y;

        int placed = 0;
        int totalAttempts = 0;
        int maxTotalAttempts = numberOfObjects * MAX_ATTEMPTS_PER_OBJECT;

        System.Collections.Generic.List<Vector3> placedPositions = new System.Collections.Generic.List<Vector3>();

        while (placed < numberOfObjects && totalAttempts < maxTotalAttempts)
        {
            totalAttempts++;

            float randX = Random.Range(0f, width);
            float randZ = Random.Range(0f, length);

            float worldX = terrainPos.x + randX;
            float worldZ = terrainPos.z + randZ;

            float normX = randX / width;
            float normZ = randZ / length;

            float sampledHeight01 = terrainData.GetInterpolatedHeight(normX, normZ) / terrainHeight;

            if (sampledHeight01 < minNormalizedHeight || sampledHeight01 > maxNormalizedHeight)
                continue;

            float slope = terrainData.GetSteepness(normX, normZ);
            if (slope > maxSlopeAngle)
                continue;

            float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainPos.y;
            Vector3 candidatePos = new Vector3(worldX, worldY, worldZ);

            bool tooClose = false;
            foreach (Vector3 existing in placedPositions)
            {
                if (Vector3.Distance(existing, candidatePos) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;

            GameObject prefab = prefabsToScatter[Random.Range(0, prefabsToScatter.Length)];
            GameObject instance = Instantiate(prefab, candidatePos, Quaternion.identity, parent);

            if (randomizeRotation)
            {
                instance.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
            }

            if (randomizeScale)
            {
                float s = Random.Range(scaleRange.x, scaleRange.y);
                instance.transform.localScale *= s;
            }

            placedPositions.Add(candidatePos);
            placed++;
        }

        Debug.Log($"[TerrainObjectScatterer] Placed {placed}/{numberOfObjects} objects after {totalAttempts} attempts.");
    }

    [ContextMenu("Clear Scattered Objects")]
    public void ClearPreviousObjects()
    {
        Transform parent = parentForSpawnedObjects != null ? parentForSpawnedObjects : this.transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
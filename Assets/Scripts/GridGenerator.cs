using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Min(1)] public int n = 3;
    public float cubeSize = 1f;

    [Header("Prefab")]
    public GameObject cubePrefab;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("Assign a cube prefab!");
            return;
        }

        //ClearChildren();

        // Offset to center grid
        float totalSize = n * cubeSize;
        Vector3 offset = new Vector3(totalSize, totalSize, totalSize) / 2f - 
                         Vector3.one * (cubeSize / 2f);

        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        for (int z = 0; z < n; z++)
        {
            Vector3 position = new Vector3(
                x * cubeSize,
                y * cubeSize,
                z * cubeSize
            ) - offset;

            GameObject cube = Instantiate(cubePrefab, transform);
            cube.transform.localPosition = position;
            cube.transform.localScale = Vector3.one * cubeSize;

            cube.name = $"Cube_{x}_{y}_{z}";
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
#endif
                Destroy(transform.GetChild(i).gameObject);
        }
    }
}
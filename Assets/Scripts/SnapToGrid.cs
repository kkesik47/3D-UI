using UnityEngine;

public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;

    void Update()
    {
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("grid");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject cube in cubes)
        {
            float dist = Vector3.Distance(transform.position, cube.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = cube;
            }
        }

        if (nearest != null && minDist < snapDistance)
        {
            transform.position = nearest.transform.position;
            transform.rotation = nearest.transform.rotation;
        }
    }
}
using UnityEngine;

public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;
    [SerializeField] LayerMask placedMask;
    [SerializeField] Renderer[] renderers; 


    void Update()
    {
        bool overlapsPlaced = false;

// Check each collider in this shape
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            // skip disabled or trigger colliders
            if (!c.enabled || c.isTrigger) continue;

            // Use Physics.OverlapBox to see if this collider hits anything on Placed
            var center = c.bounds.center;
            var halfExtents = c.bounds.extents;
            var rotation = c.transform.rotation;

            var shrink = 0.02f; // small padding
            var hits = Physics.OverlapBox(center, halfExtents - Vector3.one * shrink, rotation, LayerMask.GetMask("Placed"));
            foreach (var h in hits)
            {
                // Ignore our own colliders
                if (h.transform.root == transform.root)
                    continue;

                overlapsPlaced = true;
                break;
            }

            if (overlapsPlaced) break;
        }

        if (overlapsPlaced)
        {
            foreach (var r in renderers)
                r.material.color = Color.red;
                Debug.Log("collision detected");
        }
        else
        {
            foreach (var r in renderers)
                r.material.color = Color.green;
        } 
        
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

        if (!overlapsPlaced && nearest != null && minDist < snapDistance)
        {
            transform.position = nearest.transform.position;
            Vector3 euler = transform.eulerAngles;
            euler.x = SnapAngle(euler.x);
            euler.y = SnapAngle(euler.y);
            euler.z = SnapAngle(euler.z);
            transform.rotation = Quaternion.Euler(euler);
            
            //int placedLayer = LayerMask.NameToLayer("Placed");
            //SetLayerRecursively(gameObject, placedLayer);
        }
    }
    
    float SnapAngle(float angle)
    {
        angle = angle % 360f; // keep within 0–360

        if (angle < 45f)
            return 0f;
        else if (angle < 135f)
            return 90f;
        else if (angle < 225f)
            return 180f;
        else if (angle < 315f)
            return 270f;
        else
            return 0f;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    
    public void OnGrabbed()
    {
        // When the user picks up the object -> it's no longer placed
        int defaultLayer = LayerMask.NameToLayer("Default");
        SetLayerRecursively(gameObject, defaultLayer);
    }

    public void OnReleased()
    {
        // When the user releases the object -> check if it's correctly placed
        if (IsOnGrid()) // <- replace with your real "fits on grid" condition later
        {
            int placedLayer = LayerMask.NameToLayer("Placed");
            SetLayerRecursively(gameObject, placedLayer);
        }
        else
        {
            int defaultLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(gameObject, defaultLayer);
        }
    }

    public bool IsOnGrid()
    {
        return true;
    }

}
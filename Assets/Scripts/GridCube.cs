// GridCube.cs
using UnityEngine;

[ExecuteAlways]                 // also runs in the Editor (not just Play)
public class GridCube : MonoBehaviour
{
    [HideInInspector] public Vector3 gridPos;   // auto-filled; no manual editing

    void OnEnable() { UpdateGridPos(); }
#if UNITY_EDITOR
    void Update() { if (!Application.isPlaying) UpdateGridPos(); }
#endif
    void OnValidate() { UpdateGridPos(); }

    void UpdateGridPos()
    {
        var gm = GetComponentInParent<GridManager>();
        if (!gm) return;

        var origin = gm.transform.position;
        var cs = Mathf.Approximately(gm.cellSize, 0f) ? 1f : gm.cellSize;

        // world → grid (float grid; add Mathf.Round if you want integers)
        Vector3 local = transform.position - origin;
        gridPos = new Vector3(local.x / cs, local.y / cs, local.z / cs);
    }
}

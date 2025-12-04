using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public float cellSize = 1f;
    [Tooltip("Max distance to accept a 'nearest' grid cell")]
    public float nearestSearchRadius;
    private Vector3 gridOrigin;
    
    private readonly Dictionary<Vector3, GridCube> cubeAt = new();
    private readonly Dictionary<Vector3, bool> occupied = new();
    public readonly List<Vector3> allGridPositions = new();

    private void Awake()
    {
        Instance = this;
        gridOrigin = transform.position;
        nearestSearchRadius = cellSize * 0.6f;
        AssignGridCubes();
    }

    void AssignGridCubes()
    {
        cubeAt.Clear();
        occupied.Clear();
        allGridPositions.Clear();

        var allCubes = GetComponentsInChildren<GridCube>();
        foreach (var cube in allCubes)
        {
            Vector3 local = cube.transform.position - gridOrigin;
            Vector3 gpos = new Vector3(local.x / cellSize, local.y / cellSize, local.z / cellSize);

            cube.gridPos = gpos;
            cubeAt[gpos] = cube;
            occupied[gpos] = false;
            allGridPositions.Add(gpos);
        }
    }

    public bool CellExists(Vector3 gpos) => cubeAt.ContainsKey(gpos);
    public bool IsOccupied(Vector3 gpos) => occupied.TryGetValue(gpos, out var v) && v;
    public void SetOccupied(Vector3 gpos, bool value) { if (cubeAt.ContainsKey(gpos)) occupied[gpos] = value; }

    public bool IsGridFull()
    {
        foreach (var p in allGridPositions) if (!occupied[p]) return false;
        return true;
    }

    public Vector3 WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - gridOrigin;
        return new Vector3(local.x / cellSize, local.y / cellSize, local.z / cellSize);
    }

    public Vector3 GridToWorld(Vector3 gpos)
    {
        return gridOrigin + new Vector3(gpos.x * cellSize, gpos.y * cellSize, gpos.z * cellSize);
    }
    
    public bool TryGetNearestGridPos(Vector3 worldPos, out Vector3 nearestGPos, out Vector3 nearestWorld, out float dist)
    {
        nearestGPos = default; nearestWorld = default; dist = float.PositiveInfinity;
        bool found = false;

        foreach (var kvp in cubeAt)
        {
            Vector3 w = kvp.Value.transform.position;
            float d = Vector3.Distance(worldPos, w);
            if (d < dist) { dist = d; nearestGPos = kvp.Key; nearestWorld = w; found = true; }
        }

        return found && dist <= nearestSearchRadius;
    }
    
    public void DebugPrintGrid(string label = null)
    {
        var list = new List<Vector3>(allGridPositions);
        list.Sort((a, b) =>
        {
            int cy = a.y.CompareTo(b.y);
            if (cy != 0) return cy;
            int cz = a.z.CompareTo(b.z);
            if (cz != 0) return cz;
            return a.x.CompareTo(b.x);
        });
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(label)) sb.AppendLine($"[Grid Debug] {label}");
        sb.AppendLine($"CellSize={cellSize}, TotalCells={allGridPositions.Count}");

        float currentY = float.NaN;
        int filled = 0;

        foreach (var g in list)
        {
            if (float.IsNaN(currentY) || !Mathf.Approximately(g.y, currentY))
            {
                currentY = g.y;
                sb.AppendLine($"\n=== Layer Y={currentY} ===");
            }

            bool occ = IsOccupied(g);
            if (occ) filled++;
            
            sb.Append($"({g.x},{g.z}): {(occ ? 1 : 0)}   ");
        }

        sb.AppendLine($"\n\nFilled: {filled}/{allGridPositions.Count} | GridFull: {IsGridFull()}");
        Debug.Log(sb.ToString());
    }


}



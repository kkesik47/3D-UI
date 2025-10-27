using System.Collections.Generic;
using UnityEngine;

public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;
    [SerializeField] Renderer[] renderers;
    [SerializeField] LayerMask placedMask;
    [SerializeField] Color initialColor = Color.green;

    // Track which grid cells this piece currently occupies
    readonly List<Vector3> occupiedByThisPiece = new();
    bool isPlaced = false;

    // ====== UPDATE LOOP ======
    void Update()
    {
        bool overlapsPlaced = PhysicsOverlapPlaced();
        bool placementValid = false;
        Vector3 snapDelta = Vector3.zero;
        List<Vector3> targetGridPositions = null;

        // Compute prospective grid positions for all child ShapeCells
        if (!overlapsPlaced)
        {
            placementValid = ComputePlacement(out targetGridPositions, out snapDelta);
        }

        // Color feedback
        SetColor(overlapsPlaced || !placementValid ? Color.red : initialColor);

        // If valid and close enough, snap + commit occupancy
        if (!overlapsPlaced && placementValid && snapDelta.magnitude <= snapDistance)
        {
            // Snap rotation first
            transform.rotation = GetSnappedRotation(transform.rotation);

            // Move whole piece into alignment
            transform.position += snapDelta;



            // Clear previous occupancy, then set new
            ClearFromGrid();
            foreach (var gpos in targetGridPositions)
                GridManager.Instance.SetOccupied(gpos, true);
            occupiedByThisPiece.Clear();
            occupiedByThisPiece.AddRange(targetGridPositions);

            // NEW: print after placing
            GridManager.Instance.DebugPrintGrid($"After PLACE: {name}");

            // Optional: check win
            if (GridManager.Instance.IsGridFull())
            {
                // TODO: trigger win UI / SFX
            }

            // Put the object on "Placed" layer so your overlap filter works
            int placed = LayerMask.NameToLayer("Placed");
            SetLayerRecursively(gameObject, placed);
        }
        else
        {
            // While moving or invalid -> not placed
            int def = LayerMask.NameToLayer("Default");
            SetLayerRecursively(gameObject, def);
            // Do not ClearFromGrid() here; only clear when you actually grab/move away.
            // If you want to free as soon as invalid, uncomment:
            // ClearFromGrid();
        }
    }

    // ====== HELPERS ======

    Quaternion GetSnappedRotation(Quaternion current)
    {
        Vector3 euler = current.eulerAngles;

        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;

        return Quaternion.Euler(euler);
    }

    bool PhysicsOverlapPlaced()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (!c.enabled || c.isTrigger) continue;
            var center = c.bounds.center;
            var halfExtents = c.bounds.extents;
            var rotation = c.transform.rotation;

            var shrink = 0.02f;
            var hits = Physics.OverlapBox(center, halfExtents - Vector3.one * shrink, rotation, LayerMask.GetMask("Placed"));
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue;
                return true;
            }
        }
        return false;
    }

    // Build the intended grid positions for all ShapeCells and check if all are free.
    // Also compute how far we have to move the piece to align the first cell to the grid.
    // Picks best anchor among all ShapeCells and validates whole shape.
    // Returns: list of target grid positions + the delta needed to move the piece.
    bool ComputePlacement(out List<Vector3> gridTargets, out Vector3 deltaToSnap)
    {
        gridTargets = new List<Vector3>();
        deltaToSnap = Vector3.zero;

        var cells = GetComponentsInChildren<ShapeCell>();
        if (cells.Length == 0) return false;

        // --- 1) Choose BEST reference cell (closest to any grid cell)
        Transform bestRef = null;
        Vector3 bestRefWorld = default;
        Vector3 bestRefG = default;
        float bestDist = float.PositiveInfinity;

        foreach (var c in cells)
        {
            if (GridManager.Instance.TryGetNearestGridPos(c.transform.position, out var g, out var w, out var d))
            {
                if (d < bestDist)
                {
                    bestDist = d;
                    bestRef = c.transform;
                    bestRefG = g;
                    bestRefWorld = w;
                }
            }
        }

        if (bestRef == null) return false; // no grid nearby

        // --- 2) Compute one translation delta for the whole piece
        deltaToSnap = (bestRefWorld - bestRef.position);

        // Optional: rotation snap first (if you added this helper)
        // transform.rotation = GetSnappedRotation(transform.rotation);

        // --- 3) Predict each other cell’s landing grid position using SAME delta
        foreach (var c in cells)
        {
            Vector3 predictedWorld = c.transform.position + deltaToSnap;

            if (!GridManager.Instance.TryGetNearestGridPos(predictedWorld, out var gpos, out _, out _))
                return false; // would land outside the grid

            // Target cell must be free OR already owned by this piece (when moving within grid)
            bool free = !GridManager.Instance.IsOccupied(gpos) || occupiedByThisPiece.Contains(gpos);
            if (!free) return false;

            gridTargets.Add(gpos);
        }

        // --- 4) Success: all cells have valid targets, no overhang
        return true;
    }


    void ClearFromGrid()
    {
        foreach (var gpos in occupiedByThisPiece)
            GridManager.Instance.SetOccupied(gpos, false);
        occupiedByThisPiece.Clear();

        // NEW: print after clearing
        GridManager.Instance.DebugPrintGrid($"After CLEAR: {name}");
    }

    void SetColor(Color c)
    {
        if (renderers == null) return;
        foreach (var r in renderers) r.material.color = c;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Optional hooks you can call from a grab script / Meta XR events:
    public void OnGrabbed()
    {
        ClearFromGrid();
    }
    public void OnReleased() { /* no-op; Update() will handle snapping/occupancy */ }
}

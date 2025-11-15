using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;


public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;
    [SerializeField] Renderer[] renderers;
    [SerializeField] LayerMask placedMask;
    [SerializeField] Color initialColor = Color.green;

    // Track which grid cells this piece currently occupies
    readonly List<Vector3> occupiedByThisPiece = new();
    bool isPlaced = false;
    bool isGrabbed = false; // true while the user is holding this shape
    Grabbable grabbable;
    bool wasGrabbed = false; // previous frame



    void Start()
    {
        SetColor(initialColor);

        grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
            Debug.LogWarning($"SnapToGrid on {name} did not find a Grabbable component.");
    }

    // ====== UPDATE LOOP ======
    void Update()
    {
        // 1) Detect grab state from Oculus.Interaction.Grabbable
        bool grabbedNow = grabbable != null && grabbable.SelectingPointsCount > 0;

        // If it was not grabbed last frame but is grabbed now -> just grabbed
        if (grabbedNow && !wasGrabbed)
        {
            // Free any cells this piece had occupied
            ClearFromGrid();
        }

        wasGrabbed = grabbedNow;

        bool overlapsPlaced = PhysicsOverlapPlaced();
        bool placementValid = false;
        Vector3 snapDelta = Vector3.zero;
        List<Vector3> targetGridPositions = null;

        // 2) Compute prospective grid positions for all child ShapeCells
        if (!overlapsPlaced)
        {
            placementValid = ComputePlacement(out targetGridPositions, out snapDelta);
        }

        // 3) Color feedback: red only when overlapping another placed object
        SetColor(overlapsPlaced ? Color.red : initialColor);

        // 4) If valid and close enough, snap + commit occupancy
        //    ONLY when not currently grabbed
        if (!grabbedNow && !overlapsPlaced && placementValid && snapDelta.magnitude <= snapDistance)
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
            // Do not ClearFromGrid() here; only when grabbed or when re-placing.
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
        isGrabbed = true;

        // If this piece was already snapped before, free its cells
        ClearFromGrid();
    }

    public void OnReleased()
    {
        isGrabbed = false;
        // We do NOT snap here – Update() will snap on the next frame
    }
}

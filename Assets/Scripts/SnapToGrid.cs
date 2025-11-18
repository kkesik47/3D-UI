using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;




public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color initialColor = Color.green;

    // Track which grid cells this piece currently occupies
    readonly List<Vector3> occupiedByThisPiece = new();
    Grabbable grabbable;
    bool wasGrabbed = false; // previous frame

    // NEW: are we currently snapped into the grid?
    bool isSnapped = false;

    // 🔊 Audio when this shape snaps into the grid
    [Header("Audio Feedback")]
    public AudioSource snapAudio;

    [Header("Overlap Detection")]
    public float overlapRadius = 0.6f;   // tweak in Inspector to match your shape size

    [Header("Ghost Preview")]
    public Material ghostBaseMaterial;     // assign GhostBaseMat here
    [Range(0f, 1f)] public float ghostAlpha = 0.15f;

    GameObject ghost;
    Renderer[] ghostRenderers;


    void Start()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        SetColor(initialColor);

        grabbable = GetComponent<Grabbable>();

        // ---------- Create ghost clone ----------
        ghost = Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
        ghost.name = name + "_Ghost";

        // Remove gameplay components from ghost
        DestroyImmediate(ghost.GetComponent<SnapToGrid>());
        DestroyImmediate(ghost.GetComponent<Grabbable>());
        DestroyImmediate(ghost.GetComponent<Rigidbody>());
        foreach (var col in ghost.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var audio in ghost.GetComponentsInChildren<AudioSource>())
            Destroy(audio);

        // Grab its renderers and assign ghostBaseMaterial instances
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (var r in ghostRenderers)
        {
            if (!r) continue;
            if (ghostBaseMaterial != null)
            {
                // Use a fresh instance of the transparent base
                r.material = new Material(ghostBaseMaterial);
            }
            else
            {
                // Fallback: clone original, but note this may still be opaque
                r.material = new Material(r.material);
            }
        }

        ghost.SetActive(false);
    }

    // ====== UPDATE LOOP ======
    void Update()
    {
        // Were we snapped last frame?
        bool wasSnapped = isSnapped;
        
        // 1) Detect grab state from Oculus.Interaction.Grabbable
        bool grabbedNow = grabbable != null && grabbable.SelectingPointsCount > 0;


        // If it was not grabbed last frame but is grabbed now -> just grabbed
        if (grabbedNow && !wasGrabbed)
        {
            // Free any cells this piece had occupied
            ClearFromGrid();
        }

        wasGrabbed = grabbedNow;

        // --- NEW: compute placement using SNAPPED rotation ---
        Quaternion originalRot = transform.rotation;
        Quaternion snappedRot = GetSnappedRotation(originalRot);

        bool overlapsPlaced = IsOverlappingOtherShape();
        bool placementValid = false;
        Vector3 snapDelta = Vector3.zero;
        List<Vector3> targetGridPositions = null;

        // Temporarily rotate to snapped orientation for the math
        transform.rotation = snappedRot;

        // 2) Compute prospective grid positions for all child ShapeCells
        if (!overlapsPlaced)
        {
            placementValid = ComputePlacement(out targetGridPositions, out snapDelta);
        }

        // Restore original rotation for rendering while holding
        transform.rotation = originalRot;

        // 3) Color feedback: red only when overlapping another placed object
        Color targetColor = (grabbedNow && overlapsPlaced) ? Color.red : initialColor;
        SetColor(targetColor);

        // One unified condition for "this pose can snap"
        bool canSnap = !overlapsPlaced &&
                       placementValid &&
                       snapDelta.magnitude <= snapDistance;

        // ---------- GHOST PREVIEW ----------
        if (ghost != null)
        {
            if (grabbedNow && canSnap)
            {
                ghost.SetActive(true);

                // EXACT final pose: snapped rotation + snapped translation
                ghost.transform.rotation = snappedRot;
                ghost.transform.position = transform.position + snapDelta;

                Color c = initialColor;
                c.a = ghostAlpha;
                foreach (var r in ghostRenderers)
                {
                    if (!r) continue;
                    var mat = r.material;
                    mat.color = c;
                }
            }
            else
            {
                ghost.SetActive(false);
            }
        }

        // 4) If valid and close enough, snap + commit occupancy
        //    ONLY when not currently grabbed
        if (!grabbedNow && canSnap)
        {
            // Snap rotation first
            // Use the SAME snapped rotation we used for ComputePlacement and the ghost
            transform.rotation = snappedRot;

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

            // --- 5) Update snapped state & play audio on transition ---

            // We consider ourselves "snapped" if the same condition as the snap block is true
            bool nowSnapped = (!grabbedNow &&
                               !overlapsPlaced &&
                               placementValid &&
                               snapDelta.magnitude <= snapDistance);

            // Snap event = we were NOT snapped before, and we ARE snapped now
            if (!wasSnapped && nowSnapped && snapAudio != null)
            {
                snapAudio.Play();
            }

            // Update state for next frame
            isSnapped = nowSnapped;

            if (ghost != null)
                ghost.SetActive(false);
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


        // We are no longer placed/snapped
        isSnapped = false;

        // NEW: print after clearing
        GridManager.Instance.DebugPrintGrid($"After CLEAR: {name}");
    }

    void SetColor(Color c)
    {
        if (renderers == null || renderers.Length == 0) return;

        foreach (var r in renderers)
        {
            if (!r) continue;
            var mat = r.material;
            mat.color = c;
        }
    }


    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Optional hooks you can call from a grab script / Meta XR events:


    bool IsOverlappingOtherShape()
    {
        // All unit cubes in THIS shape
        var myCells = GetComponentsInChildren<ShapeCell>();
        if (myCells.Length == 0) return false;

        // How close two cubes can be before we consider it "overlapping"
        float cell = GridManager.Instance.cellSize;
        float threshold = cell * 0.08f;   // tweak if needed
        float thresholdSqr = threshold * threshold;

        // Check against all other shapes
        var allShapes = FindObjectsOfType<SnapToGrid>();
        foreach (var other in allShapes)
        {
            if (other == this) continue; // skip self

            var otherCells = other.GetComponentsInChildren<ShapeCell>();
            foreach (var a in myCells)
            {
                Vector3 pa = a.transform.position;

                foreach (var b in otherCells)
                {
                    Vector3 pb = b.transform.position;
                    float distSqr = (pa - pb).sqrMagnitude;

                    if (distSqr < thresholdSqr)
                    {
                        // Uncomment for debugging:
                        // Debug.Log($"{name} overlapping with {other.name}");
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

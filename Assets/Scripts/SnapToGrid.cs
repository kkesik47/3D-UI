using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;




public class SnapToGrid : MonoBehaviour
{
    public float snapDistance = 0.1f;
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color initialColor = Color.green;

    readonly List<Vector3> occupiedByThisPiece = new();
    Grabbable grabbable;
    bool wasGrabbed = false;
    bool isSnapped = false;
    
    [Header("Audio Feedback")]
    public AudioSource snapAudio;

    [Header("Overlap Detection")]
    public float overlapRadius = 0.6f;

    [Header("Ghost Preview")]
    public Material ghostBaseMaterial; 
    [Range(0f, 1f)] public float ghostAlpha = 0.15f;

    GameObject ghost;
    Renderer[] ghostRenderers;


    void Start()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        SetColor(initialColor);

        grabbable = GetComponent<Grabbable>();
        ghost = Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
        ghost.name = name + "_Ghost";
        
        DestroyImmediate(ghost.GetComponent<SnapToGrid>());
        DestroyImmediate(ghost.GetComponent<Grabbable>());
        DestroyImmediate(ghost.GetComponent<Rigidbody>());
        foreach (var col in ghost.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var audio in ghost.GetComponentsInChildren<AudioSource>())
            Destroy(audio);
        
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (var r in ghostRenderers)
        {
            if (!r) continue;
            if (ghostBaseMaterial != null)
            {
                r.material = new Material(ghostBaseMaterial);
            }
            else
            {
                r.material = new Material(r.material);
            }
        }

        ghost.SetActive(false);
    }
    
    void Update()
    {
        bool wasSnapped = isSnapped;
        bool grabbedNow = grabbable != null && grabbable.SelectingPointsCount > 0;
        
        if (grabbedNow && !wasGrabbed)
        {
            ClearFromGrid();
        }

        wasGrabbed = grabbedNow;
        Quaternion originalRot = transform.rotation;
        Quaternion snappedRot = GetSnappedRotation(originalRot);

        bool overlapsPlaced = IsOverlappingOtherShape();
        bool placementValid = false;
        Vector3 snapDelta = Vector3.zero;
        List<Vector3> targetGridPositions = null;
        transform.rotation = snappedRot;
        
        if (!overlapsPlaced)
        {
            placementValid = ComputePlacement(out targetGridPositions, out snapDelta);
        }
        
        transform.rotation = originalRot;
        Color targetColor = (grabbedNow && overlapsPlaced) ? Color.red : initialColor;
        SetColor(targetColor);
        
        bool canSnap = !overlapsPlaced &&
                       placementValid &&
                       snapDelta.magnitude <= snapDistance;

        // ---------- GHOST PREVIEW ----------
        if (ghost != null)
        {
            if (grabbedNow && canSnap)
            {
                ghost.SetActive(true);
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
        
        if (!grabbedNow && canSnap)
        {
            transform.rotation = snappedRot;
            transform.position += snapDelta;
            
            ClearFromGrid();
            foreach (var gpos in targetGridPositions)
                GridManager.Instance.SetOccupied(gpos, true);
            occupiedByThisPiece.Clear();
            occupiedByThisPiece.AddRange(targetGridPositions);

            GridManager.Instance.DebugPrintGrid($"After PLACE: {name}");
            
            if (GridManager.Instance.IsGridFull())
            {
                // TODO: trigger win UI / SFX
            }
            
            int placed = LayerMask.NameToLayer("Placed");
            SetLayerRecursively(gameObject, placed);
            bool nowSnapped = (!grabbedNow &&
                               !overlapsPlaced &&
                               placementValid &&
                               snapDelta.magnitude <= snapDistance);
            
            if (!wasSnapped && nowSnapped && snapAudio != null)
            {
                snapAudio.Play();
            }
            
            isSnapped = nowSnapped;

            if (ghost != null)
                ghost.SetActive(false);
        }
        else
        {
            int def = LayerMask.NameToLayer("Default");
            SetLayerRecursively(gameObject, def);
        }
    }

    Quaternion GetSnappedRotation(Quaternion current)
    {
        Vector3 euler = current.eulerAngles;

        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;

        return Quaternion.Euler(euler);
    }
    
    bool ComputePlacement(out List<Vector3> gridTargets, out Vector3 deltaToSnap)
    {
        gridTargets = new List<Vector3>();
        deltaToSnap = Vector3.zero;

        var cells = GetComponentsInChildren<ShapeCell>();
        if (cells.Length == 0) return false;
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

        if (bestRef == null) return false;
        deltaToSnap = (bestRefWorld - bestRef.position);
        foreach (var c in cells)
        {
            Vector3 predictedWorld = c.transform.position + deltaToSnap;
            if (!GridManager.Instance.TryGetNearestGridPos(predictedWorld, out var gpos, out _, out _))
                return false;
            bool free = !GridManager.Instance.IsOccupied(gpos) || occupiedByThisPiece.Contains(gpos);
            if (!free) return false;

            gridTargets.Add(gpos);
        }
        return true;
    }


    void ClearFromGrid()
    {
        foreach (var gpos in occupiedByThisPiece)
            GridManager.Instance.SetOccupied(gpos, false);
        occupiedByThisPiece.Clear();
        isSnapped = false;
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
    
    bool IsOverlappingOtherShape()
    {
        var myCells = GetComponentsInChildren<ShapeCell>();
        if (myCells.Length == 0) return false;
        float cell = GridManager.Instance.cellSize;
        float threshold = cell * 0.08f; 
        float thresholdSqr = threshold * threshold;
        var allShapes = FindObjectsOfType<SnapToGrid>();
        foreach (var other in allShapes)
        {
            if (other == this) continue;

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
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

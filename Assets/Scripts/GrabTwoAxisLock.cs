using UnityEngine;

public class GrabTwoAxisLock : MonoBehaviour
{
    public enum LockAxis { Free, X, Y, Z }

    [Header("Axis Lock")]
    public LockAxis lockedAxis = LockAxis.Z;

    [Header("Rotation Lock (while grabbed)")]
    public bool lockRotationWhileGrabbed = true;

    public enum RotationMode
    {
        Free,    
        LockAll,
        OnPlane  
    }

    public RotationMode rotationMode = RotationMode.OnPlane;

    [Header("Plane Indicator (optional)")]
    public GameObject planeIndicatorPrefab;
    public float planeSize = 0.3f;
    public float planeOffset = 0.002f;

    private Rigidbody rb;
    private Vector3 lockedPos;
    private Quaternion lockedRot;
    private bool wasGrabbed;

    private GameObject planeIndicator;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lockedPos = transform.position;
        lockedRot = transform.rotation;

        if (planeIndicatorPrefab != null)
        {
            planeIndicator = Instantiate(planeIndicatorPrefab);
            planeIndicator.SetActive(false);
        }
    }

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    public void SetLockedAxis(LockAxis axis)
    {
        lockedAxis = axis;
        if (rb != null && rb.isKinematic)
        {
            lockedPos = transform.position;
            lockedRot = transform.rotation;
        }
    }

    public void SetRotationMode(RotationMode mode)
    {
        rotationMode = mode;

        if (rb != null && rb.isKinematic)
            lockedRot = transform.rotation;
    }

    void LateUpdate()
    {
        ApplyLocksAndVisuals();
    }
    
    void OnBeforeRender()
    {
        ApplyLocksAndVisuals();
    }

    void ApplyLocksAndVisuals()
    {
        if (rb == null) return;

        bool isGrabbedNow = rb.isKinematic;
        if (isGrabbedNow && !wasGrabbed)
        {
            lockedPos = transform.position;
            lockedRot = transform.rotation;
        }

        UpdatePlaneIndicator(isGrabbedNow);

        if (isGrabbedNow)
        {
            ApplyAxisLock();
            ApplyRotationLock();
        }

        wasGrabbed = isGrabbedNow;
    }

    void ApplyAxisLock()
    {
        if (lockedAxis == LockAxis.Free) return;

        Vector3 p = transform.position;

        switch (lockedAxis)
        {
            case LockAxis.X: p.x = lockedPos.x; break;
            case LockAxis.Y: p.y = lockedPos.y; break;
            case LockAxis.Z: p.z = lockedPos.z; break;
        }

        transform.position = p;
        rb.position = p;
    }

    void ApplyRotationLock()
    {
        if (!lockRotationWhileGrabbed) return;
        if (rotationMode == RotationMode.Free) return;

        if (rotationMode == RotationMode.LockAll)
        {
            transform.rotation = lockedRot;
            rb.rotation = lockedRot;
            return;
        }

        if (rotationMode == RotationMode.OnPlane)
        {
            Vector3 euler = transform.eulerAngles;
            Vector3 lockedEuler = lockedRot.eulerAngles;

            switch (lockedAxis)
            {
                case LockAxis.X:
                    euler.y = lockedEuler.y;
                    euler.z = lockedEuler.z;
                    break;
                case LockAxis.Y:
                    euler.x = lockedEuler.x;
                    euler.z = lockedEuler.z;
                    break;
                case LockAxis.Z:
                    euler.y = lockedEuler.y;
                    euler.x = lockedEuler.x;
                    break;
                case LockAxis.Free:
                    return;
            }

            Quaternion planeRot = Quaternion.Euler(euler);
            transform.rotation = planeRot;
            rb.rotation = planeRot;
        }
    }

    void UpdatePlaneIndicator(bool isGrabbedNow)
    {
        if (planeIndicator == null) return;

        bool shouldShow = isGrabbedNow && lockedAxis != LockAxis.Free;

        if (!shouldShow)
        {
            if (planeIndicator.activeSelf) planeIndicator.SetActive(false);
            return;
        }

        if (!planeIndicator.activeSelf) planeIndicator.SetActive(true);

        Vector3 normal = Vector3.forward;

        switch (lockedAxis)
        {
            case LockAxis.X: normal = Vector3.right; break;
            case LockAxis.Y: normal = Vector3.up; break;
            case LockAxis.Z: normal = Vector3.forward; break;
        }

        Quaternion rot = Quaternion.LookRotation(normal);

        planeIndicator.transform.SetPositionAndRotation(
            lockedPos + normal * planeOffset,
            rot
        );

        planeIndicator.transform.localScale = new Vector3(planeSize, planeSize, 1f);
    }

    void OnDestroy()
    {
        if (planeIndicator != null)
            Destroy(planeIndicator);
    }
}

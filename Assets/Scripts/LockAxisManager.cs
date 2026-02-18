using UnityEngine;

public class LockAxisManager : MonoBehaviour
{
    [Header("Initial value used on scene start")]
    public GrabTwoAxisLock.LockAxis defaultAxis = GrabTwoAxisLock.LockAxis.Z;

    private GrabTwoAxisLock[] lockScripts;

    public int currentCondition { get; private set; } = 1;
    public GrabTwoAxisLock.LockAxis currentAxis { get; private set; }

    void Awake()
    {
        lockScripts = FindObjectsOfType<GrabTwoAxisLock>(true);
        SetAxisInternal(defaultAxis);
        currentAxis = defaultAxis;
    }

    void SetAxisInternal(GrabTwoAxisLock.LockAxis axis)
    {
        currentAxis = axis;

        if (lockScripts == null) return;

        foreach (var s in lockScripts)
        {
            if (s == null) continue;
            s.SetLockedAxis(axis);
        }

        Debug.Log($"[Study] Axis lock set to {axis}");
    }

    // --- methods to call from buttons ---

    public void SetCondition1_Free()
    {
        currentCondition = 1;
        SetAxisInternal(GrabTwoAxisLock.LockAxis.Free);
    }

    public void SetCondition2_LockZ_MoveXY()
    {
        currentCondition = 2;
        SetAxisInternal(GrabTwoAxisLock.LockAxis.Z);
    }

    public void SetCondition3_LockY_MoveXZ()
    {
        currentCondition = 3;
        SetAxisInternal(GrabTwoAxisLock.LockAxis.Y);
    }

    public void SetCondition4_LockX_MoveYZ()
    {
        currentCondition = 4;
        SetAxisInternal(GrabTwoAxisLock.LockAxis.X);
    }
}
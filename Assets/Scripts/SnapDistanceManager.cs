using UnityEngine;

public class SnapDistanceManager : MonoBehaviour
{
    [Header("Initial value used on scene start")]
    public float defaultSnapDistance = 0.1f;

    private SnapToGrid[] snapScripts;

    void Awake()
    {
        // Cache all SnapToGrid scripts in the scene
        snapScripts = FindObjectsOfType<SnapToGrid>();
        SetSnapDistanceInternal(defaultSnapDistance);
    }

    void SetSnapDistanceInternal(float value)
    {
        if (snapScripts == null) return;

        foreach (var s in snapScripts)
        {
            if (s == null) continue;
            s.snapDistance = value;
        }

        Debug.Log($"[Study] Snap distance set to {value}");
    }

    // --- methods to call from buttons ---

    public void SetCondition1() => SetSnapDistanceInternal(0.05f);
    public void SetCondition2() => SetSnapDistanceInternal(0.10f);
    public void SetCondition3() => SetSnapDistanceInternal(0.25f);
    public void SetCondition4() => SetSnapDistanceInternal(0.40f);
}
